using UnityEngine;
using FavelaAmarela.Core.Abilities;
using FavelaAmarela.Core.Combat;

namespace FavelaAmarela.Runtime.Enemies
{
    public class EnemyCombat : MonoBehaviour
    {
        [Header("Ataque")]
        [SerializeField] private float alcanceDeGolpe = 1.2f;
        [SerializeField] private float cadenciaDeAtaque = 1.2f;
        [SerializeField] private float danoDoGolpe = 20f;
        [Tooltip("Quem este inimigo aceita como alvo. Vazio = Jogador + Aliados.")]
        [SerializeField] private LayerMask camadasAlvo;

        /// <summary>
        /// Alvos legítimos de um inimigo: o jogador e quem luta do lado dele.
        ///
        /// <para>A layer <b>Aliados</b> nasceu em 2026-08-11: até então Yug-Neth estava na
        /// layer <c>Enemy</c> e os inimigos só procuravam <c>Player</c> — ou seja, o
        /// companheiro era <b>invisível para eles</b> e toda a mecânica de incapacitação
        /// (cair, bloquear os Portões, ser reanimado num Refúgio) nunca disparava.</para>
        ///
        /// <para>É layer própria, e não "põe o aliado em Player", porque o jogo vai ganhar
        /// outros aliados e NPCs ao longo da campanha — e porque coisas que miram só em
        /// Damião (câmera, gatilhos de quest) não podem passar a mirar neles junto.</para>
        /// </summary>
        private static readonly string[] LayersDeAlvo = { "Player", "Aliados" };

        private float _attackCooldown;
        private readonly Collider2D[] _bufferAlvo = new Collider2D[4];
        private ContactFilter2D _filtroAlvo;
        private IDanificavel _alvoCache;

        public event System.Action OnAtaqueDesferido;
        public bool EstaPronto => _attackCooldown <= 0f;

        private void Awake()
        {
            int padrao = LayerMask.GetMask(LayersDeAlvo);

            // Prefabs salvos antes da layer Aliados existir guardam uma máscara que só tem
            // Player. Reaproveitá-la deixaria o companheiro invisível de novo — e em silêncio,
            // que é o pior modo de um alvo não ser alvo. Só respeitamos a máscara autorada se
            // ela já enxergar algum aliado.
            bool autoradaEnxergaAliados = (camadasAlvo.value & LayerMask.GetMask("Aliados")) != 0;
            if (camadasAlvo.value == 0 || !autoradaEnxergaAliados) camadasAlvo = padrao;

            _filtroAlvo = new ContactFilter2D { useTriggers = true };
            _filtroAlvo.SetLayerMask(camadasAlvo);
        }

        private void Update()
        {
            if (_attackCooldown > 0f) _attackCooldown -= Time.deltaTime;
        }

        public bool AlvoEstaAoAlcance()
        {
            if (_alvoCache != null)
            {
                if (_alvoCache is MonoBehaviour mb && mb == null) _alvoCache = null;
                else if (_alvoCache is IDanificavel && !((MonoBehaviour)_alvoCache).enabled) _alvoCache = null;
            }

            int total = Physics2D.OverlapCircle(transform.position, alcanceDeGolpe, _filtroAlvo, _bufferAlvo);
            if (total <= 0) { _alvoCache = null; return false; }

            if (_alvoCache == null)
            {
                for (int i = 0; i < total; i++)
                {
                    _alvoCache = _bufferAlvo[i].GetComponentInParent<IDanificavel>();
                    if (_alvoCache != null) break;
                }
            }
            return _alvoCache != null;
        }

        public bool TentarAtacar()
        {
            if (!EstaPronto || !AlvoEstaAoAlcance()) return false;
            _attackCooldown = cadenciaDeAtaque;
            _alvoCache?.ReceberGolpe(new ArmaResult(true, 0f, 0f, false, 0f, danoDoGolpe));
            OnAtaqueDesferido?.Invoke();
            return true;
        }
    }
}
