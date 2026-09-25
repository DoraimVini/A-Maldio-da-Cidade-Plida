using UnityEngine;
using UnityEditor;
using FavelaAmarela.Inventario;
using System.Collections.Generic;
using System.IO;

namespace FavelaAmarela.Editor
{
    /// <summary>
    /// Script de Editor responsável por gerar os 4 assets ScriptableObject das Relíquias de Hali.
    /// Garante a criação segura dos arquivos, preservando GUIDs existentes e validando nomes.
    /// </summary>
    public static class GeradorDeReliquias
    {
        private const string Path = "Assets/FavelaAmarela/Art/Items/Reliquias";

        [MenuItem("Tools/FavelaAmarela/Gerar Relíquias (Pilar 3)")]
        public static void GerarReliquias()
        {
            if (!GarantirPastas())
            {
                Debug.LogError("[GeradorDeReliquias] Falha ao criar estrutura de pastas. Abortando.");
                return;
            }

            CriarReliquia("Item_Reliquia_Necronomicon.asset", "O Necronomicon",
                "O Al-Azif. Permite decodificar inscrições antigas, mas a proximidade com suas páginas afasta a razão.",
                ItemType.Chave, EquipmentSlot.Nenhum,
                new ModificadorFixo { Stat = StatType.TraumaAnomalia, Valor = 10f },
                new ModificadorFixo { Stat = StatType.DrenoRM, Valor = 1f }
            );

            CriarReliquia("Item_Reliquia_AnelSinalAmarelo.asset", "Anel do Sinal Amarelo",
                "Um anel forjado no escuro de Hali. Sombras e sentinelas desviam o olhar do portador.",
                ItemType.Amuleto, EquipmentSlot.Anel,
                new ModificadorFixo { Stat = StatType.Furtividade, Valor = 0.3f },
                new ModificadorFixo { Stat = StatType.RMMaxima, Valor = 20f }
            );

            CriarReliquia("Item_Reliquia_ElmoDeSet.asset", "Elmo de Set",
                "Coroa de escamas e ossos. Parte do lendário Set de Set. Seus olhos vazios ainda enxergam.",
                ItemType.Armadura, EquipmentSlot.Elmo,
                new ModificadorFixo { Stat = StatType.DefesaFisica, Valor = 5f },
                new ModificadorFixo { Stat = StatType.VitMaxima, Valor = 15f }
            );

            CriarReliquia("Item_Reliquia_PatuaLuasGemeas.asset", "Patuá das Luas Gêmeas",
                "Tecido com fios das vestes de Yhtill. A canção de Cassilda ecoa baixinho nele, afastando o escuro.",
                ItemType.Amuleto, EquipmentSlot.Amuleto,
                new ModificadorFixo { Stat = StatType.RegenRM, Valor = 1.5f }
            );

            AssetDatabase.Refresh();
            Debug.Log("[GeradorDeReliquias] As 4 Relíquias foram geradas e atualizadas com sucesso na pasta " + Path);
        }

        /// <summary>
        /// Garante a existência da hierarquia de pastas necessária.
        /// Retorna false se houver conflito (ex.: existe um arquivo com o nome de uma pasta).
        /// </summary>
        private static bool GarantirPastas()
        {
            string[] pastas = {
                "Assets/FavelaAmarela",
                "Assets/FavelaAmarela/Art",
                "Assets/FavelaAmarela/Art/Items",
                Path
            };

            string current = "Assets";
            foreach (string pasta in pastas)
            {
                if (AssetDatabase.IsValidFolder(pasta))
                {
                    current = pasta;
                    continue;
                }

                // Verifica se existe um arquivo com o nome da pasta (impediria criação)
                string parent = current;
                string folderName = pasta.Substring(pasta.LastIndexOf('/') + 1);
                string potentialFile = $"{parent}/{folderName}";

                if (AssetDatabase.LoadAssetAtPath<Object>(potentialFile) != null)
                {
                    Debug.LogError($"[GeradorDeReliquias] Não foi possível criar a pasta '{pasta}' porque existe um arquivo com o mesmo nome em '{potentialFile}'.");
                    return false;
                }

                string createdFolder = AssetDatabase.CreateFolder(parent, folderName);
                if (string.IsNullOrEmpty(createdFolder))
                {
                    Debug.LogError($"[GeradorDeReliquias] Falha ao criar a pasta '{pasta}'.");
                    return false;
                }

                current = pasta;
            }

            return true;
        }

        /// <summary>
        /// Cria ou atualiza um asset de relíquia com validação de nome e preservação de GUID.
        /// </summary>
        private static void CriarReliquia(string fileName, string nome, string desc, ItemType tipo, EquipmentSlot slot, params ModificadorFixo[] mods)
        {
            // Validação do nome do arquivo
            if (string.IsNullOrEmpty(fileName))
            {
                Debug.LogError("[GeradorDeReliquias] Nome de arquivo não pode ser nulo ou vazio.");
                return;
            }

            char[] invalidChars = System.IO.Path.GetInvalidFileNameChars();
            if (fileName.IndexOfAny(invalidChars) >= 0)
            {
                Debug.LogError($"[GeradorDeReliquias] Nome de arquivo '{fileName}' contém caracteres inválidos.");
                return;
            }

            string fullPath = $"{Path}/{fileName}";
            ItemDef asset = AssetDatabase.LoadAssetAtPath<ItemDef>(fullPath);
            bool existed = asset != null;

            if (!existed)
            {
                asset = ScriptableObject.CreateInstance<ItemDef>();
                asset.Id = System.Guid.NewGuid().ToString(); // GUID único, gerado apenas na criação
                AssetDatabase.CreateAsset(asset, fullPath);
            }
            else
            {
                // Se o asset já existe, pergunta antes de sobrescrever
                if (!EditorUtility.DisplayDialog(
                    "Relíquia já existe",
                    $"O asset '{fileName}' já existe.\n\nDeseja sobrescrever seus campos (nome, descrição, tipo, modificadores)?\n\nO GUID (identificador único) será preservado para não quebrar referências.",
                    "Sim, sobrescrever", "Cancelar"))
                {
                    Debug.Log($"[GeradorDeReliquias] Pulando '{fileName}' (mantido como está).");
                    return;
                }
            }

            // Atualiza campos (mantendo Id se já existia)
            asset.Nome = nome;
            asset.Descricao = desc;
            asset.Tipo = tipo;
            asset.SlotEquipamento = slot;
            asset.EmpilhamentoMaximo = 1;
            asset.Modificadores = new List<ModificadorFixo>(mods);

            EditorUtility.SetDirty(asset);
            AssetDatabase.SaveAssetIfDirty(asset);
            Debug.Log($"[GeradorDeReliquias] {(existed ? "Atualizado" : "Criado")}: {fileName}");
        }
    }
}
