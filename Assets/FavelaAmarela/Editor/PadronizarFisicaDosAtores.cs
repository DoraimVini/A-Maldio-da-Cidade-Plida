using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace FavelaAmarela.EditorTools
{
    /// <summary>
    /// Põe todo ator dinâmico no padrão de física do projeto.
    ///
    /// <para><b>O defeito (relatado pelo Vini em 2026-08-21):</b> <i>"peguei uma coisa muito
    /// estranha nos mobs e até no boss, você não trabalha freeze rotation"</i>. Auditando os
    /// <c>Rigidbody2D</c>, <b>quatro corpos dinâmicos estavam sem <c>FreezeRotation</c></b>:
    /// <c>Byakhee.prefab</c> (o chefe), <c>Cortesao_Palido_0</c> e <c>_1</c> (os mobs do
    /// Castelo) e o Damião da <c>cena_1</c> (legado, fora da build). O casamento com o relato é
    /// exato: os mobs e o boss.</para>
    ///
    /// <para><b>Por que gira:</b> um corpo <c>Dynamic</c> que leva impulso de colisão fora do
    /// centro ganha velocidade angular. Com <c>gravityScale 0</c> e <c>angularDamping</c> padrão
    /// (0,05), nada zera isso rápido — o <c>transform</c> roda, e como o <c>SpriteRenderer</c>
    /// está no mesmo <c>GameObject</c>, <b>o sprite gira junto</b>. Num jogo isométrico com
    /// profundidade fingida por <c>sortingOrder</c>, personagem rodando destrói a ilusão
    /// inteira — e ainda gira o colisor junto, mudando a pegada a cada quadro.</para>
    ///
    /// <para><b>Isto não estava na skill.</b> <c>favela-isometric-standards</c> manda
    /// <c>gravityScale = 0</c>, câmera sem tilt, PPU 32 e Y-sorting — e <b>não diz nada sobre
    /// rotação</b>. A lacuna estava no padrão, não só nos prefabs; a skill foi atualizada junto
    /// com esta ferramenta.</para>
    ///
    /// <para><b>Segundo achado, do mesmo levantamento:</b> o <c>CLAUDE.md</c> §5 manda
    /// <c>CollisionDetectionMode2D.Continuous</c> para atores que se movem, e <b>sete dos nove
    /// estavam em <c>Discrete</c></b> — inclusive o Damião. Discrete deixa ator rápido atravessar
    /// parede fina entre dois <c>FixedUpdate</c>.</para>
    /// </summary>
    public static class PadronizarFisicaDosAtores
    {
        private const string PastaDeArte = "Assets/FavelaAmarela/Art";

        private static readonly string[] Cenas =
        {
            "Assets/Scenes/Deserto_Hali.unity",
            "Assets/Scenes/Playtest_RuinasPalidas.unity",
            "Assets/Scenes/Santuario_Yhtill.unity",
            "Assets/Scenes/Portoes_Das_Ruinas.unity",
            "Assets/Scenes/Castelo_Carcosa.unity",
        };

        [MenuItem("Tools/FavelaAmarela/Física: padronizar os atores")]
        public static void Executar()
        {
            var resumo = new List<string>();

            foreach (var caminho in Directory.GetFiles(PastaDeArte, "*.prefab",
                                                       SearchOption.AllDirectories))
            {
                string linha = CorrigirPrefab(caminho);
                if (linha != null) resumo.Add(linha);
            }

            foreach (var cena in Cenas)
            {
                string linha = CorrigirCena(cena);
                if (linha != null) resumo.Add(linha);
            }

            Debug.Log(resumo.Count == 0
                ? "[PadronizarFisicaDosAtores] Nada a corrigir — todos já no padrão."
                : "[PadronizarFisicaDosAtores] Corrigido:\n  " + string.Join("\n  ", resumo));
        }

        private static string CorrigirPrefab(string caminho)
        {
            var raiz = PrefabUtility.LoadPrefabContents(caminho);
            if (raiz == null) return null;

            string linha = null;

            try
            {
                var mudancas = new List<string>();

                foreach (var rb in raiz.GetComponentsInChildren<Rigidbody2D>(true))
                    mudancas.AddRange(Padronizar(rb));

                if (mudancas.Count == 0) return null;

                PrefabUtility.SaveAsPrefabAsset(raiz, caminho, out bool salvou);
                linha = salvou
                    ? $"{Path.GetFileName(caminho)}: {string.Join(", ", mudancas)}"
                    : $"{Path.GetFileName(caminho)}: SaveAsPrefabAsset RECUSOU";
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(raiz);
            }

            return linha;
        }

        private static string CorrigirCena(string caminho)
        {
            if (!File.Exists(caminho)) return $"{Path.GetFileName(caminho)}: cena ausente";

            var cena = EditorSceneManager.OpenScene(caminho, OpenSceneMode.Single);
            var mudancas = new List<string>();

            foreach (var rb in Object.FindObjectsByType<Rigidbody2D>(
                         FindObjectsInactive.Include, FindObjectsSortMode.None))
            {
                var feitas = Padronizar(rb);
                foreach (var f in feitas) mudancas.Add($"{rb.name} ({f})");
            }

            if (mudancas.Count == 0) return null;

            EditorSceneManager.MarkSceneDirty(cena);
            bool salvou = EditorSceneManager.SaveScene(cena);

            return salvou
                ? $"{Path.GetFileName(caminho)}: {string.Join(", ", mudancas)}"
                : $"{Path.GetFileName(caminho)}: SaveScene RECUSOU";
        }

        /// <summary>
        /// Aplica o padrão a um corpo e devolve o que mudou. Só mexe em <c>Dynamic</c>:
        /// <c>Kinematic</c> e <c>Static</c> não recebem impulso, então travar rotação neles
        /// seria ruído.
        /// </summary>
        private static List<string> Padronizar(Rigidbody2D rb)
        {
            var mudancas = new List<string>();

            if (rb.bodyType != RigidbodyType2D.Dynamic) return mudancas;

            if ((rb.constraints & RigidbodyConstraints2D.FreezeRotation) == 0)
            {
                rb.constraints |= RigidbodyConstraints2D.FreezeRotation;
                mudancas.Add("FreezeRotation");
            }

            if (rb.collisionDetectionMode != CollisionDetectionMode2D.Continuous)
            {
                rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
                mudancas.Add("Continuous");
            }

            if (!Mathf.Approximately(rb.gravityScale, 0f))
            {
                rb.gravityScale = 0f;
                mudancas.Add("gravity→0");
            }

            // Um corpo que já girou carrega a rotação no transform: travar daqui em diante não
            // desfaz o estrago acumulado.
            if (!Mathf.Approximately(rb.transform.eulerAngles.z, 0f))
            {
                rb.transform.rotation = Quaternion.identity;
                mudancas.Add("rotação zerada");
            }

            return mudancas;
        }
    }
}
