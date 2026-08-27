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
    /// <para><b>Quem não tem <c>Rigidbody2D</c> fica de fora, de propósito.</b> Rei em Amarelo,
    /// Abdul e Pedra de Poder não têm corpo físico nenhum — já são inamovíveis por construção,
    /// e a ficção concorda com a técnica. Marcar não mudaria nada e daria a falsa impressão de
    /// estar configurado.</para>
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
                // Sem corpo físico não há o que empurrar. Marcar seria decoração.
                if (raiz.GetComponentInChildren<Rigidbody2D>(true) == null)
                    return $"{nome}: sem Rigidbody2D — já é inamovível, não marcado";

                var corpo = raiz.GetComponent<CorpoImpregnado>();
                bool novo = corpo == null;
                if (novo) corpo = raiz.AddComponent<CorpoImpregnado>();

                var so = new SerializedObject(corpo);
                so.FindProperty("resistenciaAImpulso").floatValue = resistencia;
                so.ApplyModifiedPropertiesWithoutUndo();

                PrefabUtility.SaveAsPrefabAsset(raiz, caminho);

                return $"{nome}: {resistencia:0.00} ({(novo ? "novo" : "atualizado")}) — {leitura}";
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

                if (alvo.GetComponentInChildren<Rigidbody2D>(true) == null) { semCorpo++; continue; }

                var corpo = alvo.GetComponent<CorpoImpregnado>();
                if (corpo == null) corpo = alvo.gameObject.AddComponent<CorpoImpregnado>();

                var so = new SerializedObject(corpo);
                so.FindProperty("resistenciaAImpulso").floatValue = resistencia;
                so.ApplyModifiedPropertiesWithoutUndo();

                marcados++;
            }

            if (alvos.Length == 0) resumo.Add($"{rotulo}: nenhum na cena");
            else resumo.Add($"{rotulo}: {marcados} marcado(s) em {resistencia:0.00}" +
                            (semCorpo > 0 ? $", {semCorpo} sem Rigidbody2D" : "") +
                            $" — {leitura}");

            return marcados;
        }
    }
}
