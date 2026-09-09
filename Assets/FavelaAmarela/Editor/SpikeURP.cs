using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.PackageManager;
using UnityEditor.PackageManager.Requests;
using UnityEngine;
using UnityEngine.Rendering;

namespace FavelaAmarela.EditorTools
{
    /// <summary>
    /// Spike de migração para a URP — <b>experimento na branch <c>spike_urp</c></b>, não parte
    /// da entrega.
    ///
    /// <para><b>Por que existe.</b> Eu tinha desaconselhado a migração dizendo que ela "toca
    /// todo material — inclusive o seu shader". O Vini pediu o número, e medido o projeto tem
    /// <b>um</b> material (<c>OcclusionDither</c>) e <b>um</b> shader customizado — e esse
    /// shader é <c>Lighting Off</c> com <c>UnityCG.cginc</c>, ou seja unlit puro. Dos 165
    /// arquivos de teste, <b>4</b> tocam o que a migração move. O custo não está em volume.</para>
    ///
    /// <para><b>Onde o risco está de verdade, e é por isso que isto é um spike e não um
    /// plano:</b> o <c>PixelPerfectCamera</c>. A identidade visual inteira depende dele e do
    /// <c>orthographicSize</c> derivado (4,21875 e 5,625) — é ele que já fez recusar zoom
    /// dinâmico, fit de aspect e <c>SetTargetZoom</c> três vezes. Se o comportamento mudar na
    /// URP, muda o enquadramento de todas as cenas. O spike existe para <b>medir isso</b>, não
    /// para argumentar sobre isso.</para>
    ///
    /// <para>Roda em duas etapas porque instalar pacote exige recarga de domínio: primeiro
    /// <see cref="Instalar"/>, depois <see cref="Configurar"/>.</para>
    /// </summary>
    public static class SpikeURP
    {
        private const string Marcador = "[SpikeURP]";
        private const string Pacote = "com.unity.render-pipelines.universal";

        [MenuItem("Tools/FavelaAmarela/Spike URP: 1. instalar pacote")]
        public static void Instalar()
        {
            // Sem versão fixa de propósito: o Package Manager resolve a recomendada para ESTA
            // versão do editor. Cravar "17.x" de cabeça é a receita para uma falha de resolução
            // que não diz nada sobre a migração.
            AddRequest pedido = Client.Add(Pacote);

            while (!pedido.IsCompleted) System.Threading.Thread.Sleep(100);

            if (pedido.Status != StatusCode.Success)
            {
                Debug.LogError($"{Marcador} Falhou ao instalar: {pedido.Error?.message}");
                return;
            }

            Debug.Log($"{Marcador} Instalado {pedido.Result.name} {pedido.Result.version}");
        }

        private const string Pasta = "Assets/Settings";
        private const string CaminhoDoPipeline = Pasta + "/URP_Pipeline.asset";
        private const string CaminhoDoRenderer = Pasta + "/URP_Renderer2D.asset";

        [MenuItem("Tools/FavelaAmarela/Spike URP: 2. configurar pipeline 2D")]
        public static void Configurar()
        {
            var tipoRenderer = System.AppDomain.CurrentDomain.GetAssemblies()
                .SelectMany(a => { try { return a.GetTypes(); } catch { return new System.Type[0]; } })
                .FirstOrDefault(t => t.FullName == "UnityEngine.Rendering.Universal.Renderer2DData");

            var tipoPipeline = System.AppDomain.CurrentDomain.GetAssemblies()
                .SelectMany(a => { try { return a.GetTypes(); } catch { return new System.Type[0]; } })
                .FirstOrDefault(t => t.FullName ==
                                     "UnityEngine.Rendering.Universal.UniversalRenderPipelineAsset");

            if (tipoRenderer == null || tipoPipeline == null)
            {
                Debug.LogError($"{Marcador} Tipos da URP não encontrados — o pacote instalou? " +
                               $"Renderer2DData={tipoRenderer != null}, " +
                               $"UniversalRenderPipelineAsset={tipoPipeline != null}");
                return;
            }

            Directory.CreateDirectory(Pasta);

            var renderer = ScriptableObject.CreateInstance(tipoRenderer);
            AssetDatabase.CreateAsset(renderer, CaminhoDoRenderer);

            var pipeline = ScriptableObject.CreateInstance(tipoPipeline);
            AssetDatabase.CreateAsset(pipeline, CaminhoDoPipeline);

            // Liga o Renderer 2D no pipeline pela lista serializada — a API pública para isso
            // varia entre versões da URP, e SerializedObject atravessa as duas.
            var so = new SerializedObject(pipeline);
            var lista = so.FindProperty("m_RendererDataList");

            if (lista == null)
            {
                Debug.LogError($"{Marcador} 'm_RendererDataList' não existe neste " +
                               "UniversalRenderPipelineAsset — a estrutura mudou de versão.");
                return;
            }

            lista.arraySize = 1;
            lista.GetArrayElementAtIndex(0).objectReferenceValue = renderer;
            so.ApplyModifiedPropertiesWithoutUndo();

            AssetDatabase.SaveAssets();

            GraphicsSettings.defaultRenderPipeline = (RenderPipelineAsset)pipeline;
            QualitySettings.renderPipeline = (RenderPipelineAsset)pipeline;

            AssetDatabase.SaveAssets();

            Debug.Log($"{Marcador} Pipeline 2D configurado e atribuído em Graphics e Quality.");
            Relatar();
        }

