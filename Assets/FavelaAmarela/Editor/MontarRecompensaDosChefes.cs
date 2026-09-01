using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;
using FavelaAmarela.Core.Loot;
using FavelaAmarela.Inventario;

namespace FavelaAmarela.EditorTools
{
    /// <summary>
    /// Faz os chefes largarem <b>progressão</b>, e não só o item de quest.
    ///
    /// <para><b>O pedido do Vini (2026-08-28):</b> <i>"Fazer com que os bosses dropem, além de
    /// seus itens como Necronomicon ou o Patuá, uma recompensa ao jogador para que ele sinta a
    /// progressão do personagem."</i> Veio junto de "não tem como ganhar da Byakhee", e é a
    /// resposta certa para aquilo: o problema não era o número do chefe, era não existir nada
    /// entre um chefe e o próximo que deixasse o jogador mais forte.</para>
    ///
    /// <para><b>O estado que isto conserta.</b> <c>Drop_Byakhee</c> tinha <b>uma entrada só</b>
    /// — o Anel do Sinal Amarelo, grau Relíquia, <c>tetoDeItens: 1</c>. Derrotar o chefe que
    /// fecha a Fase 1 entregava um item de rito e <b>zero</b> equipamento. E o <b>Abdul não
    /// tinha tabela de drop nenhuma</b>: o Necronomicon é entregue por código, então abatê-lo
    /// nunca largou uma peça sequer.</para>
    ///
    /// <para><b>Arma E armadura</b>, como pedido. A armadura importa mais do que parece na luta
    /// do Byakhee: ele bate 26 contra a Defesa 6 do Damião, ou seja <b>5 golpes</b> até o
    /// Colapso. Cada ponto de Defesa que o jogador acumula muda essa conta diretamente, porque
    /// a mitigação é subtrativa.</para>
    /// </summary>
    public static class MontarRecompensaDosChefes
    {
        private const string PastaDasTabelas = "Assets/FavelaAmarela/Config/Drops";
        private const string PastaDosItens = "Assets/FavelaAmarela/Config/Resources/Itens";

        /// <summary>
        /// O que cada chefe passa a largar, além do que já largava. Lista escrita à mão de
        /// propósito: <b>qual chefe recompensa com o quê é decisão de design</b>. O que ela não
        /// decide é a força do item — isso vem do grau sorteado e do nível, que sobem sozinhos.
        /// </summary>
        private static readonly Recompensa[] Recompensas =
        {
            new Recompensa("Drop_Byakhee", "Byakhee",
                new[]
                {
                    "Item_Armadura_ColeteDeSucata",

                    // O TRIO DE T2 (2026-09-01). Fechar a Fase 1 é o momento certo para o
                    // primeiro degrau de arma: o jogador chega aqui no nível 3 com uma arma T1
                    // do baú, e sai com uma de tier acima. Os três entram com a mesma chance --
                    // qual deles cai vira variedade entre partidas, em vez de um roteiro.
                    "Item_Arma_AlfanjeDasRuinasPalidas",
                    "Item_Arma_CravoDeAldebaran",
                    "Item_Arma_EstileteDeYhtill",
                }, 3,
                "fecha a Fase 1: peitoral e o PRIMEIRO DEGRAU de arma, para o jogador entrar no " +
                "Castelo diferente de como saiu da Tumba"),

            // O REI EM AMARELO (2026-09-01). Era o único confronto do Vertical Slice que
            // largava ZERO equipamento -- ele não é EnemyBase nem IDanificavel (não tem barra
            // de vida, por design), então ficava de fora do espólio por construção.
            //
            // GARANTIDO, e os três de uma vez. Nas outras tabelas a chance de 0,6 é o que faz
            // duas mortes do mesmo arquétipo renderem coisas diferentes -- mas o Rei é selado
            // UMA vez e a cena acaba. Sorteio só é justo quando se repete: aqui, azar seria
            // definitivo e não teria como ser corrigido jogando.
            //
            // PENDÊNCIA HONESTA: este é o degrau T3, e o Rei é a última luta. A casa real
            // destas três armas é o Templo do Povo Serpente (fases 2-5, que não existem);
            // enquanto ele não existir, isto é melhor que três armas autoradas que NADA no
            // jogo produz. Ver ArmaAlcancavelTests.
            new Recompensa("Drop_ReiEmAmarelo", "ReiEmAmarelo",
                new[]
                {
                    "Item_Arma_AlfanjeDoRei",
                    "Item_Arma_CravoDoSinalAmarelo",
                    "Item_Arma_EstileteDaMascaraPalida",
                }, 3,
                "o desfecho do Vertical Slice deixa de largar nada: o degrau T3 inteiro, " +
                "garantido, porque um rito que acontece uma vez só não comporta sorteio",
                garantido: true, chance: 1f, piso: GrauDeImpregnacao.Impregnado),

            new Recompensa("Drop_Abdul", "Abdul_Alhazred",
                new[] { "Item_Arma_CravoDeAklo", "Item_Armadura_CapuzDeFarrapos" }, 2,
                "primeiro chefe do jogo: a recompensa tem de ser sentida no Cultista seguinte"),
        };

