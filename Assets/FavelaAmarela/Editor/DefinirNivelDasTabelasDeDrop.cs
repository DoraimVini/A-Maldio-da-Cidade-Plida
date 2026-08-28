using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
using FavelaAmarela.Inventario;

namespace FavelaAmarela.EditorTools
{
    /// <summary>
    /// Escreve o <b>nível do item</b> em cada tabela de drop — o número que abre o pool de
    /// afixos.
    ///
    /// <para><b>O defeito (2026-08-28).</b> <c>TabelaDeDrop.nivelDoItem</c> existe no C# desde a
    /// Fase 5 da itemização, e <b>nenhum dos três assets o tinha serializado</b>: a chave
    /// simplesmente não estava no YAML. Todo item que caía no jogo nascia <b>nível 1</b>.</para>
    ///
    /// <para>Consequência medida: dos oito afixos autorados, <b>três nunca podiam rolar</b> —
    /// <c>afixo_sussurrante</c> e <c>afixo_da_vigilia</c> pedem nível 2, <c>afixo_do_sinal</c>
    /// pede 3. Trinta e sete por cento do conteúdo de afixo era inalcançável, e nada no jogo
    /// nem no console dizia isso. É o mesmo modo de falha do array dimensionado e vazio: o campo
    /// existe, o Inspector mostra um valor plausível, e o dado não está lá.</para>
    ///
    /// <para><b>Os números são PISO, não teto.</b> A partir da Fase 3 do plano o nível do item
    /// passa a ser <c>max(piso da tabela, nível do jogador + variação)</c> — a tabela diz "um
    /// chefe nunca larga lixo de nível 1", e a progressão do jogador empurra para cima.</para>
    /// </summary>
    public static class DefinirNivelDasTabelasDeDrop
    {
        private const string PastaDasTabelas = "Assets/FavelaAmarela/Config/Drops";

        /// <summary>
        /// O piso de nível de cada fonte de espólio, com a razão. Lista escrita à mão de
        /// propósito: <b>qual fonte entrega item melhor é decisão de design</b>, não algo
        /// derivável do asset. O que a lista não decide é o efeito — esse vem do
        /// <c>AfixoDef.NivelMinimoDoItem</c>, que já está autorado.
        /// </summary>
        private static readonly (string Tabela, int Nivel, string Razao)[] Pisos =
        {
            ("Drop_Cultista", 1,
             "tropa comum da Fase 1: é o piso do jogo, e o que faz o afixo de nível 1 ser o " +
             "normal em vez do teto"),

            ("Drop_BauDaTumba", 2,
             "o baú entrega uma das três armas da Tumba — recompensa de dungeon tem de valer " +
             "mais que abate de tropa, e o nível 2 abre 'sussurrante' e 'da vigília'"),

            ("Drop_Byakhee", 3,
             "chefe que fecha a Fase 1: abre o pool inteiro, incluindo 'do Sinal' (nível 3), " +
             "que hoje é conteúdo inalcançável"),

            ("Drop_Abdul", 2,
             "primeiro chefe do jogo: acima da tropa, abaixo do que fecha a fase — a " +
             "recompensa dele tem de ser sentida no Cultista seguinte, não igualar a do Byakhee"),
        };

        [MenuItem("Tools/FavelaAmarela/Itens: definir o nível das tabelas de drop")]
        public static void Executar()
        {
            var resumo = new List<string>();
            var vistas = new HashSet<string>();

            foreach (var (nome, nivel, razao) in Pisos)
            {
                vistas.Add(nome);
                resumo.Add(Aplicar(nome, nivel, razao));
            }

            // Tabela nova que ninguém acrescentou aqui fica com o default do C# e some do radar.
            // Denunciar é mais barato que descobrir depois que um chefe larga item de nível 1.
            foreach (var caminho in Directory.GetFiles(PastaDasTabelas, "*.asset").OrderBy(c => c))
            {
                string nome = Path.GetFileNameWithoutExtension(caminho);
                if (!vistas.Contains(nome))
                    resumo.Add($"{nome}: TABELA NOVA sem piso declarado — acrescente em " +
                               "DefinirNivelDasTabelasDeDrop.Pisos, com a razão");
            }

            Debug.Log("[NivelDeDrop] Concluído:\n  " + string.Join("\n  ", resumo));
        }

        private static string Aplicar(string nome, int nivel, string razao)
        {
            string caminho = $"{PastaDasTabelas}/{nome}.asset";

            var tabela = AssetDatabase.LoadAssetAtPath<TabelaDeDrop>(caminho);
            if (tabela == null) return $"{nome}: ASSET AUSENTE em '{caminho}'";

            var so = new SerializedObject(tabela);
            var prop = so.FindProperty("nivelDoItem");

            if (prop == null)
                return $"{nome}: campo 'nivelDoItem' não existe mais no TabelaDeDrop";

            int antes = prop.intValue;

            // Escreve SEMPRE, mesmo quando o valor já bate. "Já está em 1" pode significar duas
            // coisas muito diferentes: o asset declara 1, ou o asset não declara nada e o 1 vem
            // do inicializador do C#. No segundo caso a chave continua ausente do YAML — que é
            // exatamente o defeito que esta ferramenta existe para fechar. Pular a escrita
            // deixaria o dado invisível de novo, e o guarda que lê o YAML reprovaria.
            prop.intValue = nivel;
            so.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(tabela);
            AssetDatabase.SaveAssetIfDirty(tabela);

            return antes == nivel
                ? $"{nome}: {nivel} materializado no YAML (vinha do default do C#) — {razao}"
                : $"{nome}: {antes} → {nivel} — {razao}";
        }
    }
}
