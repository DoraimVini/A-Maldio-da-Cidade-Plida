using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using FavelaAmarela.Level;
using FavelaAmarela.Runtime.Enemies;
using FavelaAmarela.Runtime.GameLoop;
using FavelaAmarela.Runtime.Itens;
using FavelaAmarela.Runtime.UI;
using FavelaAmarela.Runtime.Environment;
using FavelaAmarela.CameraSystem;

namespace FavelaAmarela.EditorTools
{
    /// <summary>
    /// Constrói o <b>Castelo de Carcosa</b> — a última fase do Vertical Slice — em greybox
    /// funcional, com as quatro zonas essenciais e todos os sistemas ligados.
    ///
    /// <para><b>Por que greybox e não arte:</b> o levantamento de 2026-08-19 mostrou que o
    /// Castelo estava no estado assinatura deste projeto — <c>PressaoPsiquicaZone</c>,
    /// <c>CortesaoPalido</c>, <c>EcoDeCarcosa</c>, <c>PontoFocalDeReliquia</c> e
    /// <c>DetectorDeCostas</c> <b>todos escritos e em cena nenhuma</b>. O que falta para o VS
    /// não é pintura, é a fase existir e ligar esses sistemas. A vestimenta entra depois, em
    /// passo próprio.</para>
    ///
    /// <para><b>Sobre a arte que existe:</b> <c>Carcosa_Tiles.png</c> foi conferido e é tileset
    /// de <b>deserto</b> (dunas douradas, rocha negra, Sol Negro) — não interior de palácio,
    /// apesar do nome. A paleta preto-e-ouro serve ao "mármore negro com adornos de ouro
    /// manchado" do design, mas o arquivo está a PPU 100 e não fatiado; arrumá-lo é o passo de
    /// vestimenta, não este.</para>
    ///
    /// <para><b>Z4 (Observatório Secreto) fica de fora</b> e isso segue o design, não corta
    /// escopo: o documento a marca como <i>dungeon opcional</i>, aberta só com o Set Lendário
    /// 4/4. As quatro construídas — Z1 Portões, Z2 Salão, Z3 Biblioteca, Z5 Trono — são o
    /// caminho crítico até o Rei.</para>
    ///
    /// <para>Idempotente: reabre a cena se já existir e refaz cada peça no lugar, no molde de
    /// <c>MontarCenaDoSantuario</c>.</para>
    /// </summary>
    public static class MontarCasteloCarcosa
    {
        private const string CenaCastelo = "Assets/Scenes/Castelo_Carcosa.unity";
        private const string CenaSantuario = "Assets/Scenes/Santuario_Yhtill.unity";

        private const string PrefabJogador =
            "Assets/FavelaAmarela/Art/Characters/Damiao/Player_Damiao.prefab";
        private const string PrefabHUD = "Assets/FavelaAmarela/Art/UI/HUD_ResilienciaBar.prefab";
        private const string PrefabRei = "Assets/FavelaAmarela/Art/Enemies/ReiEmAmarelo.prefab";

        /// <summary>Identificador do <c>PontoDeChegada</c> ao entrar no Castelo.</summary>
        private const string IdChegadaNoCastelo = "PortoesInternos";

        // ── Topologia (do doc de level design, §2) ───────────────────────────
        // Z1 embaixo, o Trono no topo: o jogador sobe o castelo até o Rei.
        private static readonly Vector3 CentroZ1 = new Vector3(0f, -30f, 0f);
        private static readonly Vector3 CentroZ2 = new Vector3(0f, 0f, 0f);
        private static readonly Vector3 CentroZ3 = new Vector3(0f, 30f, 0f);
        private static readonly Vector3 CentroZ5 = new Vector3(0f, 62f, 0f);

        private static readonly Vector2 SalaPequena = new Vector2(18f, 12f);
        private static readonly Vector2 SalaGrande = new Vector2(26f, 18f);

        /// <summary>Mármore negro do palácio (design §1.1).</summary>
        private static readonly Color MarmoreNegro = new Color(0.13f, 0.12f, 0.15f);

        /// <summary>Ouro manchado dos adornos.</summary>
        private static readonly Color OuroManchado = new Color(0.42f, 0.36f, 0.20f);

