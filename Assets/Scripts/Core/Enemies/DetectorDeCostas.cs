using UnityEngine;

namespace FavelaAmarela.Core.Enemies
{
    /// <summary>
    /// Geometria pura: se um observador está de costas para um ponto. Existe para a mecânica
    /// da Máscara Pálida do Rei em Amarelo — "dar as costas" precisa de uma definição exata,
    /// não de "parece que sim".
    ///
    /// <para><c>Vector2</c>/<c>Mathf</c> são permitidos em <c>Core</c> para cálculo
    /// (`CLAUDE.md` §Core), então isto continua POCO puro e testável sem cena.</para>
    /// </summary>
    public static class DetectorDeCostas
    {
        /// <summary>
        /// Se quem está em <paramref name="posicaoDoObservador"/>, olhando para
        /// <paramref name="direcaoDoObservador"/>, está de costas para
        /// <paramref name="posicaoDoAlvo"/>.
        ///
        /// <para>"De costas" = a direção do olhar aponta para <b>longe</b> do alvo. Comparamos
        /// o olhar com o vetor observador→alvo: se apontam para lados opostos (produto escalar
        /// bem negativo), o observador deu as costas. <paramref name="limiar"/> é o quão
        /// rigoroso — <c>-1</c> exigiria alinhamento perfeito (impossível de acertar por
        /// input), <c>0</c> aceitaria só um perfil (90°). O padrão (-0.5) exige apontar dentro
        /// de ~60° da direção oposta ao Rei: folgado o bastante para não parecer injusto,
        /// apertado o bastante para "de lado" não contar como "de costas".</para>
        /// </summary>
        public static bool EstaDeCostas(
            Vector2 posicaoDoObservador,
            Vector2 direcaoDoObservador,
            Vector2 posicaoDoAlvo,
            float limiar = -0.5f)
        {
            Vector2 paraAlvo = posicaoDoAlvo - posicaoDoObservador;
            if (paraAlvo.sqrMagnitude < 0.0001f) return false; // em cima do alvo: sem "costas" que valham

            Vector2 olhar = direcaoDoObservador;
            if (olhar.sqrMagnitude < 0.0001f) return false; // sem direção definida, não arrisca

            float produtoEscalar = Vector2.Dot(olhar.normalized, paraAlvo.normalized);
            return produtoEscalar <= limiar;
        }
    }
}
