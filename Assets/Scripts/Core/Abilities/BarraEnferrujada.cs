using System;

namespace FavelaAmarela.Core.Abilities
{
    /// <summary>
    /// POCO da família "Barra Enferrujada" — a mesma arma mundana que os
    /// Cultistas Amarelos usam contra Damião. Golpe pesado e simples: cada
    /// acerto tem uma <em>chance</em> de atordoar o alvo (nem todo golpe
    /// atordoa), nunca uma garantia. Não implementa <see cref="IAnomalyPower"/>
    /// pois não distorce Carcosa nem custa Resiliência Mental.
    /// </summary>
    public sealed class BarraEnferrujada : IArma
    {
        public string NomeDaArma => "Barra Enferrujada";

        private readonly float duration;
        private readonly float cooldown;
        private readonly float probabilidadeAtordoar;
        private readonly float duracaoAtordoamento;
        private readonly Func<double> amostraAleatoria;

        private static readonly Random _randomPadrao = new Random();

        /// <param name="amostraAleatoria">
        /// Fonte de números em [0, 1) usada pra decidir o atordoamento. Injetável
        /// pra testes determinísticos (ex.: <c>() => 0.0</c> força atordoar,
        /// <c>() => 0.99</c> força não atordoar). Usa <see cref="Random"/> padrão
        /// se omitido.
        /// </param>
        public BarraEnferrujada(
            float duration = 0.3f,
            float cooldown = 0.6f,
            float probabilidadeAtordoar = 0.35f,
            float duracaoAtordoamento = 2f,
            Func<double> amostraAleatoria = null)
        {
            this.duration = duration;
            this.cooldown = cooldown;
            this.probabilidadeAtordoar = probabilidadeAtordoar;
            this.duracaoAtordoamento = duracaoAtordoamento;
            this.amostraAleatoria = amostraAleatoria ?? (() => _randomPadrao.NextDouble());
        }

        public bool CanActivate(float timeSinceLastUse) => timeSinceLastUse >= cooldown;

        public ArmaResult Execute()
        {
            bool atordoou = amostraAleatoria() < probabilidadeAtordoar;
            return new ArmaResult(
                success: true,
                durationSeconds: duration,
                cooldownSeconds: cooldown,
                atordoou: atordoou,
                duracaoAtordoamento: atordoou ? duracaoAtordoamento : 0f);
        }
    }
}
