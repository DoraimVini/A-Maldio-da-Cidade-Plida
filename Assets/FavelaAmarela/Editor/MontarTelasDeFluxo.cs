using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.UI;
using FavelaAmarela.Player;
using FavelaAmarela.Runtime.GameLoop;
using FavelaAmarela.Runtime.UI;

namespace FavelaAmarela.EditorTools
{
    /// <summary>
    /// Ferramenta de Editor. Constrói as <b>telas do fluxo de jogo</b> — Pause, Colapso e Menu
    /// — e as liga ao <see cref="GameManager"/>.
    ///
    /// <para><b>O que motivou (auditoria 2026-08-11):</b> a lógica de fluxo já existia inteira
    /// (<c>GameState</c>, a máquina de estados, Esc alternando pause, a `SequenciaDeColapso`),
    /// mas os campos <c>telaPause</c>, <c>gameplayRoot</c> e <c>sequenciaColapso</c> estavam
    /// <b>todos nulos</b> nas três cenas. Na prática: apertar Esc congelava o jogo sem nada
    /// aparecer, e morrer não mostrava tela nenhuma.</para>
    ///
    /// <para>Idempotente: refaz as telas do zero a cada execução.</para>
    /// </summary>
    public static class MontarTelasDeFluxo
    {
        private static readonly string[] Cenas =
        {
            "Assets/Scenes/Deserto_Hali.unity",
            "Assets/Scenes/Playtest_RuinasPalidas.unity",
            "Assets/Scenes/Santuario_Yhtill.unity",
        };

        private static readonly Color Amarelo = new Color(0.92f, 0.86f, 0.55f, 0.92f);
        private static readonly Color AmareloFraco = new Color(0.85f, 0.82f, 0.62f, 0.5f);

        [MenuItem("Tools/FavelaAmarela/Montar telas de fluxo (pause, colapso, menu)")]
        public static void Executar()
        {
            var cenaAtiva = EditorSceneManager.GetActiveScene();
            if (cenaAtiva.isDirty && !string.IsNullOrEmpty(cenaAtiva.path))
                EditorSceneManager.SaveScene(cenaAtiva);

            string cenaOriginal = cenaAtiva.path;
            int feitas = 0;

            foreach (var caminho in Cenas)
            {
                if (!System.IO.File.Exists(caminho)) continue;

                var cena = EditorSceneManager.OpenScene(caminho, OpenSceneMode.Single);
                if (Montar())
                {
                    EditorSceneManager.MarkSceneDirty(cena);
                    EditorSceneManager.SaveScene(cena);
                    feitas++;
                    Debug.Log($"[TelasDeFluxo] Montadas em '{cena.name}'.");
                }
            }

            if (!string.IsNullOrEmpty(cenaOriginal))
                EditorSceneManager.OpenScene(cenaOriginal, OpenSceneMode.Single);

            Debug.Log($"[TelasDeFluxo] Pronto — {feitas} cena(s). O jogo passa a abrir no Menu " +
                      "('Iniciar No Menu' foi ligado no GameManager); Esc pausa; morrer leva ao " +
                      "Colapso e volta ao Menu por qualquer tecla. Para playtest entrando direto " +
                      "na cena, desligue 'Iniciar No Menu'.");
        }

        private static bool Montar()
        {
            var gm = Object.FindAnyObjectByType<GameManager>(FindObjectsInactive.Include);
            if (gm == null)
            {
                Debug.LogWarning("[TelasDeFluxo] Sem GameManager nesta cena — pulada.");
                return false;
            }

            var canvas = Object.FindAnyObjectByType<Canvas>(FindObjectsInactive.Include);
            if (canvas == null)
            {
                Debug.LogWarning("[TelasDeFluxo] Sem Canvas nesta cena — pulada.");
                return false;
            }

            GarantirInfraDeClique(canvas);

            Destruir("Tela_Pause");
            Destruir("Tela_Colapso");
            Destruir("Tela_Menu");

            var pause = MontarPause(canvas.transform);
            var (colapso, sequencia) = MontarColapso(canvas.transform);
            var menu = MontarMenu(canvas.transform);

            var so = new SerializedObject(gm);
            so.FindProperty("telaPause").objectReferenceValue = pause;
            so.FindProperty("telaMenu").objectReferenceValue = menu;
            so.FindProperty("sequenciaColapso").objectReferenceValue = sequencia;

            // Escrito de forma explícita: as cenas já foram salvas antes deste campo existir,
            // e depender do valor padrão do C# para preencher o que falta no YAML é sutil
            // demais para confiar.
            var inicio = so.FindProperty("iniciarNoMenu");
            if (inicio != null) inicio.boolValue = true;

            so.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(gm);

            // gameplayRoot fica de fora de propósito: cada cena tem uma raiz diferente
            // (Deserto_Root, Blockout_Root...) e chutar errado esconderia o jogo inteiro.
            // O painel do Menu é opaco, então cobre a cena mesmo sem essa referência.

            return true;
        }

