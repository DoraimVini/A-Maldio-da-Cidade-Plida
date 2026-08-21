using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using FavelaAmarela.Core.Persistencia;
using UnityEngine.SceneManagement;
using UnityEngine.Tilemaps;
using FavelaAmarela.CameraSystem;
using FavelaAmarela.Player;
using FavelaAmarela.Runtime.Enemies;
using FavelaAmarela.Runtime.Environment;
using FavelaAmarela.Runtime.GameLoop;
using FavelaAmarela.Runtime.Rendering;
using FavelaAmarela.Runtime.UI;
using UnityEngine.UI;

namespace FavelaAmarela.EditorTools
{
    /// <summary>
    /// Constrói <b>Portões das Ruínas</b> — a arena que fecha a Fase 1, onde o Byakhee é o
    /// cadeado (<c>systems/boss_byakhee.md</c>).
    ///
    /// <para><b>Por que esta cena precisava existir:</b> o Byakhee estava pronto — FSM com 3
    /// fases e 10 testes, ficha calibrada por simulação, prefab com spritesheet real, tabela de
    /// espólio — e <b>em cena nenhuma</b>. <c>ByakheeAI.IniciarLuta()</c> só era chamado pelo
    /// Carcosa Debugger. Como o Anel do Sinal Amarelo é espólio garantido dele e o rito do Rei
    /// exige o Anel, <b>sem esta arena o jogo não era terminável</b>: dava para chegar ao Rei e
    /// não havia como selá-lo.</para>
    ///
    /// <para><b>Chão de losango, não quadrado</b> — mesma receita de <c>MontarArenaDeTestes</c> e
    /// <c>BuildSantuarioIsoFloor</c>. Num losango 2:1 uma unidade em Y vale metade de uma em X;
    /// testar alcance de garras num chão plano engana a leitura de distância.</para>
    ///
    /// <para>Idempotente: reabre a cena se já existir.</para>
    /// </summary>
    public static class MontarPortoesDasRuinas
    {
        private const string CenaPortoes = "Assets/Scenes/Portoes_Das_Ruinas.unity";
        private const string CenaDeserto = "Assets/Scenes/Deserto_Hali.unity";

        private const string PrefabJogador =
            "Assets/FavelaAmarela/Art/Characters/Damiao/Player_Damiao.prefab";
        private const string PrefabHUD = "Assets/FavelaAmarela/Art/UI/HUD_ResilienciaBar.prefab";
        private const string PrefabByakhee = "Assets/FavelaAmarela/Art/Enemies/Byakhee.prefab";
        private const string PrefabYugNeth = "Assets/FavelaAmarela/Art/Characters/MiGo/YugNeth.prefab";

        /// <summary>Onde Damião aparece ao vir do Deserto.</summary>
        private const string IdChegada = "PortoesDasRuinas";

        /// <summary>
        /// Metade do lado do bloco de células. 32 → losango de <b>64 × 32</b> em mundo — o mesmo
        /// da Arena de Testes, que foi <b>dobrado de propósito</b> em 2026-08-13 porque 32 × 16
        /// ficava apertado para o rasante do Byakhee atravessar.
        /// </summary>
        private const int MetadeLadoDoChao = 32;

        // ── Marcos da arena (mundo) ───────────────────────────────────────────
        //
        // O piso NÃO é um retângulo. Com cellLayout Isometric e cellSize (1, 0.5), a célula
        // (gx,gy) cai em x=(gx-gy)/2, y=(gx+gy)/4 — um bloco quadrado de células vira um
        // LOSANGO de 64 × 32 que afina até virar ponta nos extremos de Y. Toda faixa que
        // precisa ser intransponível (o gatilho de luta, os Portões) tem que ser larga o
        // bastante para a altura em que está, senão o jogador passa ao lado dela:
        //
        //   y = -7  → piso de 36 de largura → gatilho de 38
        //   y = 11  → piso de 18            → Portões de 20
        //   y = 13  → piso de 10            → passagem de 6 (essa não precisa barrar)

        private static readonly Vector3 PosChegada = new Vector3(0f, -10f, 0f);
        private static readonly Vector3 PosGatilhoDeLuta = new Vector3(0f, -7f, 0f);
        private static readonly Vector3 PosCentroDaArena = new Vector3(0f, 0f, 0f);
        private static readonly Vector3 PosByakhee = new Vector3(0f, 2f, 0f);
        private static readonly Vector3 PosPortoes = new Vector3(0f, 11f, 0f);
        private static readonly Vector3 PosPassagem = new Vector3(0f, 13f, 0f);
        private static readonly Vector3 PosVoltaAoDeserto = new Vector3(0f, -13f, 0f);