        [MenuItem("Tools/FavelaAmarela/Montar Castelo de Carcosa")]
        public static void Executar()
        {
            var ativa = EditorSceneManager.GetActiveScene();
            if (ativa.isDirty && !string.IsNullOrEmpty(ativa.path))
                EditorSceneManager.SaveScene(ativa);

            if (!Construir())
            {
                // Sem a cena no disco, registrar no Build Settings e ligar o portal do
                // Santuário só criaria ponteiros para o nada.
                Debug.LogError("[Castelo] A cena não foi salva — nada mais foi ligado.");
                return;
            }

            RegistrarEmBuildSettings();
            LigarOPortalNoSantuario();

            AssetDatabase.SaveAssets();
            Debug.Log("[Castelo] Cena montada: Z1 Portões, Z2 Salão, Z3 Biblioteca, Z5 Trono.");
        }

        /// <returns>
        /// <c>true</c> só se a cena existir no disco ao fim. <b>Conferir o retorno de
        /// <c>SaveScene</c> não é zelo excessivo:</b> na primeira execução ela falhou em
        /// silêncio, o método seguiu, e o log anunciou "Cena montada" enquanto
        /// <c>Castelo_Carcosa.unity</c> não existia — com o Build Settings e o portal do
        /// Santuário já apontando para o arquivo ausente.
        /// </returns>
        private static bool Construir()
        {
            Scene cena = System.IO.File.Exists(CenaCastelo)
                ? EditorSceneManager.OpenScene(CenaCastelo, OpenSceneMode.Single)
                : EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            var jogador = GarantirJogador();
            GarantirCamera(jogador);
            MontarBootstrapDaCena.Garantir();
            GarantirPrefabPorNome(PrefabHUD, "HUD");
            var caixa = GarantirCaixaDeDialogo();

            GarantirCalmaria();

            var raiz = GameObject.Find("Castelo_Root") ?? new GameObject("Castelo_Root");

            MontarZ1(raiz.transform, caixa);
            MontarZ2(raiz.transform);
            MontarZ3(raiz.transform);
            MontarZ5(raiz.transform);
            MontarCorredores(raiz.transform);

            // Marcar suja antes de salvar: cena recém-criada por NewScene pode não ser
            // considerada modificada, e SaveScene então não escreve nada.
            EditorSceneManager.MarkSceneDirty(cena);

            bool salvou = System.IO.File.Exists(CenaCastelo)
                ? EditorSceneManager.SaveScene(cena)
                : EditorSceneManager.SaveScene(cena, CenaCastelo);

            if (!salvou)
            {
                Debug.LogError($"[Castelo] EditorSceneManager.SaveScene recusou salvar em " +
                               $"'{CenaCastelo}'.");
                return false;
            }

            AssetDatabase.Refresh();

            // Confere no DISCO, não no retorno: é a diferença entre "a API disse que salvou" e
            // "o arquivo está lá".
            if (!System.IO.File.Exists(CenaCastelo))
            {
                Debug.LogError($"[Castelo] SaveScene devolveu true mas '{CenaCastelo}' não " +
                               "existe no disco.");
                return false;
            }

            return true;
        }

        // ── Zonas ────────────────────────────────────────────────────────────

        /// <summary>
        /// Z1 — Os Portões Internos. Área segura: chegada do Santuário e o último Refúgio
        /// antes do Rei (design §3, Z1: "Último Refúgio oficial livre de tensão").
        /// </summary>
        private static void MontarZ1(Transform raiz, TutorialHintUI caixa)
        {
            var zona = Sala(raiz, "Z1_PortoesInternos", CentroZ1, SalaPequena, MarmoreNegro);
            MarcarZona(zona, "Os Portões Internos");

            GarantirChegada(zona.transform, CentroZ1 + new Vector3(0f, -3f, 0f));
            GarantirRefugio(zona.transform, CentroZ1 + new Vector3(-5f, 0f, 0f), caixa);
        }

