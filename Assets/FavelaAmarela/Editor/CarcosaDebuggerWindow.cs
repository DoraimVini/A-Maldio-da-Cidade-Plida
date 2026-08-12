using UnityEditor;
using UnityEngine;
using FavelaAmarela.Core.Combat;
using FavelaAmarela.Inventario;
using FavelaAmarela.Player;
using FavelaAmarela.Runtime.Enemies;

namespace FavelaAmarela.EditorTools
{
    /// <summary>
    /// Ferramenta de Editor, só funciona em Play Mode (precisa dos singletons vivos).
    /// Concede artefatos e armas sob demanda e invoca os dois chefes do Vertical Slice
    /// (Byakhee, Rei em Amarelo) numa cena qualquer — pensada para a
    /// <c>Cena_ArenaDeTestes</c>, mas funciona em qualquer cena com um Player e um
    /// <c>GameManager</c>.
    ///
    /// <para><b>Por que existe:</b> o ritual de selamento do Rei exige a Coroa de Ossos, e
    /// ela não tem fonte jogável ainda (Templo da Serpente sem cena) — sem este atalho não
    /// haveria como testar a luta final de ponta a ponta. Nasceu também da calibração do
    /// Byakhee, feita inteira por simulação Python fora do jogo por falta de visibilidade ao
    /// vivo do estado da FSM; a seção "Estado ao vivo" abaixo é a resposta a isso.</para>
    ///
    /// <para><b>Nunca entra em build de jogador</b> — vive em <c>Assets/FavelaAmarela/Editor/</c>,
    /// que já é editor-only por convenção do projeto (ver `RodarTodoOWiring.cs` e vizinhos).</para>
    /// </summary>
    public sealed class CarcosaDebuggerWindow : EditorWindow
    {
        private const string FichaByakheePath = "Assets/FavelaAmarela/Config/Ficha_Byakhee.asset";

        private static readonly (string Id, string Nome)[] Artefatos =
        {
            ("necronomicon", "Necronomicon"),
            ("patua_luas_gemeas", "Patuá das Luas Gêmeas"),
            ("anel_sinal_amarelo", "Anel do Sinal Amarelo"),
            ("coroa_de_ossos", "Coroa de Ossos"),
        };

        private static readonly (string Id, string Nome)[] Armas =
        {
            ("4a3de7951b884046a800dc2b14b4acca", "Cravo de Aklo"),
            ("c360f6eeb0b14b5ba40c9ddd16161367", "Estilete de Irem"),
            ("a56ffb4e4c154feea0cf7b0cd72c8537", "Alfanje de Alhazred"),
        };

        private Vector2 _scroll;

        [MenuItem("Tools/FavelaAmarela/Carcosa Debugger")]
        private static void AbrirJanela()
        {
            GetWindow<CarcosaDebuggerWindow>("Carcosa Debugger");
        }

        private void OnEnable()
        {
            EditorApplication.update += Repaint;
        }

        private void OnDisable()
        {
            EditorApplication.update -= Repaint;
        }

        private void OnGUI()
        {
            if (!EditorApplication.isPlaying)
            {
                EditorGUILayout.HelpBox(
                    "Entre em Play Mode para usar o debugger — ele depende dos singletons " +
                    "de cena (GameManager, InventoryManager) e do Player.",
                    MessageType.Info);
                return;
            }

            _scroll = EditorGUILayout.BeginScrollView(_scroll);

            DesenharSecaoArtefatos();
            GUILayout.Space(8);
            DesenharSecaoArmas();
            GUILayout.Space(8);
            DesenharSecaoChefes();
            GUILayout.Space(8);
            DesenharEstadoAoVivo();

            EditorGUILayout.EndScrollView();
        }

        // ── Artefatos ─────────────────────────────────────────────────────────

        private void DesenharSecaoArtefatos()
        {
            EditorGUILayout.LabelField("Artefatos", EditorStyles.boldLabel);

            var artefatos = ObterArtefatosBridge();
            using (new EditorGUI.DisabledScope(artefatos == null))
            {
                foreach (var (id, nome) in Artefatos)
                {
                    if (GUILayout.Button($"Conceder: {nome}"))
                        ConcederArtefato(artefatos, id, nome);
                }
            }

            if (artefatos == null)
                EditorGUILayout.HelpBox("Nenhum Player com ArtefatosBridge encontrado na cena.", MessageType.Warning);
        }

        private static ArtefatosBridge ObterArtefatosBridge()
        {
            var jogador = GameObject.FindGameObjectWithTag("Player");
            return jogador != null ? jogador.GetComponent<ArtefatosBridge>() : null;
        }

