using UnityEngine;
using System.Collections.Generic;
using System;
using System.Linq;

namespace FavelaAmarela.Progression
{
    /// <summary>
    /// Gerencia o Nível de Exposição de Damião e os Nós desbloqueados no Labirinto de Carcosa.
    /// Em um jogo de 4h, os limites de nível são matematicamente contidos (Level Cap ~ 12).
    /// Esta classe se integra ao GerenciadorEfeitosPassivos para aplicar as passivas.
    /// </summary>
    public class ProgressionManager : MonoBehaviour
    {
        public static ProgressionManager Instance { get; private set; }

        [Header("Exposição e Limiares")]
        [SerializeField] private int nivelAtual = 1;
        [SerializeField] private int exposicaoAtual = 0;
        [SerializeField] private int pontosDeEcoDisponiveis = 0;
        
        [Tooltip("Exposição necessária para cada nível. Em um escopo de 4h, não há scaling infinito.")]
        [SerializeField] private int[] curvaDeExperiencia = new int[] 
        {
            0,      // Lvl 1
            100,    // Lvl 2
            300,    // Lvl 3
            600,    // Lvl 4
            1000,   // Lvl 5
            1500,   // Lvl 6
            2100,   // Lvl 7
            2800,   // Lvl 8
            3600,   // Lvl 9
            4500,   // Lvl 10
            5500,   // Lvl 11
            6600    // Lvl 12 (Cap)
        };

        [Header("Árvore Ativa (Labirinto de Carcosa)")]
        [SerializeField] private List<EcoDef> ecosDesbloqueados = new List<EcoDef>();

        public event Action OnExposicaoGanha;
        public event Action<int> OnLevelUp;
        public event Action<EcoDef> OnEcoDesbloqueado;

        public int NivelAtual => nivelAtual;
        public int ExposicaoAtual => exposicaoAtual;
        public int PontosDeEcoDisponiveis => pontosDeEcoDisponiveis;
        public IReadOnlyList<EcoDef> EcosDesbloqueados => ecosDesbloqueados.AsReadOnly();

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
        }

        public void AdicionarExposicao(int valor)
        {
            if (nivelAtual >= curvaDeExperiencia.Length) return; // Max level

            exposicaoAtual += valor;
            OnExposicaoGanha?.Invoke();
            VerificarLimiar();
        }

        private void VerificarLimiar()
        {
            while (nivelAtual < curvaDeExperiencia.Length && exposicaoAtual >= curvaDeExperiencia[nivelAtual])
            {
                nivelAtual++;
                pontosDeEcoDisponiveis++;
                OnLevelUp?.Invoke(nivelAtual);
            }
        }

        /// <summary>
        /// Tenta destrancar um Eco na árvore. O jogador só fará isso em Santuários de Carcosa.
        /// </summary>
        public bool TryDesbloquearEco(EcoDef eco)
        {
            if (pontosDeEcoDisponiveis <= 0)
            {
                Debug.LogWarning("[ProgressionManager] Sem pontos de Eco disponíveis.");
                return false;
            }

            if (ecosDesbloqueados.Contains(eco))
            {
                Debug.LogWarning("[ProgressionManager] Eco já desbloqueado.");
                return false;
            }

            // Verifica Pré-Requisitos
            if (eco.PreRequisitos != null && eco.PreRequisitos.Count > 0)
            {
                bool temPreRequisito = eco.PreRequisitos.Any(pr => ecosDesbloqueados.Contains(pr));
                if (!temPreRequisito)
                {
                    Debug.LogWarning("[ProgressionManager] Pré-requisitos não atendidos para este Eco.");
                    return false;
                }
            }

            // Sucesso
            pontosDeEcoDisponiveis--;
            ecosDesbloqueados.Add(eco);
            OnEcoDesbloqueado?.Invoke(eco);
            
            Debug.Log($"[ProgressionManager] Eco desbloqueado: {eco.NomeDoEco} ({eco.Caminho})");
            return true;
        }

        public ProgressionSaveData GetSaveData()
        {
            return new ProgressionSaveData(nivelAtual, exposicaoAtual, pontosDeEcoDisponiveis, ecosDesbloqueados);
        }

        public void RestoreFromSaveData(ProgressionSaveData data, Dictionary<string, EcoDef> ecodictionary)
        {
            if (data == null) return;
            
            nivelAtual = data.nivelAtual;
            exposicaoAtual = data.exposicaoAtual;
            pontosDeEcoDisponiveis = data.pontosDeEcoDisponiveis;
            
            ecosDesbloqueados.Clear();
            if (data.ecosDesbloqueadosIds != null && ecodictionary != null)
            {
                foreach (var id in data.ecosDesbloqueadosIds)
                {
                    if (ecodictionary.TryGetValue(id, out var def))
                    {
                        ecosDesbloqueados.Add(def);
                    }
                }
            }
        }
    }
}
