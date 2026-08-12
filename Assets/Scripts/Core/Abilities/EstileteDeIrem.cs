namespace FavelaAmarela.Core.Abilities
{
    /// <summary>
    /// Estilete de Irem — lâmina fina e rápida de Irem, a Cidade dos Pilares onde
    /// Alhazred aprendeu seus segredos. Arma da Mão Física (mundana).
    ///
    /// Ataque básico: rápido, dano baixo (o bônus de crítico furtivo é resolvido no
    /// receptor do golpe, e <b>não se aplica a bosses</b> — Aparições Primordiais são
    /// imunes a crítico de furtividade). Habilidade "Ferida de Aklo": aplica
    /// <b>sangramento</b> (dano contínuo por segundo) — contra o boss, troca o
    /// crítico único por dano acumulado ao longo do tempo.
    /// </summary>
    public sealed class EstileteDeIrem : IArmaComHabilidade
    {
        public string NomeDaArma => "Estilete de Irem";
        public string NomeHabilidade => "Ferida de Aklo";

        private readonly float duracaoBasico;
        private readonly float cooldownBasico;
        private readonly float danoBasico;

        private readonly float duracaoHabilidade;
        private readonly float cooldownHabilidade;
        private readonly float danoHabilidade;
        private readonly float sangramentoPorSegundo;
        private readonly float duracaoSangramento;

        private readonly int acumulosDoBasico;
        private readonly int acumulosDaHabilidade;

        public EstileteDeIrem(
            float duracaoBasico = 0.25f, float cooldownBasico = 0.3f, float danoBasico = 25f,
            float duracaoHabilidade = 0.3f, float cooldownHabilidade = 5f, float danoHabilidade = 15f,
            float sangramentoPorSegundo = 4f, float duracaoSangramento = 5f,
            int acumulosDoBasico = 1, int acumulosDaHabilidade = 3)
        {
            this.duracaoBasico = duracaoBasico;
            this.cooldownBasico = cooldownBasico;
            this.danoBasico = danoBasico;
            this.duracaoHabilidade = duracaoHabilidade;
            this.cooldownHabilidade = cooldownHabilidade;
            this.danoHabilidade = danoHabilidade;
            this.sangramentoPorSegundo = sangramentoPorSegundo;
            this.duracaoSangramento = duracaoSangramento;
            this.acumulosDoBasico = acumulosDoBasico;
            this.acumulosDaHabilidade = acumulosDaHabilidade;
        }

        public bool CanActivate(float timeSinceLastUse) => timeSinceLastUse >= cooldownBasico;

        /// <summary>
        /// Ataque básico: rápido, dano baixo, e <b>abre 1 acúmulo de sangramento</b>. É o
        /// que torna o acúmulo alcançável — com cooldown de 0,3 s, manter a pressão sobe a
        /// contagem depressa, enquanto a habilidade sozinha (cooldown 5 s) levaria quase um
        /// minuto para chegar ao teto.
        /// </summary>
        public ArmaResult Execute() => new ArmaResult(
            success: true, durationSeconds: duracaoBasico, cooldownSeconds: cooldownBasico,
            dano: danoBasico,
            sangramentoPorSegundo: sangramentoPorSegundo, duracaoSangramento: duracaoSangramento,
            acumulosDeSangramento: acumulosDoBasico);

        public bool CanActivateHabilidade(float timeSinceLastAbilityUse) => timeSinceLastAbilityUse >= cooldownHabilidade;

        /// <summary>
        /// Ferida de Aklo: abre vários acúmulos de uma vez, acelerando o caminho até o
        /// estouro. É o "empurrão" que o jogador guarda para a janela de vulnerabilidade.
        /// </summary>
        public ArmaResult ExecuteHabilidade() => new ArmaResult(
            success: true, durationSeconds: duracaoHabilidade, cooldownSeconds: cooldownHabilidade,
            dano: danoHabilidade,
            sangramentoPorSegundo: sangramentoPorSegundo, duracaoSangramento: duracaoSangramento,
            acumulosDeSangramento: acumulosDaHabilidade);
    }
}