        private readonly struct Recompensa
        {
            public readonly string Tabela, Prefab, Razao;
            public readonly string[] Itens;
            public readonly int Teto;

            /// <summary>Se as entradas ignoram chance e nível mínimo (drop roteirizado).</summary>
            public readonly bool Garantido;

            /// <summary>Probabilidade por entrada, quando não é garantida.</summary>
            public readonly float Chance;

            /// <summary>Piso de grau: a curva sobe daí, nunca desce.</summary>
            public readonly GrauDeImpregnacao Piso;

            public Recompensa(string tabela, string prefab, string[] itens, int teto, string razao,
                              bool garantido = false, float chance = 0.6f,
                              GrauDeImpregnacao piso = GrauDeImpregnacao.Marcado)
            {
                Tabela = tabela; Prefab = prefab; Itens = itens; Teto = teto; Razao = razao;
                Garantido = garantido; Chance = chance; Piso = piso;
            }
        }

        private const string PastaDosInimigos = "Assets/FavelaAmarela/Art/Enemies";

        /// <summary>
        /// Garante o <c>DropAoAbater</c> no prefab do chefe, apontando para a tabela dele.
        ///
        /// <para><b>Criar tabela não é ligar tabela.</b> O <c>Drop_Abdul</c> nasceu nesta mesma
        /// ferramenta e ficou apontando para nada: o prefab do Abdul não tinha o componente, e
        /// antes de hoje nem poderia ter — o <c>DropAoAbater</c> exigia <c>EnemyBase</c>, que o
        /// Abdul não é. É o modo de falha mais repetido deste repositório, e criar o asset sem
        /// ligar seria repeti-lo na mesma sessão em que o descrevi.</para>
        /// </summary>
        private static string LigarComponente(Recompensa r)
        {
            string caminho = $"{PastaDosInimigos}/{r.Prefab}.prefab";
            if (!System.IO.File.Exists(caminho)) return $"  {r.Prefab}: PREFAB AUSENTE";

            var tabela = AssetDatabase.LoadAssetAtPath<TabelaDeDrop>(
                $"{PastaDasTabelas}/{r.Tabela}.asset");

            if (tabela == null) return $"  {r.Prefab}: tabela '{r.Tabela}' ausente";

            var raiz = PrefabUtility.LoadPrefabContents(caminho);

            try
            {
                var drop = raiz.GetComponent<FavelaAmarela.Runtime.Itens.DropAoAbater>();
                bool novo = drop == null;
                if (novo) drop = raiz.AddComponent<FavelaAmarela.Runtime.Itens.DropAoAbater>();

                var so = new SerializedObject(drop);
                var prop = so.FindProperty("tabela");

                if (prop == null) return $"  {r.Prefab}: campo 'tabela' não existe no DropAoAbater";

                bool trocou = prop.objectReferenceValue != tabela;
                prop.objectReferenceValue = tabela;
                so.ApplyModifiedPropertiesWithoutUndo();

                PrefabUtility.SaveAsPrefabAsset(raiz, caminho, out bool gravou);

                if (!gravou) return $"  {r.Prefab}: SaveAsPrefabAsset RECUSOU";

                return novo    ? $"  {r.Prefab}: DropAoAbater CRIADO → {r.Tabela}"
                     : trocou  ? $"  {r.Prefab}: DropAoAbater religado → {r.Tabela}"
                               : $"  {r.Prefab}: DropAoAbater já apontava para {r.Tabela}";
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(raiz);
            }
        }

