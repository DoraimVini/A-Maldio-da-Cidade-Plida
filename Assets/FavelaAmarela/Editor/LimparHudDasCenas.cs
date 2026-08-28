using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;
using FavelaAmarela.Runtime.UI;

namespace FavelaAmarela.EditorTools
{
    /// <summary>
    /// Tira das cenas o HUD que agora é <b>persistente</b>, <b>preservando</b> o que não é HUD.
    ///
    /// <para><b>A armadilha que esta ferramenta existe para evitar.</b> A primeira versão
    /// simplesmente destruía o <c>GameObject</c> do <c>HUDController</c>. Em quatro das cinco
    /// cenas, <c>Tela_Pause</c> e <c>Tela_Colapso</c> eram <b>filhas</b> dele — e foram junto.
    /// Só o Deserto escapou, porque lá elas pendiam de um Canvas de cena separado.</para>
    ///
    /// <para>Pior: esse mesmo defeito existiria <b>em runtime</b> mesmo sem esta ferramenta. Com
    /// o HUD persistente nascendo antes da primeira cena, a cópia salva dentro da cena se
    /// autodestrói pela guarda de singleton — levando as telas de fluxo junto, no meio do jogo,
    /// sem nada no console.</para>
    ///
    /// <para><b>Por isso a ordem importa:</b> primeiro <b>reparenta</b> tudo que não migrou para
    /// um Canvas próprio da cena, e só então destrói o HUD.</para>
    ///
    /// <para><b>O resgate continua existindo para o que é mesmo da cena</b> — hoje só o
    /// <c>Veu_Tempestade</c> do Deserto. As telas de fluxo deixaram de ser resgatadas porque
    /// agora vivem no prefab persistente.</para>
    /// </summary>
    public static class LimparHudDasCenas
    {
        /// <summary>
        /// Todas as cenas do projeto, <b>varridas da pasta</b> — não escritas à mão.
        ///
        /// <para><b>Por que derivada.</b> A primeira versão listava cinco cenas à mão e deixou
        /// <c>Cena_ArenaDeTestes</c> de fora; o guarda <c>NenhumaCena_TemCopiaDoHud</c> acusou.
        /// Foi a <b>nona</b> lista de cenas escrita à mão a envelhecer neste projeto — e o defeito
        /// não era cosmético: a cópia esquecida se autodestruiria em runtime pela guarda de
        /// singleton do HUD persistente, levando junto tudo que estivesse pendurado nela.</para>
        ///
        /// <para>Varrer a pasta faz a lista não ter como ficar para trás: cena nova entra sozinha.</para>
        /// </summary>
        private static string[] TodasAsCenas() =>
            Directory.GetFiles("Assets/Scenes", "*.unity", SearchOption.AllDirectories)
                     .Select(c => c.Replace(Path.DirectorySeparatorChar, '/'))
                     .OrderBy(c => c)
                     .ToArray();

        /// <summary>
        /// Migraram para <c>Resources/HUD_Gameplay</c> e por isso saem das cenas.
        ///
        /// <para><c>Tela_Pause</c> e <c>Tela_Colapso</c> entraram nesta lista em 2026-08-22: elas
        /// passaram a viver no prefab persistente, com a ligação ao <c>GameStatePresenter</c> e
        /// ao <c>PlayerDeathController</c> feita em runtime pelo <c>GameLoopBootstrap</c>.</para>
        /// </summary>
        private static readonly HashSet<string> Migrados = new HashSet<string>
        {
            "PainelDeInventario", "CaixaDeDialogo", "Tela_Pause", "Tela_Colapso",
        };

        private const string NomeDoCanvasDaCena = "Canvas_Cena";

        [MenuItem("Tools/FavelaAmarela/HUD: limpar cópias das cenas")]
        public static void Executar()
        {
            var resumo = new List<string>();

            foreach (var caminho in TodasAsCenas())
            {
                if (!File.Exists(caminho))
                {
                    resumo.Add($"{Path.GetFileName(caminho)}: ausente");
                    continue;
                }

                var cena = EditorSceneManager.OpenScene(caminho, OpenSceneMode.Single);

                var huds = Object.FindObjectsByType<HUDController>(FindObjectsInactive.Include);

                int resgatados = 0, removidos = 0;

                foreach (var hud in huds)
                {
                    if (hud == null) continue;

                    var raizDoHud = hud.gameObject;

                    // 1. RESGATE, antes de qualquer destruição.
                    var filhos = new List<Transform>();
                    foreach (Transform filho in raizDoHud.transform) filhos.Add(filho);

                    foreach (var filho in filhos)
                    {
                        if (Migrados.Contains(filho.name)) continue;
                        if (EhPecaDoHud(filho.name)) continue;

                        var abrigo = GarantirCanvasDaCena(raizDoHud);
                        filho.SetParent(abrigo.transform, false);
                        resgatados++;
                    }

                    Object.DestroyImmediate(raizDoHud);
                    removidos++;
                }

                // 2. Os painéis migrados, onde quer que tenham ficado.
                foreach (var t in Object.FindObjectsByType<Transform>(FindObjectsInactive.Include)
                         .Where(t => t != null && Migrados.Contains(t.name))
                         .ToList())
                {
                    if (t == null) continue;
                    Object.DestroyImmediate(t.gameObject);
                    removidos++;
                }

                EditorSceneManager.MarkSceneDirty(cena);
                if (!EditorSceneManager.SaveScene(cena))
                {
                    resumo.Add($"{Path.GetFileName(caminho)}: SaveScene RECUSOU");
                    continue;
                }

                resumo.Add($"{Path.GetFileName(caminho)}: {removidos} removido(s), " +
                           $"{resgatados} resgatado(s) para o Canvas da cena");
            }

            Debug.Log("[LimparHud] Concluído:\n  " + string.Join("\n  ", resumo));
        }

        /// <summary>
        /// Peças que pertencem ao HUD de gameplay e portanto podem morrer com ele — elas já
        /// existem no prefab persistente.
        /// </summary>
        private static bool EhPecaDoHud(string nome) =>
            // SEM underscore no prefixo: a nomenclatura das barras e inconsistente --
            // "Barra_Vitalidade" e "Barra_Vigor" usam underscore, mas "BarraDeAcoes" e
            // "BarraDeArtefatos" nao. Exigir "Barra_" fazia a barra de acoes ser "resgatada"
            // para a cena e passar a existir DUAS vezes: uma no prefab persistente e outra em
            // cada cena.
            nome.StartsWith("Barra", System.StringComparison.Ordinal) ||
            nome == "ResilienciaBar_Root" ||
            nome == "PanicOverlay" ||
            nome == "PainelDeFicha";

        /// <summary>
        /// Acha (ou cria) um Canvas da cena para abrigar o que não é HUD. Em quatro das cinco
        /// cenas o <b>único</b> Canvas era o do próprio HUD — removê-lo deixaria as telas de
        /// fluxo sem raiz de UI, e elas não renderizariam nada.
        /// </summary>
        private static GameObject GarantirCanvasDaCena(GameObject hudParaIgnorar)
        {
            foreach (var c in Object.FindObjectsByType<Canvas>(FindObjectsInactive.Include))
            {
                if (c == null || c.gameObject == hudParaIgnorar) continue;
                return c.gameObject;
            }

            var go = new GameObject(NomeDoCanvasDaCena,
                typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));

            var canvas = go.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 90;   // abaixo do HUD persistente (100)

            var scaler = go.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            scaler.matchWidthOrHeight = 0.5f;

            return go;
        }
    }
}
