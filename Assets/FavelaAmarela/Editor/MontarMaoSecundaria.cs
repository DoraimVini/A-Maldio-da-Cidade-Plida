using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using FavelaAmarela.Inventario;

namespace FavelaAmarela.EditorTools
{
    /// <summary>
    /// Autora os dois primeiros itens de <b>Mão Secundária</b> — o slot que existia e não fazia
    /// nada.
    ///
    /// <para><b>O buraco.</b> Até 2026-08-27, <c>MaoSecundaria</c> era o índice 6 da anatomia e
    /// servia só de <i>regra</i>: uma arma <c>DuasMaos</c> o bloqueava. Não havia um único item
    /// autorado para ele — assim como para Amuleto e Anel. <b>Três dos sete slots do corpo do
    /// Damião estavam vazios.</b></para>
    ///
    /// <para><b>A escolha que o slot passa a oferecer:</b> sobreviver mais (Escudo, que apara
    /// golpe) ou conjurar mais (Foco, que desconta recarga). É decisão de <i>build</i>, que é o
    /// que uma mão secundária deve ser num ARPG.</para>
    ///
    /// <para>⚠️ <b>ARTE PROVISÓRIA E NÚMEROS PROVISÓRIOS.</b> Os 15 ícones do projeto já têm
    /// dono, então estes dois emprestam ícone — não há arte de escudo nem de foco. E os valores
    /// abaixo são um ponto de partida conservador escolhido por mim, não balanceamento
    /// jogado: são botões do Vini. O precedente de arte emprestada é o do projeto (Cassilda,
    /// fragmentos, Rei em Amarelo).</para>
    /// </summary>
    public static class MontarMaoSecundaria
    {
        private const string PastaDosItens = "Assets/FavelaAmarela/Config/Resources/Itens";
        private const string PastaDosIcones = "Assets/FavelaAmarela/Art/Items/Icones";

        [MenuItem("Tools/FavelaAmarela/Itens: montar a Mão Secundária")]
        public static void Executar()
        {
            var resumo = new List<string>
            {
                Montar(
                    arquivo: "Item_Escudo_BroquelDeCouro",
                    id: "broquel_couro_ressecado",
                    nome: "Broquel de Couro Ressecado",
                    descricao: "Couro que já foi de alguma coisa viva. Apara o que consegue.",
                    icone: "Icone_ColeteDeSucata",
                    funcao: FuncaoDeMaoSecundaria.Escudo,
                    potencia: 0.20f,      // 20% de chance de aparar (teto do sistema é 60%)
                    reducao: 0.50f,       // apara metade do golpe quando bloqueia
                    modificadores: new[] { new ModificadorFixo(StatType.DefesaFisica, 2f) }),

                Montar(
                    arquivo: "Item_Foco_EstilhacoDeAldebaran",
                    id: "estilhaco_aldebaran",
                    nome: "Estilhaço de Aldebaran",
                    descricao: "Um caco de espelho que insiste em refletir outra sala.",
                    icone: "Icone_AnelDoSinalAmarelo",
                    funcao: FuncaoDeMaoSecundaria.Foco,
                    potencia: 0.25f,      // 25% de recarga descontada (teto do sistema é 80%)
                    reducao: 0f,
                    modificadores: new[] { new ModificadorFixo(StatType.TraumaAnomalia, 3f) }),
            };

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            Debug.Log("[MaoSecundaria] Concluído:\n  " + string.Join("\n  ", resumo));
        }

        private static string Montar(string arquivo, string id, string nome, string descricao,
                                     string icone, FuncaoDeMaoSecundaria funcao,
                                     float potencia, float reducao,
                                     ModificadorFixo[] modificadores)
        {
            string caminho = $"{PastaDosItens}/{arquivo}.asset";

            var def = AssetDatabase.LoadAssetAtPath<ItemDef>(caminho);
            bool existia = def != null;

            if (!existia)
            {
                def = ScriptableObject.CreateInstance<ItemDef>();
                AssetDatabase.CreateAsset(def, caminho);
            }

            def.Id = id;
            def.Nome = nome;
            def.Descricao = descricao;
            def.Tipo = ItemType.Armadura;
            def.SlotEquipamento = EquipmentSlot.MaoSecundaria;
            def.EmpilhamentoMaximo = 1;
            def.Empunhadura = Empunhadura.UmaMao;
            def.Funcao = funcao;
            def.PotenciaDaMaoSecundaria = potencia;
            def.ReducaoAoBloquear = reducao;
            def.Modificadores = new List<ModificadorFixo>(modificadores);

            // Ícone é OBRIGATÓRIO: IconesDosItensTests varre Assets por completo e derruba a
            // suíte inteira no primeiro item sem ele.
            var sprite = AssetDatabase.LoadAssetAtPath<Sprite>($"{PastaDosIcones}/{icone}.png");
            if (sprite == null)
                return $"{arquivo}: ÍCONE '{icone}' NÃO ENCONTRADO — o item ficaria sem ícone e " +
                       "derrubaria a suíte. Nada foi gravado.";

            def.Icone = sprite;

            EditorUtility.SetDirty(def);
            AssetDatabase.SaveAssetIfDirty(def);

            string efeito = funcao == FuncaoDeMaoSecundaria.Escudo
                ? $"apara {potencia:P0} dos golpes, cortando {reducao:P0} do dano"
                : $"desconta {potencia:P0} da recarga da habilidade";

            return $"{nome}: {efeito} [{(existia ? "atualizado" : "criado")}, " +
                   $"ícone provisório '{icone}']";
        }
    }
}
