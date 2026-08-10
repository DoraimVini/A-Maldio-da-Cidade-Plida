using System;

namespace FavelaAmarela.Core.Combat
{
    /// <summary>
    /// Fórmula pura de mitigação de dano físico por defesa — a "conta" do combate,
    /// isolada num único lugar testável (nada de <c>max()</c> espalhado pelo Runtime).
    ///
    /// Modelo <b>subtrativo com piso</b> (decisão de design 2026-07-29): a defesa
    /// subtrai um valor plano do golpe ("a armadura absorve X"), mas um piso percentual
    /// garante que <b>nenhuma pilha de defesa deixe o alvo invencível</b> — sempre passa
    /// um mínimo. Escolhido por ser intuitivo para o jogador e escalar de forma segura
    /// com as armaduras coletáveis previstas.
    ///
    /// <para>Fórmula: <c>danoFinal = max(danoBruto × pisoFração, danoBruto − defesa)</c>,
    /// clampado a [0, danoBruto].</para>
    /// </summary>
    public static class MitigacaoDeDano
    {
        /// <summary>Fração mínima do golpe que sempre atravessa a defesa (15%).</summary>
        public const float PisoFracaoPadrao = 0.15f;

        /// <summary>
        /// Aplica a mitigação por defesa a um golpe.
        /// </summary>
        /// <param name="danoBruto">Dano do golpe antes da defesa. Valores &lt;= 0 resultam em 0.</param>
        /// <param name="defesa">Defesa do alvo (inata + armaduras). Valores negativos tratados como 0.</param>
        /// <param name="pisoFracao">
        /// Fração do dano bruto que sempre passa, mesmo com defesa altíssima
        /// (em [0, 1]; default <see cref="PisoFracaoPadrao"/>).
        /// </param>
        /// <returns>Dano final a aplicar na Vitalidade, em [0, <paramref name="danoBruto"/>].</returns>
        public static float Aplicar(float danoBruto, float defesa, float pisoFracao = PisoFracaoPadrao)
        {
            if (danoBruto <= 0f) return 0f;
            if (defesa < 0f) defesa = 0f;
            if (pisoFracao < 0f) pisoFracao = 0f;
            else if (pisoFracao > 1f) pisoFracao = 1f;

            float piso = danoBruto * pisoFracao;
            float aposDefesa = danoBruto - defesa;

            float final = Math.Max(piso, aposDefesa);

            // Nunca negativo nem acima do golpe original.
            if (final < 0f) final = 0f;
            else if (final > danoBruto) final = danoBruto;
            return final;
        }
    }
}
