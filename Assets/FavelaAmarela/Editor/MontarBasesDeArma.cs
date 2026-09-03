using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;
using FavelaAmarela.Inventario;

namespace FavelaAmarela.EditorTools
{
    /// <summary>
    /// Cria as <b>famílias</b> das três armas da Tumba e as liga aos <c>ItemDef</c>.
    ///
    /// <para><b>O que isto conserta.</b> Até 2026-08-27, alcance e forma do golpe eram um campo
    /// do <c>MaoFisicaBridge</c> — <c>alcance = 1.2f</c>, <b>um número só para todas as
    /// armas</b>. O Estilete de Irem e o Alfanje de Alhazred tinham exatamente a mesma pegada,
    /// a mesma área e a mesma janela. Só o dano diferia, o que num ARPG significa que trocar de
    /// arma <b>não era sentido</b>, só lido na ficha.</para>
    ///
    /// <para><b>Os números saem do design que já estava escrito</b>, em
    /// <c>armas_da_tumba.md</c>: Maca é o anti-mago de cadência média; Estilete é
    /// <i>"lâmina fina e rápida"</i> de dano por permanência; Alfanje é <i>"força bruta e
    /// espaço"</i>. Alcance, raio e janela traduzem essas três frases em geometria.</para>
    ///
    /// <para>Idempotente: rodar de novo reescreve os mesmos assets, preservando o GUID — é o
    /// padrão de <c>GeradorDeReliquias</c>.</para>
    /// </summary>
    public static class MontarBasesDeArma
    {
        private const string PastaDasBases = "Assets/FavelaAmarela/Config/Armas";
        private const string PastaDosItens = "Assets/FavelaAmarela/Config/Resources/Itens";

        /// <summary>
        /// A tabela de famílias. <b>A ordem relativa é o que importa</b>: estilete sempre mais
        /// curto e mais rápido que alfanje. Os valores absolutos são botões de balanceamento.
        /// </summary>
        private static readonly (string Asset, string ItemDef, string Familia,
                                 float Alcance, float Raio, float Janela,
                                 string Porque)[] Familias =
        {
            ("BaseArma_LaminaFina", "Item_Arma_EstileteDeIrem", "Lâmina fina",
             0.95f, 0.42f, 0.07f,
             "fura um ponto: curta, estreita e sem perdão de mira — quem erra o passo não acerta"),

            ("BaseArma_Maca", "Item_Arma_MacaDeAklo", "Maca",
             1.20f, 0.60f, 0.10f,
             "o meio-termo do arsenal: é a referência contra a qual as outras duas são lidas"),

            ("BaseArma_Alfanje", "Item_Arma_AlfanjeDeAlhazred", "Alfanje",
             1.60f, 0.85f, 0.15f,
             "varre um arco: alcança, pega mais de um e perdoa a mira — é o 'espaço' do design"),
        };

        [MenuItem("Tools/FavelaAmarela/Armas: montar as bases (famílias)")]
        public static void Executar()
        {
            GarantirPasta();

            var resumo = new List<string>();

            foreach (var f in Familias)
                resumo.Add(Montar(f));

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            Debug.Log("[BasesDeArma] Concluído:\n  " + string.Join("\n  ", resumo));
        }

        private static void GarantirPasta()
        {
            if (AssetDatabase.IsValidFolder(PastaDasBases)) return;

            string pai = Path.GetDirectoryName(PastaDasBases).Replace('\\', '/');
            string nome = Path.GetFileName(PastaDasBases);

            if (!AssetDatabase.IsValidFolder(pai))
            {
                Debug.LogError($"[BasesDeArma] Pasta pai '{pai}' não existe.");
                return;
            }

            AssetDatabase.CreateFolder(pai, nome);
        }

        private static string Montar((string Asset, string ItemDef, string Familia,
                                      float Alcance, float Raio, float Janela,
                                      string Porque) f)
        {
            string caminhoBase = $"{PastaDasBases}/{f.Asset}.asset";

            var baseDeArma = AssetDatabase.LoadAssetAtPath<BaseDeArma>(caminhoBase);
            bool existia = baseDeArma != null;

            if (!existia)
            {
                baseDeArma = ScriptableObject.CreateInstance<BaseDeArma>();
                AssetDatabase.CreateAsset(baseDeArma, caminhoBase);
            }

            baseDeArma.NomeDaFamilia = f.Familia;
            baseDeArma.Alcance = f.Alcance;
            baseDeArma.Raio = f.Raio;
            baseDeArma.JanelaAtiva = f.Janela;

            EditorUtility.SetDirty(baseDeArma);
            AssetDatabase.SaveAssetIfDirty(baseDeArma);

            // Ligar no ItemDef é a metade que faz a base existir para o jogo. Uma base criada
            // e não ligada seria mais uma peça que existe e não está em lugar nenhum -- o modo
            // de falha dominante deste repositório.
            string caminhoItem = $"{PastaDosItens}/{f.ItemDef}.asset";
            var item = AssetDatabase.LoadAssetAtPath<ItemDef>(caminhoItem);

            if (item == null)
                return $"{f.Asset}: base {(existia ? "atualizada" : "criada")}, mas o ItemDef " +
                       $"'{f.ItemDef}' NÃO FOI ENCONTRADO — a base não está ligada a nada";

            item.Base = baseDeArma;

            // A empunhadura passa a ser propriedade da família; o ItemDef segue como fonte
            // para não quebrar as regras de slot já testadas (MaoSecundariaTests).
            baseDeArma.Empunhadura = item.Empunhadura;
            EditorUtility.SetDirty(baseDeArma);

            EditorUtility.SetDirty(item);
            AssetDatabase.SaveAssetIfDirty(item);

            return $"{f.Asset}: alcance {f.Alcance:0.00} · raio {f.Raio:0.00} · " +
                   $"janela {f.Janela:0.000}s → ligada em {f.ItemDef} — {f.Porque}";
        }
    }
}
