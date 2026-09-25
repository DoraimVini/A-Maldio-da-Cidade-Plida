using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using FavelaAmarela.Runtime.UI;

namespace FavelaAmarela.EditorTools
{
    /// <summary>
    /// Ferramenta de Editor. Cria e liga o <see cref="HUDController"/> na cena.
    ///
    /// <para><b>Bug que motivou (playtest 2026-07-31):</b> a barra de Vitalidade nunca
    /// diminuía. A `VitalidadeBar` existia na cena, o dano era aplicado e a POCO
    /// `Vitalidade` caía — mas <b>nada chamava `Bind()` nela</b>, porque o `HUDController`
    /// (o único que faz essa ligação) simplesmente não estava na cena. O `GameManager` o
    /// procura com `FindAnyObjectByType`, recebia `null` e seguia sem reclamar.</para>
    ///
    /// <para>Idempotente: reaproveita um `HUDController` existente e só preenche campos vazios.</para>
    /// </summary>
    public static class MontarHUDController
    {
        [MenuItem("Tools/FavelaAmarela/Montar HUDController na cena")]
        public static void Executar()
        {
            var vitalidadeBar = Object.FindAnyObjectByType<VitalidadeBar>(FindObjectsInactive.Include);
            var barraDeAcoes = Object.FindAnyObjectByType<BarraDeAcoes>(FindObjectsInactive.Include);
            var resilienciaBar = Object.FindAnyObjectByType<ResilienciaBar>(FindObjectsInactive.Include);

            if (vitalidadeBar == null && barraDeAcoes == null && resilienciaBar == null)
            {
                Debug.LogError("[MontarHUDController] Nenhuma view de HUD na cena — nada a ligar.");
                return;
            }

            var hud = Object.FindAnyObjectByType<HUDController>(FindObjectsInactive.Include);
            if (hud == null)
            {
                // Mora no Canvas: é onde as views vivem, e assim ele é destruído junto com
                // elas em vez de sobrar um controlador órfão apontando para nada.
                var canvas = vitalidadeBar != null
                    ? vitalidadeBar.GetComponentInParent<Canvas>()
                    : Object.FindAnyObjectByType<Canvas>(FindObjectsInactive.Include);

                if (canvas == null)
                {
                    Debug.LogError("[MontarHUDController] Não achei um Canvas para hospedar o HUDController.");
                    return;
                }

                hud = Undo.AddComponent<HUDController>(canvas.gameObject);
                Debug.Log($"[MontarHUDController] HUDController criado em '{canvas.name}'.", canvas);
            }

            var so = new SerializedObject(hud);
            int ligados = 0;
            ligados += LigarSeVazio(so, "vitalidadeBar", vitalidadeBar);
            ligados += LigarSeVazio(so, "barraDeAcoes", barraDeAcoes);
            ligados += LigarSeVazio(so, "resilienciaBar", resilienciaBar);
            so.ApplyModifiedPropertiesWithoutUndo();

            EditorUtility.SetDirty(hud);
            var cena = EditorSceneManager.GetActiveScene();
            EditorSceneManager.MarkSceneDirty(cena);
            EditorSceneManager.SaveScene(cena);

            Debug.Log($"[MontarHUDController] Pronto: {ligados} view(s) ligadas ao HUDController. " +
                      "A barra de Vitalidade passa a receber Bind() no bootstrap.");

            if (resilienciaBar == null)
                Debug.LogWarning("[MontarHUDController] Não existe ResilienciaBar nesta cena — " +
                                 "a Resiliência Mental não tem barra. O HUDController cria uma " +
                                 "ResilienciaMental de fallback, mas ninguém a exibe.");
        }

        private static int LigarSeVazio(SerializedObject so, string campo, Object valor)
        {
            if (valor == null) return 0;

            var prop = so.FindProperty(campo);
            if (prop == null)
            {
                Debug.LogWarning($"[MontarHUDController] Campo '{campo}' não existe no HUDController.");
                return 0;
            }

            if (prop.objectReferenceValue != null) return 0; // já ligado: não sobrescreve

            prop.objectReferenceValue = valor;
            Debug.Log($"[MontarHUDController] '{campo}' ligado a '{valor.name}'.");
            return 1;
        }
    }
}
