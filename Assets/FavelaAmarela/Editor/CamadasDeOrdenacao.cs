using System.Collections.Generic;
using System.IO;
using System.Linq;
using FavelaAmarela.Runtime.Rendering;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Tilemaps;

namespace FavelaAmarela.EditorTools
{
    /// <summary>
    /// Cria as camadas de ordenação do projeto e põe o chão na sua.
    ///
    /// <para><b>O que isto conserta (2026-09-09).</b> O jogo inteiro estava numa <b>única</b>
    /// sorting layer (<c>Default</c>), com a profundidade decidida só pelo <c>sortingOrder</c>
    /// inteiro. Isso funciona até parar de funcionar: o chão é desenhado em
    /// <c>sortingOrder −1000</c>, escolhido à mão, e os atores sortam em <c>−y × 10</c>. O
    /// Castelo de Carcosa já alcança <c>y = +71</c>, ou seja ordem <b>−710</b>. Restam
    /// <b>290</b> de folga: um mapa que cresça 30 unidades ao norte faz os atores de lá
    /// desenharem <i>atrás do chão</i> e sumirem da tela, sem erro nenhum.</para>
    ///
    /// <para>Este projeto <b>já dobrou um mapa de tamanho</b> uma vez (2026-09-01). Camadas
    /// tiram a profundidade do jogo da mão de um número mágico.</para>
    ///
    /// <para><b>Por que a ferramenta verifica antes de mexer.</b> Inserir camadas <i>antes</i>
    /// de <c>Default</c> muda o índice dela na lista, e todo <c>SpriteRenderer</c> da cena grava
    /// <b>os dois</b>: <c>m_SortingLayerID</c> (0, que não muda) e <c>m_SortingLayer</c> (o
    /// índice, que muda). Eu não tenho certeza de qual a Unity resolve primeiro ao carregar, e
    /// errar isso reordenaria o jogo inteiro em silêncio. Então a ferramenta <b>mede</b>: fotografa
    /// as camadas de todo renderer antes, cria as camadas, e confere depois. Só mexe no chão se
    /// nada tiver escorregado.</para>
    /// </summary>
    public static class CamadasDeOrdenacao
    {
        private const string Marcador = "[Camadas]";

        /// <summary>As camadas na ordem de desenho: as primeiras ficam atrás.</summary>
        private static readonly (string Nome, int Id)[] Desejadas =
        {
            ("Fundo", 1_900_100),
            ("Chao", 1_900_200),
            ("Default", 0),
            ("Frente", 1_900_300),
        };

