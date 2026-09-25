namespace FavelaAmarela.Core.Abilities
{
    /// <summary>
    /// Alfanje de Alhazred — a própria cimitarra curva de Abdul Alhazred, enferrujada
    /// por séculos na cripta. Arma da Mão Física (mundana), pesada.
    ///
    /// Ataque básico: dano moderado, golpe em arco (a resolução em cone fica a cargo
    /// do adaptador Runtime). Habilidade "Golpe do Deserto": golpe carregado que
    /// <b>repele e desequilibra</b> o alvo (empurrão + atordoamento breve) — contra o
    /// Abdul, cria espaço para sair dos círculos de dreno de RM.
    /// </summary>
    public sealed class AlfanjeDeAlhazred : IArmaComHabilidade
    {
        public string NomeDaArma => "Alfanje de Alhazred";
        public string NomeHabilidade => "Golpe do Deserto";

        private readonly float duracaoBasico;
        private readonly float cooldownBasico;
        private readonly float danoBasico;

        private readonly float duracaoHabilidade;
        private readonly float cooldownHabilidade;
        private readonly float danoHabilidade;
        private readonly float forcaRepulsao;
        private readonly float duracaoAtordoamento;

        public AlfanjeDeAlhazred(
            float duracaoBasico = 0.45f, float cooldownBasico = 0.7f, float danoBasico = 45f,
            float duracaoHabilidade = 0.5f, float cooldownHabilidade = 5f, float danoHabilidade = 40f,
            float forcaRepulsao = 6f, float duracaoAtordoamento = 2f)
        {
            this.duracaoBasico = duracaoBasico;
            this.cooldownBasico = cooldownBasico;
            this.danoBasico = danoBasico;
            this.duracaoHabilidade = duracaoHabilidade;
            this.cooldownHabilidade = cooldownHabilidade;
            this.danoHabilidade = danoHabilidade;
            this.forcaRepulsao = forcaRepulsao;
            this.duracaoAtordoamento = duracaoAtordoamento;
        }

        public bool CanActivate(float timeSinceLastUse) => timeSinceLastUse >= cooldownBasico;

        public ArmaResult Execute() => new ArmaResult(
            success: true, durationSeconds: duracaoBasico, cooldownSeconds: cooldownBasico,
            dano: danoBasico);

        public bool CanActivateHabilidade(float timeSinceLastAbilityUse) => timeSinceLastAbilityUse >= cooldownHabilidade;

        public ArmaResult ExecuteHabilidade() => new ArmaResult(
            success: true, durationSeconds: duracaoHabilidade, cooldownSeconds: cooldownHabilidade,
            dano: danoHabilidade, atordoou: true, duracaoAtordoamento: duracaoAtordoamento,
            forcaRepulsao: forcaRepulsao);
    }
}