        /// <summary>
        /// A arte dos Portões, a mesma usada no Deserto de Hali. 4,00 × 4,12 un, pivô
        /// no pé. Ver <see cref="GarantirPortoes"/> para por que não é mais o kit Kenney.
        /// </summary>
        private const string SpriteDosPortoes =
            "Assets/FavelaAmarela/Art/Entradas/Entrada_PortoesDeCarcosa.png";

        /// <summary>
        /// Vão que a barreira precisa cobrir. Em y = 11 o losango do piso tem 18 de largura
        /// (62 − 4y), então 18 fecha a travessia de ponta a ponta.
        ///
        /// <para>A arte cobre só 4 desses 18, e tudo bem: o portão é o ponto de passagem, o
        /// resto do vão é encosta que ninguém precisa ver desenhada para não atravessar.</para>
        /// </summary>
        private const float LarguraDaMuralha = 18f;

        private const string TagPontoDeLuz = "PontoDeLuz";

        /// <summary>Fora do eixo central, para não ficar no caminho do rasante.</summary>
        private static readonly Vector3 PosPosteDeLuz = new Vector3(-4f, 8f, 0f);

        private static readonly Color PedraDosPortoes = new Color(0.30f, 0.27f, 0.22f);
        private static readonly Color BrilhoDaPassagem = new Color(0.85f, 0.78f, 0.35f);

        [MenuItem("Tools/FavelaAmarela/Montar Portões das Ruínas")]
        public static void Executar()
        {
            var ativa = EditorSceneManager.GetActiveScene();
            if (ativa.isDirty && !string.IsNullOrEmpty(ativa.path))
                EditorSceneManager.SaveScene(ativa);

            if (!Construir())
            {
                Debug.LogError("[Portões] A cena não foi salva — nada mais foi ligado.");
                return;
            }

            RegistrarEmBuildSettings();
            LigarOPortalNoDeserto();

            AssetDatabase.SaveAssets();
            Debug.Log("[Portões] Cena montada: arena, Byakhee, gatilho de luta e passagem para " +
                      "o Castelo.");
        }

        /// <returns>
        /// <c>true</c> só se a cena existir no disco ao fim. Conferir o retorno de
        /// <c>SaveScene</c> <b>e</b> o disco não é zelo: ao montar o Castelo, o salvamento falhou
        /// em silêncio e o log anunciou sucesso com o arquivo inexistente.
        /// </returns>
        private static bool Construir()
        {
            Scene cena = System.IO.File.Exists(CenaPortoes)
                ? EditorSceneManager.OpenScene(CenaPortoes, OpenSceneMode.Single)
                : EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            MontarChao();

            var jogador = GarantirJogador();
            GarantirCamera(jogador);
            MontarBootstrapDaCena.Garantir();
            GarantirPrefabPorNome(PrefabHUD, "HUD");
            GarantirTempestade();

            var raiz = GameObject.Find("Portoes_Root") ?? new GameObject("Portoes_Root");

            var caixa = GarantirCaixaDeDialogo();

            GarantirChegada(raiz.transform);
            var passagem = GarantirPassagem(raiz.transform);
            var portao = GarantirPortoes(raiz.transform, passagem);
            var volta = GarantirVoltaAoDeserto(raiz.transform);
            var poste = GarantirPosteDeLuz(raiz.transform, caixa);
            var chefe = GarantirByakhee(raiz.transform);
            GarantirGatilhoDaArena(raiz.transform, chefe, portao, poste, volta);
            GarantirCompanheiro(raiz.transform, caixa);

            EditorSceneManager.MarkSceneDirty(cena);

            bool salvou = System.IO.File.Exists(CenaPortoes)
                ? EditorSceneManager.SaveScene(cena)
                : EditorSceneManager.SaveScene(cena, CenaPortoes);

            if (!salvou)
            {
                Debug.LogError($"[Portões] SaveScene recusou salvar em '{CenaPortoes}'.");
                return false;
            }

            AssetDatabase.Refresh();

            if (!System.IO.File.Exists(CenaPortoes))
            {
                Debug.LogError($"[Portões] SaveScene devolveu true mas '{CenaPortoes}' não " +
                               "existe no disco.");
                return false;
            }

            return true;
        }

        // ── Chão ─────────────────────────────────────────────────────────────

