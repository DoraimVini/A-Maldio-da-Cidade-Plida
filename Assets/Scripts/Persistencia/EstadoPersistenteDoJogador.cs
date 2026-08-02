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

        /// <inheritdoc />
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

            var id = _maoFisica != null ? _maoFisica.IdDaArmaEquipada : null;
            return id.HasValue ? id.Value.ToString() : "";
        }

        /// <inheritdoc />
        public void AplicarEstado(string estado)
        {
            if (string.IsNullOrWhiteSpace(estado)) return; // desarmado: nada a reequipar
            if (_maoFisica == null) return;

            // Enum desconhecido (save de uma versão com outra arma) não pode quebrar o
            // load: fica desarmado, que é o estado padrão seguro.
            if (!System.Enum.TryParse(estado, out ArmaDaTumba qual))
            {
                Debug.LogWarning($"[EstadoPersistenteDoJogador] Arma '{estado}' não existe mais — " +
                                 "Damião continua desarmado.", this);
                return;
            }

            _maoFisica.EquiparArma(qual);
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
