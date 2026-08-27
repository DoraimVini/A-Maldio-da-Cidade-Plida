using System.Globalization;
using UnityEngine;
using FavelaAmarela.Core.Abilities;
using FavelaAmarela.Core.Persistencia;
using FavelaAmarela.Runtime.Combat;
using FavelaAmarela.Player;

namespace FavelaAmarela.Runtime.Persistencia
{
    /// <summary>
    /// Camada Runtime (MonoBehaviour). Faz o estado de Damião sobreviver à troca de cena:
    /// <b>a arma empunhada</b> e a <b>Vitalidade corrente</b>.
    ///
    /// <para><b>Bug que motivou (playtest 2026-07-31):</b> sair da Tumba fazia o jogador
    /// perder a arma do baú. <c>SceneManager.LoadScene</c> destrói tudo e recria a cena do
    /// zero, e não havia nada guardando o estado entre uma cena e outra.</para>
    ///
    /// <para>Usa chaves <b>globais</b> de <see cref="ChavesDeSave"/> em vez de um
    /// <see cref="ObjetoPersistente"/>: o jogador não é um objeto de cenário cuja identidade
    /// precise ser rastreada — há um só Damião, e ele existe em toda cena.</para>
    /// </summary>
    [RequireComponent(typeof(MaoFisicaBridge), typeof(VitalidadeBridge))]
    [AddComponentMenu("Favela Amarela/Persistência/Estado Persistente do Jogador")]
    public sealed class EstadoPersistenteDoJogador : MonoBehaviour, IPersistente
    {
        private MaoFisicaBridge _maoFisica;
        private VitalidadeBridge _vitalidade;

        /// <inheritdoc />
        public string ChaveDePersistencia => ChavesDeSave.ArmaEquipada;

        private void Awake()
        {
            _maoFisica = GetComponent<MaoFisicaBridge>();
            _vitalidade = GetComponent<VitalidadeBridge>();
        }

        private void Start()
        {
            // Registro no Start, não no Awake: o GerenciadorDeSave pode ainda não ter
            // acordado, e a ordem de Awake entre GameObjects não é garantida — a mesma
            // armadilha que travou a barra de Vitalidade.
            var gerenciador = GerenciadorDeSave.Instancia;
            if (gerenciador == null) return;

            gerenciador.Registrar(this);

            // Aplica na chegada: é o que devolve a arma ao voltar de outra cena.
            if (gerenciador.Registro.TentarObter(ChaveDePersistencia, out var estado))
                AplicarEstado(estado);

            if (gerenciador.Registro.TentarObter(ChavesDeSave.VitalidadeAtual, out var vida))
                AplicarVitalidade(vida);
        }

        private void OnDestroy() => GerenciadorDeSave.Instancia?.Desregistrar(this);

        /// <summary>
        /// Grava a <b>Vitalidade</b>. A arma <b>não</b> viaja mais por aqui.
        ///
        /// <para><b>Por que o canal da arma foi colapsado (2026-08-27).</b> A mesma arma era
        /// persistida por <b>dois caminhos independentes</b>: o <c>equipSlotData</c> do
        /// inventário, por <c>ItemDef.Id</c>, e esta chave, pelo <b>nome do valor do enum</b>
        /// <c>TipoArmaFisica</c>. Eles convergiam só porque ambos terminavam em
        /// <c>EquiparArma(TipoArmaFisica)</c>.</para>
        ///
        /// <para>Com a arma montada por dado (<c>HabilidadeDef</c>), o enum deixou de descrever
        /// a arma — ele é identificador, não comportamento. Manter o segundo canal passaria a
        /// gravar algo que não reconstrói mais nada, e dois canais que discordam são piores que
        /// um só.</para>
        ///
        /// <para><b>O inventário já bastava</b>, e por construção: o equipamento é restaurado
        /// pelo <c>EstadoPersistenteDoInventario</c>, e a <c>MaoFisicaBridge</c> reage por dois
        /// caminhos que se cobrem — o evento <c>OnSlotChanged</c> (se o inventário restaurar
        /// depois) e a leitura do slot corrente no <c>Start</c> (se restaurar antes). Foi
        /// justamente essa dupla cobertura que consertou o "quando saio da Tumba a arma some"
        /// do playtest de 2026-07-31.</para>
        ///
        /// <para>A chave <c>ArmaEquipada</c> continua sendo a identidade deste componente para
        /// não órfãozar partidas salvas; ela só passou a carregar vazio.</para>
        /// </summary>
        public string CapturarEstado()
        {
            // A Vitalidade viaja por chave própria: são dois dados independentes, e
            // misturá-los numa string só dificultaria ler o JSON ao depurar.
            var gerenciador = GerenciadorDeSave.Instancia;
            if (gerenciador != null && _vitalidade?.Vitalidade != null)
            {
                gerenciador.Registro.Definir(
                    ChavesDeSave.VitalidadeAtual,
                    _vitalidade.Vitalidade.Atual.ToString(CultureInfo.InvariantCulture));
            }

            return "";
        }

        /// <summary>
        /// Não faz nada com a arma — quem a devolve é o inventário (ver
        /// <see cref="CapturarEstado"/>).
        ///
        /// <para>Continua existindo para <b>consumir em silêncio</b> o valor de partidas
        /// salvas antes de 2026-08-27, que ainda carregam um nome de enum aqui. Reequipar por
        /// esse valor agora seria um segundo caminho competindo com o inventário, e foi
        /// exatamente essa competição que o colapso removeu.</para>
        /// </summary>
        public void AplicarEstado(string estado)
        {
            // Intencionalmente vazio. Ver o XML doc acima antes de "consertar".
        }

        private void AplicarVitalidade(string valor)
        {
            if (!float.TryParse(valor, NumberStyles.Float, CultureInfo.InvariantCulture, out float atual))
                return;

            var vitalidade = _vitalidade?.Vitalidade;
            if (vitalidade == null) return;

            // Vitalidade nasce cheia; só precisamos tirar o que faltava.
            float diferenca = vitalidade.Atual - atual;
            if (diferenca > 0f) vitalidade.Ferir(diferenca);
        }
    }
}