        private static void Destruir(string nome)
        {
            var go = GameObject.Find(nome);
            if (go != null) Object.DestroyImmediate(go);
        }

        /// <summary>
        /// Garante <c>EventSystem</c> e <c>GraphicRaycaster</c> — sem os dois, <b>nenhum
        /// botão responde a clique</b>, e sem erro nenhum no console.
        ///
        /// <para>O projeto nunca precisou disso: todo o HUD até aqui era só exibição, e o
        /// input é lido por polling de teclado. O Menu é a primeira UI clicável do jogo.</para>
        ///
        /// <para>Usa o <c>InputSystemUIInputModule</c>, e não o <c>StandaloneInputModule</c>
        /// antigo: o projeto está no Input System novo, e o módulo velho reclama em runtime.</para>
        /// </summary>
        private static void GarantirInfraDeClique(Canvas canvas)
        {
            if (canvas.GetComponent<GraphicRaycaster>() == null)
            {
                canvas.gameObject.AddComponent<GraphicRaycaster>();
                EditorUtility.SetDirty(canvas);
                Debug.Log("[TelasDeFluxo] GraphicRaycaster acrescentado ao Canvas.");
            }

            if (Object.FindAnyObjectByType<EventSystem>(FindObjectsInactive.Include) != null) return;

            var go = new GameObject("EventSystem", typeof(EventSystem), typeof(InputSystemUIInputModule));
            Undo.RegisterCreatedObjectUndo(go, "EventSystem");
            Debug.Log("[TelasDeFluxo] EventSystem criado — sem ele nenhum botão responderia a clique.");
        }

        // ── Pause ─────────────────────────────────────────────────────────────

        private static GameObject MontarPause(Transform pai)
        {
            var tela = Overlay(pai, "Tela_Pause", new Color(0.02f, 0.02f, 0.015f, 0.75f));

            Texto(tela.transform, "Titulo", "PAUSADO",
                new Vector2(0.1f, 0.52f), new Vector2(0.9f, 0.64f), 34, TextAnchor.MiddleCenter, Amarelo);

            Texto(tela.transform, "Dica", "Esc para continuar",
                new Vector2(0.1f, 0.42f), new Vector2(0.9f, 0.5f), 14, TextAnchor.MiddleCenter, AmareloFraco);

            tela.SetActive(false);
            return tela;
        }

        // ── Colapso ───────────────────────────────────────────────────────────

        private static (GameObject, SequenciaDeColapso) MontarColapso(Transform pai)
        {
            // A raiz NÃO leva Image nem CanvasGroup, e fica sempre ativa.
            //
            // `SequenciaDeColapso.Awake` faz `painelColapso.gameObject.SetActive(false)`. Se o
            // CanvasGroup morasse na mesma raiz, ela desligaria o próprio objeto onde vive — e
            // `Tocar()` não conseguiria iniciar coroutine nenhuma ("Coroutine couldn't be
            // started because the game object is inactive"). O script sempre esperou que o
            // painel fosse um FILHO: é ele que a sequência reativa ao tocar.
            var tela = new GameObject("Tela_Colapso", typeof(RectTransform));
            tela.transform.SetParent(pai, false);
            Esticar(tela.GetComponent<RectTransform>());

            var painel = Overlay(tela.transform, "Painel", new Color(0.03f, 0.02f, 0.02f, 1f));

            var grupo = painel.AddComponent<CanvasGroup>();
            grupo.alpha = 0f;
            grupo.blocksRaycasts = false;

            var texto = Texto(painel.transform, "Frase", "",
                new Vector2(0.12f, 0.4f), new Vector2(0.88f, 0.6f), 22, TextAnchor.MiddleCenter, Amarelo);

            // Sem isto, morrer é beco sem saída: a máquina de estados permite Colapso → Menu,
            // mas ninguém no projeto fazia essa transição.
            var avisoRetorno = Texto(painel.transform, "Aviso", "",
                new Vector2(0.12f, 0.24f), new Vector2(0.88f, 0.32f), 14, TextAnchor.MiddleCenter, AmareloFraco);
            avisoRetorno.enabled = false;

            // Os dois componentes ficam na RAIZ, que nunca é desligada — senão o Update do
            // retorno pararia junto com o painel e o jogador ficaria preso na tela de morte.
            var retorno = tela.AddComponent<RetornoDoColapso>();
            var soRetorno = new SerializedObject(retorno);
            soRetorno.FindProperty("aviso").objectReferenceValue = avisoRetorno;
            soRetorno.ApplyModifiedPropertiesWithoutUndo();

            var sequencia = tela.AddComponent<SequenciaDeColapso>();

            var so = new SerializedObject(sequencia);
            so.FindProperty("painelColapso").objectReferenceValue = grupo;
            so.FindProperty("textoColapso").objectReferenceValue = texto;

            // O sprite que se dissolve é o do próprio Damião.
            var jogador = Object.FindAnyObjectByType<PlayerMovement>(FindObjectsInactive.Include);
            var sr = jogador != null ? jogador.GetComponent<SpriteRenderer>() : null;
            if (sr != null) so.FindProperty("damiaoSprite").objectReferenceValue = sr;

            so.ApplyModifiedPropertiesWithoutUndo();

            return (tela, sequencia);
        }