        /// <summary>
        /// Losango isométrico com anel de colisão em volta.
        ///
        /// <para><b>O anel tem duas células</b>, não uma: com uma só, um ator rápido — e o
        /// rasante do Byakhee é o caso — atravessa entre dois <c>FixedUpdate</c> mesmo com
        /// <c>Continuous</c>. E o tile do anel é <b>outro</b> tile, com
        /// <c>colliderType Grid</c>: reaproveitar o tile do piso (que é <c>None</c>) faz o
        /// <c>TilemapCollider2D</c> não gerar geometria nenhuma — colisor em cena que não colide
        /// com coisa alguma. Os dois erros já custaram um playtest cada na Arena de Testes; aqui
        /// eles entram corrigidos de nascença.</para>
        /// </summary>
        private static void MontarChao()
        {
            if (GameObject.Find("PortoesFloorGrid") != null) return;

            var tilePiso = MontarArenaDeTestes.GarantirTileDoLosango();
            var tileColisao = MontarArenaDeTestes.GarantirTileDeColisao();

            var gridGO = new GameObject("PortoesFloorGrid", typeof(Grid));
            var grid = gridGO.GetComponent<Grid>();
            grid.cellSize = new Vector3(1f, 0.5f, 1f);
            grid.cellLayout = GridLayout.CellLayout.Isometric;

            var pisoGO = new GameObject("PortoesFloor", typeof(Tilemap), typeof(TilemapRenderer));
            pisoGO.transform.SetParent(gridGO.transform, false);
            pisoGO.GetComponent<TilemapRenderer>().sortingOrder = -1000;

            var piso = pisoGO.GetComponent<Tilemap>();
            for (int gx = -MetadeLadoDoChao; gx < MetadeLadoDoChao; gx++)
                for (int gy = -MetadeLadoDoChao; gy < MetadeLadoDoChao; gy++)
                    piso.SetTile(new Vector3Int(gx, gy, 0), tilePiso);

            var colGO = new GameObject("Colisao", typeof(Tilemap), typeof(TilemapRenderer));
            colGO.transform.SetParent(gridGO.transform, false);
            colGO.GetComponent<TilemapRenderer>().enabled = false;

            var colisao = colGO.GetComponent<Tilemap>();
            const int borda = MetadeLadoDoChao;

            for (int gx = -borda - 2; gx <= borda + 1; gx++)
                for (int gy = -borda - 2; gy <= borda + 1; gy++)
                {
                    bool dentroDoPiso = gx >= -borda && gx < borda && gy >= -borda && gy < borda;
                    if (!dentroDoPiso) colisao.SetTile(new Vector3Int(gx, gy, 0), tileColisao);
                }

            colGO.AddComponent<TilemapCollider2D>();
        }

        // ── Peças da arena ───────────────────────────────────────────────────

