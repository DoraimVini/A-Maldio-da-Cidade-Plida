using UnityEngine;
using FavelaAmarela.Inventario;
using System;

namespace FavelaAmarela.Player
{
    public class GerenciadorDeVigor : MonoBehaviour
    {
        [Header("Configuração Base")]
        [SerializeField] private float vigorMaximoBase = 100f;
        [SerializeField] private float custoCorridaPorSegundo = 12f;
        [SerializeField] private float custoEsquiva = 25f;
        [SerializeField] private float regeneracaoPorSegundo = 25f;
        [SerializeField] private float regeneracaoExaustoPorSegundo = 15f;
        [SerializeField] private float limiarExaustao = 30f;

        // Estado
        private float _vigorAtual;
        private bool _estaExausto;
        
        // Referências
        private GerenciadorEfeitosPassivos _efeitosPassivos;

        // Eventos
        public event Action<float, float> OnVigorChanged; // (atual, max)
        public event Action<bool> OnExaustaoChanged; // (ficou exausto)

        // Propriedades Públicas
        public float VigorAtual => _vigorAtual;
        public float VigorMaximo => vigorMaximoBase + (_efeitosPassivos != null ? _efeitosPassivos.GetBonus(StatType.VigorMaximo) : 0f);
        public bool EstaExausto => _estaExausto;
        public bool PodeExecutarAcao(float custo) => _vigorAtual >= custo;

        void Start()
        {
            _efeitosPassivos = GetComponent<GerenciadorEfeitosPassivos>();
            _vigorAtual = VigorMaximo; // Inicia cheio
        }

        void Update()
        {
            if (!_estaExausto || _vigorAtual >= limiarExaustao)
                _estaExausto = false;

            // A regeneração é aplicada em Update, assumindo que o consumo é feito via métodos externos.
            // Isso evita que o Update precise saber o estado do input.
            float taxa = _estaExausto ? regeneracaoExaustoPorSegundo : regeneracaoPorSegundo;
            // Bônus de regeneração de equipamentos/ecos
            float bonusRegen = _efeitosPassivos != null ? _efeitosPassivos.GetBonus(StatType.RegeneracaoVigor) : 0f;
            taxa += bonusRegen;

            if (!_estaExausto)
            {
                _vigorAtual = Mathf.Clamp(_vigorAtual + taxa * Time.deltaTime, 0f, VigorMaximo);
                OnVigorChanged?.Invoke(_vigorAtual, VigorMaximo);
            }
            else
            {
                // Regeneração mais lenta na exaustão
                _vigorAtual += taxa * Time.deltaTime;
                if (_vigorAtual >= limiarExaustao)
                {
                    _estaExausto = false;
                    _vigorAtual = limiarExaustao; // Sai da exaustão exatamente no limiar
                }
                else if (_vigorAtual > 0)
                {
                    _vigorAtual = 0; // Permanece zerado se não atingiu o limiar
                }
                OnVigorChanged?.Invoke(_vigorAtual, VigorMaximo);
            }
        }

        /// <summary>
        /// Chamado pelo PlayerMovement a cada frame enquanto corre.
        /// </summary>
        public void ConsumirCorrida(float deltaTime)
        {
            if (_estaExausto) return;
            
            float bonusCusto = _efeitosPassivos != null ? _efeitosPassivos.GetBonus(StatType.CustoCorridaVigor) : 0f;
            float custoReal = Mathf.Max(0, custoCorridaPorSegundo - bonusCusto) * deltaTime;
            
            _vigorAtual = Mathf.Clamp(_vigorAtual - custoReal, 0f, VigorMaximo);
            if (_vigorAtual <= 0f)
            {
                _vigorAtual = 0f;
                _estaExausto = true;
                OnExaustaoChanged?.Invoke(true);
            }
            OnVigorChanged?.Invoke(_vigorAtual, VigorMaximo);
        }

        /// <summary>
        /// Chamado pelo PlayerMovement ao tentar executar dash.
        /// Retorna true se o dash foi autorizado.
        /// </summary>
        public bool TentarConsumirEsquiva()
        {
            if (_estaExausto) return false;
            
            float bonusCusto = _efeitosPassivos != null ? _efeitosPassivos.GetBonus(StatType.CustoEsquivaVigor) : 0f;
            float custoReal = Mathf.Max(0, custoEsquiva - bonusCusto);
            
            if (_vigorAtual >= custoReal)
            {
                _vigorAtual -= custoReal;
                if (_vigorAtual <= 0f)
                {
                    _vigorAtual = 0f;
                    _estaExausto = true;
                    OnExaustaoChanged?.Invoke(true);
                }
                OnVigorChanged?.Invoke(_vigorAtual, VigorMaximo);
                return true;
            }
            return false;
        }

        /// <summary>
        /// Restaura completamente o Vigor (ex.: ao usar um item).
        /// </summary>
        public void RestaurarCompletamente()
        {
            _vigorAtual = VigorMaximo;
            _estaExausto = false;
            OnVigorChanged?.Invoke(_vigorAtual, VigorMaximo);
        }
    }
}
