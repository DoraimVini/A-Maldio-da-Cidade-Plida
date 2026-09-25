using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using FavelaAmarela.Runtime.Enemies;
using FavelaAmarela.Runtime.GameLoop;

namespace FavelaAmarela.EditorTools
{
    /// <summary>
    /// Utilitário de Editor: transforma em <b>prefabs</b> os atores da Tumba que hoje
    /// existem soltos na cena — <b>Abdul Alhazred</b>, as <b>Pedras de Poder</b> e o
    /// <b>Baú da Tumba</b>.
    ///
    /// <para><b>Por quê:</b> todo ator do projeto é prefab (Damião, Cultista, Espectro,
    /// Coisa do Cemitério, Yug-Neth) — só estes três ficaram de fora porque
    /// <c>SetupArenaDoAbdul</c> os criou direto na cena. Sem prefab, não há fonte única:
    /// ajustar ficha, collider ou sprite do boss exige reeditar a cena, e refazer a cena
    /// perderia o trabalho.</para>
    ///
    /// <para>Usa <see cref="PrefabUtility.SaveAsPrefabAssetAndConnect"/>: o objeto que já
    /// está na cena <b>vira uma instância</b> do prefab novo, preservando posição e todas
    /// as referências que apontam para ele (o campo <c>yugNethNaArena</c> do Abdul, o
    /// <c>abdul</c> de cada Pedra, etc.). Idempotente: pula o que já é prefab.</para>
    /// </summary>
    public static class ExtrairPrefabsDaTumba
    {
        private const string PastaEnemies = "Assets/FavelaAmarela/Art/Enemies";
        private const string PastaItems = "Assets/FavelaAmarela/Art/Items";

        [MenuItem("Tools/FavelaAmarela/Extrair Prefabs da Tumba (Abdul, Pedras, Baú)")]
        public static void Extrair()
        {
            var relatorio = new List<string>();

            var abdul = Object.FindAnyObjectByType<AbdulAlhazredAI>(FindObjectsInactive.Include);
            SalvarComoPrefab(abdul, $"{PastaEnemies}/Abdul_Alhazred.prefab", relatorio);

            // Só a primeira Pedra vira prefab; as demais viram instâncias dela, para o
            // ajuste de arte/collider valer para as quatro de uma vez.
            var pedras = Object.FindObjectsByType<PedraDePoder>(FindObjectsInactive.Include);
            ExtrairFamilia(pedras, $"{PastaEnemies}/PedraDePoder.prefab", relatorio);

            var bau = Object.FindAnyObjectByType<BauDaTumba>(FindObjectsInactive.Include);
            SalvarComoPrefab(bau, $"{PastaItems}/Bau_DaTumba.prefab", relatorio);

            EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());

            Debug.Log("[ExtrairPrefabs] " + (relatorio.Count == 0
                ? "Nada a fazer — tudo já é prefab."
                : string.Join("\n  · ", relatorio.ToArray())) +
                "\nCena NÃO foi salva — confira antes.");
        }

        /// <summary>
        /// Converte o primeiro item em prefab e <b>substitui os demais por instâncias
        /// dele</b>, preservando posição/rotação/escala e o nome de cada um.
        /// </summary>
        private static void ExtrairFamilia<T>(T[] itens, string caminho, List<string> relatorio)
            where T : Component
        {
            if (itens == null || itens.Length == 0) return;

            var prefab = SalvarComoPrefab(itens[0], caminho, relatorio);
            if (prefab == null) return;

            for (int i = 1; i < itens.Length; i++)
            {
                var antigo = itens[i];
                if (antigo == null) continue;
                if (PrefabUtility.IsPartOfPrefabInstance(antigo.gameObject)) continue;

                var t = antigo.transform;
                var nova = (GameObject)PrefabUtility.InstantiatePrefab(prefab, t.parent);
                nova.name = antigo.name;
                nova.transform.SetPositionAndRotation(t.position, t.rotation);
                nova.transform.localScale = t.localScale;

                Object.DestroyImmediate(antigo.gameObject);
                relatorio.Add($"{nova.name}: substituído por instância de {Path.GetFileName(caminho)}");
            }
        }

        private static GameObject SalvarComoPrefab(Component alvo, string caminho, List<string> relatorio)
        {
            if (alvo == null)
            {
                relatorio.Add($"{Path.GetFileNameWithoutExtension(caminho)}: nenhum na cena — pulado");
                return null;
            }

            if (PrefabUtility.IsPartOfPrefabInstance(alvo.gameObject))
            {
                relatorio.Add($"{alvo.name}: já é instância de prefab — pulado");
                return PrefabUtility.GetCorrespondingObjectFromSource(alvo.gameObject);
            }

            Directory.CreateDirectory(Path.GetDirectoryName(caminho)!);

            // Conecta: o objeto da cena vira instância do prefab recém-criado, então
            // nenhuma referência existente para ele quebra.
            var prefab = PrefabUtility.SaveAsPrefabAssetAndConnect(
                alvo.gameObject, caminho, InteractionMode.AutomatedAction);

            relatorio.Add($"{alvo.name} → {caminho}");
            return prefab;
        }
    }
}

