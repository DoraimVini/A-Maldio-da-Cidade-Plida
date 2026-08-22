using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using FavelaAmarela.Runtime.GameLoop;
using FavelaAmarela.Runtime.UI;

namespace FavelaAmarela.EditorTools
{
    /// <summary>
    /// Ferramenta de Editor. Cria a <b>cena do menu principal</b> e a coloca no <b>índice 0</b>
    /// do build.
    ///
    /// <para><b>Dois problemas resolvidos de uma vez (2026-08-11):</b></para>
    /// <list type="number">
    ///   <item>O menu era overlay repetido nas 3 cenas de jogo. Isso obrigava a carregar o
    ///   Deserto inteiro — tempestade, inimigos, tilemaps — só para cobrir tudo com uma tela
    ///   preta, e a congelar o tempo porque o mundo ficava vivo por trás.</item>
    ///   <item>O índice 0 do build era a <c>SampleScene</c>, um blockout de protótipo
    ///   abandonado. Um build gerado hoje <b>abriria nele</b>: sala vazia, sem jogador nem
    ///   HUD. Passou despercebido porque no Editor sempre se dá Play numa cena já aberta.</item>
    /// </list>
    ///
    /// <para>Idempotente: refaz a cena do zero e reordena o build sem duplicar entradas.</para>
    /// </summary>
    public static class MontarCenaDeMenu
    {
        private const string CaminhoDaCena = "Assets/Scenes/Cena_Menu.unity";
        private const string SampleScene = "Assets/Scenes/SampleScene.unity";

        private static readonly Color Amarelo = new Color(0.92f, 0.86f, 0.55f, 0.92f);
        private static readonly Color AmareloFraco = new Color(0.85f, 0.82f, 0.62f, 0.5f);

        [MenuItem("Tools/FavelaAmarela/Montar cena de menu (e corrigir o build)")]
        public static void Executar()
        {
            var atual = EditorSceneManager.GetActiveScene();
            if (atual.isDirty && !string.IsNullOrEmpty(atual.path))
                EditorSceneManager.SaveScene(atual);

            var cena = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            Montar();

            EditorSceneManager.MarkSceneDirty(cena);
            EditorSceneManager.SaveScene(cena, CaminhoDaCena);

            AjustarBuildSettings();

            // Encadeado de propósito (2026-08-22). Este montador constrói os botões com o
            // sprite EMBUTIDO da Unity; quem depois troca pela moldura do Dark Ages UI é o
            // AplicarCaraDaInterface. Sem esta chamada, toda reexecução daqui devolvia o menu
            // aos retângulos chapados — foi o que aconteceu hoje, e quem pegou foi o guarda
            // CenaMenu_UsaAMolduraDoDarkAgesUI, não o log (que reportou sucesso).
            //
            // Mesmo padrão que o BuildHUDCompleto já usa para o PainelDeFicha e a caixa de
            // diálogo: a dependência vira estrutural em vez de virar um passo que alguém
            // precisa lembrar de repetir.
            AplicarCaraDaInterface.Aplicar();

            Debug.Log($"[CenaDeMenu] Pronto. '{CaminhoDaCena}' criada e posta no índice 0 do " +
                      "build; a SampleScene saiu. O jogo agora abre no menu. " +
                      "Cara da interface reaplicada em seguida.");
        }

