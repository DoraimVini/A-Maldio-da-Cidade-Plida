using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

namespace FavelaAmarela.EditorTools
{
    /// <summary>
    /// Padroniza o <see cref="CanvasScaler"/> de todas as cenas e aplica a moldura do Dark Ages
    /// UI aos painéis do menu, que a passada anterior não alcançou.
    ///
    /// <para><b>Problema 1 — a UI saía do enquadramento.</b> As cinco cenas foram montadas por
    /// ferramentas diferentes, em épocas diferentes, e o <c>CanvasScaler</c> nunca foi
    /// padronizado. Medido em 2026-08-19:</para>
    /// <list type="bullet">
    /// <item><c>Deserto_Hali</c> — <b>ConstantPixelSize</b>: a UI é desenhada em pixels fixos e
    /// <b>não acompanha o viewport</b>, então as barras da HUD estouram a borda.</item>
    /// <item><c>Tumba_De_Alhazred</c> e <c>Santuario_Yhtill</c> — <b>sem CanvasScaler
    /// nenhum</b>, que é o mesmo comportamento do ConstantPixelSize, só que implícito.</item>
    /// <item><c>Cena_ArenaDeTestes</c> — ScaleWithScreenSize, mas com referência <b>640×360</b>
    /// contra 1920×1080 do menu: a mesma UI aparece em escalas diferentes conforme a cena.</item>
    /// </list>
    ///
    /// <para><b>Por que 1920×1080 e <c>matchWidthOrHeight = 0.5</c>:</b> é a referência que o
    /// menu já usava (a única cena cuja UI estava correta), e o <c>0.5</c> reparte a compensação
    /// entre largura e altura — com <c>0</c> (o valor atual de todas) a UI escala <b>só pela
    /// largura</b>, e numa tela mais baixa que a referência ela transborda verticalmente.</para>
    ///
    /// <para><b>Problema 2 — o menu não tinha moldura.</b> <c>AplicarCaraDaInterface</c> escolhe
    /// os painéis por <b>nome</b>, e a lista dela (<c>PainelDeFicha</c>, <c>Janela</c>,
    /// <c>Tela_Pause</c>…) não tem nenhum dos nomes do menu — os objetos lá se chamam
    /// <c>Menu</c>, <c>Confirmacao</c> e <c>Botao_*</c>. O log daquela rodada registrou
    /// "Cena_Menu: 0 painel(is)" e ninguém leu. Conferido no YAML: os 7 <c>Image</c> da cena
    /// ainda apontam para o sprite embutido da Unity.</para>
    ///
    /// <para>Os <b>botões</b> recebem a moldura de slot (discreta), não a ornamentada: três
    /// espirais douradas empilhadas competiriam entre si e com o título.</para>
    /// </summary>
    public static class PadronizarCanvasDasCenas
    {
        /// <summary>
        /// As cenas saem do <b>disco</b>, não de uma lista.
        ///
        /// <para>Aqui havia um array escrito à mão que o próprio comentário dele já denunciava:
        /// <i>"Acrescentadas em 2026-08-20. Terceira lista de cenas do projeto que tinha parado
        /// antes do Castelo e dos Portões existirem"</i>. Uma lista que envelheceu, foi
        /// remendada, e ficou lista. Cena nova agora entra sozinha.</para>
        /// </summary>
        private static IEnumerable<string> Cenas() =>
            System.IO.Directory
                .GetFiles("Assets/Scenes", "*.unity", System.IO.SearchOption.AllDirectories)
                .Select(c => c.Replace(System.IO.Path.DirectorySeparatorChar, '/'))
                .OrderBy(c => c);

        /// <summary>Referência que o menu já usava — a única cena cuja UI estava correta.</summary>
        private static readonly Vector2 ResolucaoDeReferencia = new Vector2(1920f, 1080f);

        /// <summary>Painéis de fundo do menu: recebem a moldura ornamentada.</summary>
        private static readonly HashSet<string> PaineisDoMenu = new HashSet<string>
        {
            "Menu", "Confirmacao",
        };

