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
    /// Ferramenta de Editor. Constrói as telas de fluxo que vivem <b>dentro da cena de jogo</b>
    /// — Pause e Colapso — e as liga ao <see cref="GameManager"/>.
    ///
    /// <para>O <b>menu principal não está aqui</b>: virou cena própria em 2026-08-11, montada
    /// por <c>MontarCenaDeMenu</c>. Estas duas continuam sendo overlay porque precisam do
    /// mundo — o pause mostra onde o jogador parou, e o Colapso dissolve o sprite do Damião.</para>
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

        [MenuItem("Tools/FavelaAmarela/Montar telas de fluxo (pause, colapso)")]
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

            Debug.Log($"[TelasDeFluxo] Pronto — {feitas} cena(s). Esc abre o menu de pause " +
                      "(Continuar / Sair do jogo); morrer toca o Colapso e, após 3 s, qualquer " +
                      "tecla leva à cena de menu. O menu principal é montado à parte, por " +
                      "'Montar cena de menu'.");
        }

        private static bool Montar()
        {
            var bootstrap = Object.FindAnyObjectByType<FavelaAmarela.Runtime.GameLoop.GameLoopBootstrap>(
                FindObjectsInactive.Include);
            if (bootstrap == null)
            {
                Debug.LogWarning("[TelasDeFluxo] Sem GameLoopBootstrap nesta cena — pulada.");
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

            // O menu principal saiu daqui em 2026-08-11: virou cena própria (`Cena_Menu`).
            // Como overlay, ele obrigava a carregar o Deserto inteiro só para cobri-lo com uma
            // tela preta. Restos de execuções antigas são removidos.
            Destruir("Tela_Menu");

            var pause = MontarPause(canvas.transform);
            var (colapso, sequencia) = MontarColapso(canvas.transform);

            // Os dois campos migraram para componentes diferentes na Fase 2 (2026-08-14):
            // telaPause vive no GameStatePresenter (quem a liga/desliga) e sequenciaColapso no
            // PlayerDeathController (quem conhece a causa da morte). Escrever no lugar errado
            // aqui produziria uma cena que parece montada e não funciona.
            var presenter = bootstrap.GetComponent<FavelaAmarela.Runtime.GameLoop.GameStatePresenter>();
            if (presenter != null)
            {
                var soP = new SerializedObject(presenter);
                soP.FindProperty("telaPause").objectReferenceValue = pause;
                soP.ApplyModifiedPropertiesWithoutUndo();
                EditorUtility.SetDirty(presenter);
            }
            else
            {
                Debug.LogWarning("[TelasDeFluxo] Sem GameStatePresenter — a tela de pause não " +
                                 "será ligada.");
            }

            var morte = bootstrap.GetComponent<FavelaAmarela.Runtime.GameLoop.PlayerDeathController>();
            if (morte != null)
            {
                var soM = new SerializedObject(morte);
                soM.FindProperty("sequenciaColapso").objectReferenceValue = sequencia;
                soM.ApplyModifiedPropertiesWithoutUndo();
                EditorUtility.SetDirty(morte);
            }
            else
            {
                Debug.LogWarning("[TelasDeFluxo] Sem PlayerDeathController — a sequência de " +
                                 "Colapso não será ligada.");
            }

            // gameplayRoot fica de fora de propósito: cada cena tem uma raiz diferente
            // (Deserto_Root, Blockout_Root...) e chutar errado esconderia o jogo inteiro.
            // Desde que o menu virou cena própria, ele também deixou de ser necessário —
            // não há mais overlay precisando esconder o mundo.

            return true;
        }

        /// <summary>
        /// Remove todos os objetos com este nome, <b>inclusive os inativos</b>.
        ///
        /// <para><b>Bug que motivou (2026-08-11):</b> a versão anterior usava
        /// <c>GameObject.Find</c>, que <b>só enxerga objetos ativos</b>. Como estas telas
        /// nascem desativadas, cada execução não achava a anterior e criava mais uma —
        /// acumulando seis <c>Tela_Pause</c> e quatro <c>Tela_Menu</c> nas cenas. A
        /// ferramenta se dizia idempotente e não era.</para>
        /// </summary>
        private static void Destruir(string nome)
        {
            var cena = EditorSceneManager.GetActiveScene();
            if (!cena.IsValid()) return;

            foreach (var raiz in cena.GetRootGameObjects())
            {
                // includeInactive: é justamente o que o Find não fazia.
                var achados = raiz.GetComponentsInChildren<Transform>(includeInactive: true);

                foreach (var t in achados)
                {
                    if (t == null || t.name != nome) continue;
                    Object.DestroyImmediate(t.gameObject);
                }
            }
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
            // Semitransparente de propósito: ver o mundo congelado atrás é parte da
            // informação do pause — o jogador enxerga onde parou.
            var tela = Overlay(pai, "Tela_Pause", new Color(0.02f, 0.02f, 0.015f, 0.8f));

            Texto(tela.transform, "Titulo", "PAUSADO",
                new Vector2(0.1f, 0.66f), new Vector2(0.9f, 0.78f), 34, TextAnchor.MiddleCenter, Amarelo);

            var continuar = Botao(tela.transform, "Botao_Continuar", "Continuar", 0.5f);
            var sair = Botao(tela.transform, "Botao_Sair", "Sair do jogo", 0.4f);

            // Previstos e NÃO construídos (decisão do Vini, 2026-08-11): Opções,
            // Enciclopédia e "Voltar ao menu principal". Botão morto ensina o jogador a
            // desconfiar da interface — entram quando existirem de verdade.
            Texto(tela.transform, "Dica", "Esc para continuar",
                new Vector2(0.1f, 0.3f), new Vector2(0.9f, 0.36f), 14, TextAnchor.MiddleCenter, AmareloFraco);

            var comp = tela.AddComponent<MenuDePause>();
            var so = new SerializedObject(comp);
            so.FindProperty("botaoContinuar").objectReferenceValue = continuar;
            so.FindProperty("botaoSair").objectReferenceValue = sair;
            so.ApplyModifiedPropertiesWithoutUndo();

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

            // Precisa bloquear raycast, senão os botões da morte não recebem clique. Não há
            // risco de o painel invisível roubar cliques do jogo: fora do Colapso ele está
            // com o GameObject desligado (a própria SequenciaDeColapso o desliga no Awake).
            grupo.blocksRaycasts = true;

            var texto = Texto(painel.transform, "Frase", "",
                new Vector2(0.12f, 0.4f), new Vector2(0.88f, 0.6f), 22, TextAnchor.MiddleCenter, Amarelo);

            // As saídas da morte. Morrer NÃO devolve ao menu principal (decisão do Vini,
            // 2026-08-11): o padrão é despertar no último Refúgio, e a tela-título é apenas
            // uma das opções — mandar o jogador para lá a cada morte desestimula em vez de punir.
            var opcoes = new GameObject("Opcoes", typeof(RectTransform));
            opcoes.transform.SetParent(painel.transform, false);
            Esticar(opcoes.GetComponent<RectTransform>());

            var retomar = Botao(opcoes.transform, "Botao_Retomar", "Despertar no último refúgio", 0.28f);
            var menu = Botao(opcoes.transform, "Botao_Menu", "Menu principal", 0.18f);
            var rotuloRetomar = retomar.GetComponentInChildren<Text>();

            opcoes.SetActive(false);

            // Os dois componentes ficam na RAIZ, que nunca é desligada — senão o Update do
            // retorno pararia junto com o painel e o jogador ficaria preso na tela de morte.
            var retorno = tela.AddComponent<RetornoDoColapso>();
            var soRetorno = new SerializedObject(retorno);
            soRetorno.FindProperty("grupoDeOpcoes").objectReferenceValue = opcoes;
            soRetorno.FindProperty("botaoRetomar").objectReferenceValue = retomar;
            soRetorno.FindProperty("rotuloRetomar").objectReferenceValue = rotuloRetomar;
            soRetorno.FindProperty("botaoMenu").objectReferenceValue = menu;
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
