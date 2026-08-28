namespace FavelaAmarela.Core.Abilities.Efeitos
{
    /// <summary>
    /// Dano como <b>percentual do dano branco da arma</b> — o efeito que faz a habilidade
    /// escalar com o equipamento em vez de ter número próprio.
    ///
    /// <para><b>Por que ele existe e o <see cref="EfeitoDeDano"/> continua (2026-08-28).</b>
    /// Reinterpretar o <c>Dano</c> plano como percentual mudaria o significado de todo asset já
    /// autorado em silêncio — <c>Valor: 45</c> viraria 45%. Pior: o dano plano continua
    /// <b>legítimo</b> onde não há arma nenhuma (golpe de inimigo, habilidade de valor fixo).
    /// Os dois convivem, e <c>ResolucaoDeGolpe</c> soma um ao outro.</para>
    ///
    /// <para><b>Somam-se quando repetidos</b>, como o dano plano: duas linhas de 50% dão 100%.
    /// É o que permite compor "o golpe base da arma" com "o bônus da habilidade" em efeitos
    /// separados, e é o mesmo contrato que <c>EfeitosDeHabilidadeTests</c> já guarda para o
    /// dano.</para>
    /// </summary>
    public sealed class EfeitoDeDanoDaArma : IEfeitoDeHabilidade
    {
        private readonly float _percentual;

        /// <summary>Nome legível, usado em diagnóstico e no tooltip do item.</summary>
        public string Nome => $"{_percentual * 100f:0.##}% do dano da arma";

        /// <param name="percentual">
        /// Fração do dano branco (1,0 = 100%). Negativo é tratado como zero — habilidade que
        /// <i>subtrai</i> dano da arma não é conceito deste jogo, e um sinal trocado no
        /// Inspector não pode virar cura acidental.
        /// </param>
        public EfeitoDeDanoDaArma(float percentual)
            => _percentual = percentual < 0f ? 0f : percentual;

        /// <inheritdoc/>
        public void Aplicar(ConstrutorDeGolpe golpe)
            => golpe.PercentualDoDanoDaArma += _percentual;
    }
}