        /// <summary>
        /// Z2 — O Salão do Banquete Fossilizado. Hub central: nobres petrificados como cobertura
        /// e <c>CortesaoPalido</c> patrulhando (design §3, Z2).
        /// </summary>
        private static void MontarZ2(Transform raiz)
        {
            var zona = Sala(raiz, "Z2_SalaoDoBanquete", CentroZ2, SalaGrande, MarmoreNegro);
            MarcarZona(zona, "O Salão do Banquete Fossilizado");

            // Nobreza fossilizada: obstáculos que servem de cobertura para o stealth visual.
            var posturas = new[]
            {
                new Vector3(-8f, 4f, 0f), new Vector3(-3f, 5f, 0f), new Vector3(3f, 5f, 0f),
                new Vector3(8f, 4f, 0f), new Vector3(-6f, -4f, 0f), new Vector3(6f, -4f, 0f),
            };
            for (int i = 0; i < posturas.Length; i++)
                Estatua(zona.transform, $"Nobre_Fossilizado_{i}", CentroZ2 + posturas[i]);

            // Dois Cortesãos patrulhando eixos opostos.
            Cortesao(zona.transform, "Cortesao_Palido_0",
                     CentroZ2 + new Vector3(-9f, 0f, 0f),
                     CentroZ2 + new Vector3(9f, 0f, 0f));
            Cortesao(zona.transform, "Cortesao_Palido_1",
                     CentroZ2 + new Vector3(0f, 6f, 0f),
                     CentroZ2 + new Vector3(0f, -6f, 0f));
        }

        /// <summary>
        /// Z3 — A Biblioteca Esquecida. Aqui entra a <b>Pressão Psíquica</b>: os Espelhos de
        /// Aldebaran drenam RM enquanto o jogador estiver virado para eles (design §3, Z3 e
        /// §4.1). Os Ecos punem ficar parado.
        /// </summary>
        private static void MontarZ3(Transform raiz)
        {
            var zona = Sala(raiz, "Z3_BibliotecaEsquecida", CentroZ3, SalaGrande, MarmoreNegro);
            MarcarZona(zona, "A Biblioteca Esquecida");

            // Três espelhos, cada um com sua zona de pressão apontando para si.
            var pontos = new[]
            {
                new Vector3(-9f, 6f, 0f), new Vector3(9f, 6f, 0f), new Vector3(0f, -7f, 0f),
            };
            for (int i = 0; i < pontos.Length; i++)
                EspelhoComPressao(zona.transform, i, CentroZ3 + pontos[i]);

            Eco(zona.transform, "Eco_De_Carcosa_0", CentroZ3 + new Vector3(-5f, -2f, 0f));
            Eco(zona.transform, "Eco_De_Carcosa_1", CentroZ3 + new Vector3(5f, -2f, 0f));
        }

        /// <summary>
        /// Z5 — O Trono de Aldebaran. O Rei e os quatro pontos focais do rito de selamento
        /// (design §3, Z5). Os ids das relíquias vêm do próprio <c>ReiEmAmareloAI</c>, não de
        /// uma lista escrita à mão aqui — duas listas divergiriam em silêncio.
        /// </summary>
        private static void MontarZ5(Transform raiz)
        {
            var zona = Sala(raiz, "Z5_TronoDeAldebaran", CentroZ5, SalaGrande, MarmoreNegro);
            MarcarZona(zona, "O Trono de Aldebaran");

            var rei = GarantirRei(zona.transform, CentroZ5 + new Vector3(0f, 5f, 0f));
            if (rei == null) return;

            string[] ids = LerIdsDasReliquias(rei);

            var cantos = new[]
            {
                new Vector3(-9f, -5f, 0f), new Vector3(9f, -5f, 0f),
                new Vector3(-9f, 3f, 0f), new Vector3(9f, 3f, 0f),
            };

            for (int i = 0; i < ids.Length && i < cantos.Length; i++)
                PontoFocal(zona.transform, i, CentroZ5 + cantos[i], ids[i], rei);
        }

        /// <summary>
        /// Lê <c>idsDasReliquiasExigidas</c> do próprio Rei: é ele quem define o rito, e copiar
        /// os ids para cá criaria uma segunda fonte de verdade.
        /// </summary>
        private static string[] LerIdsDasReliquias(ReiEmAmareloAI rei)
        {
            var so = new SerializedObject(rei);
            var arr = so.FindProperty("idsDasReliquiasExigidas");

            if (arr == null || arr.arraySize == 0)
            {
                Debug.LogWarning("[Castelo] O Rei não declara relíquias exigidas — nenhum ponto " +
                                 "focal será criado.");
                return new string[0];
            }

            var ids = new string[arr.arraySize];
            for (int i = 0; i < arr.arraySize; i++)
                ids[i] = arr.GetArrayElementAtIndex(i).stringValue;

            return ids;
        }

