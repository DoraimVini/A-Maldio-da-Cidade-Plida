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

        /// <summary>Velocidade de patrulha.</summary>
        public float VelocidadeErrante { get; }

        /// <summary>Velocidade de perseguição.</summary>
        public float VelocidadeCaca { get; }

        /// <summary>Alcance do golpe corpo-a-corpo.</summary>
        public float AlcanceDeGolpe { get; }

        /// <summary>Cadência de ataque.</summary>
        public float CadenciaDeAtaque { get; }

        /// <summary>
        /// Teto da Resiliência Mental (a "mente") desta unidade — o alvo do canal anômalo.
        /// <b>Zero significa que a unidade não tem mente a ferir</b>: ela ignora
        /// silenciosamente todo Trauma de Anomalia, sem que ninguém precise de um
        /// <c>if</c> por tipo de inimigo. É o default justamente porque a carne é a regra
        /// e a mente é a exceção — um Cultista comum cai por dano físico; uma criatura de
        /// Carcosa pode ser desfeita pelos dois vetores.
        /// </summary>
        public float ResilienciaMax { get; }

        /// <param name="vitalidadeMax">Teto da Vitalidade. Deve ser maior que zero.</param>
        /// <param name="ataque">Poder ofensivo físico (&gt;= 0).</param>
        /// <param name="defesa">Mitigação física (&gt;= 0).</param>
        /// <param name="conjuracao">Poder ofensivo anômalo (&gt;= 0). Default 0 — a maioria das unidades não conjura.</param>
        /// <param name="resistenciaAnomala">Mitigação anômala (&gt;= 0). Default 0.</param>
        /// <param name="velocidadeErrante">Velocidade de patrulha.</param>
        /// <param name="velocidadeCaca">Velocidade de perseguição.</param>
        /// <param name="alcanceDeGolpe">Alcance do golpe corpo-a-corpo.</param>
        /// <param name="cadenciaDeAtaque">Cadência de ataque.</param>
        /// <param name="resilienciaMax">
        /// Teto da Resiliência Mental (&gt;= 0). Default 0 — a unidade não tem mente a ferir
        /// e é imune ao canal anômalo.
        /// </param>
        public FichaDeAtributos(
            float vitalidadeMax,
            float ataque,
            float defesa,
            float conjuracao = 0f,
            float resistenciaAnomala = 0f,
            float velocidadeErrante = 1.5f,
            float velocidadeCaca = 3.5f,
            float alcanceDeGolpe = 1.2f,
            float cadenciaDeAtaque = 1.2f,
            float resilienciaMax = 0f)
        {
            if (vitalidadeMax <= 0f)
                throw new ArgumentOutOfRangeException(nameof(vitalidadeMax),
                    "VitalidadeMax deve ser maior que zero.");
            ExigirNaoNegativo(ataque, nameof(ataque));
            ExigirNaoNegativo(defesa, nameof(defesa));
            ExigirNaoNegativo(conjuracao, nameof(conjuracao));
            ExigirNaoNegativo(resistenciaAnomala, nameof(resistenciaAnomala));
            ExigirNaoNegativo(resilienciaMax, nameof(resilienciaMax));

            ResilienciaMax = resilienciaMax;
            VitalidadeMax = vitalidadeMax;
            Ataque = ataque;
            Defesa = defesa;
            Conjuracao = conjuracao;
            ResistenciaAnomala = resistenciaAnomala;
            VelocidadeErrante = velocidadeErrante;
            VelocidadeCaca = velocidadeCaca;
            AlcanceDeGolpe = alcanceDeGolpe;
            CadenciaDeAtaque = cadenciaDeAtaque;
        }

        /// <summary>
        /// Devolve uma ficha nova com os bônus somados, <b>preservando todo o resto</b>.
        ///
        /// <para><b>Por que existe:</b> o <c>VitalidadeBridge</c> recalculava a ficha final
        /// chamando o construtor com 3 dos 10 parâmetros. Os outros sete voltavam ao default a
        /// cada troca de equipamento — <see cref="ResistenciaAnomala"/> e
        /// <see cref="ResilienciaMax"/> iam a <b>zero</b>, e velocidades, alcance e cadência
        /// voltavam ao valor de fábrica. Tomar dano cósmico cheio sem motivo aparente era o único
        /// sintoma.</para>
        ///
        /// <para>Mesma correção aplicada ao <c>ArmaResult</c> em 2026-08-12: quando um tipo tem
        /// muitos campos, reconstruí-lo por posição é uma armadilha que cobra na próxima vez que
        /// alguém acrescentar um campo. Aqui não há o que esquecer — o que não recebe bônus é
        /// copiado.</para>
        /// </summary>
        /// <param name="bonusVitalidade">Somado a <see cref="VitalidadeMax"/>.</param>
        /// <param name="bonusDefesa">Somado a <see cref="Defesa"/>.</param>
        /// <param name="bonusAtaque">Somado a <see cref="Ataque"/>.</param>
        /// <param name="bonusResistenciaAnomala">Somado a <see cref="ResistenciaAnomala"/>.</param>
        /// <param name="bonusResilienciaMax">Somado a <see cref="ResilienciaMax"/>.</param>
        public FichaDeAtributos ComBonus(
            float bonusVitalidade = 0f,
            float bonusDefesa = 0f,
            float bonusAtaque = 0f,
            float bonusResistenciaAnomala = 0f,
            float bonusResilienciaMax = 0f)
            => new FichaDeAtributos(
                // Piso de 0 (e de 1 na Vitalidade). Bônus podem ser NEGATIVOS: `ModificadorFixo`
                // é um float livre, e item amaldiçoado é mecânica prevista. Sem o piso, um
                // -8 de Defesa sobre base 6 lançaria ArgumentOutOfRangeException do construtor
                // dentro do recálculo de equipamento, matando a atualização inteira de
                // atributos — proibido pela regra 7 do CLAUDE.md (nunca deixar exceção escapar
                // de um caminho de Inspector/equipamento).
                Math.Max(1f, VitalidadeMax + bonusVitalidade),
                Math.Max(0f, Ataque + bonusAtaque),
                Math.Max(0f, Defesa + bonusDefesa),
                Conjuracao,
                Math.Max(0f, ResistenciaAnomala + bonusResistenciaAnomala),
                VelocidadeErrante,
                VelocidadeCaca,
                AlcanceDeGolpe,
                CadenciaDeAtaque,
                Math.Max(0f, ResilienciaMax + bonusResilienciaMax));

        private static void ExigirNaoNegativo(float valor, string nome)
        {
            if (valor < 0f)
                throw new ArgumentOutOfRangeException(nome, $"{nome} não pode ser negativo.");
        }
    }
}
