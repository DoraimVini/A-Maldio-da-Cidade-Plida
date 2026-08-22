using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using FavelaAmarela.Player;
using FavelaAmarela.Runtime.Quests;
using FavelaAmarela.Runtime.Rendering;
using FavelaAmarela.Runtime.UI;

namespace FavelaAmarela.EditorTools
{
    /// <summary>
    /// Ferramenta de Editor. Monta a quest de Cassilda — <b>"A Canção Incompleta"</b> — no
    /// Deserto e na Tumba: instancia o prefab da rainha dentro do Santuário e os 3
    /// fragmentos nos lugares que o design especifica (<c>lore/cassilda_e_byakhee.md</c>
    /// §II), com as duas primeiras estrofes da Canção de Cassilda distribuídas entre eles.
    ///
    /// <para><b>3 fragmentos, não 5</b> (decisão do Vini, 2026-08-01): os de nº 4 e 5 ficam
    /// no Templo da Serpente, dungeon que não existe. Com 5, a quest seria impossível de
    /// fechar no Vertical Slice.</para>
    ///
    /// <para><b>Conteúdo de Cassilda mora no prefab</b> (<c>MontarPrefabDaCassilda</c>), não
    /// aqui — esta ferramenta só instancia, posiciona e liga as duas referências de <b>cena</b>
    /// que um prefab não pode guardar sozinho: a caixa de diálogo e o painel de escolha do
    /// recital. Rode "Montar Prefab da Cassilda" primeiro se o asset ainda não existir.</para>
    ///
    /// <para>Idempotente: reaproveita pelo nome e só reescreve os valores.</para>
    /// </summary>
    public static class MontarSantuarioDeYhtill
    {
        private const string CaminhoPrefabCassilda = "Assets/FavelaAmarela/Art/Characters/Cassilda/Cassilda.prefab";

        private const string CenaDeserto = "Assets/Scenes/Deserto_Hali.unity";
        private const string CenaTumba = "Assets/Scenes/Playtest_RuinasPalidas.unity";

        /// <summary>
        /// Cassilda mora <b>dentro</b> do Santuário desde que ele virou cena própria
        /// (2026-08-02). Antes ficava solta no overworld, o que contradizia a ideia de um
        /// santuário — e o marco no Deserto virou só a porta.
        /// </summary>
        private const string CenaDaCassilda = "Assets/Scenes/Santuario_Yhtill.unity";

        /// <summary>Onde Cassilda fica dentro do Santuário.</summary>
        private static readonly Vector2 PosicaoDeCassilda = new Vector2(0f, 2.5f);

        private readonly struct Fragmento
        {
            public readonly int Indice;
            public readonly string Nome, Texto;
            public readonly Vector2 Posicao;
            public readonly string Cena;

            public Fragmento(int indice, string nome, Vector2 posicao, string cena, string texto)
            {
                Indice = indice; Nome = nome; Posicao = posicao; Cena = cena; Texto = texto;
            }
        }

        // As duas primeiras estrofes da Canção de Cassilda ficam distribuídas nestes 3
        // fragmentos (decisão do Vini, 2026-08-02) — não há fragmento próprio para elas,
        // porque a quest continua em 3, não 5. As estrofes 3 e 4 (o que Damião responde no
        // recital) NÃO estão em nenhum texto do mundo: a resposta certa é reconhecível pelo
        // tom, não decorável, e o fragmento da Vaine é o que planta o epíteto "Perdida
        // Carcosa" que decide a 4ª. Ver falaDeRecapitulacao no prefab da Cassilda.
        private static readonly Fragmento[] Fragmentos =
        {
            new Fragmento(0, "Diário de Lady Seraphel", new Vector2(-10f, -12f), CenaDeserto,
                "Acordei no deserto sem lembrar como cheguei. Somos quatro: eu, Morthis, " +
                "Vaine e Aldaron. A rainha ficou no Santuário — diz que não pode partir.\n\n" +
                "O deserto cheira a cinzas. E há uma melodia no vento que eu conheço de antes " +
                "de ter nascido. Escrevo os versos antes que me escapem:\n\n" +
                "\"Ao longo da costa as ondas de nuvem se quebram,\nOs sóis gêmeos afundam por " +
                "trás do lago,\nAs sombras se alongam\nEm Carcosa.\"\n— Lady Seraphel, nobreza de Yhtill"),

            new Fragmento(1, "Anotações de Lord Morthis", new Vector2(12f, 4f), CenaTumba,
                "Perdemos Seraphel na entrada. Não estava morta — simplesmente não estava " +
                "mais. A geometria deste lugar engole quem não presta atenção.\n\nOs seres " +
                "daqui são cegos e caçam pelo som. Aprendi a andar devagar. O silêncio é a " +
                "única moeda que vale aqui — e ainda assim a música não para dentro da minha " +
                "cabeça:\n\n\"Estranha é a noite em que as estrelas negras sobem,\nE estranhas " +
                "luas circulam pelos céus...\"\n— Lord Morthis de Yhtill"),

            new Fragmento(2, "Carta de Lady Vaine", new Vector2(30f, -12f), CenaTumba,
                "Rainha, Morthis não acordou esta manhã. Sua forma desapareceu enquanto " +
                "dormia, como Seraphel. Estou sozinha com Aldaron, e ele quer ir mais " +
                "fundo.\n\nEle terminou a canção em voz alta ontem, do princípio ao fim. " +
                "Escrevo até onde é seguro escrever:\n\n\"...Mas ainda mais estranha é\na " +
                "Perdida Carcosa.\"\n\nO resto eu ouvi e não vou registrar. Se a senhora ler " +
                "isto, vai querer o final. O final é o que nos come, minha rainha. Que ele " +
                "fique com Aldaron.\n— Lady Vaine de Yhtill"),
        };