        [MenuItem("Tools/FavelaAmarela/Itens: montar a recompensa dos chefes")]
        public static void Executar()
        {
            var resumo = new List<string>();

            foreach (var r in Recompensas)
            {
                resumo.Add(Aplicar(r));
                resumo.Add(LigarComponente(r));
            }

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            Debug.Log("[RecompensaDeChefe] Concluído:\n  " + string.Join("\n  ", resumo));
        }

        private static string Aplicar(Recompensa r)
        {
            string nome = r.Tabela, razao = r.Razao;
            string[] itens = r.Itens;
            int teto = r.Teto;

            string caminho = $"{PastaDasTabelas}/{nome}.asset";
            var tabela = AssetDatabase.LoadAssetAtPath<TabelaDeDrop>(caminho);

            // O Abdul não tinha tabela nenhuma: criar é parte do conserto, não um caso de erro.
            bool criada = tabela == null;
            if (criada)
            {
                tabela = ScriptableObject.CreateInstance<TabelaDeDrop>();
                AssetDatabase.CreateAsset(tabela, caminho);
            }

            var so = new SerializedObject(tabela);
            var entradas = so.FindProperty("entradas");

            if (entradas == null || !entradas.isArray)
                return $"{nome}: campo 'entradas' não existe mais no TabelaDeDrop";

            var jaPresentes = new HashSet<Object>();
            for (int i = 0; i < entradas.arraySize; i++)
            {
                var item = entradas.GetArrayElementAtIndex(i).FindPropertyRelative("Item");
                if (item?.objectReferenceValue != null) jaPresentes.Add(item.objectReferenceValue);
            }

            var acrescentados = new List<string>();

            foreach (var idDoItem in itens)
            {
                var def = AssetDatabase.LoadAssetAtPath<ItemDef>($"{PastaDosItens}/{idDoItem}.asset");

                if (def == null)
                {
                    acrescentados.Add($"{idDoItem} AUSENTE");
                    continue;
                }

                // Idempotente: rodar de novo não empilha o mesmo item.
                if (jaPresentes.Contains(def)) continue;

                entradas.arraySize++;
                var nova = entradas.GetArrayElementAtIndex(entradas.arraySize - 1);

                nova.FindPropertyRelative("Item").objectReferenceValue = def;

                // Grau MÍNIMO Marcado por padrão: a curva pode subir daí, nunca descer. Chefe
                // que larga item cinza é chefe que não recompensa -- e "recompensa" era o
                // pedido.
                nova.FindPropertyRelative("Grau").enumValueIndex = (int)r.Piso;

                nova.FindPropertyRelative("Garantido").boolValue = r.Garantido;
                nova.FindPropertyRelative("Chance").floatValue = r.Chance;
                nova.FindPropertyRelative("QuantidadeMin").intValue = 1;
                nova.FindPropertyRelative("QuantidadeMax").intValue = 1;
                nova.FindPropertyRelative("NivelMinimo").intValue = 1;

                acrescentados.Add(def.name);
            }

            var tetoProp = so.FindProperty("tetoDeItens");
            if (tetoProp != null && tetoProp.intValue < teto) tetoProp.intValue = teto;

            // Piso de nível: o do chefe já foi definido em DefinirNivelDasTabelasDeDrop; uma
            // tabela recém-criada precisa do dela.
            var nivelProp = so.FindProperty("nivelDoItem");
            if (nivelProp != null && nivelProp.intValue < 1) nivelProp.intValue = 2;

            so.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(tabela);
            AssetDatabase.SaveAssetIfDirty(tabela);

            string oQue = acrescentados.Count == 0
                ? "nada a acrescentar (já tinha tudo)"
                : string.Join(" + ", acrescentados);

            return $"{nome}{(criada ? " [CRIADA]" : "")}: {oQue}, teto {teto} — {razao}";
        }
    }
}