        /// <summary>Botões: moldura discreta, para não competirem entre si nem com o título.</summary>
        private static bool EhBotaoDoMenu(string nome) =>
            nome.StartsWith("Botao_", System.StringComparison.Ordinal);

        /// <summary>
        /// Prefabs que trazem <c>Canvas</c> próprio. Precisam ser corrigidos <b>separadamente</b>
        /// das cenas: o <c>HUD_ResilienciaBar</c> é instanciado nas três cenas jogáveis e trazia
        /// um <c>CanvasScaler</c> em <c>ConstantPixelSize</c> a 800×600 <b>dentro do prefab</b> —
        /// então padronizar só o Canvas da cena deixava a HUD estourando a borda do mesmo jeito.
        /// Foi essa a causa real do sintoma que o Vini relatou; a correção de cena sozinha teria
        /// dado a impressão de resolver.
        /// </summary>
        private static readonly string[] PrefabsComCanvas =
        {
            "Assets/FavelaAmarela/Art/UI/HUD_ResilienciaBar.prefab",
        };

        [MenuItem("Tools/FavelaAmarela/Padronizar Canvas e moldura do menu")]
        public static void Executar()
        {
            var resumo = new List<string>();

            foreach (var caminho in PrefabsComCanvas)
                resumo.Add(PadronizarPrefab(caminho));

            foreach (var caminho in Cenas())
            {
                if (!System.IO.File.Exists(caminho))
                {
                    resumo.Add($"{System.IO.Path.GetFileName(caminho)}: ausente");
                    continue;
                }

                var cena = EditorSceneManager.OpenScene(caminho, OpenSceneMode.Single);

                int scalers = PadronizarScalers();
                int cameras = PadronizarCameras(caminho);
                int paineis = 0, botoes = 0;

                if (caminho.EndsWith("Cena_Menu.unity"))
                    (paineis, botoes) = AplicarMolduraNoMenu();

                if (scalers > 0 || cameras > 0 || paineis > 0 || botoes > 0)
                {
                    EditorSceneManager.MarkSceneDirty(cena);
                    EditorSceneManager.SaveScene(cena);
                }

                resumo.Add($"{System.IO.Path.GetFileNameWithoutExtension(caminho)}: " +
                           $"{scalers} scaler(s), {cameras} câmera(s), {paineis} painel(is), " +
                           $"{botoes} botão(ões)");
            }

            AssetDatabase.SaveAssets();
            Debug.Log("[PadronizarCanvas] Concluído:\n  " + string.Join("\n  ", resumo));
        }

        // ── Escala do mundo (zoom da câmera) ───────────────────────

        /// <summary>
        /// Põe as câmeras da cena no padrão do jogo. Mexe <b>só na câmera</b>: a escala do mapa
        /// em si (tamanho das salas, distâncias) o Vini decidiu tratar fora do Vertical Slice.
        ///
        /// <para>O zoom, a projeção, a rotação e o <c>PixelPerfectCamera</c> saem todos de
        /// <see cref="PadraoDeCamera"/>. As constantes que moravam aqui (4,21875 e 5,625) viraram
        /// <c>EscalaDePixel.TamanhoOrtografico</c> — eram a única cópia certa entre sete, e
        /// mesmo assim uma <c>if (!cam.orthographic) continue;</c> impedia a ferramenta de
        /// tocar na única cena que estava em perspectiva.</para>
        /// </summary>
        private static int PadronizarCameras(string caminho)
        {
            int ampliacao = PadraoDeCamera.AmpliacaoDe(caminho);
            int mexidas = 0;

            foreach (var cam in Object.FindObjectsByType<Camera>(FindObjectsInactive.Include))
            {
                // Só a câmera de JOGO, e a marca disso é ela seguir o Damião. A do menu não
                // enquadra mundo nenhum: dar a ela uma resolução de referência de pixel art
                // mexeria no layout da UI para resolver um problema que ela não tem. É uma
                // regra derivada, não outra lista de cenas.
                if (cam.GetComponent<FavelaAmarela.CameraSystem.IsometricCameraController>() == null)
                    continue;

                if (PadraoDeCamera.Aplicar(cam, ampliacao).Count > 0) mexidas++;
            }

            return mexidas;
        }

