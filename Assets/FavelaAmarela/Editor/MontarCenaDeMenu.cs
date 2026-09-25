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

            Debug.Log($"[CenaDeMenu] Pronto. '{CaminhoDaCena}' criada e posta no índice 0 do " +
                      "build; a SampleScene saiu. O jogo agora abre no menu.");
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
                new Vector2(0.1f, 0.7f), new Vector2(0.9f, 0.82f), 44, TextAnchor.MiddleCenter, Amarelo);

            var continuar = Botao(raiz.transform, "Botao_Continuar", "Continuar", 0.52f);
            var nova = Botao(raiz.transform, "Botao_NovaPartida", "Nova peregrinação", 0.42f);
            var sair = Botao(raiz.transform, "Botao_Sair", "Sair", 0.32f);

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
                new Vector2(0.1f, 0.54f), new Vector2(0.9f, 0.64f), 22, TextAnchor.MiddleCenter, Amarelo);

            var confirmar = Botao(painel.transform, "Botao_Confirmar", "Apagar e recomeçar", 0.44f);
            var cancelar = Botao(painel.transform, "Botao_Cancelar", "Voltar", 0.34f);

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

            var rt = go.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(0.36f, alturaCentro - 0.035f);
            rt.anchorMax = new Vector2(0.64f, alturaCentro + 0.035f);
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;

            var img = go.GetComponent<Image>();
            img.sprite = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/UISprite.psd");
            img.type = Image.Type.Sliced;
            img.color = new Color(0.85f, 0.80f, 0.60f, 0.14f);

            Texto(go.transform, "Rotulo", rotulo, Vector2.zero, Vector2.one, 22,
                TextAnchor.MiddleCenter, Amarelo);

            return go.GetComponent<Button>();
        }

        private static Text Texto(Transform pai, string nome, string conteudo,
            Vector2 ancoraMin, Vector2 ancoraMax, int tamanho, TextAnchor alinhamento, Color cor)
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

            return texto;
        }
    }
}