        [MenuItem("Tools/FavelaAmarela/Montar Santuário de Yhtill e fragmentos")]
        public static void Executar()
        {
            // Salva sem perguntar. `SaveCurrentModifiedScenesIfUserWantsTo` abre um
            // diálogo MODAL, e uma ferramenta disparada pela ponte MCP trava a Unity
            // inteira esperando um clique que ninguém vê.
            var cenaAtiva = EditorSceneManager.GetActiveScene();
            if (cenaAtiva.isDirty && !string.IsNullOrEmpty(cenaAtiva.path))
                EditorSceneManager.SaveScene(cenaAtiva);

            string cenaOriginal = cenaAtiva.path;

            MontarCassildaNoSantuario();
            MontarNoDeserto();
            MontarNaTumba();

            if (!string.IsNullOrEmpty(cenaOriginal))
                EditorSceneManager.OpenScene(cenaOriginal, OpenSceneMode.Single);

            Debug.Log("[Santuário] Pronto — Cassilda e os 3 fragmentos montados.");
        }

        private static void MontarCassildaNoSantuario()
        {
            if (!System.IO.File.Exists(CenaDaCassilda))
            {
                Debug.LogError($"[Santuário] Cena '{CenaDaCassilda}' não existe — rode antes " +
                               "'Tools/FavelaAmarela/Montar cena do Santuario de Yhtill'.");
                return;
            }

            var cena = EditorSceneManager.OpenScene(CenaDaCassilda, OpenSceneMode.Single);
            var caixa = Object.FindAnyObjectByType<TutorialHintUI>(FindObjectsInactive.Include);
            var jogador = Object.FindAnyObjectByType<PlayerMovement>(FindObjectsInactive.Include);
            var painel = MontarPainelDeEscolha(jogador);

            MontarCassilda(caixa, painel);

            EditorSceneManager.MarkSceneDirty(cena);
            EditorSceneManager.SaveScene(cena);
        }

        private static void MontarNoDeserto()
        {
            var cena = EditorSceneManager.OpenScene(CenaDeserto, OpenSceneMode.Single);
            var caixa = Object.FindAnyObjectByType<TutorialHintUI>(FindObjectsInactive.Include);

            foreach (var f in Fragmentos)
                if (f.Cena == CenaDeserto) MontarFragmento(f, caixa);

            EditorSceneManager.MarkSceneDirty(cena);
            EditorSceneManager.SaveScene(cena);
        }

        private static void MontarNaTumba()
        {
            var cena = EditorSceneManager.OpenScene(CenaTumba, OpenSceneMode.Single);
            var caixa = Object.FindAnyObjectByType<TutorialHintUI>(FindObjectsInactive.Include);

            foreach (var f in Fragmentos)
                if (f.Cena == CenaTumba) MontarFragmento(f, caixa);

            EditorSceneManager.MarkSceneDirty(cena);
            EditorSceneManager.SaveScene(cena);
        }

        /// <summary>
        /// Instancia o prefab da Cassilda (ou reaproveita a instância já presente na cena) e
        /// liga só as duas referências que o prefab não pode guardar sozinho — o resto do
        /// conteúdo (falas, estrofes do recital) vem de <c>MontarPrefabDaCassilda</c>.
        /// </summary>
        private static void MontarCassilda(TutorialHintUI caixa, PainelDeEscolha painel)
        {
            var npcExistente = Object.FindAnyObjectByType<CassildaNPC>(FindObjectsInactive.Include);

            // Autocorreção: uma Cassilda solta (de antes do prefab existir) tem conteúdo
            // desatualizado e nenhum vínculo com o asset — substitui por uma instância real
            // do prefab. Sem outro objeto na cena referencia essa instância diretamente (o
            // GerenciadorDeSave usa chaves fixas, não a identidade do GameObject).
            if (npcExistente != null && PrefabUtility.GetPrefabInstanceStatus(npcExistente.gameObject)
                == PrefabInstanceStatus.NotAPrefab)
            {
                Debug.Log("[Santuário] Cassilda existente não é instância do prefab — " +
                          "substituindo por uma que é.");
                Object.DestroyImmediate(npcExistente.gameObject);
                npcExistente = null;
            }

            GameObject go;

            if (npcExistente != null)
            {
                go = npcExistente.gameObject;
            }
            else
            {
                var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(CaminhoPrefabCassilda);
                if (prefab == null)
                {
                    Debug.LogError("[Santuário] Prefab da Cassilda não encontrado em " +
                                   $"{CaminhoPrefabCassilda} — rode 'Tools/FavelaAmarela/" +
                                   "Montar Prefab da Cassilda' antes.");
                    return;
                }

                go = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
                Undo.RegisterCreatedObjectUndo(go, "Instanciar Cassilda");
            }

            go.name = "Cassilda";
            go.transform.position = PosicaoDeCassilda;

            var npc = go.GetComponent<CassildaNPC>();
            var so = new SerializedObject(npc);
            if (caixa != null) so.FindProperty("caixaDeTexto").objectReferenceValue = caixa;
            if (painel != null) so.FindProperty("painelDeEscolha").objectReferenceValue = painel;
            so.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(go);

            Debug.Log($"[Santuário] Cassilda em {PosicaoDeCassilda}.", go);
        }