        // ── Peças ────────────────────────────────────────────────────────────

        private static GameObject Sala(Transform raiz, string nome, Vector3 centro,
                                        Vector2 tamanho, Color cor)
        {
            var t = raiz.Find(nome);
            var go = t != null ? t.gameObject : new GameObject(nome);
            if (t == null) go.transform.SetParent(raiz, false);
            go.transform.position = centro;

            var piso = go.transform.Find("Piso")?.gameObject;
            if (piso == null)
            {
                piso = new GameObject("Piso", typeof(SpriteRenderer));
                piso.transform.SetParent(go.transform, false);
            }

            var sr = piso.GetComponent<SpriteRenderer>();
            sr.sprite = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/UISprite.psd");
            sr.drawMode = SpriteDrawMode.Sliced;
            sr.size = tamanho;
            sr.color = cor;
            sr.sortingOrder = -1000;

            Parede(go.transform, "Parede_Norte", new Vector3(0f, tamanho.y / 2f, 0f), new Vector2(tamanho.x, 0.5f));
            Parede(go.transform, "Parede_Sul", new Vector3(0f, -tamanho.y / 2f, 0f), new Vector2(tamanho.x, 0.5f));
            Parede(go.transform, "Parede_Leste", new Vector3(tamanho.x / 2f, 0f, 0f), new Vector2(0.5f, tamanho.y));
            Parede(go.transform, "Parede_Oeste", new Vector3(-tamanho.x / 2f, 0f, 0f), new Vector2(0.5f, tamanho.y));

            return go;
        }

        private static void Parede(Transform raiz, string nome, Vector3 pos, Vector2 tamanho)
        {
            var t = raiz.Find(nome);
            var go = t != null ? t.gameObject : new GameObject(nome);
            if (t == null) go.transform.SetParent(raiz, false);

            go.transform.localPosition = pos;
            go.layer = LayerMask.NameToLayer("Obstacle");

            var col = go.GetComponent<BoxCollider2D>();
            if (col == null) col = go.AddComponent<BoxCollider2D>();
            col.size = tamanho;
        }

        /// <summary>
        /// Corredores entre as zonas: vãos estreitos que fazem o Castelo ser percorrido em
        /// sequência, e não em campo aberto. Sem colisor — são só o piso do vão.
        /// </summary>
        private static void MontarCorredores(Transform raiz)
        {
            Corredor(raiz, "Corredor_Z1_Z2", CentroZ1, CentroZ2, SalaPequena.y, SalaGrande.y);
            Corredor(raiz, "Corredor_Z2_Z3", CentroZ2, CentroZ3, SalaGrande.y, SalaGrande.y);
            Corredor(raiz, "Corredor_Z3_Z5", CentroZ3, CentroZ5, SalaGrande.y, SalaGrande.y);
        }

        private static void Corredor(Transform raiz, string nome, Vector3 de, Vector3 para,
                                      float alturaDe, float alturaPara)
        {
            float y0 = de.y + alturaDe / 2f;
            float y1 = para.y - alturaPara / 2f;

            var t = raiz.Find(nome);
            var go = t != null ? t.gameObject : new GameObject(nome, typeof(SpriteRenderer));
            if (t == null) go.transform.SetParent(raiz, false);

            go.transform.position = new Vector3(0f, (y0 + y1) / 2f, 0f);

            var sr = go.GetComponent<SpriteRenderer>();
            if (sr == null) sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/UISprite.psd");
            sr.drawMode = SpriteDrawMode.Sliced;
            sr.size = new Vector2(4f, Mathf.Abs(y1 - y0));
            sr.color = MarmoreNegro;
            sr.sortingOrder = -1000;
        }

        private static void MarcarZona(GameObject zona, string nome)
        {
            var marca = zona.GetComponent<CasteloDeCarcosaZone>();
            if (marca == null) marca = zona.AddComponent<CasteloDeCarcosaZone>();

            var so = new SerializedObject(marca);
            so.FindProperty("nomeDaZona").stringValue = nome;
            so.ApplyModifiedPropertiesWithoutUndo();

            // O aviso de zona precisa de gatilho para disparar ao entrar.
            var col = zona.GetComponent<BoxCollider2D>();
            if (col == null) col = zona.AddComponent<BoxCollider2D>();
            col.isTrigger = true;
            col.size = SalaGrande;
        }

