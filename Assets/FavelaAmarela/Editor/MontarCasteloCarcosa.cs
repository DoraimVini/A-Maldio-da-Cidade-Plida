using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Tilemaps;
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
        private const string PrefabYugNeth = "Assets/FavelaAmarela/Art/Characters/MiGo/YugNeth.prefab";

        /// <summary>Identificador do <c>PontoDeChegada</c> ao entrar no Castelo.</summary>
        private const string IdChegadaNoCastelo = "PortoesInternos";

        // ── Topologia (do doc de level design, §2) ───────────────────────────
        // Z1 embaixo, o Trono no topo: o jogador sobe o castelo até o Rei.
        private static readonly Vector3 CentroZ1 = new Vector3(0f, -30f, 0f);
        private static readonly Vector3 CentroZ2 = new Vector3(0f, 0f, 0f);
        private static readonly Vector3 CentroZ3 = new Vector3(0f, 30f, 0f);
        private static readonly Vector3 CentroZ5 = new Vector3(0f, 62f, 0f);

        // Caixa envolvente do losango de cada sala, em unidades de mundo: raio r em células dá
        // um losango de 2r de largura por r de altura. Usadas só pelo gatilho de zona, que é um
        // BoxCollider2D — uma caixa sobre um losango sobra nos cantos, e para "você entrou na
        // Biblioteca" isso é aceitável.
        private static readonly Vector2 SalaPequena = new Vector2(RaioSalaPequena * 2f, RaioSalaPequena);
        private static readonly Vector2 SalaGrande = new Vector2(RaioSalaGrande * 2f, RaioSalaGrande);

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
            RemoverOAtalhoDoSantuario();

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

            MontarChaoIsometrico(raiz.transform);

            MontarZ1(raiz.transform, caixa);
            MontarZ2(raiz.transform);
            MontarZ3(raiz.transform);
            MontarZ5(raiz.transform);
            MontarDesfecho(raiz.transform);

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
            var zona = Sala(raiz, "Z1_PortoesInternos", CentroZ1);
            MarcarZona(zona, "Os Portões Internos", SalaPequena);

            GarantirChegada(zona.transform, CentroZ1 + new Vector3(0f, -3f, 0f));
            GarantirPostoDoArtesao(zona.transform, CentroZ1 + new Vector3(5f, 1f, 0f), caixa);
            GarantirRefugio(zona.transform, CentroZ1 + new Vector3(-5f, 0f, 0f), caixa);
        }

        /// <summary>
        /// Z2 — O Salão do Banquete Fossilizado. Hub central: nobres petrificados como cobertura
        /// e <c>CortesaoPalido</c> patrulhando (design §3, Z2).
        /// </summary>
        private static void MontarZ2(Transform raiz)
        {
            var zona = Sala(raiz, "Z2_SalaoDoBanquete", CentroZ2);
            MarcarZona(zona, "O Salão do Banquete Fossilizado", SalaGrande);

            // Nobreza fossilizada: obstáculos que servem de cobertura para o stealth visual.
            var posturas = new[]
            {
                new Vector3(-10f, 2f, 0f), new Vector3(-3f, 5f, 0f), new Vector3(3f, 5f, 0f),
                new Vector3(10f, 2f, 0f), new Vector3(-6f, -4f, 0f), new Vector3(6f, -4f, 0f),
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
            var zona = Sala(raiz, "Z3_BibliotecaEsquecida", CentroZ3);
            MarcarZona(zona, "A Biblioteca Esquecida", SalaGrande);

            // Três espelhos, cada um com sua zona de pressão apontando para si.
            var pontos = new[]
            {
                new Vector3(-10f, 2f, 0f), new Vector3(10f, 2f, 0f), new Vector3(0f, -7f, 0f),
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
            var zona = Sala(raiz, "Z5_TronoDeAldebaran", CentroZ5);
            MarcarZona(zona, "O Trono de Aldebaran", SalaGrande);

            var rei = GarantirRei(zona.transform, CentroZ5 + new Vector3(0f, 5f, 0f));
            if (rei == null) return;

            string[] ids = LerIdsDasReliquias(rei);

            var cantos = new[]
            {
                new Vector3(-10f, -1f, 0f), new Vector3(10f, -1f, 0f),
                new Vector3(0f, -6f, 0f), new Vector3(0f, 4f, 0f),
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

        /// <summary>
        /// Liga o <b>desfecho</b> à vitória sobre o Rei.
        ///
        /// <para><b>O buraco:</b> <c>ReiEmAmareloAI.OnVitoria</c> tinha <b>zero assinantes</b>.
        /// O evento existia com o comentário "quem monta a cena decide o que fazer com isso" e
        /// ninguém decidia — completar o rito, o clímax do Vertical Slice, só repintava o Rei.
        /// Achado auditando a cadeia do rito em 2026-08-20.</para>
        ///
        /// <para>O painel espelha o da <c>SequenciaDeColapso</c>: os dois fins do jogo passam a
        /// ter a mesma forma. <b>A linha do desfecho é provisória</b> e fica serializada no
        /// componente, para o Vini trocar no Inspector sem recompilar.</para>
        /// </summary>
        private static void MontarDesfecho(Transform raiz)
        {
            var rei = Object.FindAnyObjectByType<ReiEmAmareloAI>(FindObjectsInactive.Include);
            if (rei == null)
            {
                Debug.LogWarning("[Castelo] Sem Rei em cena — desfecho não ligado.");
                return;
            }

            var canvas = Object.FindAnyObjectByType<Canvas>(FindObjectsInactive.Include);
            if (canvas == null)
            {
                Debug.LogWarning("[Castelo] Sem Canvas — desfecho não ligado.");
                return;
            }

            // O painel nasce inativo; quem o acende é a coroutine do selamento.
            var t = canvas.transform.Find("Painel_Desfecho");
            var painelGo = t != null ? t.gameObject
                                     : new GameObject("Painel_Desfecho", typeof(CanvasGroup), typeof(Image));
            if (t == null) painelGo.transform.SetParent(canvas.transform, false);

            var rt = painelGo.GetComponent<RectTransform>();
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;

            var fundo = painelGo.GetComponent<Image>();
            if (fundo == null) fundo = painelGo.AddComponent<Image>();
            fundo.color = new Color(0.02f, 0.02f, 0.03f, 1f);

            var grupo = painelGo.GetComponent<CanvasGroup>();
            if (grupo == null) grupo = painelGo.AddComponent<CanvasGroup>();

            var tt = painelGo.transform.Find("Linha");
            var textoGo = tt != null ? tt.gameObject : new GameObject("Linha", typeof(Text));
            if (tt == null) textoGo.transform.SetParent(painelGo.transform, false);

            var rtTexto = textoGo.GetComponent<RectTransform>();
            rtTexto.anchorMin = new Vector2(0.1f, 0.4f);
            rtTexto.anchorMax = new Vector2(0.9f, 0.6f);
            rtTexto.offsetMin = Vector2.zero;
            rtTexto.offsetMax = Vector2.zero;

            var texto = textoGo.GetComponent<Text>();
            if (texto == null) texto = textoGo.AddComponent<Text>();
            // Resources, e "LegacyRuntime.ttf": o nome antigo LANÇA na Unity 6.
            texto.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            texto.fontSize = 28;
            texto.color = new Color(0.92f, 0.86f, 0.55f);
            texto.alignment = TextAnchor.MiddleCenter;
            texto.raycastTarget = false;

            // O componente vive no mesmo GameObject do Rei? Não: numa raiz própria, para
            // sobreviver caso o Rei seja destruído por algum efeito no fim da luta.
            var host = raiz.Find("Desfecho");
            var hostGo = host != null ? host.gameObject : new GameObject("Desfecho");
            if (host == null) hostGo.transform.SetParent(raiz, false);

            var seq = hostGo.GetComponent<SequenciaDeSelamento>();
            if (seq == null) seq = hostGo.AddComponent<SequenciaDeSelamento>();

            var so = new SerializedObject(seq);
            so.FindProperty("rei").objectReferenceValue = rei;
            so.FindProperty("painel").objectReferenceValue = grupo;
            so.FindProperty("texto").objectReferenceValue = texto;
            so.ApplyModifiedPropertiesWithoutUndo();

            painelGo.SetActive(false);

            Debug.Log("[Castelo] Desfecho ligado ao OnVitoria do Rei (a linha de texto é " +
                      "provisória — trocar no Inspector).");
        }

        // ── Chão isométrico ──────────────────────────────────────────────────

        /// <summary>
        /// Raio de cada sala, <b>em células</b>. Um bloco quadrado de células vira um losango
        /// 2:1 em mundo: raio <c>r</c> → losango de <c>2r</c> de largura por <c>r</c> de altura.
        /// </summary>
        private const int RaioSalaPequena = 10;
        private const int RaioSalaGrande = 15;

        /// <summary>Meia-largura do corredor em células — <c>|gx-gy| &lt;= 4</c> dá 4 de vão em mundo.</summary>
        private const int MeiaLarguraDoCorredor = 4;

        /// <summary>
        /// Espessura do anel de colisão. <b>Duas células, não uma:</b> com uma só, um ator rápido
        /// atravessa entre dois <c>FixedUpdate</c> mesmo com <c>Continuous</c> — custou um
        /// playtest na Arena de Testes.
        /// </summary>
        private const int EspessuraDaColisao = 2;

        // Centro de cada sala em células. Andar em (+1,+1) move em +Y no mundo sem mexer em X,
        // então centros na diagonal gx==gy empilham as salas verticalmente: world y = c/2.
        private const int CelulaZ1 = -60;   // world y = -30
        private const int CelulaZ2 = 0;     // world y =   0
        private const int CelulaZ3 = 60;    // world y =  30
        private const int CelulaZ5 = 124;   // world y =  62

        /// <summary>
        /// Pinta o chão do Castelo como <b>losangos isométricos</b> e deriva a colisão da borda
        /// do que foi pintado.
        ///
        /// <para><b>O defeito que isto corrige:</b> a primeira versão desenhava cada sala como um
        /// <c>SpriteRenderer</c> retangular em espaço de mundo. O Castelo era <b>top-down</b>
        /// enquanto Deserto, Santuário e Portões são isométricos — relatado pelo Vini. Num
        /// losango 2:1 uma unidade em Y vale metade de uma em X; um chão retangular mente sobre
        /// distância e profundidade, e a fase inteira lia como outro jogo.</para>
        ///
        /// <para><b>E resolve as portas pela raiz.</b> A colisão é gerada a partir das células de
        /// piso: onde o corredor encosta na sala há piso, logo não há parede. As passagens
        /// existem por construção, em vez de dependerem de eu calcular um vão — que foi
        /// exatamente o cálculo que deixou o jogador lacrado no Z1.</para>
        ///
        /// <para>Mesma receita de <c>BuildSantuarioIsoFloor</c> e <c>MontarArenaDeTestes</c>.</para>
        /// </summary>
        private static void MontarChaoIsometrico(Transform raiz)
        {
            RemoverGeometriaTopDown(raiz);

            var tilePiso = MontarArenaDeTestes.GarantirTileDoLosango();
            var tileColisao = MontarArenaDeTestes.GarantirTileDeColisao();

            var t = raiz.Find("Castelo_Grid");
            var gridGo = t != null ? t.gameObject : new GameObject("Castelo_Grid", typeof(Grid));
            if (t == null) gridGo.transform.SetParent(raiz, false);

            var grid = gridGo.GetComponent<Grid>();
            if (grid == null) grid = gridGo.AddComponent<Grid>();
            grid.cellSize = new Vector3(1f, 0.5f, 1f);
            grid.cellLayout = GridLayout.CellLayout.Isometric;

            var piso = GarantirTilemapFilho(gridGo.transform, "Piso_Castelo", desenha: true);
            piso.ClearAllTiles();

            var celulas = new HashSet<Vector3Int>();

            Losango(celulas, CelulaZ1, RaioSalaPequena);
            Losango(celulas, CelulaZ2, RaioSalaGrande);
            Losango(celulas, CelulaZ3, RaioSalaGrande);
            Losango(celulas, CelulaZ5, RaioSalaGrande);

            LigarSalas(celulas, CelulaZ1, RaioSalaPequena, CelulaZ2, RaioSalaGrande);
            LigarSalas(celulas, CelulaZ2, RaioSalaGrande, CelulaZ3, RaioSalaGrande);
            LigarSalas(celulas, CelulaZ3, RaioSalaGrande, CelulaZ5, RaioSalaGrande);

            foreach (var c in celulas) piso.SetTile(c, tilePiso);

            GerarColisao(gridGo.transform, celulas, tileColisao);

            Debug.Log($"[Castelo] Chão isométrico: {celulas.Count} células de piso.");
        }

        /// <summary>Bloco quadrado de células em torno de (c,c) — losango 2:1 em mundo.</summary>
        private static void Losango(HashSet<Vector3Int> destino, int centro, int raio)
        {
            for (int gx = centro - raio; gx <= centro + raio; gx++)
                for (int gy = centro - raio; gy <= centro + raio; gy++)
                    destino.Add(new Vector3Int(gx, gy, 0));
        }

        /// <summary>
        /// Corredor que liga duas salas. Em mundo, <c>gx+gy</c> é o eixo Y (÷4) e <c>gx-gy</c> é
        /// o eixo X (÷2): fixar <c>|gx-gy|</c> dá uma faixa reta e estreita subindo a fase.
        /// </summary>
        private static void LigarSalas(HashSet<Vector3Int> destino,
                                        int centroA, int raioA, int centroB, int raioB)
        {
            int somaInicio = 2 * (centroA + raioA);
            int somaFim = 2 * (centroB - raioB);

            for (int soma = somaInicio; soma <= somaFim; soma++)
                for (int dif = -MeiaLarguraDoCorredor; dif <= MeiaLarguraDoCorredor; dif++)
                {
                    // gx+gy = soma e gx-gy = dif só têm solução inteira com a mesma paridade.
                    // Sem esta guarda o corredor sai furado em xadrez, e o anel de colisão
                    // transforma cada furo numa pilastra invisível no meio da passagem.
                    if (((soma + dif) & 1) != 0) continue;

                    destino.Add(new Vector3Int((soma + dif) / 2, (soma - dif) / 2, 0));
                }
        }

        /// <summary>
        /// Anel de colisão derivado do piso: toda célula vizinha que <b>não</b> é piso vira
        /// parede. As portas aparecem sozinhas onde o corredor encosta na sala.
        /// </summary>
        private static void GerarColisao(Transform raizDoGrid, HashSet<Vector3Int> piso,
                                          TileBase tileColisao)
        {
            var paredes = new HashSet<Vector3Int>();

            foreach (var c in piso)
                for (int dx = -EspessuraDaColisao; dx <= EspessuraDaColisao; dx++)
                    for (int dy = -EspessuraDaColisao; dy <= EspessuraDaColisao; dy++)
                    {
                        if (dx == 0 && dy == 0) continue;
                        var n = new Vector3Int(c.x + dx, c.y + dy, c.z);
                        if (!piso.Contains(n)) paredes.Add(n);
                    }

            var colisao = GarantirTilemapFilho(raizDoGrid, "Colisao", desenha: false);
            colisao.ClearAllTiles();

            foreach (var w in paredes) colisao.SetTile(w, tileColisao);

            var colisor = colisao.GetComponent<TilemapCollider2D>();
            if (colisor == null) colisor = colisao.gameObject.AddComponent<TilemapCollider2D>();

            ConsolidarColisaoDosTilemaps.Padronizar(colisor);
        }

        private static Tilemap GarantirTilemapFilho(Transform raiz, string nome, bool desenha)
        {
            var t = raiz.Find(nome);
            var go = t != null ? t.gameObject
                               : new GameObject(nome, typeof(Tilemap), typeof(TilemapRenderer));
            if (t == null) go.transform.SetParent(raiz, false);

            var mapa = go.GetComponent<Tilemap>();
            if (mapa == null) mapa = go.AddComponent<Tilemap>();

            var render = go.GetComponent<TilemapRenderer>();
            if (render == null) render = go.AddComponent<TilemapRenderer>();
            render.enabled = desenha;
            render.sortingOrder = -1000;

            return mapa;
        }

        /// <summary>
        /// Apaga o piso e as paredes retangulares da versão top-down. <b>Destrói em vez de
        /// desativar:</b> deixados na cena, os colisores das paredes antigas continuariam
        /// barrando o jogador por cima do chão novo, e o motivo seria invisível no Editor.
        /// </summary>
        private static void RemoverGeometriaTopDown(Transform raiz)
        {
            var condenados = new List<GameObject>();

            foreach (var t in raiz.GetComponentsInChildren<Transform>(true))
            {
                if (t == raiz) continue;

                string n = t.name;
                if (n == "Piso" || n.StartsWith("Parede_") || n.StartsWith("Lateral_")
                    || n.StartsWith("Corredor_"))
                    condenados.Add(t.gameObject);
            }

            foreach (var go in condenados)
                if (go != null) Object.DestroyImmediate(go);

            if (condenados.Count > 0)
                Debug.Log($"[Castelo] {condenados.Count} peça(s) da geometria top-down removida(s).");
        }

        /// <summary>Zona da sala: só o GameObject e a marcação — o chão é do Tilemap.</summary>
        private static GameObject Sala(Transform raiz, string nome, Vector3 centro)
        {
            var t = raiz.Find(nome);
            var go = t != null ? t.gameObject : new GameObject(nome);
            if (t == null) go.transform.SetParent(raiz, false);
            go.transform.position = centro;
            return go;
        }

        /// <summary>
        /// Põe no Z1 a <c>TravessiaDoCompanheiro</c> em <b>modo aposentadoria</b> e o posto onde
        /// Yug-Neth fica.
        ///
        /// <para><b>A virada de papel:</b> ele atravessa para o Castelo com Damião, como faz em
        /// toda cena — mas aqui chega e <b>deixa de ser companheiro</b>, virando o NPC que ensina
        /// o artesanato (decisão do Vini, 2026-08-20). O artesanato em si é conteúdo pós-Vertical
        /// Slice e <b>não</b> foi implementado; o que existe é a virada acontecer em jogo.</para>
        ///
        /// <para><b>Por que no Z1 e não mais adiante:</b> o Z1 é a área segura da fase — chegada,
        /// Refúgio, nenhuma ameaça. É o único lugar do Castelo onde parar para conversar não
        /// compete com o dreno de RM nem com uma patrulha.</para>
        /// </summary>
        private static void GarantirPostoDoArtesao(Transform raiz, Vector3 pos, TutorialHintUI caixa)
        {
            var t = raiz.Find("Posto_Do_Artesao");
            var posto = t != null ? t.gameObject : new GameObject("Posto_Do_Artesao");
            if (t == null) posto.transform.SetParent(raiz, false);
            posto.transform.position = pos;

            var travessia = Object.FindAnyObjectByType<TravessiaDoCompanheiro>(
                FindObjectsInactive.Include);

            if (travessia == null)
            {
                var go = GameObject.Find("Travessia_DoCompanheiro")
                         ?? new GameObject("Travessia_DoCompanheiro");
                go.transform.SetParent(raiz, false);
                travessia = go.GetComponent<TravessiaDoCompanheiro>();
                if (travessia == null) travessia = go.AddComponent<TravessiaDoCompanheiro>();
            }

            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabYugNeth);
            if (prefab == null)
                Debug.LogWarning($"[Castelo] Prefab do Yug-Neth ausente em '{PrefabYugNeth}' — " +
                                 "ele não vai aparecer no Castelo.");

            var so = new SerializedObject(travessia);
            so.FindProperty("prefabYugNeth").objectReferenceValue = prefab;
            so.FindProperty("aposentarAoChegar").boolValue = true;
            so.FindProperty("postoDeArtesao").objectReferenceValue = posto.transform;
            so.FindProperty("caixaDeTexto").objectReferenceValue = caixa;
            so.ApplyModifiedPropertiesWithoutUndo();

            Debug.Log("[Castelo] Yug-Neth entra no Castelo e se aposenta de companheiro (vira " +
                      "o artesão). O artesanato em si é pós-VS e não foi implementado.");
        }

        // ── Peças ────────────────────────────────────────────────────────────

        private static void MarcarZona(GameObject zona, string nome, Vector2 tamanho)
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
            // O tamanho REAL da sala. Em 2026-08-20 eu troquei a assinatura e as chamadas para
            // receber 'tamanho' e esqueci o corpo, que seguiu usando SalaGrande — o parâmetro
            // ficou sem uso e o Z1 seguiu com gatilho de sala grande. Parâmetro não usado não
            // gera erro de compilação; só aparece lendo o corpo.
            col.size = tamanho;
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
            // Ancorada ACIMA do rodapé: a barra de itens e a de ações ocupam de y=48 a y=180
            // (de 1080), e a caixa ia de 0.04 a 0.28 — ou seja, POR CIMA das duas. Era isso o
            // "os diálogos não se encaixam na UI" que o Vini relatou. Frações, e não pixels,
            // para a caixa acompanhar o viewport em qualquer resolução.
            rt.anchorMin = new Vector2(0.08f, 0.20f);
            rt.anchorMax = new Vector2(0.92f, 0.44f);
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
            // ×3: a caixa vive no canvas de referência 1920×1080, e este número
            // vinha da época de 640×360.
            texto.fontSize = 60;
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
        /// <b>Remove</b> o atalho Santuário → Castelo.
        ///
        /// <para><b>Ele existiu por um motivo que acabou.</b> Foi criado em 2026-08-19 porque o
        /// Castelo era uma cena solta, alcançável só pelo Editor — um portal direto era melhor
        /// que nada. Com os Portões das Ruínas em cena (2026-08-20), o caminho verdadeiro passou
        /// a existir: Deserto → Portões → Castelo, como o GDD sempre descreveu.</para>
        ///
        /// <para><b>Por que remover e não só deixar quieto:</b> o atalho pula o Byakhee, e o
        /// Byakhee é a única fonte do Anel do Sinal Amarelo — uma das três relíquias que o rito
        /// do Rei exige. Manter o atalho é manter um caminho que leva ao chefe final <b>sem o que
        /// é preciso para vencê-lo</b>, e que não dá nenhum sinal disso ao jogador. Decisão do
        /// Vini, 2026-08-20.</para>
        ///
        /// <para>A remoção vive aqui, e não numa ferramenta à parte, para ser <b>idempotente</b>:
        /// esta ferramenta é quem criava o atalho, então rodá-la de novo tem que continuar
        /// tirando — e não ressuscitar o que foi decidido remover.</para>
        /// </summary>
        private static void RemoverOAtalhoDoSantuario()
        {
            if (!System.IO.File.Exists(CenaSantuario))
            {
                Debug.LogWarning("[Castelo] Santuário ausente — nada a remover.");
                return;
            }

            var cena = EditorSceneManager.OpenScene(CenaSantuario, OpenSceneMode.Single);

            var raiz = GameObject.Find("Santuario_Root")?.transform;
            var t = raiz != null ? raiz.Find("Portal_ParaOCastelo") : null;

            if (t == null)
            {
                Debug.Log("[Castelo] O atalho do Santuário já não existe.");
                return;
            }

            Object.DestroyImmediate(t.gameObject);

            EditorSceneManager.MarkSceneDirty(cena);
            if (!EditorSceneManager.SaveScene(cena))
            {
                Debug.LogError("[Castelo] Falha ao salvar o Santuário sem o atalho.");
                return;
            }

            Debug.Log("[Castelo] Atalho Santuário → Castelo removido. O caminho agora é " +
                      "Deserto → Portões das Ruínas → Castelo.");
        }
    }
}