        private static void GarantirChegada(Transform raiz)
        {
            var go = GarantirFilho(raiz, "Chegada_DoDeserto");
            go.transform.position = PosChegada;

            var ponto = go.GetComponent<PontoDeChegada>();
            if (ponto == null) ponto = go.AddComponent<PontoDeChegada>();

            var so = new SerializedObject(ponto);
            so.FindProperty("identificador").stringValue = IdChegada;
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        /// <summary>
        /// Os Portões das Ruínas: <b>a mesma arte que já marca os Portões no Deserto de Hali</b>
        /// (<c>Entrada_PortoesDeCarcosa</c>).
        ///
        /// <para><b>O que havia antes e por que caiu (2026-08-20):</b> a versão anterior montava
        /// um portão do kit isométrico da Kenney ladeado por uma fileira de peças de parede do
        /// mesmo kit. O Vini reportou no playtest que ficou "feio e errado", e estava — por
        /// motivos mensuráveis, não de gosto:</para>
        ///
        /// <list type="bullet">
        ///   <item>As peças do kit têm 256 × 512 px a PPU 32, ou seja <b>8 × 16 unidades</b>.
        ///   O Damião tem 2,20. O portão era <b>sete vezes</b> a altura dele.</item>
        ///   <item>Só 229 das 512 linhas têm arte — o resto é margem transparente. Como o pivô é
        ///   central, o "levante" de metade da altura punha a peça no lugar errado por
        ///   construção: o cálculo supunha que a arte preenchia o quadro.</item>
        ///   <item>As peças eram enfileiradas em X puro, com passo igual à largura cheia. Peça
        ///   isométrica não ladrilha assim: para formar parede contínua o passo é meia largura
        ///   em X e um quarto em Y. Em X puro elas nunca iam se encostar.</item>
        /// </list>
        ///
        /// <para><b>A arte do mapa resolve os três de uma vez.</b> Ela tem 128 × 132 px a PPU 32
        /// = <b>4,00 × 4,12 unidades</b> — proporção de portão contra um Damião de 2,20 — e o
        /// pivô já está no pé (alignment 7, <c>0.5, 0</c>), o que dispensa qualquer levante. E
        /// ela já desenha os batentes, a plataforma e o entulho: não sobra parede para ladrilhar,
        /// então a fileira de peças some inteira. Era ela a parte feia.</para>
        ///
        /// <para>O vão continua barrado por um <c>BoxCollider2D</c> largo — a arte cobre 4 de
        /// largura, o piso tem mais que isso, e o resto se barra sem precisar ser desenhado.</para>
        /// </summary>
        private static PortaoDosPortoes GarantirPortoes(Transform raiz, GameObject passagem)
        {
            var arte = AssetDatabase.LoadAssetAtPath<Sprite>(SpriteDosPortoes);

            if (arte == null)
            {
                Debug.LogError($"[Portões] A arte dos Portões não carregou ({SpriteDosPortoes}). " +
                               "Confira se o import está como Sprite.");
                return null;
            }

            var go = GarantirFilho(raiz, "Os_Portoes");
            go.transform.position = PosPortoes;
            go.layer = LayerMask.NameToLayer("Obstacle");

            RemoverMuralhaAntiga(go.transform);

            // Pivô no pé (alignment 7): a posição do objeto JÁ é a base do portão. Sem levante.
            var batente = GarantirPeca(go.transform, "Batente", 0f, 0f, arte);

            var col = go.GetComponent<BoxCollider2D>();
            if (col == null) col = go.AddComponent<BoxCollider2D>();
            col.isTrigger = false;
            col.size = new Vector2(LarguraDaMuralha, 1f);
            col.offset = new Vector2(0f, 0.5f);

            var portao = go.GetComponent<PortaoDosPortoes>();
            if (portao == null) portao = go.AddComponent<PortaoDosPortoes>();

            var so = new SerializedObject(portao);
            so.FindProperty("batente").objectReferenceValue = batente;
            // Um quadro só: o estado se lê pela cor. Ver PortaoDosPortoes.
            so.FindProperty("spriteFechado").objectReferenceValue = arte;
            so.FindProperty("spriteAberto").objectReferenceValue = null;
            so.FindProperty("barreira").objectReferenceValue = col;
            so.FindProperty("passagemParaOCastelo").objectReferenceValue = passagem;
            so.ApplyModifiedPropertiesWithoutUndo();

            Debug.Log($"[Portões] Portão montado com a arte do mapa: " +
                      $"{arte.bounds.size.x:0.00} x {arte.bounds.size.y:0.00} un " +
                      $"(Damião tem 2,20); barreira de {LarguraDaMuralha:0.0} de vão.");

            return portao;
        }

        /// <summary>
        /// Apaga as peças da muralha Kenney de uma cena montada pela versão anterior. Sem isto,
        /// remontar deixaria os blocos de 16 unidades para trás — a ferramenta é idempotente por
        /// reaproveitar objetos pelo nome, e o que ela não conhece ela não toca.
        /// </summary>
        private static void RemoverMuralhaAntiga(Transform portoes)
        {
            for (int i = portoes.childCount - 1; i >= 0; i--)
            {
                var f = portoes.GetChild(i);
                if (f.name.StartsWith("Muralha_"))
                    Object.DestroyImmediate(f.gameObject);
            }
        }

        private static SpriteRenderer GarantirPeca(Transform raiz, string nome, float x, float y,
                                                    Sprite sprite)
        {
            var t = raiz.Find(nome);
            var go = t != null ? t.gameObject : new GameObject(nome, typeof(SpriteRenderer));
            if (t == null) go.transform.SetParent(raiz, false);

            go.transform.localPosition = new Vector3(x, y, 0f);

            var sr = go.GetComponent<SpriteRenderer>();
            if (sr == null) sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = sprite;
            sr.drawMode = SpriteDrawMode.Simple;
            sr.color = Color.white;

            // Y-sort dinâmico: sem isto o Damião passaria sempre por trás (ou sempre pela
            // frente) da muralha, e a profundidade do losango se perde justo na peça mais alta
            // da cena.
            if (go.GetComponent<DynamicYSort>() == null) go.AddComponent<DynamicYSort>();

            return sr;
        }

        /// <summary>
        /// A passagem para o Castelo. Nasce <b>desativada</b> — quem a acende é o
        /// <see cref="ArenaDosPortoes"/> ao abater o chefe.
        /// </summary>
        private static GameObject GarantirPassagem(Transform raiz)
        {
            var go = GarantirFilho(raiz, "Passagem_ParaOCastelo");
            go.transform.position = PosPassagem;

            var sr = go.GetComponent<SpriteRenderer>();
            if (sr == null) sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/UISprite.psd");
            sr.drawMode = SpriteDrawMode.Sliced;
            sr.size = new Vector2(6f, 2f);
            sr.color = BrilhoDaPassagem;

            var col = go.GetComponent<BoxCollider2D>();
            if (col == null) col = go.AddComponent<BoxCollider2D>();
            col.isTrigger = true;
            col.size = new Vector2(6f, 2f);

            LigarPortal(go, "Castelo_Carcosa", "PortoesInternos");

            // Não desativo aqui: quem apaga a passagem no Awake é o PortaoDosPortoes, para o
            // estado inicial ter um dono só. Duas peças desativando o mesmo objeto é como um
            // deles some numa refatoração e ninguém percebe.
            return go;
        }

        /// <summary>
        /// Volta ao Deserto, atrás do gatilho de luta: dá para desistir enquanto não se entrou
        /// na arena. Depois de começar, o caminho é para a frente.
        /// </summary>
        private static GameObject GarantirVoltaAoDeserto(Transform raiz)
        {
            var go = GarantirFilho(raiz, "Volta_AoDeserto");
            go.transform.position = PosVoltaAoDeserto;

            var col = go.GetComponent<BoxCollider2D>();
            if (col == null) col = go.AddComponent<BoxCollider2D>();
            col.isTrigger = true;
            col.size = new Vector2(10f, 2f);

            // "Refugio_PortoesDasRuinas", e não "PortoesDasRuinas": aquele id existe no
            // Deserto (o Refúgio ao lado do marco dos Portões); este NÃO existia lá. Um
            // chegarEm sem ponto correspondente não dá erro nenhum — o PontoDeChegada.Pendente
            // fica setado, ninguém consome, e o jogador cai na posição padrão da cena, longe
            // dos Portões. Achado auditando o casamento chegarEm × PontoDeChegada.
            LigarPortal(go, "Deserto_Hali", "Refugio_PortoesDasRuinas");
            return go;
        }

        private static PortalDeCena LigarPortal(GameObject go, string cenaDestino, string chegarEm)
        {
            var portal = go.GetComponent<PortalDeCena>();
            if (portal == null) portal = go.AddComponent<PortalDeCena>();

            var so = new SerializedObject(portal);
            so.FindProperty("cenaDestino").stringValue = cenaDestino;
            so.FindProperty("chegarEm").stringValue = chegarEm;
            so.ApplyModifiedPropertiesWithoutUndo();

            return portal;
        }

        private static ByakheeAI GarantirByakhee(Transform raiz)
        {
            var existente = Object.FindAnyObjectByType<ByakheeAI>(FindObjectsInactive.Include);
            GameObject go;

            if (existente != null)
            {
                go = existente.gameObject;
            }
            else
            {
                var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabByakhee);
                if (prefab == null)
                {
                    Debug.LogError($"[Portões] Prefab do Byakhee ausente em '{PrefabByakhee}'.");
                    return null;
                }

                go = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
                go.transform.SetParent(raiz, false);
            }

            go.transform.position = PosByakhee;

            // O centro da arena é um objeto próprio: o ByakheeAI orbita em torno dele, e usar o
            // transform do próprio chefe faria a órbita seguir quem se move.
            var centro = GarantirFilho(raiz, "Centro_DaArena");
            centro.transform.position = PosCentroDaArena;

            var ai = go.GetComponent<ByakheeAI>();
            if (ai == null)
            {
                Debug.LogError("[Portões] O prefab do Byakhee não tem ByakheeAI.");
                return null;
            }

            var so = new SerializedObject(ai);
            so.FindProperty("centroDaArena").objectReferenceValue = centro.transform;
            // Os números de combate ficam nos defaults calibrados por simulação
            // (boss_byakhee.md, tabela de balanceamento). Não mexer sem refazer a simulação.
            so.ApplyModifiedPropertiesWithoutUndo();

            return ai;
        }

