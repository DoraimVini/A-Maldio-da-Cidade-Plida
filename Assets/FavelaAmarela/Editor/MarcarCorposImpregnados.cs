using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using FavelaAmarela.Runtime.Combat;

namespace FavelaAmarela.EditorTools
{
    /// <summary>
    /// Aplica a <b>tabela de impregnação</b> ao elenco — quanto cada corpo ainda obedece à
    /// física.
    ///
    /// <para><b>A regra que isto materializa</b> (2026-08-27, resposta à pergunta do Vini "por
    /// que as coisas funcionam nessa realidade?"): <i>em Carcosa, quanto mais uma coisa está
    /// impregnada, menos ela se comporta como matéria.</i> O impacto vira <b>legibilidade</b> —
    /// o jogador descobre o que uma coisa é pela forma como ela reage ao golpe, sem uma linha
    /// de diálogo explicando.</para>
    ///
    /// <para><b>Os números são de balanceamento, não de lore.</b> Foram escolhidos por mim para
    /// dar a leitura descrita em cada linha da tabela; são botões para o Vini girar, e a ficção
    /// é que manda a ordem relativa (um Cultista sempre cede mais que um Eco).</para>
    ///
    /// <para><b>Quem não tem <c>Rigidbody2D</c> É marcado também</b> — com resistência 1,00.
    /// Rei em Amarelo, Abdul, Pedra de Poder e o Eco de Carcosa não têm corpo físico: já são
    /// inamovíveis por construção, e a ficção concorda com a técnica.</para>
    ///
    /// <para>A versão anterior os <b>pulava</b>, argumentando que "marcar não mudaria nada".
    /// Estava certa sobre o efeito e errada sobre o registro: olhando o prefab, <i>"não cede
    /// porque falta um componente"</i> e <i>"não cede porque decidimos assim"</i> são
    /// indistinguíveis. Marcar torna a imobilidade uma <b>decisão legível</b> em vez de um
    /// acidente — e guardável por teste.</para>
    /// </summary>
    public static class MarcarCorposImpregnados
    {
        private const string PastaDosInimigos = "Assets/FavelaAmarela/Art/Enemies";

        /// <summary>
        /// A tabela. Chave = nome do prefab; valor = quanto o corpo resiste ao impulso.
        /// </summary>
        private static readonly (string Prefab, float Resistencia, string Leitura)[] Tabela =
        {
            ("Cultista",         0.15f, "ainda é gente: leva o safanão e vai para trás"),
            ("EsqueletoInvocado",0.10f, "ossos montados às pressas, quase sem massa"),
            ("CoisaDoCemiterio", 0.60f, "caça pesada e ancorada, cambaleia pouco"),
            ("Byakhee",          0.75f, "criatura de fora, mas ainda presa a este plano"),
            ("EspectroHali",     0.90f, "quase não é corpo — o golpe atravessa mais do que empurra"),

            // Os três sem Rigidbody2D. Marcá-los NÃO muda comportamento — sem corpo físico,
            // RepulsaoDeImpacto.GarantirPara já devolve null e eles nunca cedem. O que muda é
            // que a imobilidade deixa de ser ACIDENTE (a ausência de um componente) e vira
            // DECISÃO registrada no dado, guardada por teste.
            //
            // A versão anterior desta tabela os pulava explicitamente. Estava certa sobre o
            // efeito e errada sobre o registro: "não faz nada porque falta uma peça" e "não faz
            // nada porque decidimos assim" são indistinguíveis olhando o prefab.
            ("Abdul_Alhazred",   1.00f, "o feiticeiro está ancorado pelo próprio ritual"),
            ("ReiEmAmarelo",     1.00f, "não está AQUI para ser empurrado"),
            ("PedraDePoder",     1.00f, "âncora de pedra: é o cenário que a segura, não o contrário"),
        };

        /// <summary>
        /// Atores que só existem montados em cena (o Castelo é construído por ferramenta, não
        /// por prefab), identificados pelo componente que os define.
        /// </summary>
        private const string CenaDoCastelo = "Assets/Scenes/Castelo_Carcosa.unity";

        [MenuItem("Tools/FavelaAmarela/Física: marcar corpos impregnados")]
        public static void Executar()
        {
            var resumo = new List<string>();

            foreach (var (nome, resistencia, leitura) in Tabela)
                resumo.Add(AplicarNoPrefab(nome, resistencia, leitura));

            resumo.AddRange(AplicarNaCenaDoCastelo());

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            Debug.Log("[CorposImpregnados] Concluído:\n  " + string.Join("\n  ", resumo));
        }

