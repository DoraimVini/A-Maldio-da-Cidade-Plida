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

        public EstileteDeIrem(
            float duracaoBasico = 0.25f, float cooldownBasico = 0.3f, float danoBasico = 5f,
            float duracaoHabilidade = 0.3f, float cooldownHabilidade = 5f, float danoHabilidade = 3f,
            float sangramentoPorSegundo = 3f, float duracaoSangramento = 5f)
        {
            this.duracaoBasico = duracaoBasico;
            this.cooldownBasico = cooldownBasico;
            this.danoBasico = danoBasico;
            this.duracaoHabilidade = duracaoHabilidade;
            this.cooldownHabilidade = cooldownHabilidade;
            this.danoHabilidade = danoHabilidade;
            this.sangramentoPorSegundo = sangramentoPorSegundo;
            this.duracaoSangramento = duracaoSangramento;
        }

        public bool CanActivate(float timeSinceLastUse) => timeSinceLastUse >= cooldownBasico;

        public ArmaResult Execute() => new ArmaResult(
            success: true, durationSeconds: duracaoBasico, cooldownSeconds: cooldownBasico,
            dano: danoBasico);

        public bool CanActivateHabilidade(float timeSinceLastAbilityUse) => timeSinceLastAbilityUse >= cooldownHabilidade;

        public ArmaResult ExecuteHabilidade() => new ArmaResult(
            success: true, durationSeconds: duracaoHabilidade, cooldownSeconds: cooldownHabilidade,
            dano: danoHabilidade, sangramentoPorSegundo: sangramentoPorSegundo, duracaoSangramento: duracaoSangramento);
    }
}
