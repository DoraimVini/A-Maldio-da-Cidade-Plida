using FavelaAmarela.Core.Abilities;

namespace FavelaAmarela.Core.Combat
{
    /// <summary>
    /// Contrato para qualquer entidade que pode receber um golpe de arma física
    /// (Cultista Amarelo, Aparição Primordial/boss, etc.). Desacopla o resolvedor de
    /// golpe (<c>MaoFisicaBridge</c>) de tipos concretos: antes ele só reconhecia o
    /// Cultista, o que tornava todo o resto imune "de graça" (é por isso que a Coisa
    /// do Cemitério é imortal — ela simplesmente não implementa isto).
    /// </summary>
    public interface IDanificavel
    {
        /// <summary>
        /// Se é uma Aparição Primordial (Vulto/boss). Aparições Primordiais são
        /// <b>imunes ao crítico de furtividade</b> — furtividade não resolve a luta
        /// contra bosses, só serve para chegar até ela.
        /// </summary>
        bool EhAparicaoPrimordial { get; }

        /// <summary>Aplica o resultado de um golpe físico (dano + efeitos do <see cref="ArmaResult"/>).</summary>
        void ReceberGolpe(ArmaResult resultado);

        /// <summary>
        /// Se esta entidade já foi <b>abatida</b>.
        ///
        /// <para><b>Por que a interface precisa disto (2026-09-09).</b> O som de impacto passou
        /// a sair da <c>Hurtbox</c>, que é o único ponto por onde todo golpe que acerta passa.
        /// O som de <b>abate</b> não pôde ir junto: a Hurtbox vê o golpe chegar e não sabe se
        /// ele derrubou alguém. Ele ficava no <c>AudioDeCombate</c>, que exige
        /// <c>EnemyBase</c> — e só o Cultista e o Byakhee têm. Abdul, Esqueletos, Pedras de
        /// Poder e o próprio Damião morriam <b>em silêncio</b>.</para>
        ///
        /// <para>Com este membro, a Hurtbox compara antes e depois de entregar o golpe e
        /// reconhece a transição. Todos os cinco implementadores já tinham o estado — a
        /// <c>Vitalidade</c> de cada um —, só não o expunham por um contrato comum.</para>
        ///
        /// <para><b>Limite conhecido:</b> isto reconhece morte <b>por golpe</b>. Quem cai por
        /// sangramento, por expirar o tempo de vida ou por Colapso mental não passa pela
        /// Hurtbox, e continua caindo calado.</para>
        /// </summary>
        bool EstaAbatido { get; }
    }
}