        /// <summary>
        /// Painel de escolha do recital, no mesmo Canvas do HUD — mesmo componente e mesmo
        /// padrão de montagem usados na conversa ramificada do Abdul.
        /// </summary>
        private static PainelDeEscolha MontarPainelDeEscolha(PlayerMovement jogador)
        {
            var existente = Object.FindAnyObjectByType<PainelDeEscolha>(FindObjectsInactive.Include);
            if (existente != null) return existente;

            var hud = Object.FindAnyObjectByType<HUDController>(FindObjectsInactive.Include);
            if (hud == null)
            {
                Debug.LogWarning("[Santuário] Nenhum HUDController na cena — o painel de " +
                                 "escolha do recital não foi criado.");
                return null;
            }

            var raizGO = new GameObject("Painel_Escolha", typeof(RectTransform));
            raizGO.transform.SetParent(hud.transform, false);
            var rt = raizGO.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(0.5f, 0.5f);
            rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = Vector2.zero;
            rt.sizeDelta = new Vector2(360f, 120f);

            var fundo = raizGO.AddComponent<Image>();
            fundo.color = new Color(0.05f, 0.04f, 0.03f, 0.9f);

            var textoGO = new GameObject("Texto", typeof(RectTransform));
            textoGO.transform.SetParent(raizGO.transform, false);
            var rtTexto = textoGO.GetComponent<RectTransform>();
            rtTexto.anchorMin = Vector2.zero;
            rtTexto.anchorMax = Vector2.one;
            rtTexto.offsetMin = Vector2.zero;
            rtTexto.offsetMax = Vector2.zero;

            var texto = textoGO.AddComponent<Text>();
            texto.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            texto.fontSize = 48;
            texto.color = new Color(0.93f, 0.90f, 0.75f);
            texto.alignment = TextAnchor.MiddleLeft;
            texto.horizontalOverflow = HorizontalWrapMode.Wrap;

            var painelGO = new GameObject("PainelDeEscolha");
            painelGO.transform.SetParent(hud.transform, false);
            var painel = painelGO.AddComponent<PainelDeEscolha>();
            raizGO.transform.SetParent(painelGO.transform, false);

            var so = new SerializedObject(painel);
            so.FindProperty("raiz").objectReferenceValue = raizGO;
            so.FindProperty("texto").objectReferenceValue = texto;
            if (jogador != null)
            {
                so.FindProperty("playerInput").objectReferenceValue = jogador.GetComponent<PlayerInput>();
                so.FindProperty("movimentoDoJogador").objectReferenceValue = jogador;
            }
            so.ApplyModifiedPropertiesWithoutUndo();

            return painel;
        }

        private static void MontarFragmento(Fragmento f, TutorialHintUI caixa)
        {
            string nomeObjeto = $"Fragmento_{f.Indice}";
            var go = GameObject.Find(nomeObjeto);
            if (go == null)
            {
                go = new GameObject(nomeObjeto);
                Undo.RegisterCreatedObjectUndo(go, "Criar fragmento");
            }

            go.transform.position = f.Posicao;

            var sr = go.GetComponent<SpriteRenderer>();
            if (sr == null)
            {
                sr = go.AddComponent<SpriteRenderer>();
                sr.sprite = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/Knob.psd");
                sr.color = new Color(0.95f, 0.93f, 0.82f);  // papel velho
            }

            if (go.GetComponent<DynamicYSort>() == null) go.AddComponent<DynamicYSort>();

            var col = go.GetComponent<CircleCollider2D>();
            if (col == null) col = go.AddComponent<CircleCollider2D>();
            col.isTrigger = true;
            col.radius = 0.8f;

            var frag = go.GetComponent<FragmentoDeYhtill>();
            if (frag == null) frag = go.AddComponent<FragmentoDeYhtill>();

            var so = new SerializedObject(frag);
            so.FindProperty("indice").intValue = f.Indice;
            so.FindProperty("nomeDoFragmento").stringValue = f.Nome;
            so.FindProperty("texto").stringValue = f.Texto;
            if (caixa != null) so.FindProperty("caixaDeTexto").objectReferenceValue = caixa;
            so.ApplyModifiedPropertiesWithoutUndo();

            EditorUtility.SetDirty(go);
            Debug.Log($"[Santuário] {f.Nome} em {f.Posicao}", go);
        }
    }
}
