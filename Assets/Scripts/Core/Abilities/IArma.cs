namespace FavelaAmarela.Core.Abilities
{
    /// <summary>
    /// Resultado imutável de uma execução de <see cref="IArma"/> — tanto do ataque
    /// básico (<see cref="IArma.Execute"/>) quanto da habilidade
    /// (<see cref="IArmaComHabilidade.ExecuteHabilidade"/>). Cada arma preenche só
    /// os campos que seu golpe/habilidade usa; o resto fica no default.
    /// </summary>
    public readonly struct ArmaResult
    {
        public readonly bool Success;
        public readonly float DurationSeconds;
        public readonly float CooldownSeconds;

        /// <summary>Se este golpe específico atordoou o alvo (nem toda arma usa isso).</summary>
        public readonly bool Atordoou;

        /// <summary>Duração do atordoamento, se <see cref="Atordoou"/> for true.</summary>
        public readonly float DuracaoAtordoamento;

        /// <summary>Dano físico aplicado ao alvo (subtraído da vida de um <c>IDanificavel</c>).</summary>
        public readonly float Dano;

        /// <summary>
        /// Se este golpe cancela a canalização anômala do alvo (habilidade do
        /// Cravo de Aklo). Usado contra as conjurações do Abdul (ventos congelantes,
        /// círculos de dreno) na janela de conjuração.
        /// </summary>
        public readonly bool InterrompeConjuracao;

        /// <summary>Dano contínuo por segundo que <b>cada acúmulo</b> de sangramento contribui (Estilete de Irem).</summary>
        public readonly float SangramentoPorSegundo;

        /// <summary>Duração, em segundos, do sangramento (se <see cref="SangramentoPorSegundo"/> &gt; 0).</summary>
        public readonly float DuracaoSangramento;

        /// <summary>
        /// Quantos acúmulos de sangramento este golpe aplica. O ataque básico do Estilete
        /// traz 1; a habilidade (Ferida de Aklo) traz vários. Ao chegar ao teto, as feridas
        /// estouram — ver <c>Sangramento</c>.
        /// </summary>
        public readonly int AcumulosDeSangramento;

        /// <summary>Força de repulsão (empurrão) aplicada ao alvo, em unidades (habilidade do Alfanje de Alhazred).</summary>
        public readonly float ForcaRepulsao;

        public ArmaResult(bool success, float durationSeconds, float cooldownSeconds,
            bool atordoou = false, float duracaoAtordoamento = 0f,
            float dano = 0f, bool interrompeConjuracao = false,
            float sangramentoPorSegundo = 0f, float duracaoSangramento = 0f,
            float forcaRepulsao = 0f, int acumulosDeSangramento = 0)
        {
            AcumulosDeSangramento = acumulosDeSangramento;
            Success = success;
            DurationSeconds = durationSeconds;
            CooldownSeconds = cooldownSeconds;
            Atordoou = atordoou;
            DuracaoAtordoamento = duracaoAtordoamento;
            Dano = dano;
            InterrompeConjuracao = interrompeConjuracao;
            SangramentoPorSegundo = sangramentoPorSegundo;
            DuracaoSangramento = duracaoSangramento;
            ForcaRepulsao = forcaRepulsao;
        }
    }

    /// <summary>
    /// Contrato para armas físicas equipadas na Mão Física de Damião — mundanas,
    /// sem custo de Resiliência Mental (diferente de <see cref="IAnomalyPower"/>,
    /// que é pra Mão Anômala). Cada família de arma implementa isso e define seu
    /// próprio "verbo de combate": a Barra Enferrujada atordoa por chance, a
    /// Lâmina do Sinal bonifica ataque furtivo, etc.
    /// </summary>
    public interface IArma
    {
        /// <summary>Nome diegético da arma.</summary>
        string NomeDaArma { get; }

        /// <summary>Só valida cooldown — arma física não tem custo de recurso.</summary>
        bool CanActivate(float timeSinceLastUse);

        ArmaResult Execute();
    }

    /// <summary>
    /// Arma física que, além do ataque básico (<see cref="IArma"/>), tem uma
    /// <em>habilidade</em> em botão separado, com cooldown próprio. Cada arma da
    /// Tumba de Alhazred define a sua — é o que muda a build do jogador conforme
    /// a arma que ele dropou do baú (Cravo interrompe, Estilete sangra, Alfanje
    /// repele). A habilidade é a "ferramenta de boss": os Vultos/Aparições
    /// Primordiais são imunes a crítico furtivo, então a furtividade não resolve
    /// a luta — a habilidade da arma sim.
    /// </summary>
    public interface IArmaComHabilidade : IArma
    {
        /// <summary>Nome diegético da habilidade (input separado do ataque básico).</summary>
        string NomeHabilidade { get; }

        /// <summary>Valida o cooldown da habilidade, independente do ataque básico.</summary>
        bool CanActivateHabilidade(float timeSinceLastAbilityUse);

        /// <summary>Executa a habilidade da arma (efeito no <see cref="ArmaResult"/>).</summary>
        ArmaResult ExecuteHabilidade();
    }
}
