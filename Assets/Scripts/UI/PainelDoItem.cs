using System.Text;
using UnityEngine;
using UnityEngine.UI;
using FavelaAmarela.Inventario;
using FavelaAmarela.Player;
using FavelaAmarela.Runtime.Combat;

namespace FavelaAmarela.Runtime.UI
{
    /// <summary>
    /// Camada Runtime (MonoBehaviour). A terceira coluna da tela de inventário: <b>o item
    /// escolhido</b> — nome, o que é, o que faz, e o que muda se tomar o lugar do que está
    /// vestido. Sem item escolhido, mostra <b>o Damião</b>: a carne, a defesa e os bônus que os
    /// itens estão dando.
    ///
    /// <para><b>Substitui o <c>PainelDeFicha</c> na tela do jogador (2026-09-28).</b> A ficha
    /// era instrumento de diagnóstico ("SEM EFEITO PASSIVO", nomes de classe) pendurado fora da
    /// borda da janela, uma palavra por linha. O diagnóstico foi para o Carcosa Debugger; aqui
    /// fica só o que o jogador precisa, no vocabulário do jogo.</para>
    ///
    /// <para>Quem escreve o texto é a <see cref="DescricaoDeItem"/> (sem Unity, testada); este
    /// componente só distribui nas caixas e pinta.</para>
    /// </summary>
    [AddComponentMenu("FavelaAmarela/UI/Painel do Item")]
    public sealed class PainelDoItem : MonoBehaviour
    {
        [Header("Caixas [ASSET]")]
        [Tooltip("Nome do item (ou 'Damião').")]
        [SerializeField] private Text titulo;

        [Tooltip("O que o item é: tipo, mãos, nível, grau.")]
        [SerializeField] private Text subtitulo;

        [Tooltip("A descrição autorada no ItemDef.")]
        [SerializeField] private Text descricao;

        [Tooltip("O que o item faz, uma linha por efeito.")]
        [SerializeField] private Text atributos;

        [Tooltip("A troca contra o que está vestido, em verde e vermelho.")]
        [SerializeField] private Text comparacao;

        [Tooltip("Recusas e confirmações ('A mochila está cheia...').")]
        [SerializeField] private Text aviso;

        [Tooltip("O que dá para fazer com o item escolhido.")]
        [SerializeField] private Text dica;

        [Header("Cores")]
        [SerializeField] private Color corDoNome = new Color(0.93f, 0.89f, 0.78f);
        [SerializeField] private Color corMarcado = new Color(0.62f, 0.78f, 0.95f);
        [SerializeField] private Color corImpregnado = new Color(0.95f, 0.82f, 0.35f);
        [SerializeField] private Color corReliquia = new Color(0.98f, 0.58f, 0.25f);
        [SerializeField] private Color corGanho = new Color(0.55f, 0.85f, 0.45f);
        [SerializeField] private Color corPerda = new Color(0.92f, 0.42f, 0.35f);

        private VitalidadeBridge _corpoDeDamiao;
        private readonly StringBuilder _sb = new StringBuilder(512);

        /// <summary>
        /// Liga o resumo do Damião ao corpo dele. Chamado pelo <c>GameLoopBootstrap</c> a cada
        /// cena — o HUD é persistente e o Damião não, então não há como ligar pelo Inspector.
        /// </summary>
        public void Bind(VitalidadeBridge corpoDeDamiao) => _corpoDeDamiao = corpoDeDamiao;

        /// <summary>Mostra um item. <paramref name="vestido"/> é o que ocupa o slot dele (pode ser nulo).</summary>
        /// <param name="noCorpo">Se o item escolhido está no corpo (muda a dica, some a comparação).</param>
        public void MostrarItem(ItemInstance item, ItemInstance vestido, bool noCorpo)
        {
            if (item?.Def == null) { MostrarDamiao(); return; }

            Escrever(titulo, item.NomeExibido());
            if (titulo != null) titulo.color = CorDoGrau(item);
            Escrever(subtitulo, DescricaoDeItem.Subtitulo(item));
            Escrever(descricao, item.Def.Descricao ?? "");

            Escrever(atributos, string.Join("\n", DescricaoDeItem.Atributos(item)));
            Escrever(comparacao, noCorpo ? "" : Comparacao(item, vestido));
            Escrever(dica, Dica(item, noCorpo));
            LimparAviso();
        }

