using UnityEngine;
using FavelaAmarela.Core.Abilities;
using FavelaAmarela.Core.Combat;
using FavelaAmarela.Runtime.Combat;

namespace FavelaAmarela.Runtime.Enemies
{
    /// <summary>
    /// Processa efeitos de ArmaResult: sangramento e atordoamento.
    /// Notifica a FSM quando um atordoamento é aplicado.
    /// </summary>
    [RequireComponent(typeof(EnemyBase))]
    public class EnemyStatusEffects : MonoBehaviour
    {
        [Header("Sangramento")]
        [SerializeField] private bool mostrarNumerosDeSangramento = true;
        [SerializeField] private Color corDoSangramento = new Color(0.85f, 0.15f, 0.2f);

        private EnemyBase _enemyBase;
        private EnemyStateMachine _fsm;
        private Sangramento _sangramento = new Sangramento();
        private float _danoPendente;

        private void Awake()
        {
            _enemyBase = GetComponent<EnemyBase>();
            _fsm = GetComponent<EnemyStateMachine>();
            _enemyBase.OnGolpeRecebido += ProcessarGolpe;
        }

        private void Update()
        {
            EscoarSangramento(Time.deltaTime);
        }

        private void ProcessarGolpe(ArmaResult resultado)
        {
            if (resultado.Atordoou && _fsm != null)
            {
                _fsm.Atordoar(resultado.DuracaoAtordoamento);
            }

            if (resultado.AcumulosDeSangramento > 0)
            {
                _sangramento.Aplicar(resultado.AcumulosDeSangramento,
                    resultado.SangramentoPorSegundo, resultado.DuracaoSangramento);
            }
        }

        private void EscoarSangramento(float dt)
        {
            if (!_sangramento.Ativo || _enemyBase.EstaAbatido) return;
            var tick = _sangramento.Tick(dt);

            if (tick.DanoContinuo > 0f)
            {
                _enemyBase.Vitalidade.Ferir(tick.DanoContinuo);
                _danoPendente += tick.DanoContinuo;
                if (_danoPendente >= 1f && mostrarNumerosDeSangramento)
                {
                    DanoFlutuante.Mostrar(transform.position, _danoPendente, corDoSangramento);
                    _danoPendente = 0f;
                }
            }

            if (tick.Explodiu)
            {
                float estouro = ExplosaoDeSangramento.Calcular(
                    _enemyBase.Atributos.VitalidadeMax, _enemyBase.EhAparicaoPrimordial);
                if (estouro > 0f)
                {
                    _enemyBase.Vitalidade.Ferir(estouro);
                    if (mostrarNumerosDeSangramento)
                        DanoFlutuante.Mostrar(transform.position, estouro, corDoSangramento);
                }
            }
        }

        private void OnDestroy()
        {
            if (_enemyBase != null)
                _enemyBase.OnGolpeRecebido -= ProcessarGolpe;
        }
    }
}
