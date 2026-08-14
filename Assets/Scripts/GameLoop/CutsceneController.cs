using UnityEngine;
using FavelaAmarela.Runtime.Combat;

namespace FavelaAmarela.Runtime.GameLoop
{
    /// <summary>
    /// Camada Runtime. Segura a <b>invulnerabilidade de cutscene</b>: enquanto Damião está preso
    /// numa sequência roteirizada (a queda Z4→Z5, por exemplo) ele não pode agir — e também não
    /// pode morrer, senão a cena vira derrota por acidente.
    ///
    /// <para>Protege os <b>dois</b> canais, e essa dupla é o ponto: as fontes de morte
    /// instantânea por toque (Coisa do Cemitério, <c>ColapsoTrigger</c>, o Rei em Amarelo)
    /// consultam <see cref="JogadorInvulneravel"/> e desistem; e o dano físico comum é barrado
    /// propagando para <c>VitalidadeBridge.IgnorarDano</c> — senão Damião morreria de porrada de
    /// Cultista no meio de uma cutscene.</para>
    ///
    /// <para>Extraído do <c>GameManager</c> em 2026-08-13.</para>
    /// </summary>
    [AddComponentMenu("Favela Amarela/GameLoop/Cutscene Controller")]
    public sealed class CutsceneController : MonoBehaviour
    {
        private VitalidadeBridge _vitalidade;

        /// <summary>
        /// Verdadeiro enquanto uma sequência roteirizada estiver em curso. Fontes de morte
        /// instantânea devem respeitar isto e não aplicar o Colapso.
        /// </summary>
        public bool JogadorInvulneravel { get; private set; }

        /// <summary>
        /// Liga a <c>VitalidadeBridge</c> de Damião, para a invulnerabilidade alcançar também o
        /// dano físico. Pode vir nula (cena sem Damião corpóreo): nesse caso só a proteção contra
        /// morte instantânea funciona.
        /// </summary>
        public void Bind(VitalidadeBridge vitalidade)
        {
            _vitalidade = vitalidade;

            // Reaplica o estado corrente: se um bind acontecer no meio de uma cutscene (troca de
            // cena roteirizada), a bridge nova precisa nascer já ignorando dano.
            if (_vitalidade != null) _vitalidade.IgnorarDano = JogadorInvulneravel;
        }

        /// <summary>Liga/desliga a invulnerabilidade e propaga para a Vitalidade.</summary>
        public void DefinirInvulneravel(bool valor)
        {
            JogadorInvulneravel = valor;
            if (_vitalidade != null) _vitalidade.IgnorarDano = valor;
        }
    }
}