        private static void ConcederArtefato(ArtefatosBridge artefatos, string id, string nome)
        {
            if (artefatos.Inventario.Contem(id))
            {
                Debug.Log($"[CarcosaDebugger] '{nome}' já está equipado.");
                return;
            }

            int slot = artefatos.EquiparNoPrimeiroSlotLivre(id);
            Debug.Log(slot >= 0
                ? $"[CarcosaDebugger] '{nome}' concedido no slot {slot}."
                : $"[CarcosaDebugger] Falha ao conceder '{nome}' — sem slot livre ou id inválido.");
        }

        // ── Armas ────────────────────────────────────────────────────────────

        private void DesenharSecaoArmas()
        {
            EditorGUILayout.LabelField("Armas da Tumba", EditorStyles.boldLabel);

            var inv = InventoryManager.Instance;
            using (new EditorGUI.DisabledScope(inv == null))
            {
                foreach (var (id, nome) in Armas)
                {
                    if (GUILayout.Button($"Conceder e equipar: {nome}"))
                        ConcederArma(inv, id, nome);
                }
            }

            if (inv == null)
                EditorGUILayout.HelpBox("InventoryManager.Instance está nulo.", MessageType.Warning);
        }

        /// <summary>
        /// Mesma sequência do <c>BauDaTumba</c>: guarda na mochila, acha o slot onde caiu e
        /// equipa nele. Não existe atalho "equipar por id" no <c>InventoryManager</c>.
        /// </summary>
        private static void ConcederArma(InventoryManager inv, string id, string nome)
        {
            bool coube = inv.Main.Add(new ItemInstance(id, 1));
            if (!coube)
            {
                Debug.LogWarning($"[CarcosaDebugger] Mochila cheia — '{nome}' não coube.");
                return;
            }

            for (int i = 0; i < inv.Main.Capacidade; i++)
            {
                var slot = inv.Main.GetSlot(i);
                if (slot != null && slot.Def != null && slot.Def.Id == id)
                {
                    inv.Equipar(i);
                    Debug.Log($"[CarcosaDebugger] '{nome}' concedido e equipado.");
                    return;
                }
            }

            Debug.LogWarning($"[CarcosaDebugger] '{nome}' foi para a mochila mas não achei o slot para equipar.");
        }

        // ── Invocar chefes ───────────────────────────────────────────────────

        private void DesenharSecaoChefes()
        {
            EditorGUILayout.LabelField("Invocar chefe", EditorStyles.boldLabel);

            var jogador = GameObject.FindGameObjectWithTag("Player");
            using (new EditorGUI.DisabledScope(jogador == null))
            {
                if (GUILayout.Button("Invocar Byakhee"))
                    InvocarByakhee(jogador);

                if (GUILayout.Button("Invocar Rei em Amarelo"))
                    InvocarReiEmAmarelo(jogador);

                var rei = FindAnyObjectByType<ReiEmAmareloAI>();
                using (new EditorGUI.DisabledScope(rei == null))
                {
                    if (GUILayout.Button("Rei: iniciar ritual (libera os pontos focais)"))
                        rei.IniciarRitual();
                }
            }

            if (jogador == null)
                EditorGUILayout.HelpBox("Nenhum objeto com a tag Player na cena.", MessageType.Warning);
        }

        private const string PrefabByakheePath = "Assets/FavelaAmarela/Art/Enemies/Byakhee.prefab";

        private static void InvocarByakhee(GameObject jogador)
        {
            var posicao = jogador.transform.position + new Vector3(3f, 2f, 0f);

            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabByakheePath);
            if (prefab != null)
            {
                var instancia = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
                instancia.name = "Byakhee (Debug)";
                instancia.transform.position = posicao;
                Debug.Log("[CarcosaDebugger] Byakhee invocado (prefab).");
                return;
            }

            // Sem o prefab (rode 'Tools/FavelaAmarela/Montar Prefab do Byakhee'): cai no
            // corpo mínimo construído em runtime, quadrado colorido no lugar da arte.
            var ficha = AssetDatabase.LoadAssetAtPath<FichaAtributosConfig>(FichaByakheePath);
            if (ficha == null)
            {
                Debug.LogError($"[CarcosaDebugger] Não achei a Ficha em '{FichaByakheePath}'.");
                return;
            }

            var go = CriarCorpoDoChefe("Byakhee (Debug)", posicao);

            // EnemyBase.Awake lê o campo privado `ficha` — precisa ser preenchido com o
            // objeto ainda inativo, senão Awake já rodou com ficha nula.
            var enemyBase = go.AddComponent<EnemyBase>();
            var so = new SerializedObject(enemyBase);
            so.FindProperty("ficha").objectReferenceValue = ficha;
            so.ApplyModifiedPropertiesWithoutUndo();

