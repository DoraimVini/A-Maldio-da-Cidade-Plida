using System.Text;
using UnityEngine;
using UnityEngine.UI;
using FavelaAmarela.Core.Combat;
using FavelaAmarela.Inventario;
using FavelaAmarela.Player;
using FavelaAmarela.Runtime.Combat;

namespace FavelaAmarela.Runtime.UI
{
    /// <summary>
    /// Camada Runtime (MonoBehaviour). A <b>ficha de Damião</b>: mostra cada atributo como
    /// <c>base (+bônus) = final</c>, para que o efeito de um item deixe de ser invisível.
    ///
    /// <para><b>Por que existe (2026-08-14):</b> a auditoria do inventário
    /// (<c>systems/inventario_analise.md</c>) achou dois problemas que só eram invisíveis por
    /// falta desta tela: sete <see cref="StatType"/> não eram consumidos por ninguém, e o
    /// recálculo da ficha zerava <c>ResistenciaAnomala</c> a cada troca de equipamento. Nenhum
    /// dos dois avisava — nem console, nem teste. Com a ficha à vista, "Resistência Anômala: 0"
    /// salta na primeira troca.</para>
    ///
    /// <para><b>Sem tecla própria de propósito:</b> este painel vive <b>sob a raiz do painel de
    /// inventário</b> e liga/desliga junto com ela. Criar uma ação de input nova exigiria editar
    /// o asset do Input System; e ficha junto de mochila é o arranjo padrão do gênero. Como só
    /// redesenha em <c>OnEnable</c> e em <c>OnBonusChanged</c>, não custa nada enquanto
    /// fechado.</para>
    ///
    /// <para><b>Um Text só, não um por linha:</b> a ficha é um instrumento de diagnóstico antes
    /// de ser peça de UI. Compor tudo numa string evita ~20 referências serializadas que
    /// poderiam ficar meio ligadas — precisamente o modo de falha que esta tela existe para
    /// expor.</para>
    /// </summary>
    [AddComponentMenu("FavelaAmarela/UI/Painel de Ficha")]
    public sealed class PainelDeFicha : MonoBehaviour
    {
        [Header("Saída")]
        [Tooltip("Onde a ficha inteira é escrita, em várias linhas. [ASSET]")]
        [SerializeField] private Text corpo;

        [Header("Fonte dos atributos")]
        [Tooltip("VitalidadeBridge de Damião — dona da ficha final. [CENA]")]
        [SerializeField] private VitalidadeBridge vitalidadeDoJogador;

        private GerenciadorEfeitosPassivos _passivas;
        private readonly StringBuilder _sb = new StringBuilder(512);

        private void OnEnable()
        {
            _passivas = GerenciadorEfeitosPassivos.Instance;
            if (_passivas != null) _passivas.OnBonusChanged += Redesenhar;

            // Aplica o estado corrente na hora: OnBonusChanged só dispara em mudança, e abrir a
            // tela não muda bônus nenhum. Sem isto a ficha nasceria em branco — mesma armadilha
            // que o GameStatePresenter e o MaoFisicaBridge já pagaram.
            Redesenhar();
        }

        private void OnDisable()
        {
            if (_passivas != null) _passivas.OnBonusChanged -= Redesenhar;
            _passivas = null;
        }

        private void Redesenhar()
        {
            if (corpo == null) return;

            if (vitalidadeDoJogador == null)
            {
                corpo.text = "Ficha indisponível: sem VitalidadeBridge ligada.";
                return;
            }

            var f = vitalidadeDoJogador.Atributos;
            if (f == null)
            {
                corpo.text = "Ficha indisponível: atributos ainda não calculados.";
                return;
            }

            _sb.Length = 0;

            // ── Só o que esta ficha REALMENTE governa ────────────────────────
            //
            // `Ataque` e `ResilienciaMax` ficam de fora de propósito, e a omissão é a parte
            // importante: para Damião o dano do golpe vem do POCO da arma (via MaoFisicaBridge),
            // não de `ficha.Ataque` — que vale 0 no asset. E a Resiliência Mental de verdade é o
            // POCO criado pelo GameLoopBootstrap (100), não `ficha.ResilienciaMax` (0). Exibi-los
            // mostraria "Trauma Físico: 0" para quem acabou de matar um Cultista, e uma
            // Resiliência que contradiz a barra do HUD. Num painel de diagnóstico, número errado
            // é pior que número ausente: gera diagnóstico falso.
            _sb.AppendLine("— Ficha —");
            Linha("Vitalidade Corpórea", f.VitalidadeMax, StatType.VitMaxima);
            Linha("Defesa Física", f.Defesa, StatType.DefesaFisica);
            Linha("Resistência Anômala", f.ResistenciaAnomala, StatType.DefesaAnomalia);
            if (f.Conjuracao > 0f) Linha("Conjuração", f.Conjuracao, null);

            // ── Bônus de itens que não cabem na ficha ────────────────────────
            //
            // Aqui entram os StatType cujo efeito mora em outro sistema (arma, vigor) ou em
            // sistema nenhum. É esta seção que responde "o item que peguei fez alguma coisa?".
            DesenharBonusDeItens();

            corpo.text = _sb.ToString();
        }