        private static void Montar()
        {
            new GameObject("EventSystem", typeof(EventSystem), typeof(InputSystemUIInputModule));

            var camera = new GameObject("Camera", typeof(Camera)).GetComponent<Camera>();
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = new Color(0.03f, 0.025f, 0.02f);
            camera.orthographic = true;

            var canvasGo = new GameObject("Canvas",
                typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            var canvas = canvasGo.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;

            var escala = canvasGo.GetComponent<CanvasScaler>();
            escala.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            escala.referenceResolution = new Vector2(1920f, 1080f);

            var raiz = Overlay(canvas.transform, "Menu", new Color(0.03f, 0.025f, 0.02f, 1f));

            Texto(raiz.transform, "Titulo", "CAMINHO PARA CARCOSA",
                // 0,70..0,87 => 183 px. A 132 pt a linha ocupa ~152 px; com 0,82 (129,6 px)
                // o titulo do jogo era truncado e nao aparecia.
                new Vector2(0.1f, 0.70f), new Vector2(0.9f, 0.87f), 132, TextAnchor.MiddleCenter, Amarelo);

            var continuar = Botao(raiz.transform, "Botao_Continuar", "Continuar", 0.55f);
            var nova = Botao(raiz.transform, "Botao_NovaPartida", "Nova peregrinação", 0.43f);
            var sair = Botao(raiz.transform, "Botao_Sair", "Sair", 0.31f);

            var confirmacao = MontarConfirmacao(canvas.transform);

            var comp = raiz.AddComponent<MenuPrincipal>();
            var so = new SerializedObject(comp);
            so.FindProperty("botaoContinuar").objectReferenceValue = continuar;
            so.FindProperty("botaoNovaPartida").objectReferenceValue = nova;
            so.FindProperty("botaoSair").objectReferenceValue = sair;
            so.FindProperty("painelDeConfirmacao").objectReferenceValue = confirmacao.painel;
            so.FindProperty("botaoConfirmar").objectReferenceValue = confirmacao.confirmar;
            so.FindProperty("botaoCancelar").objectReferenceValue = confirmacao.cancelar;
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        private static (GameObject painel, Button confirmar, Button cancelar) MontarConfirmacao(Transform pai)
        {
            var painel = Overlay(pai, "Confirmacao", new Color(0.05f, 0.04f, 0.03f, 0.97f));

            Texto(painel.transform, "Aviso", "Isso apaga o progresso. Continuar?",
                new Vector2(0.1f, 0.56f), new Vector2(0.9f, 0.70f), 66, TextAnchor.MiddleCenter, Amarelo);

            var confirmar = Botao(painel.transform, "Botao_Confirmar", "Apagar e recomeçar", 0.44f);
            var cancelar = Botao(painel.transform, "Botao_Cancelar", "Voltar", 0.32f);

            painel.SetActive(false);
            return (painel, confirmar, cancelar);
        }

        /// <summary>
        /// Põe a cena do menu no índice 0 e tira a SampleScene. O índice 0 é a cena que o
        /// jogo carrega ao executar — é a diferença entre abrir no menu e abrir num blockout
        /// de protótipo.
        /// </summary>
        private static void AjustarBuildSettings()
        {
            var cenas = EditorBuildSettings.scenes
                .Where(c => c.path != CaminhoDaCena && c.path != SampleScene)
                .ToList();

            cenas.Insert(0, new EditorBuildSettingsScene(CaminhoDaCena, true));

            EditorBuildSettings.scenes = cenas.ToArray();

            Debug.Log("[CenaDeMenu] Build settings: " +
                      string.Join(" | ", cenas.Select((c, i) => $"{i}={System.IO.Path.GetFileNameWithoutExtension(c.path)}")));
        }

        // ── Peças ─────────────────────────────────────────────────────────────

        private static void Esticar(RectTransform rt)
        {
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
        }

        private static GameObject Overlay(Transform pai, string nome, Color cor)
        {
            var go = new GameObject(nome, typeof(Image));
            go.transform.SetParent(pai, false);
            Esticar(go.GetComponent<RectTransform>());

            var img = go.GetComponent<Image>();
            img.sprite = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/UISprite.psd");
            img.type = Image.Type.Sliced;
            img.color = cor;

            return go;
        }

        private static Button Botao(Transform pai, string nome, string rotulo, float alturaCentro)
        {
            var go = new GameObject(nome, typeof(Image), typeof(Button));
            go.transform.SetParent(pai, false);

            // Geometria recalculada em 2026-08-21 a partir da resolucao de referencia do
            // canvas (1920 x 1080), e nao por tentativa e erro.
            //
            // ALTURA: meia-altura 0,046 => 0,092 x 1080 = 99 px. Uma linha de 66 pt ocupa
            // ~76 px, entao sobram ~23 px de folga. Antes era 0,035 => 75,6 px, MENOR que a
            // propria linha -- e com VerticalOverflow em Truncate a Unity simplesmente
            // ESCONDIA o texto. Foi por isso que os botoes apareciam vazios.
            //
            // LARGURA: 0,30..0,70 => 768 px. O rotulo mais longo ("Nova peregrinacao", 17
            // caracteres a ~33 px cada) mede ~560 px. Antes era 0,36..0,64 => 537 px, estreito
            // demais: o texto quebrava em duas linhas, dobrava para ~152 px de altura e era
            // truncado de novo.
            var rt = go.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(0.30f, alturaCentro - 0.046f);
            rt.anchorMax = new Vector2(0.70f, alturaCentro + 0.046f);
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;

            var img = go.GetComponent<Image>();
            img.sprite = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/UISprite.psd");
            img.type = Image.Type.Sliced;
            img.color = new Color(0.85f, 0.80f, 0.60f, 0.14f);

            Texto(go.transform, "Rotulo", rotulo, Vector2.zero, Vector2.one, 66,
                TextAnchor.MiddleCenter, Amarelo);

            return go.GetComponent<Button>();
        }

        private static Text Texto(Transform pai, string nome, string conteudo,
            Vector2 ancoraMin, Vector2 ancoraMax, int tamanho, TextAnchor alinhamento, Color cor)
            // Os tamanhos passados aqui foram multiplicados por 3 em 2026-08-20: o canvas do
            // menu tem referencia 1920x1080 e estes numeros vinham da epoca de 640x360, entao
            // as letras saiam a um terco do pretendido. Mesma correcao do BuildHUDCompleto.
        {
            var go = new GameObject(nome, typeof(Text));
            go.transform.SetParent(pai, false);

            var rt = go.GetComponent<RectTransform>();
            rt.anchorMin = ancoraMin;
            rt.anchorMax = ancoraMax;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;

            var texto = go.GetComponent<Text>();
            texto.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            texto.text = conteudo;
            texto.fontSize = tamanho;
            texto.alignment = alinhamento;
            texto.color = cor;
            texto.raycastTarget = false;

            // Rede de seguranca: com VerticalOverflow em Truncate (o padrao), texto que nao
            // cabe na caixa e simplesmente ESCONDIDO -- sem erro, sem aviso, sem nada. Foi
            // assim que o titulo e os cinco botoes do menu ficaram vazios depois que as fontes
            // triplicaram e as caixas nao acompanharam. Em Overflow o texto vaza para fora e
            // fica FEIO, que e infinitamente melhor que ficar invisivel: feio se ve e se
            // conserta, invisivel passa para a build.
            texto.verticalOverflow = VerticalWrapMode.Overflow;

            return texto;
        }
    }
}
