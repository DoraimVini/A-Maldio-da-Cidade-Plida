using System;

namespace FavelaAmarela.Core.Preferencias
{
    /// <summary>
    /// O que o jogador escolhe <b>sobre o jogo</b>, e não dentro dele: volume, janela e
    /// sincronização de quadros.
    ///
    /// <para><b>Por que existe (2026-08-29).</b> O projeto não tinha controle de volume nem
    /// opção de vídeo nenhuma — nem no menu principal, nem na pausa. Para um jogo que vai ser
    /// vendido isso não é polimento: é a primeira coisa que um jogador procura quando o som
    /// está alto demais às duas da manhã, e a ausência dela é motivo de análise negativa antes
    /// de qualquer julgamento sobre o jogo em si.</para>
    ///
    /// <para><b>Separado do save de propósito.</b> Preferência não é progresso: apagar a
    /// partida para recomeçar não pode zerar o volume que a pessoa ajustou. Quem persiste é a
    /// <c>PreferenciasBridge</c>, em arquivo próprio.</para>
    ///
    /// <para><b>Sobre sincronização vertical.</b> A documentação da Unity 6.4 é explícita:
    /// <i>"It's recommended to use QualitySettings.vSyncCount over Application.targetFrameRate
    /// because vSyncCount implements a hardware-based synchronization mechanism, whereas
    /// targetFrameRate is a software-based timing method and is subject to microstuttering"</i>
    /// — e <i>"if vSyncCount != 0, then targetFrameRate is ignored"</i>. Por isso o padrão aqui
    /// é <b>VSync ligado</b>, e o limite de quadros só existe para quem o desligar.</para>
    /// </summary>
    public sealed class PreferenciasDoJogador
    {
        /// <summary>Limite de quadros que significa "sem limite" (o <c>-1</c> da Unity).</summary>
        public const int SemLimiteDeQuadros = -1;

        private float _volumeGeral = 0.8f;
        private bool _telaCheia = true;
        private bool _sincronizacaoVertical = true;
        private int _limiteDeQuadros = SemLimiteDeQuadros;

        /// <summary>Disparado a cada mudança. Quem aplica no motor escuta isto.</summary>
        public event Action OnMudou;

        /// <summary>
        /// Volume geral, de 0 a 1. É o único canal que existe hoje — o jogo tem efeitos e
        /// <b>não tem música</b>, e um painel com três barras para um canal só mentiria sobre o
        /// que o jogador controla.
        /// </summary>
        public float VolumeGeral
        {
            get => _volumeGeral;
            set => Definir(ref _volumeGeral, Limitar(value));
        }

        /// <summary>Tela cheia ou janela.</summary>
        public bool TelaCheia
        {
            get => _telaCheia;
            set => Definir(ref _telaCheia, value);
        }

        /// <summary>
        /// Sincronização vertical. <b>Ligada por padrão</b>, por recomendação explícita da
        /// documentação da Unity 6.4: é sincronização por hardware, sem o microstuttering do
        /// limite por software.
        /// </summary>
        public bool SincronizacaoVertical
        {
            get => _sincronizacaoVertical;
            set => Definir(ref _sincronizacaoVertical, value);
        }

        /// <summary>
        /// Teto de quadros por segundo, ou <see cref="SemLimiteDeQuadros"/>.
        ///
        /// <para><b>Só tem efeito com <see cref="SincronizacaoVertical"/> desligada</b> — com
        /// ela ligada a Unity ignora este número. <see cref="LimiteEfetivoDeQuadros"/> devolve o
        /// que de fato vale, para nenhuma tela mostrar um valor que não está acontecendo.</para>
        /// </summary>
        public int LimiteDeQuadros
        {
            get => _limiteDeQuadros;
            set => Definir(ref _limiteDeQuadros, Sanear(value));
        }

        /// <summary>
        /// O limite que <b>realmente</b> vale, considerando a sincronização vertical. Com VSync
        /// ligada devolve <see cref="SemLimiteDeQuadros"/>, porque é isso que a Unity faz —
        /// mostrar "60" numa opção que o motor ignora seria a interface mentindo.
        /// </summary>
        public int LimiteEfetivoDeQuadros =>
            _sincronizacaoVertical ? SemLimiteDeQuadros : _limiteDeQuadros;

        /// <summary>Volta tudo ao padrão de fábrica, num evento só.</summary>
        public void Restaurar()
        {
            _volumeGeral = 0.8f;
            _telaCheia = true;
            _sincronizacaoVertical = true;
            _limiteDeQuadros = SemLimiteDeQuadros;

            OnMudou?.Invoke();
        }

        /// <summary>
        /// Aplica valores vindos do disco <b>sem disparar um evento por campo</b>: carregar
        /// quatro preferências dispararia quatro reconfigurações do motor, e a tela piscaria
        /// no arranque.
        /// </summary>
        public void Restaurar(float volume, bool telaCheia, bool vsync, int limite)
        {
            _volumeGeral = Limitar(volume);
            _telaCheia = telaCheia;
            _sincronizacaoVertical = vsync;
            _limiteDeQuadros = Sanear(limite);

            OnMudou?.Invoke();
        }

        private void Definir<T>(ref T campo, T valor) where T : IEquatable<T>
        {
            if (campo.Equals(valor)) return;   // sem evento quando nada mudou

            campo = valor;
            OnMudou?.Invoke();
        }

        private static float Limitar(float v) => v < 0f ? 0f : v > 1f ? 1f : v;

        /// <summary>
        /// Qualquer valor abaixo de 1 vira "sem limite". Zero seria um jogo congelado, e
        /// negativos que não sejam o sentinela não têm significado — saneá-los aqui evita que
        /// um arquivo de preferências corrompido trave a partida.
        /// </summary>
        private static int Sanear(int v) => v < 1 ? SemLimiteDeQuadros : v;
    }
}