        /// <summary>Aplica a mesma padronização dentro de um prefab que carrega Canvas próprio.</summary>
        private static string PadronizarPrefab(string caminho)
        {
            string nome = System.IO.Path.GetFileNameWithoutExtension(caminho);

            if (!System.IO.File.Exists(caminho)) return $"{nome}: prefab ausente";

            var raiz = PrefabUtility.LoadPrefabContents(caminho);
            try
            {
                int mexidos = 0;

                foreach (var scaler in raiz.GetComponentsInChildren<CanvasScaler>(true))
                {
                    AplicarPadrao(scaler);
                    mexidos++;
                }

                // Canvas sem scaler nenhum dentro do prefab também precisa ganhar um.
                foreach (var canvas in raiz.GetComponentsInChildren<Canvas>(true))
                {
                    if (canvas.GetComponent<CanvasScaler>() != null) continue;
                    AplicarPadrao(canvas.gameObject.AddComponent<CanvasScaler>());
                    mexidos++;
                }

                if (mexidos > 0) PrefabUtility.SaveAsPrefabAsset(raiz, caminho);
                return $"{nome} (prefab): {mexidos} scaler(s)";
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(raiz);
            }
        }

        private static void AplicarPadrao(CanvasScaler scaler)
        {
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = ResolucaoDeReferencia;
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            scaler.matchWidthOrHeight = 0.5f;
            EditorUtility.SetDirty(scaler);
        }

        /// <summary>
        /// Põe todo <c>Canvas</c> da cena em <c>ScaleWithScreenSize</c> a 1920×1080. Cria o
        /// <c>CanvasScaler</c> se ele não existir — duas cenas não tinham nenhum.
        /// </summary>
        private static int PadronizarScalers()
        {
            int mexidos = 0;

            foreach (var canvas in Object.FindObjectsByType<Canvas>(FindObjectsInactive.Include))
            {
                // Só o Canvas raiz tem CanvasScaler; sub-canvases herdam a escala do pai.
                if (canvas.transform.parent != null &&
                    canvas.GetComponentInParent<Canvas>() != canvas) continue;

                var scaler = canvas.GetComponent<CanvasScaler>();
                if (scaler == null) scaler = canvas.gameObject.AddComponent<CanvasScaler>();

                Undo.RecordObject(scaler, "Padronizar CanvasScaler");

                // matchWidthOrHeight = 0.5 e não 0: com 0 a UI escala só pela largura, e numa
                // tela mais baixa que a referência ela transborda verticalmente — que é o
                // sintoma relatado.
                AplicarPadrao(scaler);
                mexidos++;
            }

            return mexidos;
        }

        private static (int paineis, int botoes) AplicarMolduraNoMenu()
        {
            int paineis = 0, botoes = 0;

            foreach (var img in Object.FindObjectsByType<Image>(FindObjectsInactive.Include))
            {
                string nome = img.gameObject.name;

                if (PaineisDoMenu.Contains(nome))
                {
                    Undo.RecordObject(img, "Aplicar painel do menu");
                    PaletaDaInterface.AplicarPainel(img);
                    EditorUtility.SetDirty(img);
                    paineis++;
                }
                else if (EhBotaoDoMenu(nome))
                {
                    // Duas tentativas erradas antes desta, ambas vistas no jogo rodando:
                    // a moldura de SLOT é quadrada (64×64) e esticada num botão de 76px de
                    // altura vira listras horizontais ilegíveis; a de PAINEL tem borda de 23px
                    // por lado — 46px de moldura em 76px de altura, e as bordas quase se tocam.
                    // A de BOTÃO é a única do tilesheet já desenhada na proporção de barra.
                    Undo.RecordObject(img, "Aplicar moldura de botão");
                    PaletaDaInterface.AplicarBotao(img);
                    EditorUtility.SetDirty(img);
                    botoes++;
                }
            }

            return (paineis, botoes);
        }
    }
}