        /// <summary>
        /// O gatilho que desperta o Byakhee e abre os Portões — a peça que o
        /// <c>ByakheeAI.HandleDerrotado</c> nomeia como responsável pela abertura.
        /// </summary>
        private static void GarantirGatilhoDaArena(Transform raiz, ByakheeAI chefe,
                                                    PortaoDosPortoes portao, RefugioDeLuz poste,
                                                    GameObject volta)
        {
            var go = GarantirFilho(raiz, "Gatilho_DaArena");
            go.transform.position = PosGatilhoDeLuta;

            var col = go.GetComponent<BoxCollider2D>();
            if (col == null) col = go.AddComponent<BoxCollider2D>();
            col.isTrigger = true;
            col.size = new Vector2(38f, 1.5f);

            var arena = go.GetComponent<ArenaDosPortoes>();
            if (arena == null) arena = go.AddComponent<ArenaDosPortoes>();

            var so = new SerializedObject(arena);
            so.FindProperty("chefe").objectReferenceValue = chefe;
            so.FindProperty("portao").objectReferenceValue = portao;
            so.FindProperty("refugio").objectReferenceValue = poste;
            so.FindProperty("voltaAoDeserto").objectReferenceValue = volta;
            so.FindProperty("luzDoPoste").objectReferenceValue =
                poste != null ? poste.GetComponent<SpriteRenderer>() : null;
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        /// <summary>
        /// O Poste de Luz que acende ao fim da luta.
        ///
        /// <para><b>É a resposta ao Yug-Neth.</b> O GDD fazia dele a "chave dimensional dos
        /// Portões", o que somava mais um bloqueio à fase. Trocado por uma recompensa: o
        /// <c>RefugioDeLuz</c> já ancora a Resiliência, cura, <b>grava a partida</b> e
        /// <b>reanima o companheiro</b> — então vencer o Byakhee é o que devolve o Yug-Neth de
        /// pé, em vez de exigi-lo de pé para passar.</para>
        ///
        /// <para>Recebe a tag <c>PontoDeLuz</c> e um <c>PontoDeChegada</c> irmão, como os três
        /// Refúgios do Deserto: sem o ponto irmão o renascimento cai na posição padrão da cena
        /// em vez de sob a luz — e o próprio <c>RefugioDeLuz</c> avisa isso em execução.</para>
        /// </summary>
        private static RefugioDeLuz GarantirPosteDeLuz(Transform raiz, TutorialHintUI caixa)
        {
            var go = GarantirFilho(raiz, "Refugio_DosPortoes");
            go.transform.position = PosPosteDeLuz;

            if (TagExiste(TagPontoDeLuz)) go.tag = TagPontoDeLuz;

            var sr = go.GetComponent<SpriteRenderer>();
            if (sr == null) sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/UISprite.psd");
            sr.drawMode = SpriteDrawMode.Sliced;
            sr.size = new Vector2(0.8f, 2.6f);

            // Nasce apagado. Quem pinta de aceso é o ArenaDosPortoes, no abate — um dono só
            // para o estado inicial.
            sr.color = new Color(0.22f, 0.21f, 0.20f);

            if (go.GetComponent<DynamicYSort>() == null) go.AddComponent<DynamicYSort>();

            // Círculo, não retângulo: a luz de um poste é radial, e o volume deve casar com o
            // que o jogador enxerga (mesma razão do MontarRefugiosDeLuz).
            var col = go.GetComponent<CircleCollider2D>();
            if (col == null) col = go.AddComponent<CircleCollider2D>();
            col.isTrigger = true;
            col.radius = 1.8f;

            var ponto = go.GetComponent<PontoDeChegada>();
            if (ponto == null) ponto = go.AddComponent<PontoDeChegada>();
            var soPonto = new SerializedObject(ponto);
            soPonto.FindProperty("identificador").stringValue = "Refugio_DosPortoes";
            soPonto.ApplyModifiedPropertiesWithoutUndo();

            var refugio = go.GetComponent<RefugioDeLuz>();
            if (refugio == null) refugio = go.AddComponent<RefugioDeLuz>();

            if (caixa != null)
            {
                var so = new SerializedObject(refugio);
                so.FindProperty("caixaDeTexto").objectReferenceValue = caixa;
                so.ApplyModifiedPropertiesWithoutUndo();
            }

            return refugio;
        }

        private static bool TagExiste(string tag)
            => UnityEditorInternal.InternalEditorUtility.tags.Contains(tag);

        /// <summary>
        /// Caixa de texto da cena, montada <b>inline</b> e não por <c>MontarCaixaDeDialogo</c>:
        /// aquela ferramenta percorre as cenas jogáveis com <c>OpenScene(..., Single)</c>, o que
        /// FECHA esta cena recém-criada e ainda não salva. Foi exatamente o que aconteceu ao
        /// montar o Castelo — o handle morreu e o <c>SaveScene</c> recusou salvar, em silêncio.
        /// </summary>
        private static TutorialHintUI GarantirCaixaDeDialogo()
        {
            var existente = Object.FindAnyObjectByType<TutorialHintUI>(FindObjectsInactive.Include);
            if (existente != null) return existente;

            var canvas = Object.FindAnyObjectByType<Canvas>(FindObjectsInactive.Include);
            if (canvas == null)
            {
                var goCanvas = new GameObject("Canvas_Portoes",
                    typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
                canvas = goCanvas.GetComponent<Canvas>();
                canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            }

            var painel = new GameObject("CaixaDeDialogo", typeof(CanvasGroup), typeof(Image));
            painel.transform.SetParent(canvas.transform, false);

            var rt = painel.GetComponent<RectTransform>();
            // Ancorada ACIMA do rodapé: a barra de itens e a de ações ocupam de y=48 a y=180
            // (de 1080), e a caixa ia de 0.04 a 0.28 — ou seja, POR CIMA das duas. Era isso o
            // "os diálogos não se encaixam na UI" que o Vini relatou. Frações, e não pixels,
            // para a caixa acompanhar o viewport em qualquer resolução.
            rt.anchorMin = new Vector2(0.08f, 0.20f);
            rt.anchorMax = new Vector2(0.92f, 0.44f);
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
            painel.GetComponent<Image>().color = new Color(0.05f, 0.05f, 0.07f, 0.88f);

            var goTexto = new GameObject("Texto", typeof(Text));
            goTexto.transform.SetParent(painel.transform, false);
            var rtTexto = goTexto.GetComponent<RectTransform>();
            rtTexto.anchorMin = new Vector2(0.04f, 0.08f);
            rtTexto.anchorMax = new Vector2(0.96f, 0.92f);
            rtTexto.offsetMin = Vector2.zero;
            rtTexto.offsetMax = Vector2.zero;

            var texto = goTexto.GetComponent<Text>();
            // LegacyRuntime.ttf, não "Arial.ttf": na Unity 6 o nome antigo não só foi
            // removido como faz a busca LANÇAR ArgumentException — e a fonte vem por
            // Resources, não por AssetDatabase. Já é
            // conhecido no projeto (FonteBuiltinTests) e mesmo assim eu digitei o antigo.
            texto.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            // ×3: a caixa vive no canvas de referência 1920×1080, e este número
            // vinha da época de 640×360.
            texto.fontSize = 66;
            texto.color = new Color(0.92f, 0.88f, 0.75f);
            texto.alignment = TextAnchor.MiddleLeft;

            var comp = painel.AddComponent<TutorialHintUI>();
            var so = new SerializedObject(comp);
            so.FindProperty("grupo").objectReferenceValue = painel.GetComponent<CanvasGroup>();
            so.FindProperty("texto").objectReferenceValue = texto;
            so.ApplyModifiedPropertiesWithoutUndo();

            return comp;
        }

        /// <summary>
        /// Traz Yug-Neth para a arena.
        ///
        /// <para><b>Ele não entrava.</b> A <c>TravessiaDoCompanheiro</c> estava no Deserto, no
        /// Santuário e no Castelo — e ausente daqui, porque montei esta cena sem ela. Relatado
        /// pelo Vini no playtest da luta.</para>
        ///
        /// <para>Sem <c>aposentarAoChegar</c>: nos Portões ele ainda é companheiro. A
        /// aposentadoria acontece só na entrada do Castelo.</para>
        /// </summary>
        private static void GarantirCompanheiro(Transform raiz, TutorialHintUI caixa)
        {
            var travessia = Object.FindAnyObjectByType<TravessiaDoCompanheiro>(
                FindObjectsInactive.Include);

            if (travessia == null)
            {
                var t = raiz.Find("Travessia_DoCompanheiro");
                var go = t != null ? t.gameObject : new GameObject("Travessia_DoCompanheiro");
                if (t == null) go.transform.SetParent(raiz, false);
                travessia = go.GetComponent<TravessiaDoCompanheiro>();
                if (travessia == null) travessia = go.AddComponent<TravessiaDoCompanheiro>();
            }

            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabYugNeth);
            if (prefab == null)
                Debug.LogWarning($"[Portões] Prefab do Yug-Neth ausente em '{PrefabYugNeth}'.");

            var so = new SerializedObject(travessia);
            so.FindProperty("prefabYugNeth").objectReferenceValue = prefab;
            so.FindProperty("aposentarAoChegar").boolValue = false;
            so.FindProperty("caixaDeTexto").objectReferenceValue = caixa;
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        // ── Infraestrutura de cena ───────────────────────────────────────────

        private static GameObject GarantirFilho(Transform raiz, string nome)
        {
            var t = raiz.Find(nome);
            if (t != null) return t.gameObject;

            var go = new GameObject(nome);
            go.transform.SetParent(raiz, false);
            return go;
        }

        /// <summary>
        /// Instancia Damião e acrescenta o que o prefab <b>não</b> traz.
        ///
        /// <para><c>ArtefatosBridge</c> e <c>GerenciadorDeVigor</c> são adicionados por wiring
        /// nas cenas, não pelo prefab. Sem o Vigor a Esquiva não tem recurso para cobrar — e
        /// esquivar é a única defesa em luta de chefe. A Arena de Testes descobriu isso do jeito
        /// caro, num playtest.</para>
        /// </summary>
        private static GameObject GarantirJogador()
        {
            var existente = GameObject.FindGameObjectWithTag("Player");
            GameObject go;

            if (existente != null)
            {
                go = existente;
            }
            else
            {
                var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabJogador);
                if (prefab == null)
                {
                    Debug.LogError($"[Portões] Prefab do jogador ausente em '{PrefabJogador}'.");
                    return null;
                }

                go = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
            }

            go.transform.position = PosChegada;

            if (go.GetComponent<ArtefatosBridge>() == null) go.AddComponent<ArtefatosBridge>();
            if (go.GetComponent<GerenciadorDeVigor>() == null) go.AddComponent<GerenciadorDeVigor>();

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
            cam.orthographicSize = 7f; // um pouco mais aberta que o Castelo: a luta é aérea
            cam.backgroundColor = new Color(0.07f, 0.06f, 0.08f);
            cam.transform.rotation = Quaternion.identity; // sem tilt — favela-isometric-standards
            cam.transform.position = new Vector3(0f, 0f, -10f);

            var ctrl = cam.GetComponent<IsometricCameraController>();
            if (ctrl == null) ctrl = cam.gameObject.AddComponent<IsometricCameraController>();

            if (jogador != null)
            {
                var so = new SerializedObject(ctrl);
                so.FindProperty("target").objectReferenceValue = jogador.transform;
                so.FindProperty("orthographicSize").floatValue = 7f;
                so.ApplyModifiedPropertiesWithoutUndo();
            }
        }

        private static void GarantirPrefabPorNome(string caminho, string nome)
        {
            if (GameObject.Find(nome) != null) return;

            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(caminho);
            if (prefab == null)
            {
                Debug.LogWarning($"[Portões] Prefab '{caminho}' não encontrado.");
                return;
            }

            var go = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
            go.name = nome;
        }

        /// <summary>
        /// Os Portões ficam no Deserto de Hali, então <b>há</b> tempestade aqui — ao contrário do
        /// Castelo. O driver precisa existir: sem ele o <c>EnvironmentState</c> fica no valor
        /// inicial e a arena teria clima por acidente, não por escolha.
        /// </summary>
        private static void GarantirTempestade()
        {
            var driver = Object.FindAnyObjectByType<TempestadeAmbiente>(FindObjectsInactive.Include);
            if (driver == null)
                driver = new GameObject("Tempestade").AddComponent<TempestadeAmbiente>();

            var so = new SerializedObject(driver);
            so.FindProperty("minimoInicial").floatValue = 0.2f;
            so.FindProperty("maximoInicial").floatValue = 0.6f;
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        // ── Ligações externas ────────────────────────────────────────────────

        private static void RegistrarEmBuildSettings()
        {
            var cenas = EditorBuildSettings.scenes.ToList();
            if (cenas.Any(c => c.path == CenaPortoes)) return;

            cenas.Add(new EditorBuildSettingsScene(CenaPortoes, true));
            EditorBuildSettings.scenes = cenas.ToArray();
            Debug.Log("[Portões] Registrado no Build Settings.");
        }

        /// <summary>
        /// Põe um <c>PortalDeCena</c> no marco <c>Portoes_DasRuinas</c> do Deserto, que até agora
        /// era <b>pura decoração</b> — Transform, SpriteRenderer e <c>DynamicYSort</c>, sem
        /// colisor e sem portal. O jogador passava ao lado dos Portões e nada acontecia.
        /// </summary>
        private static void LigarOPortalNoDeserto()
        {
            if (!System.IO.File.Exists(CenaDeserto))
            {
                Debug.LogWarning($"[Portões] '{CenaDeserto}' não existe — portal não ligado.");
                return;
            }

            var cena = EditorSceneManager.OpenScene(CenaDeserto, OpenSceneMode.Single);

            var marco = GameObject.Find("Portoes_DasRuinas");
            if (marco == null)
            {
                Debug.LogWarning("[Portões] Marco 'Portoes_DasRuinas' não achado no Deserto.");
                return;
            }

            // Gatilho como FILHO, não no próprio marco: o marco tem SpriteRenderer e
            // DynamicYSort, e pôr um trigger grande nele mudaria o que o Y-sort mede.
            var t = marco.transform.Find("Entrada_DosPortoes");
            var go = t != null ? t.gameObject : new GameObject("Entrada_DosPortoes");
            if (t == null) go.transform.SetParent(marco.transform, false);
            go.transform.localPosition = Vector3.zero;

            var col = go.GetComponent<BoxCollider2D>();
            if (col == null) col = go.AddComponent<BoxCollider2D>();
            col.isTrigger = true;
            col.size = new Vector2(3f, 2f);

            var portal = LigarPortal(go, "Portoes_Das_Ruinas", IdChegada);

            // A Tumba passa a ser obrigatória (decisão do Vini, 2026-08-20): é lá que Yug-Neth
            // é libertado de Abdul, e sem ele o jogador chega ao Byakhee sem arma e sem
            // companheiro, e ao Castelo sem o NPC que ensina o artesanato. Trancar aqui é mais
            // barato que espalhar conteúdo alternativo por duas cenas.
            if (portal != null)
            {
                var sp = new SerializedObject(portal);
                sp.FindProperty("chaveExigida").stringValue = ChavesDeSave.AbdulResolvido;
                sp.FindProperty("caixaDeTexto").objectReferenceValue =
                    Object.FindAnyObjectByType<TutorialHintUI>(FindObjectsInactive.Include);
                sp.ApplyModifiedPropertiesWithoutUndo();

                if (sp.FindProperty("caixaDeTexto").objectReferenceValue == null)
                    Debug.LogWarning("[Portões] Portal trancado sem caixa de texto no Deserto: " +
                                     "o jogador esbarraria sem explicação. Rode o Build do HUD " +
                                     "no Deserto antes desta ferramenta.");
            }

            EditorSceneManager.MarkSceneDirty(cena);
            if (!EditorSceneManager.SaveScene(cena))
            {
                Debug.LogError("[Portões] Falha ao salvar o Deserto com o portal.");
                return;
            }

            Debug.Log($"[Portões] Portal ligado no Deserto → Portoes_Das_Ruinas, trancado por " +
                      $"'{ChavesDeSave.AbdulResolvido}' (a Tumba é obrigatória).");
        }
    }
}
