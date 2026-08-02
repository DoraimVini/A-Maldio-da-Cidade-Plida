using System;

namespace FavelaAmarela.Core.Combat
{
    /// <summary>
    /// Ficha de atributos base de uma unidade do jogo (Cultista Amarelo, Damião,
    /// Aparição Primordial, Espectro...). POCO puro — o <c>ScriptableObject</c> de
    /// autoria (Runtime) e os assets de ficha nascem <b>por cima</b> deste tipo, nunca
    /// o contrário (o Core não conhece Unity).
    ///
    /// Toda unidade tem uma ficha. Ela é a fonte dos números que o resto do combate
    /// consome: a <see cref="Vitalidade"/> nasce de <see cref="VitalidadeMax"/>, o golpe
    /// usa <see cref="Ataque"/>/<see cref="Conjuracao"/> como dano bruto, e a mitigação
    /// (<see cref="MitigacaoDeDano"/>) usa <see cref="Defesa"/>/<see cref="ResistenciaAnomala"/>
    /// do defensor. Físico e anômalo (mágico) são dois canais separados:
    /// <list type="bullet">
    ///   <item>Físico: <see cref="Ataque"/> mitigado por <see cref="Defesa"/> → Vitalidade.</item>
    ///   <item>Anômalo: <see cref="Conjuracao"/> mitigado por <see cref="ResistenciaAnomala"/> → Resiliência Mental.</item>
    /// </list>
    /// </summary>
    public sealed class FichaDeAtributos
    {
        /// <summary>Teto da Vitalidade corpórea (a "carne"). Deve ser &gt; 0.</summary>
        public float VitalidadeMax { get; }

        /// <summary>Poder ofensivo físico — dano bruto do golpe corpo-a-corpo da unidade.</summary>
        public float Ataque { get; }

        /// <summary>Mitigação física — subtraída do dano físico recebido (ver <see cref="MitigacaoDeDano"/>).</summary>
        public float Defesa { get; }

        /// <summary>Poder ofensivo anômalo — dano bruto das magias/conjurações da unidade (0 se não conjura).</summary>
        public float Conjuracao { get; }

        /// <summary>Mitigação anômala — subtraída do dano de conjuração recebido (defesa mágica).</summary>
        public float ResistenciaAnomala { get; }

        /// <param name="vitalidadeMax">Teto da Vitalidade. Deve ser maior que zero.</param>
        /// <param name="ataque">Poder ofensivo físico (&gt;= 0).</param>
        /// <param name="defesa">Mitigação física (&gt;= 0).</param>
        /// <param name="conjuracao">Poder ofensivo anômalo (&gt;= 0). Default 0 — a maioria das unidades não conjura.</param>
        /// <param name="resistenciaAnomala">Mitigação anômala (&gt;= 0). Default 0.</param>
        public FichaDeAtributos(
            float vitalidadeMax,
            float ataque,
            float defesa,
            float conjuracao = 0f,
            float resistenciaAnomala = 0f)
        {
            if (vitalidadeMax <= 0f)
                throw new ArgumentOutOfRangeException(nameof(vitalidadeMax),
                    "VitalidadeMax deve ser maior que zero.");
            ExigirNaoNegativo(ataque, nameof(ataque));
            ExigirNaoNegativo(defesa, nameof(defesa));
            ExigirNaoNegativo(conjuracao, nameof(conjuracao));
            ExigirNaoNegativo(resistenciaAnomala, nameof(resistenciaAnomala));

            VitalidadeMax = vitalidadeMax;
            Ataque = ataque;
            Defesa = defesa;
            Conjuracao = conjuracao;
            ResistenciaAnomala = resistenciaAnomala;
        }

        private static void ExigirNaoNegativo(float valor, string nome)
        {
            if (valor < 0f)
                throw new ArgumentOutOfRangeException(nome, $"{nome} não pode ser negativo.");
        }
    }
}