        /// <summary>Sem item escolhido: o Damião, a carne e os bônus dos itens.</summary>
        public void MostrarDamiao()
        {
            Escrever(titulo, "Damião");
            if (titulo != null) titulo.color = corDoNome;
            Escrever(subtitulo, "O que ele carrega no corpo");
            Escrever(descricao, "");
            Escrever(atributos, ResumoDoCorpo());
            Escrever(comparacao, BonusDosItens());
            Escrever(dica, "Clique num item para ver o que ele faz.");
            LimparAviso();
        }

        /// <summary>Escreve uma recusa ou confirmação, até a próxima escolha.</summary>
        public void Avisar(string texto) => Escrever(aviso, texto);

        /// <summary>Apaga o aviso.</summary>
        public void LimparAviso() => Escrever(aviso, "");

        /// <summary>O texto do aviso agora — para testes.</summary>
        public string AvisoAtual => aviso != null ? aviso.text : "";

        /// <summary>O título agora — para testes.</summary>
        public string TituloAtual => titulo != null ? titulo.text : "";

        // ── Composição ───────────────────────────────────────────────────────

        private string Comparacao(ItemInstance item, ItemInstance vestido)
        {
            var linhas = DescricaoDeItem.Comparar(item, vestido);
            if (linhas.Count == 0) return "";

            _sb.Length = 0;
            _sb.Append(vestido?.Def != null ? $"No lugar de {vestido.NomeExibido()}:" : "Vestindo:");
            foreach (var l in linhas)
            {
                _sb.Append('\n');
                if (l.Sinal == 0) { _sb.Append(l.Texto); continue; }
                string cor = ColorUtility.ToHtmlStringRGB(l.Sinal > 0 ? corGanho : corPerda);
                _sb.Append("<color=#").Append(cor).Append('>').Append(l.Texto).Append("</color>");
            }
            return _sb.ToString();
        }

        private static string Dica(ItemInstance item, bool noCorpo)
        {
            if (noCorpo) return "Clique na mochila para guardá-lo.";
            if (DescricaoDeItem.SeVeste(item.Def)) return "Clique no Corpo para vestir · Delete para abandonar.";
            if (item.Def.Tipo == ItemType.Consumivel) return "Use pela barra (1 a 8) · Delete para abandonar.";
            return "Clique em outra casa para mover · Delete para abandonar.";
        }

        private string ResumoDoCorpo()
        {
            var ficha = _corpoDeDamiao != null ? _corpoDeDamiao.Atributos : null;
            if (ficha == null) return "";

            _sb.Length = 0;
            _sb.Append("Vitalidade Corpórea ").Append(ficha.VitalidadeMax.ToString("0.#"));
            _sb.Append("\nDefesa Física ").Append(ficha.Defesa.ToString("0.#"));
            return _sb.ToString();
        }

        /// <summary>Todo bônus que os itens vestidos estão dando, somado, no vocabulário do jogo.</summary>
        private string BonusDosItens()
        {
            var passivas = GerenciadorEfeitosPassivos.Instance;
            if (passivas == null) return "";

            _sb.Length = 0;
            foreach (StatType stat in System.Enum.GetValues(typeof(StatType)))
            {
                float bonus = passivas.GetBonus(stat);
                if (Mathf.Approximately(bonus, 0f)) continue;

                _sb.Append(_sb.Length == 0 ? "Dos itens:" : "").Append('\n')
                   .Append(NomesDeAtributo.Linha(stat, bonus));
                if (NomesDeAtributo.NaoTemEfeito(stat)) _sb.Append(" (adormecido)");
            }
            return _sb.ToString();
        }

        private Color CorDoGrau(ItemInstance item) => item.Grau switch
        {
            FavelaAmarela.Core.Loot.GrauDeImpregnacao.Marcado => corMarcado,
            FavelaAmarela.Core.Loot.GrauDeImpregnacao.Impregnado => corImpregnado,
            FavelaAmarela.Core.Loot.GrauDeImpregnacao.Reliquia => corReliquia,
            _ => corDoNome,
        };

        private static void Escrever(Text alvo, string texto)
        {
            if (alvo == null) return;
            alvo.text = texto;
            alvo.enabled = !string.IsNullOrEmpty(texto);
        }
    }
}
