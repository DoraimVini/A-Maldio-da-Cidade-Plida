using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using FavelaAmarela.Runtime.Enemies;

namespace FavelaAmarela.EditorTools
{
    /// <summary>
    /// Liga os quadros do vulto nos <see cref="EcoDeCarcosa"/> das cenas do Build Settings.
    ///
    /// <para><b>O que isto conserta.</b> Medido em 2026-09-04 no YAML do
    /// <c>Castelo_Carcosa.unity</c>: <c>Eco_De_Carcosa_0</c> e <c>Eco_De_Carcosa_1</c>, os dois
    /// da Biblioteca (Z3), tinham <b>zero filhos e nenhum <c>SpriteRenderer</c></b>. E
    /// <c>EcoDeCarcosa.AtivarEco()</c> mostra o Eco fazendo
    /// <c>foreach (Transform child in transform) child.SetActive(true)</c> — ou seja, ligava um
    /// conjunto vazio. O Eco se manifestava, drenava 3 de Resiliência Mental por segundo e
    /// <b>nada aparecia na tela</b>.</para>
    ///
    /// <para>Num jogo em que Resiliência zerada é derrota, isso não é falta de polimento: é uma
    /// regra que o jogador não tem como aprender. Ele fica parado lendo a Biblioteca, a sanidade
    /// cai, e a única pista do porquê era um <c>Debug.Log</c> no console do Editor.</para>
    ///
    /// <para><b>Idempotente:</b> reescreve a mesma lista de quadros. Rodar duas vezes não
    /// acumula nada. O <c>Visual_Eco</c> em si não é criado aqui — quem o cria é
    /// <c>EcoDeCarcosa.GarantirVisual</c>, em runtime, para uma cena futura que nasça sem ele
    /// não repetir este bug.</para>
    /// </summary>
    public static class VestirOsEcosDeCarcosa
    {
        private const string PastaDaArte = "Assets/FavelaAmarela/Art/Enemies/EcoDeCarcosa";
        private const int Quadros = 4;

        [MenuItem("Tools/FavelaAmarela/Cena: vestir os Ecos de Carcosa")]
        public static void Executar()
        {
            if (!Application.isBatchMode &&
                !EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
            {
                Debug.Log("[EcosDeCarcosa] Cancelado — havia cena modificada por salvar.");
                return;
            }

            var quadros = Enumerable.Range(0, Quadros)
                .Select(i => AssetDatabase.LoadAssetAtPath<Sprite>($"{PastaDaArte}/Eco_{i}.png"))
                .ToArray();

            if (quadros.Any(q => q == null))
            {
                Debug.LogError($"[EcosDeCarcosa] Falta quadro em '{PastaDaArte}'. Esperados " +
                               $"Eco_0..Eco_{Quadros - 1}.png. Sem eles a ferramenta ligaria " +
                               "referências vazias, que é o mesmo que não rodar — só que em " +
                               "silêncio.");
                return;
            }

            var log = new StringBuilder("[EcosDeCarcosa]\n");
            int vestidos = 0;

            foreach (var entrada in EditorBuildSettings.scenes)
            {
                if (!entrada.enabled || !File.Exists(entrada.path)) continue;

                Scene cena = EditorSceneManager.OpenScene(entrada.path, OpenSceneMode.Single);
                string nomeDaCena = Path.GetFileNameWithoutExtension(entrada.path);
                bool mexeu = false;

                foreach (var eco in cena.GetRootGameObjects()
                             .SelectMany(r => r.GetComponentsInChildren<EcoDeCarcosa>(true)))
                {
                    Escrever(eco, "quadros", quadros);
                    EditorUtility.SetDirty(eco);

                    log.AppendLine($"   {nomeDaCena} / {eco.name}  <- {Quadros} quadro(s)");
                    vestidos++;
                    mexeu = true;
                }

                if (!mexeu) continue;

                EditorSceneManager.MarkSceneDirty(cena);
                EditorSceneManager.SaveScene(cena);
            }

            log.AppendLine($"   total: {vestidos} Eco(s)");

            if (vestidos == 0)
                log.AppendLine("   NENHUM Eco encontrado — ou eles foram removidos das cenas, " +
                               "ou o componente foi renomeado.");

            Debug.Log(log.ToString());
        }

        /// <summary>
        /// Escreve num campo serializado privado. <c>SerializedObject</c> e não reflexão pura:
        /// só ele marca a cena como suja de um jeito que sobrevive ao salvamento — atribuir por
        /// <c>FieldInfo</c> muda o objeto em memória e a Unity grava o valor antigo. Mesma
        /// função que <c>MontarPosteNosRefugios</c> usa.
        /// </summary>
        private static void Escrever(Object alvo, string campo, Sprite[] valor)
        {
            var so = new SerializedObject(alvo);
            var prop = so.FindProperty(campo);

            if (prop == null)
            {
                Debug.LogError($"[EcosDeCarcosa] Campo '{campo}' não existe em " +
                               $"{alvo.GetType().Name}. Ele foi renomeado, e a ligação ficaria " +
                               "muda.", alvo);
                return;
            }

            prop.arraySize = valor.Length;
            for (int i = 0; i < valor.Length; i++)
                prop.GetArrayElementAtIndex(i).objectReferenceValue = valor[i];

            so.ApplyModifiedPropertiesWithoutUndo();
        }
    }
}