        /// <summary>
        /// Lista todo <see cref="StatType"/> com bônus diferente de zero, marcando os que não são
        /// consumidos por sistema nenhum. É o que transforma "achei um item" em "o item fez
        /// efeito" — ou em "este atributo não está ligado a nada".
        /// </summary>
        private void DesenharBonusDeItens()
        {
            if (_passivas == null) return;

            bool escreveuCabecalho = false;

            foreach (StatType stat in System.Enum.GetValues(typeof(StatType)))
            {
                float bonus = _passivas.GetBonus(stat);
                if (Mathf.Approximately(bonus, 0f)) continue;

                if (!escreveuCabecalho)
                {
                    _sb.AppendLine().AppendLine("— Bônus de itens —");
                    escreveuCabecalho = true;
                }

                _sb.Append(stat).Append(": ")
                   .Append(bonus > 0f ? "+" : "").Append(bonus.ToString("0.##"));

                // "PASSIVO" não é preserva-verdade decorativo: RMMaxima, por exemplo, funciona
                // como CONSUMÍVEL (VitalidadeBridge.AplicarEfeitoConsumivel chama Ancorar), mas
                // não faz nada como bônus de item equipado. Dizer só "SEM EFEITO" mentiria sobre
                // metade dos usos.
                if (!AtributoConsomeBonus(stat)) _sb.Append("   SEM EFEITO PASSIVO");

                _sb.AppendLine();
            }
        }

        /// <summary>
        /// Escreve uma linha da ficha. Quando o atributo tem um <see cref="StatType"/>
        /// correspondente, mostra o bônus agregado — e <b>marca quando o bônus existe mas não
        /// entrou no valor final</b>, que é como um atributo morto se denuncia.
        /// </summary>
        private void Linha(string rotulo, float valorFinal, StatType? origem)
        {
            _sb.Append(rotulo).Append(": ").Append(valorFinal.ToString("0.##"));

            if (origem.HasValue && _passivas != null)
            {
                float bonus = _passivas.GetBonus(origem.Value);
                if (!Mathf.Approximately(bonus, 0f))
                {
                    _sb.Append("   (itens: ")
                       .Append(bonus > 0f ? "+" : "")
                       .Append(bonus.ToString("0.##"));

                    // Se há bônus de item mas ele não aparece no total, o StatType não está
                    // ligado a nada. Sem esta marca, o jogador (e o dev) leem um número correto
                    // e concluem que o item funcionou.
                    if (!AtributoConsomeBonus(origem.Value))
                        _sb.Append(" — SEM EFEITO");

                    _sb.Append(')');
                }
            }

            _sb.AppendLine();
        }

        /// <summary>
        /// Quais <see cref="StatType"/> algum sistema de fato consome hoje. Ver o levantamento
        /// completo em <c>systems/inventario_analise.md</c>: dos 15 declarados, 7 não são lidos
        /// por ninguém. Esta lista é o que separa "o item deu +5" de "o item diz que deu +5".
        /// </summary>
        private static bool AtributoConsomeBonus(StatType stat) => stat switch
        {
            StatType.VitMaxima => true,        // VitalidadeBridge
            StatType.DefesaFisica => true,     // VitalidadeBridge
            StatType.TraumaFisico => true,     // MaoFisicaBridge
            StatType.TraumaAnomalia => true,   // MaoFisicaBridge
            StatType.VigorMaximo => true,      // GerenciadorDeVigor
            StatType.RegeneracaoVigor => true, // GerenciadorDeVigor
            StatType.CustoEsquivaVigor => true,// GerenciadorDeVigor
            StatType.CustoCorridaVigor => true,// GerenciadorDeVigor
            StatType.RegenRM => true,          // GerenciadorEfeitosPassivos.Update → Ancorar
            StatType.DrenoRM => true,          // GerenciadorEfeitosPassivos.Update → SofrerTrauma
            _ => false,
        };
    }
}