        [MenuItem("Tools/FavelaAmarela/Render: criar camadas de ordenação")]
        public static void Executar()
        {
            if (!Application.isBatchMode &&
                !EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
            {
                Debug.Log(Marcador + " Cancelado — havia cena modificada por salvar.");
                return;
            }

            var antes = Fotografar();
            Debug.Log($"{Marcador} Antes: {antes.Count} renderers em " +
                      $"{antes.Values.Distinct().Count()} camada(s) distinta(s).");

            if (!CriarCamadas()) return;

            var depois = Fotografar();

            var escorregou = antes
                .Where(kv => depois.TryGetValue(kv.Key, out var agora) && agora != kv.Value)
                .ToArray();

            if (escorregou.Length > 0)
            {
                Debug.LogError($"{Marcador} ABORTADO: {escorregou.Length} renderer(s) mudaram " +
                               "de camada só por eu ter criado as camadas. O chão NÃO foi " +
                               "movido. Primeiro: " + escorregou[0].Key +
                               $" ({escorregou[0].Value} -> {depois[escorregou[0].Key]})");
                return;
            }

            Debug.Log($"{Marcador} Verificado: nenhum renderer mudou de camada. Movendo o chão.");
            MoverOChao();
        }

        /// <summary>Camada de cada renderer das cenas do build, por caminho completo.</summary>
        private static Dictionary<string, string> Fotografar()
        {
            var mapa = new Dictionary<string, string>();

            foreach (var entrada in EditorBuildSettings.scenes)
            {
                if (!entrada.enabled || !File.Exists(entrada.path)) continue;

                Scene cena = EditorSceneManager.OpenScene(entrada.path, OpenSceneMode.Single);
                string nome = Path.GetFileNameWithoutExtension(entrada.path);

                foreach (var r in cena.GetRootGameObjects()
                             .SelectMany(g => g.GetComponentsInChildren<Renderer>(true)))
                    mapa[nome + "/" + Caminho(r.transform)] = r.sortingLayerName;
            }

            return mapa;
        }

        private static string Caminho(Transform t)
        {
            string s = t.name;
            while (t.parent != null) { t = t.parent; s = t.name + "/" + s; }
            return s;
        }

        private static bool CriarCamadas()
        {
            var ativo = AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/TagManager.asset")
                                     .FirstOrDefault();

            if (ativo == null)
            {
                Debug.LogError(Marcador + " TagManager.asset não abriu.");
                return false;
            }

            var so = new SerializedObject(ativo);
            var lista = so.FindProperty("m_SortingLayers");

            var existentes = new List<string>();
            for (int i = 0; i < lista.arraySize; i++)
                existentes.Add(lista.GetArrayElementAtIndex(i)
                                    .FindPropertyRelative("name").stringValue);

            if (Desejadas.All(d => existentes.Contains(d.Nome)))
            {
                Debug.Log(Marcador + " As camadas já existem — nada a criar.");
                return true;
            }

            // Reconstrói na ordem desejada, preservando o uniqueID de quem já existe.
            lista.ClearArray();

            for (int i = 0; i < Desejadas.Length; i++)
            {
                lista.InsertArrayElementAtIndex(i);
                var e = lista.GetArrayElementAtIndex(i);
                e.FindPropertyRelative("name").stringValue = Desejadas[i].Nome;
                e.FindPropertyRelative("uniqueID").intValue = Desejadas[i].Id;
                e.FindPropertyRelative("locked").boolValue = false;
            }

            so.ApplyModifiedPropertiesWithoutUndo();
            AssetDatabase.SaveAssets();

            Debug.Log($"{Marcador} Camadas criadas, de trás para a frente: " +
                      string.Join(" | ", Desejadas.Select(d => d.Nome)));
            return true;
        }

        /// <summary>Põe todo Tilemap de chão na camada <c>Chao</c>, atrás de tudo.</summary>
        private static void MoverOChao()
        {
            int movidos = 0;

            foreach (var entrada in EditorBuildSettings.scenes)
            {
                if (!entrada.enabled || !File.Exists(entrada.path)) continue;

                Scene cena = EditorSceneManager.OpenScene(entrada.path, OpenSceneMode.Single);
                bool mexeu = false;

                foreach (var tr in cena.GetRootGameObjects()
                             .SelectMany(g => g.GetComponentsInChildren<TilemapRenderer>(true)))
                {
                    // Só o que HOJE está atrás de tudo — o chão. Um tilemap de parede que
                    // participe do Y-sort com os atores fica onde está.
                    if (tr.sortingOrder > -500) continue;

                    tr.sortingLayerName = "Chao";
                    tr.sortingOrder = 0;    // dentro da camada dela, a ordem volta a ser relativa
                    EditorUtility.SetDirty(tr);
                    mexeu = true;
                    movidos++;
                }

                if (mexeu) EditorSceneManager.SaveScene(cena);
            }

            Debug.Log($"{Marcador} {movidos} tilemap(s) de chão movidos para a camada 'Chao'. " +
                      "O número mágico -1000 deixou de governar a profundidade do jogo.");
        }

        // ── sombras ──────────────────────────────────────────────────────────

        private const string CaminhoDaSombra = "Assets/FavelaAmarela/Art/Vfx/sombra_chao.png";

        /// <summary>
        /// Atores que pisam no chão. Lista <b>explícita</b> em vez de varredura: sombra é
        /// decisão de ficção, não de componente.
        ///
        /// <para>De fora ficam <c>ConeDeGelo</c> (projétil), as Patuás e o Necronomicon
        /// (itens no chão), a <c>PedraDePoder</c> (parte do cenário da luta) — e o
        /// <c>EspectroHali</c>, porque <b>espectro que projeta sombra sólida contradiz a
        /// ficção</b>.</para>
        /// </summary>
        private static readonly string[] Atores =
        {
            "Player_Damiao", "Cassilda", "YugNeth",
            "Abdul_Alhazred", "Byakhee", "CoisaDoCemiterio", "Cultista",
            "EsqueletoInvocado", "ReiEmAmarelo",
        };

        [MenuItem("Tools/FavelaAmarela/Render: pôr sombra nos atores")]
        public static void PorSombras()
        {
            var sprite = AssetDatabase.LoadAssetAtPath<Sprite>(CaminhoDaSombra);

            if (sprite == null)
            {
                Debug.LogError($"{Marcador} Sprite da sombra não encontrada em {CaminhoDaSombra}.");
                return;
            }

            int postas = 0, jaTinham = 0;

            foreach (var guid in AssetDatabase.FindAssets("t:Prefab"))
            {
                string caminho = AssetDatabase.GUIDToAssetPath(guid);
                string nome = Path.GetFileNameWithoutExtension(caminho);

                if (!Atores.Contains(nome)) continue;

                var raiz = PrefabUtility.LoadPrefabContents(caminho);

                try
                {
                    if (raiz.GetComponent<SpriteRenderer>() == null)
                    {
                        Debug.LogWarning($"{Marcador} {nome} não tem SpriteRenderer na raiz — " +
                                         "pulado.");
                        continue;
                    }

                    if (raiz.GetComponent<SombraDeChao>() != null) { jaTinham++; continue; }

                    var s = raiz.AddComponent<SombraDeChao>();

                    var so = new SerializedObject(s);
                    so.FindProperty("sprite").objectReferenceValue = sprite;
                    so.ApplyModifiedPropertiesWithoutUndo();

                    PrefabUtility.SaveAsPrefabAsset(raiz, caminho);
                    postas++;
                    Debug.Log($"{Marcador} sombra em {nome}");
                }
                finally
                {
                    PrefabUtility.UnloadPrefabContents(raiz);
                }
            }

            AssetDatabase.SaveAssets();
            Debug.Log($"{Marcador} {postas} sombra(s) postas, {jaTinham} já tinham.");
        }
    }
}
