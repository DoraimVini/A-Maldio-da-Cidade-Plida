namespace FavelaAmarela.Core.Combat
{
    /// <summary>
    /// O bloco de combate de uma arma — <b>a arma como fonte do dano</b>, que é o que a
    /// tornava impossível de melhorar até 2026-08-28.
    ///
    /// <para><b>O que estava invertido.</b> O dano morava no <c>HabilidadeDef</c>, que é
    /// <b>um asset só, pendurado na família</b>: todo Alfanje que existisse apontava para o
    /// mesmo <c>Valor: 45</c>. Duas cópias eram sempre idênticas e um Alfanje melhor era
    /// <i>inexprimível</i> — não havia campo onde escrever que este é melhor que aquele. A Forja
    /// do Carcosa Debugger não conseguia criar arma mais forte porque o número não estava no
    /// item. "Os itens são fracos demais" não era balanceamento: era a ausência do eixo.</para>
    ///
    /// <para>Num ARPG a arma carrega a <b>faixa de dano branco</b>, a chance e o multiplicador
    /// de crítico e a precisão; a habilidade é um <b>percentual dela</b>. É isso que faz trocar
    /// de arma melhorar todas as habilidades de uma vez, e é isso que dá sentido ao tier.</para>
    ///
    /// <para><b>Faixa, não número fixo.</b> Dano branco de ARPG é intervalo — é o que dá textura
    /// a golpes repetidos e o que os afixos de "aumento percentual" multiplicam. Um valor único
    /// faria toda pancada sair igual, que é o que o jogo faz hoje.</para>
    /// </summary>
    public readonly struct PerfilDeArma
    {
        /// <summary>Piso do dano branco, antes de habilidade, afixo e crítico.</summary>
        public readonly float DanoMin;

        /// <summary>Teto do dano branco.</summary>
        public readonly float DanoMax;

        /// <summary>Probabilidade [0..1] de o golpe ser crítico.</summary>
        public readonly float ChanceCritica;

        /// <summary>Quanto o crítico multiplica o dano (1,5 = +50%).</summary>
        public readonly float MultiplicadorCritico;

        /// <summary>
        /// Probabilidade [0..1] de acertar. O Vini escolheu o modelo do D2: falhar aqui é
        /// <b>errar de verdade</b> — dano zero —, não golpe de raspão.
        /// </summary>
        public readonly float Precisao;

        public PerfilDeArma(float danoMin, float danoMax, float chanceCritica,
                            float multiplicadorCritico, float precisao)
        {
            // Faixa invertida é erro de autoria, não caso de jogo: ordena em vez de estourar.
            if (danoMax < danoMin) (danoMin, danoMax) = (danoMax, danoMin);

            DanoMin = danoMin < 0f ? 0f : danoMin;
            DanoMax = danoMax < 0f ? 0f : danoMax;

            ChanceCritica = Limitar(chanceCritica);
            Precisao = Limitar(precisao);

            // Crítico que reduz dano seria armadilha silenciosa de autoria.
            MultiplicadorCritico = multiplicadorCritico < 1f ? 1f : multiplicadorCritico;
        }

        /// <summary>
        /// O punho de Damião. Dano zero de propósito — o gesto desarmado faz barulho e entra no
        /// estado Atacando, mas não mata (<c>ficha_de_atributos.md</c>). Precisão cheia para o
        /// desarmado nunca "errar": errar um golpe que já não causa dano só confundiria.
        /// </summary>
        public static PerfilDeArma Desarmado => new PerfilDeArma(0f, 0f, 0f, 1f, 1f);

        /// <summary>Se esta arma tem dano branco para rolar.</summary>
        public bool TemDanoBranco => DanoMax > 0f;

        /// <summary>
        /// Rola o dano branco dentro da faixa. Recebe a fonte para o sorteio ser determinístico
        /// em teste — mesma injeção de <c>Bloqueio</c> e <c>GeradorDeItem</c>.
        /// </summary>
        public float RolarDanoBranco(Loot.IFonteDeAleatoriedade fonte)
        {
            if (!TemDanoBranco) return 0f;
            if (fonte == null) return (DanoMin + DanoMax) * 0.5f;

            return DanoMin + (DanoMax - DanoMin) * Limitar(fonte.ProximoValor());
        }

        private static float Limitar(float v) => v < 0f ? 0f : (v > 1f ? 1f : v);
    }
}