        // ── Menu ──────────────────────────────────────────────────────────────

        private static GameObject MontarMenu(Transform pai)
        {
            // Opaco: o Menu cobre a cena mesmo sem gameplayRoot atribuído.
            var tela = Overlay(pai, "Tela_Menu", new Color(0.03f, 0.025f, 0.02f, 1f));

            // Título oficial (decisão do Vini, 2026-08-11). O projeto tem outros nomes
            // circulando — "A Maldição da Cidade Pálida" (repositório), "Peregrino Amarelo"
            // (pasta), "Favela Amarela" (namespaces) —, mas o que o jogador vê é este.
            Texto(tela.transform, "Titulo", "CAMINHO PARA CARCOSA",
                new Vector2(0.1f, 0.72f), new Vector2(0.9f, 0.84f), 30, TextAnchor.MiddleCenter, Amarelo);

            var continuar = Botao(tela.transform, "Botao_Continuar", "Continuar", 0.52f);
            var nova = Botao(tela.transform, "Botao_NovaPartida", "Nova peregrinação", 0.42f);
            var sair = Botao(tela.transform, "Botao_Sair", "Sair", 0.32f);

            var confirmacao = MontarConfirmacao(tela.transform);

            var comp = tela.AddComponent<MenuPrincipal>();
            var so = new SerializedObject(comp);
            so.FindProperty("botaoContinuar").objectReferenceValue = continuar;
            so.FindProperty("botaoNovaPartida").objectReferenceValue = nova;
            so.FindProperty("botaoSair").objectReferenceValue = sair;
            so.FindProperty("painelDeConfirmacao").objectReferenceValue = confirmacao.painel;
            so.FindProperty("botaoConfirmar").objectReferenceValue = confirmacao.confirmar;
            so.FindProperty("botaoCancelar").objectReferenceValue = confirmacao.cancelar;
            so.ApplyModifiedPropertiesWithoutUndo();

            tela.SetActive(false);
            return tela;
        }

        private static (GameObject painel, Button confirmar, Button cancelar) MontarConfirmacao(Transform pai)
        {
            var painel = Overlay(pai, "Confirmacao", new Color(0.05f, 0.04f, 0.03f, 0.96f));

            Texto(painel.transform, "Aviso", "Isso apaga o progresso. Continuar?",
                new Vector2(0.1f, 0.54f), new Vector2(0.9f, 0.64f), 18, TextAnchor.MiddleCenter, Amarelo);

            var confirmar = Botao(painel.transform, "Botao_Confirmar", "Apagar e recomeçar", 0.44f);
            var cancelar = Botao(painel.transform, "Botao_Cancelar", "Voltar", 0.34f);

            painel.SetActive(false);
            return (painel, confirmar, cancelar);
        }

        // ── Peças ─────────────────────────────────────────────────────────────

        /// <summary>Ancora o retângulo à tela inteira.</summary>
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
            rt.anchorMin = new Vector2(0.34f, alturaCentro - 0.035f);
            rt.anchorMax = new Vector2(0.66f, alturaCentro + 0.035f);
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;

            var img = go.GetComponent<Image>();
            img.sprite = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/UISprite.psd");
            img.type = Image.Type.Sliced;
            img.color = new Color(0.85f, 0.80f, 0.60f, 0.14f);

            Texto(go.transform, "Rotulo", rotulo,
                Vector2.zero, Vector2.one, 16, TextAnchor.MiddleCenter, Amarelo);

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
