using System;
using UnityEngine;

namespace FavelaAmarela.Inventario
{
    /// <summary>Onde o afixo entra no nome e no orçamento do item.</summary>
    public enum TipoDeAfixo
    {
        /// <summary>Vem antes do nome. Um item Marcado recebe um.</summary>
        Prefixo,

        /// <summary>Vem depois do nome. Só o Impregnado recebe.</summary>
        Sufixo,
    }

    /// <summary>
    /// Um modificador que <b>pode</b> cair num item, com a faixa dentro da qual ele rola.
    ///
    /// <para><b>A invariante em vigor desde 2026-08-27</b> (<c>CLAUDE.md</c> §1,
    /// <c>loot_e_drop.md</c>): <i>o gerador nunca inventa um afixo — ele escolhe de um pool
    /// autorado e rola dentro de uma faixa autorada.</i> É uma invariante mais fraca que a
    /// anterior ("nunca gera atributos"), mas continua sendo um teto real: o conteúdo é
    /// autorado por uma pessoa; o que varia é o valor, entre limites que essa pessoa escreveu.</para>
    ///
    /// <para><b>Por que a anterior caiu:</b> sem geração, uma arma de nível máximo entregava
    /// exatamente os mesmos status de uma arma de nível 1 — não havia curva de poder, e a
    /// segunda cópia de um item nunca interessava, que é o loop de loot mais fraco que um ARPG
    /// pode ter.</para>
    /// </summary>
    [CreateAssetMenu(menuName = "Favela Amarela/Afixo", fileName = "Afixo_")]
    public sealed class AfixoDef : ScriptableObject
    {
        [Header("Identidade")]
        [Tooltip("Identificador estável. Vai para o save junto do valor rolado.")]
        public string Id;

        [Tooltip("Como aparece no nome do item. Prefixo vem antes, sufixo depois.")]
        public TipoDeAfixo Tipo = TipoDeAfixo.Prefixo;

        [Tooltip("O texto que entra no nome. Ex.: \"do Sinal\", \"Marcado\".")]
        public string Rotulo;

        [Header("O que concede")]
        [Tooltip("Qual atributo. ATENÇÃO: cinco StatType não têm consumidor no jogo " +
                 "(RCMaxima, Velocidade, Furtividade, DefesaAnomalia, e RMMaxima como passiva). " +
                 "Um afixo que role qualquer um deles produz um item que MENTE para o jogador.")]
        public StatType Stat;

        [Tooltip("Menor valor que este afixo pode rolar.")]
        public float ValorMin = 1f;

        [Tooltip("Maior valor que este afixo pode rolar. Igual ao mínimo = valor fixo.")]
        public float ValorMax = 1f;

        [Tooltip("Se o valor rolado cresce com o NÍVEL DO ITEM, pela mesma lei que escala o " +
                 "dano branco. Ligue para valores absolutos (dano, vitalidade, defesa); " +
                 "DESLIGUE para taxas e frações (regeneração por segundo, redução de ruído).")]
        public bool EscalaComONivelDoItem = true;

        [Header("Quando pode cair")]
        [Tooltip("Nível DO ITEM a partir do qual este afixo entra no pool. Note: nível do " +
                 "ITEM, não do jogador — comparar com o nível do jogador faz uma zona inicial " +
                 "dropar tier máximo assim que ele sobe, que é o bug clássico do gênero.")]
        [Min(1)]
        public int NivelMinimoDoItem = 1;

        [Tooltip("Peso relativo no sorteio. O gerador renormaliza DEPOIS de filtrar por " +
                 "legalidade — senão o gate de nível enviesa a distribuição em silêncio.")]
        [Min(0.01f)]
        public float Peso = 1f;

        [Tooltip("Slots em que este afixo é legal. Vazio = qualquer slot equipável.")]
        public EquipmentSlot[] SlotsPermitidos = Array.Empty<EquipmentSlot>();

        [Tooltip("Afixos do mesmo grupo nunca caem juntos no mesmo item. É o que impede " +
                 "\"+5 Vitalidade\" e \"+8 Vitalidade\" na mesma peça. Vazio = usa o próprio Id.")]
        public string GrupoDeExclusao;

        /// <summary>O grupo efetivo — o próprio <see cref="Id"/> quando não há grupo autorado.</summary>
        public string Grupo => string.IsNullOrWhiteSpace(GrupoDeExclusao) ? Id : GrupoDeExclusao;

        /// <summary>Se este afixo pode cair num item deste slot e deste nível.</summary>
        public bool EhLegalPara(EquipmentSlot slot, int nivelDoItem)
        {
            if (nivelDoItem < NivelMinimoDoItem) return false;
            if (SlotsPermitidos == null || SlotsPermitidos.Length == 0) return true;

            for (int i = 0; i < SlotsPermitidos.Length; i++)
                if (SlotsPermitidos[i] == slot) return true;

            return false;
        }

        /// <summary>
        /// Rola o valor dentro da faixa autorada. A ordem de <see cref="ValorMin"/> e
        /// <see cref="ValorMax"/> é normalizada aqui: um asset com o mínimo maior que o máximo
        /// é erro de autoria, não motivo para o item sair sem afixo.
        /// </summary>
        public float Rolar(Core.Loot.IFonteDeAleatoriedade fonte) => Rolar(fonte, 1);

        /// <summary>
        /// Rola o valor <b>no nível do item</b>, pela mesma lei que escala o dano branco.
        ///
        /// <para><b>O defeito que isto conserta (2026-09-01).</b> O valor era rolado sempre na
        /// mesma faixa, independente do nível. A <b>base</b> escalava +25% por nível e o
        /// <b>afixo não</b> — então o <c>afixo_cravado</c> (+2 a 5 de dano) valia de 4% a 11%
        /// num Alfanje de nível 1, e de <b>1% a 3%</b> num de nível 12. O afixo saía de
        /// marginal para invisível conforme o jogador subia.</para>
        ///
        /// <para>Isso ataca a razão de existir de um ARPG: o jogador pega a quadragésima espada
        /// porque os <i>afixos</i> dela podem ser melhores. Com afixos planos, todo drop é a
        /// mesma arma com um erro de arredondamento — que foi exatamente o relato do Vini:
        /// <i>"todos os drops fracos"</i>.</para>
        ///
        /// <para><b>Nem todo afixo escala</b>, e por isso é decisão autorada. Multiplicar uma
        /// taxa por segundo (regeneração) pelo fator do nível 12 daria regeneração absurda;
        /// multiplicar uma fração de redução de ruído a estouraria. Ver
        /// <see cref="EscalaComONivelDoItem"/>.</para>
        /// </summary>
        public float Rolar(Core.Loot.IFonteDeAleatoriedade fonte, int nivelDoItem)
        {
            float min = Mathf.Min(ValorMin, ValorMax);
            float max = Mathf.Max(ValorMin, ValorMax);

            float bruto = fonte == null || Mathf.Approximately(min, max)
                ? min
                : min + (max - min) * Mathf.Clamp01(fonte.ProximoValor());

            if (!EscalaComONivelDoItem) return bruto;

            // A MESMA lei do dano branco: um afixo que crescesse por outra regra divergiria da
            // base em silêncio, e a arma de nível alto voltaria a ter afixo decorativo.
            return bruto * Core.Progression.EscalaDeNivel.FatorDeDano(nivelDoItem);
        }

#if UNITY_EDITOR
        private void OnValidate()
        {
            if (string.IsNullOrEmpty(Id))
            {
                Id = name;
                UnityEditor.EditorUtility.SetDirty(this);
            }
        }
#endif
    }
}
