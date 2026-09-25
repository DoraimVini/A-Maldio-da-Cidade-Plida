// Assets/Scripts/Inventario/ItemDatabase.cs
using System.Collections.Generic;
using UnityEngine;

namespace FavelaAmarela.Inventario
{
    /// <summary>
    /// Banco de dados centralizado de todos os ItemDef do jogo.
    /// Responsável por carregar e cachear definições a partir de GUIDs.
    /// É um Singleton que persiste entre as cenas.
    /// </summary>
    public class ItemDatabase : MonoBehaviour
    {
        public static ItemDatabase Instance { get; private set; }

        [Header("Catálogo de Itens")]
        [SerializeField] private ItemDef[] todosOsItens;

        private Dictionary<string, ItemDef> _lookup;
        private Dictionary<string, ItemDef> lookup
        {
            get
            {
                if (_lookup == null) _lookup = new Dictionary<string, ItemDef>();
                return _lookup;
            }
        }
        // Sem isto o método nunca era chamado por ninguém: ItemDatabase.Instance ficava
        // null fora dos testes (que chamam InitializeForTesting() manualmente), e todo
        // ItemInstance.Def resolvia null — armas, Patuá e futuros consumíveis, todos.
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void GarantirInstancia()
        {
            if (Instance != null) return;
            var go = new GameObject("[Singleton] ItemDatabase");
            go.AddComponent<ItemDatabase>();
            DontDestroyOnLoad(go);
        }

        void Awake()
        {
            _lookup = new Dictionary<string, ItemDef>();
            
            if (Instance == null)
            {
                Instance = this;
                if (Application.isPlaying)
                {
                    DontDestroyOnLoad(gameObject);
                }
            }
            else
            {
                if (Application.isPlaying)
                {
                    Destroy(gameObject);
                }
                else
                {
                    DestroyImmediate(gameObject);
                }
                return;
            }
            
            if (todosOsItens == null || todosOsItens.Length == 0)
            {
                todosOsItens = Resources.LoadAll<ItemDef>("");
            }

            ConstruirCache();
        }

        public static void ClearInstanceForTesting()
        {
            Instance = null;
        }

        public void InitializeForTesting()
        {
            Instance = this;
            _lookup = new Dictionary<string, ItemDef>();
        }

        private void ConstruirCache()
        {
            if (todosOsItens == null) return;
            foreach (var item in todosOsItens)
            {
                if (item == null) continue;

                if (string.IsNullOrEmpty(item.Id))
                {
                    Debug.LogWarning($"ItemDef '{item.name}' não tem ID. Ignorado no banco de dados.");
                    continue;
                }

                if (lookup.ContainsKey(item.Id))
                {
                    Debug.LogError($"ID duplicado encontrado: {item.Id} ({item.name} e {lookup[item.Id].name}). Cada ItemDef deve ter um ID único.");
                    continue;
                }

                lookup[item.Id] = item;
            }
            Debug.Log($"[ItemDatabase] {lookup.Count} itens carregados no cache.");
        }

        /// <summary>
        /// Obtém um ItemDef a partir do GUID.
        /// Retorna null se não encontrado.
        /// </summary>
        public ItemDef Get(string id)
        {
            if (string.IsNullOrEmpty(id)) return null;
            lookup.TryGetValue(id, out var def);
            return def;
        }

        /// <summary>
        /// Registra dinamicamente um ItemDef (útil para testes ou conteúdo gerado).
        /// </summary>
        public void Registrar(ItemDef item)
        {
            if (item == null || string.IsNullOrEmpty(item.Id)) return;
            lookup[item.Id] = item;
        }
    }
}