        /// <summary>
        /// Desliga o pipeline sem desinstalar o pacote — para capturar a linha de base built-in
        /// no MESMO projeto, que isola a variável "pipeline" de todas as outras.
        /// </summary>
        [MenuItem("Tools/FavelaAmarela/Spike URP: desligar pipeline (built-in)")]
        public static void Desligar()
        {
            GraphicsSettings.defaultRenderPipeline = null;
            QualitySettings.renderPipeline = null;
            AssetDatabase.SaveAssets();
            Debug.Log($"{Marcador} Pipeline desligado — voltou ao built-in.");
        }

        [MenuItem("Tools/FavelaAmarela/Spike URP: religar pipeline")]
        public static void Religar()
        {
            var p = AssetDatabase.LoadAssetAtPath<RenderPipelineAsset>(CaminhoDoPipeline);

            if (p == null) { Debug.LogError($"{Marcador} {CaminhoDoPipeline} não existe."); return; }

            GraphicsSettings.defaultRenderPipeline = p;
            QualitySettings.renderPipeline = p;
            AssetDatabase.SaveAssets();
            Debug.Log($"{Marcador} Pipeline religado: {p.name}");
        }

        /// <summary>
        /// O que o spike precisa responder, medido em vez de suposto: o pipeline está ativo, o
        /// shader do dither compila, e o <c>PixelPerfectCamera</c> continua existindo.
        /// </summary>
        [MenuItem("Tools/FavelaAmarela/Spike URP: 3. relatar")]
        public static void Relatar()
        {
            var atual = GraphicsSettings.defaultRenderPipeline;
            Debug.Log($"{Marcador} Pipeline ativo: {(atual == null ? "BUILT-IN" : atual.GetType().Name)}");

            var shader = AssetDatabase.LoadAssetAtPath<Shader>(
                "Assets/FavelaAmarela/Art/Shaders/SpriteDitherOcclusion.shader");

            if (shader == null)
                Debug.LogError($"{Marcador} Shader do dither não encontrado.");
            else
                Debug.Log($"{Marcador} SpriteDitherOcclusion: " +
                          (shader.isSupported ? "COMPILA" : "*** NÃO COMPILA ***"));

            var mat = AssetDatabase.LoadAssetAtPath<Material>(
                "Assets/FavelaAmarela/Art/Materials/OcclusionDither.mat");

            if (mat != null)
                Debug.Log($"{Marcador} Material OcclusionDither -> shader '{mat.shader.name}', " +
                          (mat.shader.isSupported ? "ok" : "*** NÃO SUPORTADO ***"));

            var ppc = System.AppDomain.CurrentDomain.GetAssemblies()
                .SelectMany(a => { try { return a.GetTypes(); } catch { return new System.Type[0]; } })
                .Where(t => t.Name == "PixelPerfectCamera")
                .ToArray();

            Debug.Log($"{Marcador} PixelPerfectCamera encontrado em {ppc.Length} assembly(ies): " +
                      string.Join(", ", ppc.Select(t => t.Assembly.GetName().Name)));
        }
    }
}
