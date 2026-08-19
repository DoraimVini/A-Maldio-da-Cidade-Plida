using UnityEngine;
using FavelaAmarela.Runtime.Combat;

namespace FavelaAmarela.Runtime.GameLoop
{
    /// <summary>
    /// Volume que causa Colapso imediato ao toque (ex.: cair num abismo).
    ///
    /// <para><b>2026-08-18:</b> passou a resolver a mente e a invulnerabilidade <b>a partir de
    /// quem entrou</b>, em vez de alcançar <c>GameManager.Instance</c>. Damião carrega as duas
    /// coisas como componentes, então quem o toca já tem tudo em mãos.</para>
    /// </summary>
    [RequireComponent(typeof(Collider2D))]
    public class ColapsoTrigger : MonoBehaviour
    {
        private void OnTriggerEnter2D(Collider2D collision)
        {
            if (!collision.CompareTag("Player")) return;

            var mente = collision.GetComponentInParent<ResilienciaBridge>();
            if (mente == null) return;

            // A invulnerabilidade de cutscene é checada DENTRO da bridge — não aqui. Antes, cada
            // fonte de morte instantânea replicava esse if, e uma fonte nova nascia sem ele.
            mente.ForcarColapso();
        }
    }
}
