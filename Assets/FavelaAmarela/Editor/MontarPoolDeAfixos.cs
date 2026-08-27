using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using FavelaAmarela.Inventario;

namespace FavelaAmarela.EditorTools
{
    /// <summary>
    /// Autora o <b>pool inicial de afixos</b> — o conteúdo sem o qual o gerador existe e não
    /// produz nada.
    ///
    /// <para><b>Duas regras vinculantes.</b></para>
    ///
    /// <para><b>1. Só <c>StatType</c> que o jogo consome.</b> Cinco dos quinze são decorativos:
    /// <c>RCMaxima</c>, <c>Velocidade</c> e <c>Furtividade</c> não têm consumidor nenhum;
    /// <c>DefesaAnomalia</c> é <b>exibida na ficha e não aplicada no combate</b>; e
    /// <c>RMMaxima</c> só funciona como consumível. Um afixo que role qualquer um deles produz
    /// um item que <b>mente para o jogador</b> — ele lê o número, paga o slot e não recebe
    /// nada. Guardado por <c>PoolDeAfixosTests</c>.</para>
    ///
    /// <para><b>2. Nomes diegéticos</b> (skill <c>favela-lore-enforcer</c>). Nada de
    /// "Comum/Raro/Épico": os rótulos são do vocabulário de Carcosa, e o grau já tem os nomes
    /// dele — Inerte, Marcado, Impregnado, Relíquia.</para>
    ///
    /// <para><b>Fica de fora, de propósito:</b> a <b>contrapartida do grau Impregnado</b>. O
    /// <c>loot_e_drop.md</c> registra que "o que exatamente o grau alto cobra" é decisão em
    /// aberto do Vini — inventar aqui seria decidir design por conta própria, que o
    /// <c>CLAUDE.md</c> §1 proíbe.</para>
    /// </summary>
    public static class MontarPoolDeAfixos
    {
        /// <summary>Precisa estar sob <c>Resources/</c>: é de lá que o catálogo carrega.</summary>
        private const string Pasta = "Assets/FavelaAmarela/Config/Resources/Afixos";

        private static readonly (string Id, TipoDeAfixo Tipo, string Rotulo, StatType Stat,
                                 float Min, float Max, int Nivel, float Peso, string Grupo,
                                 EquipmentSlot[] Slots)[] Pool =
        {
            // ── Prefixos ──────────────────────────────────────────────────────
            ("afixo_cravado", TipoDeAfixo.Prefixo, "Cravado", StatType.TraumaFisico,
             2f, 5f, 1, 1f, "trauma_fisico", new[] { EquipmentSlot.Arma }),

            ("afixo_sussurrante", TipoDeAfixo.Prefixo, "Sussurrante", StatType.TraumaAnomalia,
             3f, 8f, 2, 0.7f, "trauma_anomalia", new[] { EquipmentSlot.Arma }),

            ("afixo_endurecido", TipoDeAfixo.Prefixo, "Endurecido", StatType.DefesaFisica,
             1f, 3f, 1, 1f, "defesa_fisica",
             new[] { EquipmentSlot.Elmo, EquipmentSlot.Peitoral, EquipmentSlot.Grevas }),

            ("afixo_encorpado", TipoDeAfixo.Prefixo, "Encorpado", StatType.VitMaxima,
             8f, 20f, 1, 1f, "vitalidade",
             new[] { EquipmentSlot.Elmo, EquipmentSlot.Peitoral, EquipmentSlot.Grevas }),

            // ── Sufixos ───────────────────────────────────────────────────────
            ("afixo_do_sinal", TipoDeAfixo.Sufixo, "do Sinal", StatType.TraumaAnomalia,
             2f, 6f, 3, 0.6f, "trauma_anomalia", new EquipmentSlot[0]),

            ("afixo_de_irem", TipoDeAfixo.Sufixo, "de Irem", StatType.VigorMaximo,
             5f, 15f, 1, 1f, "vigor", new EquipmentSlot[0]),

            ("afixo_da_vigilia", TipoDeAfixo.Sufixo, "da Vigília", StatType.RegenRM,
             0.2f, 0.6f, 2, 0.8f, "regen_rm", new EquipmentSlot[0]),

            ("afixo_do_peregrino", TipoDeAfixo.Sufixo, "do Peregrino", StatType.RegeneracaoVigor,
             0.5f, 1.5f, 1, 1f, "regen_vigor", new EquipmentSlot[0]),
        };

        [MenuItem("Tools/FavelaAmarela/Itens: montar o pool de afixos")]
        public static void Executar()
        {
            GarantirPasta();

            var resumo = new List<string>();

            foreach (var a in Pool)
                resumo.Add(Montar(a));

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            Debug.Log($"[PoolDeAfixos] {Pool.Length} afixo(s):\n  " + string.Join("\n  ", resumo));
        }

        private static void GarantirPasta()
        {
            if (AssetDatabase.IsValidFolder(Pasta)) return;
            AssetDatabase.CreateFolder("Assets/FavelaAmarela/Config/Resources", "Afixos");
        }

        private static string Montar((string Id, TipoDeAfixo Tipo, string Rotulo, StatType Stat,
                                      float Min, float Max, int Nivel, float Peso, string Grupo,
                                      EquipmentSlot[] Slots) a)
        {
            string caminho = $"{Pasta}/{a.Id}.asset";

            var def = AssetDatabase.LoadAssetAtPath<AfixoDef>(caminho);
            bool existia = def != null;

            if (!existia)
            {
                def = ScriptableObject.CreateInstance<AfixoDef>();
                AssetDatabase.CreateAsset(def, caminho);
            }

            def.Id = a.Id;
            def.Tipo = a.Tipo;
            def.Rotulo = a.Rotulo;
            def.Stat = a.Stat;
            def.ValorMin = a.Min;
            def.ValorMax = a.Max;
            def.NivelMinimoDoItem = a.Nivel;
            def.Peso = a.Peso;
            def.GrupoDeExclusao = a.Grupo;
            def.SlotsPermitidos = a.Slots;

            EditorUtility.SetDirty(def);
            AssetDatabase.SaveAssetIfDirty(def);

            string onde = a.Slots.Length == 0 ? "qualquer slot" : string.Join("/", a.Slots);

            return $"{a.Rotulo} ({a.Tipo}): {a.Stat} {a.Min:0.##}–{a.Max:0.##}, " +
                   $"nível {a.Nivel}+, peso {a.Peso:0.##}, {onde} " +
                   $"[{(existia ? "atualizado" : "criado")}]";
        }
    }
}
