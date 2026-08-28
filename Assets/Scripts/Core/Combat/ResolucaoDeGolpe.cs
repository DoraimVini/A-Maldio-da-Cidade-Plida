using FavelaAmarela.Core.Abilities;
using FavelaAmarela.Core.Loot;

namespace FavelaAmarela.Core.Combat
{
    /// <summary>
    /// Fecha o dano de um golpe: faixa branca → percentual da habilidade → afixos → acerto →
    /// crítico. O que sai daqui vai para <see cref="MitigacaoDeDano"/>, que <b>não muda</b>.
    ///
    /// <para><b>Onde esta camada entra.</b> Até 2026-08-28 o <c>ArmaResult</c> já saía do
    /// <c>HabilidadeComposta.Execute()</c> com o dano final — um número fixo autorado na
    /// habilidade. Agora ele sai com um <b>percentual</b> (quanto do dano da arma este golpe
    /// aproveita) e é aqui que o percentual encontra a arma.</para>
    ///
    /// <para><b>Sorteio por GOLPE, não por alvo.</b> Uma pancada que varre três inimigos acerta
    /// ou erra os três juntos, e critica os três juntos. É a leitura de "você deu um golpe bom":
    /// o <c>ArmaResult</c> é imutável e entregue a todos os alvos da janela, e rolar por alvo
    /// exigiria mover a resolução para dentro da <c>Hitbox</c>. Quando os inimigos ganharem
    /// Evasão própria, a rolagem por alvo passa a valer a pena — <b>hoje ninguém tem</b>.</para>
    ///
    /// <para><b>Quantos números a fonte consome</b> (importa para teste determinístico):
    /// <b>2</b> quando o golpe erra (faixa + acerto) e <b>3</b> quando conecta (faixa + acerto +
    /// crítico). O retorno antecipado no erro é de propósito — um golpe que não aconteceu não
    /// critica.</para>
    ///
    /// <para>Molde copiado de <see cref="Bloqueio"/>: POCO puro, aleatoriedade injetada, e um
    /// resultado que carrega o <i>flag</i> junto do número para a UI e o áudio se pendurarem.</para>
    /// </summary>
    public static class ResolucaoDeGolpe
    {
        /// <summary>
        /// Resolve o dano físico de um golpe já composto.
        /// </summary>
        /// <param name="golpe">
        /// O resultado vindo da arma, com <c>PercentualDoDanoDaArma</c> preenchido e
        /// <c>Dano</c> contendo o que for <b>plano</b> (efeito de dano fixo + bônus de
        /// equipamento já somado por <c>ComBonus</c>).
        /// </param>
        /// <param name="perfil">O bloco de combate da arma empunhada.</param>
        /// <param name="aumentoPercentual">
        /// Aumento de dano físico dos afixos, em fração (0,25 = +25%). Multiplica o total.
        /// </param>
        /// <param name="bonusChanceCritica">Chance de crítico somada pelo equipamento, em fração.</param>
        /// <param name="bonusDanoCritico">Multiplicador de crítico somado pelo equipamento.</param>
        /// <param name="bonusPrecisao">Precisão somada pelo equipamento, em fração.</param>
        /// <param name="fonte">Fonte de aleatoriedade. <c>null</c> resolve na média, sem erro nem crítico.</param>
        public static ArmaResult Resolver(ArmaResult golpe, PerfilDeArma perfil,
                                          float aumentoPercentual = 0f,
                                          float bonusChanceCritica = 0f,
                                          float bonusDanoCritico = 0f,
                                          float bonusPrecisao = 0f,
                                          IFonteDeAleatoriedade fonte = null)
        {
            // Golpe sem percentual não passa por aqui: é dano plano de inimigo ou de habilidade
            // de valor fixo, e reescrevê-lo seria mudar o que ele significa.
            if (golpe.PercentualDoDanoDaArma <= 0f) return golpe;

            float branco = perfil.RolarDanoBranco(fonte);
            float dano = branco * golpe.PercentualDoDanoDaArma + golpe.Dano;

            if (aumentoPercentual > 0f) dano *= 1f + aumentoPercentual;
            if (dano < 0f) dano = 0f;

            // ── Acerto ────────────────────────────────────────────────────────
            // O sorteio é consumido SEMPRE que há fonte, mesmo com precisão 1,0 — e a primeira
            // versão disto tinha um `precisao < 1f` no curto-circuito, o que fazia o número de
            // rolagens depender do valor da precisão. Contrato instável quebra todo teste
            // determinístico, e foi o próprio teste de crítico que denunciou: esperava 3
            // números e recebeu 2.
            float precisao = Limitar(perfil.Precisao + bonusPrecisao);

            if (fonte != null && fonte.ProximoValor() > precisao)
                return golpe.ComDanoResolvido(0f, critico: false, errou: true);

            // ── Crítico ───────────────────────────────────────────────────────
            // Mesma regra: consome sempre, para o contrato ser "2 ao errar, 3 ao acertar".
            float chance = Limitar(perfil.ChanceCritica + bonusChanceCritica);
            bool critico = fonte != null && fonte.ProximoValor() < chance;

            if (critico)
            {
                float multiplicador = perfil.MultiplicadorCritico + bonusDanoCritico;
                if (multiplicador < 1f) multiplicador = 1f;
                dano *= multiplicador;
            }

            return golpe.ComDanoResolvido(dano, critico, errou: false);
        }

        private static float Limitar(float v) => v < 0f ? 0f : (v > 1f ? 1f : v);
    }
}