        private static void Estatua(Transform raiz, string nome, Vector3 pos)
        {
            var t = raiz.Find(nome);
            var go = t != null ? t.gameObject : new GameObject(nome, typeof(SpriteRenderer));
            if (t == null) go.transform.SetParent(raiz, false);

            go.transform.position = pos;
            go.layer = LayerMask.NameToLayer("Obstacle");

            var sr = go.GetComponent<SpriteRenderer>();
            if (sr == null) sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/UISprite.psd");
            sr.drawMode = SpriteDrawMode.Sliced;
            sr.size = new Vector2(1.2f, 2f);
            sr.color = new Color(0.78f, 0.76f, 0.72f);   // pedra branca (design §3, Z2)

            var col = go.GetComponent<BoxCollider2D>();
            if (col == null) col = go.AddComponent<BoxCollider2D>();
            col.size = new Vector2(1.2f, 1f);
            col.offset = new Vector2(0f, -0.5f);
        }

        private static void Cortesao(Transform raiz, string nome, Vector3 a, Vector3 b)
        {
            var t = raiz.Find(nome);
            var go = t != null ? t.gameObject : new GameObject(nome, typeof(SpriteRenderer));
            if (t == null) go.transform.SetParent(raiz, false);

            go.transform.position = a;

            var sr = go.GetComponent<SpriteRenderer>();
            if (sr == null) sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/UISprite.psd");
            sr.drawMode = SpriteDrawMode.Sliced;
            sr.size = new Vector2(0.8f, 1.4f);
            sr.color = OuroManchado;

            var corpo = go.GetComponent<Rigidbody2D>();
            if (corpo == null) corpo = go.AddComponent<Rigidbody2D>();
            corpo.gravityScale = 0f;
            corpo.collisionDetectionMode = CollisionDetectionMode2D.Continuous;

            var col = go.GetComponent<BoxCollider2D>();
            if (col == null) col = go.AddComponent<BoxCollider2D>();
            col.size = new Vector2(0.6f, 0.6f);

            var ia = go.GetComponent<FavelaAmarela.Core.Combat.CortesaoPalido>();
            if (ia == null) ia = go.AddComponent<FavelaAmarela.Core.Combat.CortesaoPalido>();

            // Pontos de patrulha como filhos: o array é de Transform, então precisam existir
            // como objetos de cena.
            var p0 = GarantirPontoDePatrulha(go.transform, "Ponto_A", a);
            var p1 = GarantirPontoDePatrulha(go.transform, "Ponto_B", b);

            var so = new SerializedObject(ia);
            var arr = so.FindProperty("pontosDePatrulha");
            arr.arraySize = 2;
            arr.GetArrayElementAtIndex(0).objectReferenceValue = p0;
            arr.GetArrayElementAtIndex(1).objectReferenceValue = p1;
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        private static Transform GarantirPontoDePatrulha(Transform pai, string nome, Vector3 pos)
        {
            var t = pai.Find(nome);
            var go = t != null ? t.gameObject : new GameObject(nome);
            if (t == null) go.transform.SetParent(pai, false);
            go.transform.position = pos;
            return go.transform;
        }

        private static void Eco(Transform raiz, string nome, Vector3 pos)
        {
            var t = raiz.Find(nome);
            var go = t != null ? t.gameObject : new GameObject(nome);
            if (t == null) go.transform.SetParent(raiz, false);

            go.transform.position = pos;

            if (go.GetComponent<EcoDeCarcosa>() == null) go.AddComponent<EcoDeCarcosa>();
        }

        /// <summary>
        /// Espelho de Aldebaran com a zona de Pressão Psíquica apontada para ele. O
        /// <c>pontoDeFocoCorrompido</c> é o próprio espelho: é olhar para ele que drena.
        /// </summary>
        private static void EspelhoComPressao(Transform raiz, int indice, Vector3 pos)
        {
            var nome = $"Espelho_De_Aldebaran_{indice}";
            var t = raiz.Find(nome);
            var go = t != null ? t.gameObject : new GameObject(nome, typeof(SpriteRenderer));
            if (t == null) go.transform.SetParent(raiz, false);

            go.transform.position = pos;

            var sr = go.GetComponent<SpriteRenderer>();
            if (sr == null) sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/UISprite.psd");
            sr.drawMode = SpriteDrawMode.Sliced;
            sr.size = new Vector2(1.6f, 2.6f);
            sr.color = new Color(0.55f, 0.60f, 0.72f);   // vidro frio

            var zonaNome = $"Pressao_Psiquica_{indice}";
            var zt = raiz.Find(zonaNome);
            var zona = zt != null ? zt.gameObject : new GameObject(zonaNome);
            if (zt == null) zona.transform.SetParent(raiz, false);

            zona.transform.position = pos;

            var col = zona.GetComponent<CircleCollider2D>();
            if (col == null) col = zona.AddComponent<CircleCollider2D>();
            col.isTrigger = true;
            col.radius = 6f;

            var pressao = zona.GetComponent<PressaoPsiquicaZone>();
            if (pressao == null) pressao = zona.AddComponent<PressaoPsiquicaZone>();

            var so = new SerializedObject(pressao);
            so.FindProperty("pontoDeFocoCorrompido").objectReferenceValue = go.transform;
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        private static ReiEmAmareloAI GarantirRei(Transform raiz, Vector3 pos)
        {
            var existente = Object.FindAnyObjectByType<ReiEmAmareloAI>(FindObjectsInactive.Include);
            if (existente != null)
            {
                existente.transform.position = pos;
                return existente;
            }

            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabRei);
            if (prefab == null)
            {
                Debug.LogError($"[Castelo] Prefab do Rei não encontrado em {PrefabRei}.");
                return null;
            }

            var go = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
            go.transform.SetParent(raiz, false);
            go.transform.position = pos;
            return go.GetComponent<ReiEmAmareloAI>();
        }

        private static void PontoFocal(Transform raiz, int indice, Vector3 pos,
                                        string artefatoId, ReiEmAmareloAI rei)
        {
            var nome = $"Ponto_Focal_{artefatoId}";
            var t = raiz.Find(nome);
            var go = t != null ? t.gameObject : new GameObject(nome, typeof(SpriteRenderer));
            if (t == null) go.transform.SetParent(raiz, false);

            go.transform.position = pos;

            var sr = go.GetComponent<SpriteRenderer>();
            if (sr == null) sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/UISprite.psd");
            sr.drawMode = SpriteDrawMode.Sliced;
            sr.size = new Vector2(1.4f, 1.4f);
            sr.color = OuroManchado;

            var col = go.GetComponent<CircleCollider2D>();
            if (col == null) col = go.AddComponent<CircleCollider2D>();
            col.isTrigger = true;
            col.radius = 1.2f;

            var foco = go.GetComponent<PontoFocalDeReliquia>();
            if (foco == null) foco = go.AddComponent<PontoFocalDeReliquia>();

            var so = new SerializedObject(foco);
            so.FindProperty("artefatoId").stringValue = artefatoId;
            so.FindProperty("rei").objectReferenceValue = rei;
            so.FindProperty("spriteDoPonto").objectReferenceValue = sr;
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        // ── Infraestrutura da cena ───────────────────────────────────────────

        private static GameObject GarantirJogador()
        {
            var existente = GameObject.FindGameObjectWithTag("Player");
            if (existente != null) return existente;

            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabJogador);
            if (prefab == null)
            {
                Debug.LogError($"[Castelo] Prefab do jogador não encontrado em {PrefabJogador}.");
                return null;
            }

            var go = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
            go.transform.position = CentroZ1 + new Vector3(0f, -3f, 0f);
            return go;
        }

        private static void GarantirCamera(GameObject jogador)
        {
            var cam = Object.FindAnyObjectByType<UnityEngine.Camera>(FindObjectsInactive.Include);
            if (cam == null)
            {
                var go = new GameObject("Main Camera", typeof(UnityEngine.Camera), typeof(AudioListener));
                go.tag = "MainCamera";
                cam = go.GetComponent<UnityEngine.Camera>();
            }

            cam.orthographic = true;
            cam.orthographicSize = 6f;
            // Mais escuro que o Santuário: o Castelo é claustrofóbico (design §1.2).
            cam.backgroundColor = new Color(0.04f, 0.03f, 0.05f);
            cam.transform.position = new Vector3(0f, CentroZ1.y, -10f);

            var ctrl = cam.GetComponent<IsometricCameraController>();
            if (ctrl == null) ctrl = cam.gameObject.AddComponent<IsometricCameraController>();

            if (jogador != null)
            {
                var so = new SerializedObject(ctrl);
                so.FindProperty("target").objectReferenceValue = jogador.transform;
                so.FindProperty("orthographicSize").floatValue = 6f;
                so.ApplyModifiedPropertiesWithoutUndo();
            }
        }

        private static void GarantirPrefabPorNome(string caminho, string nome)
        {
            if (GameObject.Find(nome) != null) return;

            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(caminho);
            if (prefab == null)
            {
                Debug.LogWarning($"[Castelo] Prefab '{caminho}' não encontrado.");
                return;
            }

            var go = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
            go.name = nome;
        }

        /// <summary>
        /// O Castelo está fora do tempo e do espaço (design §1.1) — não há tempestade de areia
        /// aqui. Como no Santuário, o driver precisa <b>existir com faixa 0–0</b>: sem ele o
        /// <c>EnvironmentState</c> fica no valor inicial e o Castelo teria tempestade por
        /// acidente.
        /// </summary>
        private static void GarantirCalmaria()
        {
            var driver = Object.FindAnyObjectByType<TempestadeAmbiente>(FindObjectsInactive.Include);
            if (driver == null)
                driver = new GameObject("Vazio_Cosmico").AddComponent<TempestadeAmbiente>();

            var so = new SerializedObject(driver);
            so.FindProperty("minimoInicial").floatValue = 0f;
            so.FindProperty("maximoInicial").floatValue = 0f;
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        private static TutorialHintUI GarantirCaixaDeDialogo()
        {
            var existente = Object.FindAnyObjectByType<TutorialHintUI>(FindObjectsInactive.Include);
            if (existente != null) return existente;

            // Montada INLINE, e não por MontarCaixaDeDialogo.Executar(): aquela ferramenta
            // percorre as cenas jogáveis com OpenScene(..., Single), o que FECHA esta cena
            // recém-criada e ainda não salva. Na primeira tentativa foi exatamente isso que
            // aconteceu — o handle morreu e SaveScene recusou salvar, em silêncio. O
            // MontarCenaDoSantuario constrói inline pelo mesmo motivo.
            var canvas = Object.FindAnyObjectByType<Canvas>(FindObjectsInactive.Include);
            if (canvas == null)
            {
                var goCanvas = new GameObject("Canvas_Castelo",
                    typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
                canvas = goCanvas.GetComponent<Canvas>();
                canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            }

            var painel = new GameObject("CaixaDeDialogo", typeof(CanvasGroup), typeof(Image));
            painel.transform.SetParent(canvas.transform, false);

            var rt = painel.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(0.08f, 0.04f);
            rt.anchorMax = new Vector2(0.92f, 0.28f);
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;

            var fundo = painel.GetComponent<Image>();
            fundo.sprite = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/UISprite.psd");
            fundo.color = new Color(0.05f, 0.04f, 0.02f, 0.85f);
            fundo.raycastTarget = false;

            var goTexto = new GameObject("Texto", typeof(Text));
            goTexto.transform.SetParent(painel.transform, false);

            var rtTexto = goTexto.GetComponent<RectTransform>();
            rtTexto.anchorMin = Vector2.zero;
            rtTexto.anchorMax = Vector2.one;
            rtTexto.offsetMin = new Vector2(24f, 18f);
            rtTexto.offsetMax = new Vector2(-24f, -18f);

            var texto = goTexto.GetComponent<Text>();
            // Unity 6: a fonte embutida é LegacyRuntime.ttf, e vem por Resources —
            // AssetDatabase.GetBuiltinExtraResource com "Arial.ttf" LANÇA exceção.
            texto.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            texto.fontSize = 20;
            texto.color = new Color(0.93f, 0.89f, 0.72f);
            texto.raycastTarget = false;
            texto.horizontalOverflow = HorizontalWrapMode.Wrap;

            var comp = painel.AddComponent<TutorialHintUI>();
            var so = new SerializedObject(comp);
            so.FindProperty("grupo").objectReferenceValue = painel.GetComponent<CanvasGroup>();
            so.FindProperty("texto").objectReferenceValue = texto;
            so.ApplyModifiedPropertiesWithoutUndo();

            return comp;
        }

        private static void GarantirChegada(Transform raiz, Vector3 pos)
        {
            var t = raiz.Find("Chegada_DoSantuario");
            var go = t != null ? t.gameObject : new GameObject("Chegada_DoSantuario");
            if (t == null) go.transform.SetParent(raiz, false);

            go.transform.position = pos;

            var ponto = go.GetComponent<PontoDeChegada>();
            if (ponto == null) ponto = go.AddComponent<PontoDeChegada>();

            var so = new SerializedObject(ponto);
            var prop = so.FindProperty("identificador");
            if (prop != null) prop.stringValue = IdChegadaNoCastelo;
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void GarantirRefugio(Transform raiz, Vector3 pos, TutorialHintUI caixa)
        {
            var t = raiz.Find("Refugio_DosPortoes");
            var go = t != null ? t.gameObject : new GameObject("Refugio_DosPortoes", typeof(SpriteRenderer));
            if (t == null) go.transform.SetParent(raiz, false);

            go.transform.position = pos;

            var sr = go.GetComponent<SpriteRenderer>();
            if (sr == null) sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/UISprite.psd");
            sr.drawMode = SpriteDrawMode.Sliced;
            sr.size = new Vector2(1.2f, 2.4f);
            sr.color = new Color(0.92f, 0.86f, 0.55f);

            var col = go.GetComponent<CircleCollider2D>();
            if (col == null) col = go.AddComponent<CircleCollider2D>();
            col.isTrigger = true;
            col.radius = 2f;

            var refugio = go.GetComponent<RefugioDeLuz>();
            if (refugio == null) refugio = go.AddComponent<RefugioDeLuz>();

            if (caixa != null)
            {
                var so = new SerializedObject(refugio);
                so.FindProperty("caixaDeTexto").objectReferenceValue = caixa;
                so.ApplyModifiedPropertiesWithoutUndo();
            }
        }

        // ── Ligações externas ────────────────────────────────────────────────

        private static void RegistrarEmBuildSettings()
        {
            var cenas = EditorBuildSettings.scenes.ToList();
            if (cenas.Any(c => c.path == CenaCastelo)) return;

            cenas.Add(new EditorBuildSettingsScene(CenaCastelo, true));
            EditorBuildSettings.scenes = cenas.ToArray();
            Debug.Log("[Castelo] Registrado no Build Settings.");
        }

        /// <summary>
        /// Sem esta ligação o Castelo existiria como cena solta, alcançável só pelo Editor — a
        /// falha exata que o roadmap já registrou em outros pontos: a peça existe e nada leva
        /// até ela.
        /// </summary>
        private static void LigarOPortalNoSantuario()
        {
            if (!System.IO.File.Exists(CenaSantuario))
            {
                Debug.LogWarning("[Castelo] Santuário ausente — nenhum portal foi ligado.");
                return;
            }

            var cena = EditorSceneManager.OpenScene(CenaSantuario, OpenSceneMode.Single);

            var raiz = GameObject.Find("Santuario_Root")?.transform;
            var t = raiz != null ? raiz.Find("Portal_ParaOCastelo") : null;

            var go = t != null ? t.gameObject : new GameObject("Portal_ParaOCastelo", typeof(SpriteRenderer));
            if (t == null && raiz != null) go.transform.SetParent(raiz, false);

            go.transform.position = new Vector3(6.5f, 4.5f, 0f);

            var sr = go.GetComponent<SpriteRenderer>();
            if (sr == null) sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/UISprite.psd");
            sr.drawMode = SpriteDrawMode.Sliced;
            sr.size = new Vector2(2f, 3f);
            sr.color = new Color(0.20f, 0.16f, 0.28f);

            var col = go.GetComponent<BoxCollider2D>();
            if (col == null) col = go.AddComponent<BoxCollider2D>();
            col.isTrigger = true;
            col.size = new Vector2(2f, 3f);

            var portal = go.GetComponent<PortalDeCena>();
            if (portal == null) portal = go.AddComponent<PortalDeCena>();

            portal.DefinirCenaDestino("Castelo_Carcosa");
            portal.DefinirChegada(IdChegadaNoCastelo);
            EditorUtility.SetDirty(portal);

            EditorSceneManager.MarkSceneDirty(cena);
            EditorSceneManager.SaveScene(cena);

            Debug.Log("[Castelo] Portal ligado no Santuário → Castelo_Carcosa.");
        }
    }
}
