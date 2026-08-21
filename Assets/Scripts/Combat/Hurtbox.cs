using UnityEngine;
using FavelaAmarela.Core.Abilities;
using FavelaAmarela.Core.Combat;

namespace FavelaAmarela.Runtime.Combat
{
    /// <summary>
    /// A área que <b>recebe</b> dano — separada do colisor que barra movimento.
    ///
    /// <para><b>Por que existe (2026-08-21):</b> até aqui cada personagem tinha <b>um</b>
    /// colisor fazendo três trabalhos ao mesmo tempo: barrar movimento, receber dano e ser
    /// detectado. Um número só não consegue ser bom para os três. O colisor do Damião era
    /// <b>1,467</b> de pegada — largo demais para andar (ele entalava em quina) e largo demais
    /// para apanhar (levava dano de longe). Encolher para andar melhor significava ficar difícil
    /// de acertar; alargar para acertar significava entalar. Separar desfaz o impasse.</para>
    ///
    /// <para><b>Fica num filho, não na raiz.</b> A raiz carrega o <c>Rigidbody2D</c> e o colisor
    /// sólido de movimento (a pegada no chão, achatada 2:1 — ver <c>RevisarColisores</c>). A
    /// hurtbox é um <c>GameObject</c> filho, na camada <c>PlayerHurtbox</c> (13) ou
    /// <c>EnemyHurtbox</c> (14), com colisor <b>trigger</b> cobrindo o <b>corpo desenhado</b> —
    /// que é o que o jogador enxerga e espera que seja atingível.</para>
    ///
    /// <para><b>As quatro camadas já existiam e não eram usadas por nada.</b>
    /// <c>PlayerHitbox</c> (11), <c>EnemyHitbox</c> (12), <c>PlayerHurtbox</c> (13),
    /// <c>EnemyHurtbox</c> (14) estavam declaradas em <c>TagManager.asset</c> desde sempre, com
    /// zero prefabs, zero cenas e zero código as referenciando. Alguém planejou esta separação e
    /// não a construiu.</para>
    /// </summary>
    [RequireComponent(typeof(Collider2D))]
    [AddComponentMenu("Favela Amarela/Combate/Hurtbox (área que recebe dano)")]
    public sealed class Hurtbox : MonoBehaviour
    {
        [Tooltip("Quem leva o dano. Vazio: procura um IDanificavel no pai ao acordar.")]
        [SerializeField] private MonoBehaviour donoExplicito;

        private IDanificavel _dono;

        /// <summary>Quem recebe o golpe que entrar nesta área. Pode ser nulo se mal configurada.</summary>
        public IDanificavel Dono => _dono;

        private void Awake()
        {
            // O colisor precisa ser trigger: a hurtbox não empurra nada, só é consultada.
            var col = GetComponent<Collider2D>();
            if (col != null && !col.isTrigger)
            {
                Debug.LogWarning($"[Hurtbox] '{name}' não estava como trigger — corrigido em " +
                                 "runtime. Uma hurtbox sólida empurraria o dono pelo cenário.",
                                 this);
                col.isTrigger = true;
            }

            _dono = donoExplicito as IDanificavel;

            // Sem dono explícito, sobe a hierarquia: a hurtbox é filha de quem ela protege.
            if (_dono == null) _dono = GetComponentInParent<IDanificavel>();

            if (_dono == null)
                Debug.LogError($"[Hurtbox] '{name}' não achou um IDanificavel — golpes que " +
                               "acertarem esta área não vão ferir ninguém.", this);
        }

        /// <summary>
        /// Entrega o golpe a quem esta área protege. Chamado pela <see cref="Hitbox"/> que a
        /// atingiu, não por trigger — o disparo é da hitbox, que sabe a janela ativa.
        /// </summary>
        public void Receber(ArmaResult resultado) => _dono?.ReceberGolpe(resultado);
    }
}