        private static string AplicarNoPrefab(string nome, float resistencia, string leitura)
        {
            string caminho = $"{PastaDosInimigos}/{nome}.prefab";

            if (!File.Exists(caminho)) return $"{nome}: PREFAB AUSENTE em '{caminho}'";

            var raiz = PrefabUtility.LoadPrefabContents(caminho);

            try
            {
                // Sem Rigidbody2D o ator já é inamovível na prática. Marcamos assim mesmo,
                // com resistência 1,00, para a imobilidade ser LEGÍVEL no prefab em vez de
                // deduzida da ausência de um componente.
                bool semCorpo = raiz.GetComponentInChildren<Rigidbody2D>(true) == null;

                var corpo = raiz.GetComponent<CorpoImpregnado>();
                bool novo = corpo == null;
                if (novo) corpo = raiz.AddComponent<CorpoImpregnado>();

                var so = new SerializedObject(corpo);
                so.FindProperty("resistenciaAImpulso").floatValue = resistencia;
                so.ApplyModifiedPropertiesWithoutUndo();

                PrefabUtility.SaveAsPrefabAsset(raiz, caminho);

                string nota = semCorpo ? " [sem Rigidbody2D: inamovível por construção]" : "";
                return $"{nome}: {resistencia:0.00} ({(novo ? "novo" : "atualizado")}) — {leitura}{nota}";
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(raiz);
            }
        }

        /// <summary>
        /// Cortesão Pálido e Eco de Carcosa vivem montados na cena do Castelo, não em prefab —
        /// o Castelo inteiro é construído por <c>MontarCasteloCarcosa</c>.
        /// </summary>
        private static IEnumerable<string> AplicarNaCenaDoCastelo()
        {
            var resumo = new List<string>();

            if (!File.Exists(CenaDoCastelo))
            {
                resumo.Add($"Castelo: cena ausente em '{CenaDoCastelo}'");
                return resumo;
            }

            var cena = EditorSceneManager.OpenScene(CenaDoCastelo, OpenSceneMode.Single);

            int cortesaos = MarcarPorTipo<FavelaAmarela.Core.Combat.CortesaoPalido>(
                0.45f, resumo, "Cortesão Pálido", "já foi gente: cambaleia, não voa");

            int ecos = MarcarPorTipo<FavelaAmarela.Runtime.Enemies.EcoDeCarcosa>(
                1.00f, resumo, "Eco de Carcosa", "nunca foi corpo: o golpe acerta e nada cede");

            if (cortesaos > 0 || ecos > 0)
            {
                EditorSceneManager.MarkSceneDirty(cena);
                if (!EditorSceneManager.SaveScene(cena))
                    resumo.Add("Castelo: SaveScene RECUSOU");
            }

            return resumo;
        }

        private static int MarcarPorTipo<T>(float resistencia, List<string> resumo,
                                            string rotulo, string leitura) where T : Component
        {
            var alvos = Object.FindObjectsByType<T>(FindObjectsInactive.Include,
                                                    FindObjectsSortMode.None);

            int marcados = 0;
            int semCorpo = 0;

            foreach (var alvo in alvos)
            {
                if (alvo == null) continue;

                // Marcado mesmo sem corpo físico, pelo mesmo motivo dos prefabs: registrar a
                // imobilidade em vez de deduzi-la da ausência de um componente.
                if (alvo.GetComponentInChildren<Rigidbody2D>(true) == null) semCorpo++;

                var corpo = alvo.GetComponent<CorpoImpregnado>();
                if (corpo == null) corpo = alvo.gameObject.AddComponent<CorpoImpregnado>();

                var so = new SerializedObject(corpo);
                so.FindProperty("resistenciaAImpulso").floatValue = resistencia;
                so.ApplyModifiedPropertiesWithoutUndo();

                marcados++;
            }

            if (alvos.Length == 0) resumo.Add($"{rotulo}: nenhum na cena");
            else resumo.Add($"{rotulo}: {marcados} marcado(s) em {resistencia:0.00}" +
                            (semCorpo > 0 ? $", {semCorpo} sem Rigidbody2D (inamovível por construção)" : "") +
                            $" — {leitura}");

            return marcados;
        }
    }
}