            go.AddComponent<ByakheeAI>();

            go.SetActive(true);
            Debug.Log("[CarcosaDebugger] Byakhee invocado (placeholder — sem prefab).");
        }

        private const string PrefabReiEmAmareloPath = "Assets/FavelaAmarela/Art/Enemies/ReiEmAmarelo.prefab";

        private static void InvocarReiEmAmarelo(GameObject jogador)
        {
            var posicao = jogador.transform.position + new Vector3(-3f, 2f, 0f);

            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabReiEmAmareloPath);
            if (prefab != null)
            {
                var instancia = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
                instancia.name = "Rei em Amarelo (Debug)";
                instancia.transform.position = posicao;
                Debug.Log("[CarcosaDebugger] Rei em Amarelo invocado (prefab). Use 'iniciar ritual' para começar.");
                return;
            }

            // Sem o prefab (rode 'Tools/FavelaAmarela/Montar Prefab do Rei em Amarelo'):
            // cai no corpo mínimo construído em runtime, sem EnemyBase/Vitalidade de
            // propósito — o design não prevê barra de vida para este confronto.
            var go = CriarCorpoDoChefe("Rei em Amarelo (Debug)", posicao);
            go.AddComponent<ReiEmAmareloAI>();
            go.SetActive(true);
            Debug.Log("[CarcosaDebugger] Rei em Amarelo invocado (placeholder — sem prefab). " +
                      "Use 'iniciar ritual' para começar.");
        }

        /// <summary>
        /// Corpo mínimo compartilhado pelos dois chefes: um quadrado colorido no lugar da
        /// arte (ainda não existe), na camada Enemy para o `MaoFisicaBridge` conseguir mirar
        /// nele via `Physics2D.OverlapCircle`. Começa inativo — quem chama termina de montar
        /// os componentes específicos antes de <c>SetActive(true)</c>, para nenhum `Awake`
        /// disparar cedo demais com dependência ainda não configurada.
        /// </summary>
        private static GameObject CriarCorpoDoChefe(string nome, Vector3 posicao)
        {
            var go = new GameObject(nome);
            go.SetActive(false);
            go.transform.position = posicao;
            go.layer = LayerMask.NameToLayer("Enemy");

            var sprite = go.AddComponent<SpriteRenderer>();
            sprite.sprite = CriarSpritePlaceholder();

            var colisor = go.AddComponent<CircleCollider2D>();
            colisor.radius = 0.5f;

            return go;
        }

        private static Sprite _spritePlaceholder;

        /// <summary>Quadrado branco 32×32 (PPU 32, convenção do projeto) — tingido por
        /// cada AI conforme o estado da FSM. Gerado uma vez e reaproveitado.</summary>
        private static Sprite CriarSpritePlaceholder()
        {
            if (_spritePlaceholder != null) return _spritePlaceholder;

            var tex = new Texture2D(32, 32) { filterMode = FilterMode.Point };
            var pixels = new Color[32 * 32];
            for (int i = 0; i < pixels.Length; i++) pixels[i] = Color.white;
            tex.SetPixels(pixels);
            tex.Apply();

            _spritePlaceholder = Sprite.Create(tex, new Rect(0, 0, 32, 32), new Vector2(0.5f, 0.5f), 32f);
            return _spritePlaceholder;
        }

        // ── Estado ao vivo ───────────────────────────────────────────────────

        private void DesenharEstadoAoVivo()
        {
            EditorGUILayout.LabelField("Estado ao vivo", EditorStyles.boldLabel);

            var byakhee = FindAnyObjectByType<ByakheeAI>();
            if (byakhee != null)
            {
                EditorGUILayout.LabelField("Byakhee", $"Estado: {byakhee.Fsm.CurrentState}  |  " +
                    $"Pode receber dano: {byakhee.Fsm.PodeReceberDano}  |  " +
                    $"Dreno/s: {byakhee.Fsm.DrenoDeResilienciaPorSegundo:0.0}");
            }

            var rei = FindAnyObjectByType<ReiEmAmareloAI>();
            if (rei != null)
            {
                EditorGUILayout.LabelField("Rei em Amarelo",
                    $"Estado: {rei.Fsm.CurrentState}  |  " +
                    $"Relíquias: {rei.Fsm.ReliquiasAtivas}/{rei.Fsm.TotalDeReliquiasExigidas}  |  " +
                    $"Ciclos: {rei.Fsm.CiclosSobrevividos}/{rei.Fsm.TotalDeCiclos}");
            }

            if (byakhee == null && rei == null)
                EditorGUILayout.HelpBox("Nenhum chefe na cena ainda.", MessageType.None);
        }
    }
}
