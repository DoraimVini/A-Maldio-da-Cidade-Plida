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

        // ── Construção automática ─────────────────────────────────────────────

        /// <summary>Fração da largura do sprite coberta pela hurtbox (tira a margem vazia).</summary>
        private const float FatorLargura = 0.72f;

        /// <summary>Fração da altura do sprite coberta pela hurtbox.</summary>
        private const float FatorAltura = 0.86f;

        /// <summary>
        /// Garante que <paramref name="dono"/> tenha uma hurtbox, criando-a a partir do
        /// <c>SpriteRenderer</c> se ainda não existir.
        ///
        /// <para><b>Por que isto existe (2026-08-21):</b> a primeira versão montava a hurtbox
        /// prefab por prefab, a partir de uma lista escrita à mão no Editor. O Vini apontou o
        /// problema: <i>"não deveria funcionar por camada? Tudo na camada Enemy é atingível pelo
        /// jogador"</i>. Ele estava certo — e uma lista de prefabs é exatamente o modo de falha
        /// que mais se repete neste projeto: <b>seis</b> listas escritas à mão já envelheceram
        /// aqui. Uma sétima era questão de tempo.</para>
        ///
        /// <para>Agora a garantia vive no <b>código</b>, chamada de <c>Awake</c> por quem
        /// implementa <c>IDanificavel</c>. Um inimigo novo que herde <c>EnemyBase</c> ganha
        /// hurtbox <b>sem wiring nenhum</b>, e não há lista para esquecer de atualizar.</para>
        ///
        /// <para><b>Idempotente:</b> se já houver um filho "Hurtbox" — vindo do prefab, por
        /// exemplo — ele é reaproveitado e nada é recriado.</para>
        /// </summary>
        /// <param name="dono">O objeto danificável que ganha a área.</param>
        /// <param name="nomeDaCamada">
        /// <c>PlayerHurtbox</c> ou <c>EnemyHurtbox</c>. Resolvido por nome porque a ordem das
        /// camadas no <c>TagManager</c> não é contrato.
        /// </param>
        public static Hurtbox GarantirPara(GameObject dono, string nomeDaCamada)
        {
            if (dono == null) return null;

            var existente = dono.GetComponentInChildren<Hurtbox>(true);
            if (existente != null) return existente;

            var sr = dono.GetComponentInChildren<SpriteRenderer>(true);
            if (sr == null || sr.sprite == null)
            {
                Debug.LogWarning($"[Hurtbox] '{dono.name}' não tem sprite — sem corpo desenhado " +
                                 "não dá para derivar a área atingível. Fica sem hurtbox; o " +
                                 "golpe ainda o encontra pelo colisor de movimento.", dono);
                return null;
            }

            int camada = LayerMask.NameToLayer(nomeDaCamada);
            if (camada < 0)
            {
                Debug.LogError($"[Hurtbox] Camada '{nomeDaCamada}' não existe no TagManager.", dono);
                return null;
            }

            var go = new GameObject("Hurtbox");
            go.transform.SetParent(dono.transform, false);
            go.transform.localPosition = Vector3.zero;
            go.layer = camada;

            // sprite.bounds já vem em unidades locais (dividido pela PPU) e já considera o
            // pivô — serve tanto para pivô no rodapé quanto no centro, sem caso especial.
            var b = sr.sprite.bounds;

            var caixa = go.AddComponent<BoxCollider2D>();
            caixa.isTrigger = true;
            caixa.size = new Vector2(b.size.x * FatorLargura, b.size.y * FatorAltura);
            caixa.offset = new Vector2(b.center.x, b.center.y);

            return go.AddComponent<Hurtbox>();
        }
    }
}
