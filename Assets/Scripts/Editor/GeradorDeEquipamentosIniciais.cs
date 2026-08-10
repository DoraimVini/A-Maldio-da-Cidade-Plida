using UnityEngine;
using UnityEditor;
using FavelaAmarela.Runtime.Combat;

namespace FavelaAmarela.Editor
{
    [InitializeOnLoad]
    public static class GeradorDeEquipamentosIniciais
    {
        static GeradorDeEquipamentosIniciais()
        {
            EditorApplication.delayCall += GerarAssets;
        }

        private static void GerarAssets()
        {
            string pathPatua = "Assets/FavelaAmarela/Config/Equip_Patua.asset";
            if (AssetDatabase.LoadAssetAtPath<EquipamentoConfig>(pathPatua) == null)
            {
                var patua = ScriptableObject.CreateInstance<EquipamentoConfig>();
                patua.id = "patua_luas_gemeas";
                patua.nome = "Patuá das Luas Gêmeas";
                patua.descricao = "Um amuleto gasto, exalando um frio estranho.";
                patua.slot = SlotDeEquipamento.MaoEsquerda; // Temporariamente Mão Esquerda (off-hand)
                patua.bonusResistenciaAnomala = 10f; // Bônus sugerido para o Byakhee
                
                AssetDatabase.CreateAsset(patua, pathPatua);
                Debug.Log($"[GeradorDeEquipamentos] Criado {pathPatua}");
            }

            string pathNecro = "Assets/FavelaAmarela/Config/Equip_Necronomicon.asset";
            if (AssetDatabase.LoadAssetAtPath<EquipamentoConfig>(pathNecro) == null)
            {
                var necro = ScriptableObject.CreateInstance<EquipamentoConfig>();
                necro.id = "necronomicon_fragmento";
                necro.nome = "Fragmento do Necronomicon";
                necro.descricao = "Páginas arrancadas, pesadas com o sangue de tolos.";
                necro.slot = SlotDeEquipamento.MaoEsquerda; // Temporariamente Mão Esquerda
                necro.bonusConjuracao = 15f; 
                
                AssetDatabase.CreateAsset(necro, pathNecro);
                Debug.Log($"[GeradorDeEquipamentos] Criado {pathNecro}");
            }

            AssetDatabase.SaveAssets();
        }
    }
}
