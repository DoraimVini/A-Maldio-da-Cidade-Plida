using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
using FavelaAmarela.Runtime.Enemies;
using FavelaAmarela.Runtime.Itens;
using FavelaAmarela.Runtime.Progression;

namespace FavelaAmarela.EditorTools
{
    /// <summary>
    /// Calibra quanta <b>Exposição</b> cada abate concede — sem isso, o eixo de progressão
    /// existe no código e não acontece em jogo.
    ///
    /// <para><b>O defeito, medido em 2026-08-28.</b> A curva pede <b>100</b> de Exposição para o
    /// nível 2, e <c>EnemyBase.exposicaoAoAbater</c> vale <b>1</b> em todo o elenco. Seriam
    /// <b>cem abates</b> para subir um nível. Pior: a concessão mora dentro do <c>EnemyBase</c>,
    /// e o elenco tem <b>nove prefabs com apenas dois <c>EnemyBase</c></b> — o <b>Abdul</b>,
    /// primeiro chefe do jogo, concedia <b>zero</b>.</para>
    ///
    /// <para>Isso tornava inerte tudo que as Fases 1–3 construíram: o dano branco escala por
    /// nível, a ficha escala por nível, e a curva de raridade desliza por nível. Com o jogador
    /// travado no 1, nada disso saía do lugar — e a luta do Byakhee continuaria sendo o que o
    /// Vini jogou: <i>"não tem como ganhar"</i>.</para>
    ///
    /// <para><b>Por que subir a concessão em vez de baixar a curva.</b> A curva
    /// (0, 100, 300, 600, 1000…) é dimensionada para a campanha de <b>seis fases</b>, não para o
    /// recorte do Vertical Slice. Achatá-la para caber em treze abates estragaria o jogo
    /// completo para consertar a demo. O número anômalo é o <b>1 por abate</b>.</para>
    ///
    /// <para><b>A conta do caminho crítico</b>, contada nas cenas (não estimada):</para>
    /// <list type="bullet">
    ///   <item>Deserto de Hali: <b>11</b> Cultistas × 25 = <b>275</b> → nível 2 (100)</item>
    ///   <item>Tumba de Alhazred: <b>2</b> Cultistas × 25 + Abdul 150 = <b>200</b> → total 475</item>
    ///   <item>Portões das Ruínas: o jogador encara o Byakhee com <b>475</b> de Exposição —
    ///         <b>nível 3</b> (300), a caminho do 4 (600)</item>
    /// </list>
    ///
    /// <para>Nível 3 é exatamente o alvo do plano: é onde a luta do Byakhee deixa de ser
    /// 14 golpes contra 5 e vira uma troca de 9 por 9, vencível com as três armas da Tumba sem
    /// depender do Set Lendário (que nem tem fonte jogável).</para>
    /// </summary>
    public static class CalibrarExposicaoDoElenco
    {
        private const string PastaDosInimigos = "Assets/FavelaAmarela/Art/Enemies";

        /// <summary>
        /// Quanto cada abate vale, com a razão. Lista escrita à mão de propósito: <b>quanto vale
        /// matar cada coisa é decisão de design</b>. O que ela não decide é o efeito — esse vem
        /// da curva, já autorada no <c>ProgressionBridge</c>.
        /// </summary>
        private static readonly (string Prefab, int Exposicao, string Razao)[] Tabela =
        {
            ("Cultista", 25,
             "a tropa do jogo: 11 no Deserto (275) levam sozinhos ao nível 2, e os 2 da Tumba " +
             "encostam no 3. É a base da progressão da Fase 1"),

            ("Byakhee", 200,
             "chefe que fecha a Fase 1. Sozinho vale oito Cultistas — derrotar um chefe tem de " +
             "ser sentido no nível, não só no espólio"),

            ("Abdul_Alhazred", 150,
             "primeiro chefe. Abaixo do Byakhee de propósito: é o começo da curva, e ele " +
             "concedia ZERO até hoje por não ser um EnemyBase"),

            // 2026-09-01: o Rei passou a largar espólio, e o guarda
            // TodoAtorQueLargaEspolio_ConcedeExposicao pegou na mesma rodada -- ator que
            // recompensa com item tem de recompensar com nível. O guarda estava certo.
            ("ReiEmAmarelo", 400,
             "o desfecho. Selar o Rei é o maior feito do jogo e vale o dobro do Byakhee. Que " +
             "não haja luta depois não muda a regra: Exposição é Aprofundamento, e entender " +
             "Carcosa é exatamente o que acabou de acontecer"),
        };

        /// <summary>
        /// Quem é abatível e <b>deliberadamente</b> não concede nada. Estar nesta lista é uma
        /// decisão registrada; estar fora das duas é um esquecimento, e o resumo grita.
        /// </summary>
        private static readonly (string Prefab, string Razao)[] SemExposicao =
        {
            ("EsqueletoInvocado",
             "INVOCADO pelo Abdul em fluxo infinito. Qualquer valor aqui viraria farm de chefe: " +
             "o jogador pararia de lutar e ficaria colhendo esqueletos"),

            ("PedraDePoder",
             "âncora do ritual do Abdul, não inimigo. Quebrá-la é mecânica da luta; premiar " +
             "isso premiaria ESTENDER a luta em vez de vencê-la"),
        };

        /// <summary>
        /// Quem sequer é abatível — não implementa <c>IDanificavel</c>. Não é omissão desta
        /// ferramenta: não existe momento em que conceder.
        /// </summary>
        private static readonly string[] NaoAbativeis =
        {
            "CoisaDoCemiterio", "EspectroHali", "ConeDeGelo", "ReiEmAmarelo",
        };

        [MenuItem("Tools/FavelaAmarela/Progressão: calibrar a Exposição do elenco")]
        public static void Executar()
        {
            var resumo = new List<string>();
            var vistos = new HashSet<string>();

            foreach (var (prefab, exposicao, razao) in Tabela)
            {
                vistos.Add(prefab);
                resumo.Add(Aplicar(prefab, exposicao, razao));
            }

            foreach (var (prefab, razao) in SemExposicao)
            {
                vistos.Add(prefab);
                resumo.Add($"{prefab}: ZERO por decisão — {razao}");
            }

            foreach (var prefab in NaoAbativeis)
            {
                vistos.Add(prefab);
                resumo.Add($"{prefab}: não é abatível (sem IDanificavel) — nada a conceder");
            }

            // Prefab novo que ninguém acrescentou aqui fica valendo o padrão e some do radar —
            // que é exatamente como o elenco inteiro chegou até hoje concedendo 1.
            foreach (var caminho in Directory.GetFiles(PastaDosInimigos, "*.prefab").OrderBy(c => c))
            {
                string nome = Path.GetFileNameWithoutExtension(caminho);
                if (!vistos.Contains(nome))
                    resumo.Add($"{nome}: NÃO CALIBRADO — acrescente a uma das listas de " +
                               "CalibrarExposicaoDoElenco, com a razão");
            }

            AssetDatabase.SaveAssets();
            Debug.Log("[Exposicao] Concluído:\n  " + string.Join("\n  ", resumo));
        }

        private static string Aplicar(string prefab, int exposicao, string razao)
        {
            string caminho = $"{PastaDosInimigos}/{prefab}.prefab";
            if (!File.Exists(caminho)) return $"{prefab}: PREFAB AUSENTE";

            var raiz = PrefabUtility.LoadPrefabContents(caminho);

            try
            {
                string resultado = Escrever(raiz, prefab, exposicao, razao);

                PrefabUtility.SaveAsPrefabAsset(raiz, caminho, out bool gravou);
                return gravou ? resultado : $"{prefab}: SaveAsPrefabAsset RECUSOU";
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(raiz);
            }
        }

        /// <summary>
        /// Escreve pelo caminho que o ator tem. <c>EnemyBase</c> concede por campo próprio;
        /// quem não é <c>EnemyBase</c> (o Abdul) recebe o componente <c>ExposicaoAoAbater</c>,
        /// que se pendura no mesmo <c>IFonteDeEspolio</c> do espólio.
        /// </summary>
        private static string Escrever(GameObject raiz, string prefab, int exposicao, string razao)
        {
            var corpo = raiz.GetComponent<EnemyBase>();

            if (corpo != null)
            {
                var so = new SerializedObject(corpo);
                var prop = so.FindProperty("exposicaoAoAbater");

                if (prop == null)
                    return $"{prefab}: campo 'exposicaoAoAbater' não existe mais no EnemyBase";

                int antes = prop.intValue;
                prop.intValue = exposicao;
                so.ApplyModifiedPropertiesWithoutUndo();

                return $"{prefab}: EnemyBase {antes} → {exposicao} — {razao}";
            }

            // Não é EnemyBase. Só pode conceder se souber avisar que foi derrotado — que é o
            // mesmo contrato do espólio, de propósito.
            if (raiz.GetComponent<IFonteDeEspolio>() == null)
                return $"{prefab}: não é EnemyBase e não implementa IFonteDeEspolio — não há " +
                       "momento em que conceder. Nada escrito";

            var componente = raiz.GetComponent<ExposicaoAoAbater>();
            bool novo = componente == null;
            if (novo) componente = raiz.AddComponent<ExposicaoAoAbater>();

            var soComp = new SerializedObject(componente);
            var campo = soComp.FindProperty("exposicao");

            if (campo == null)
                return $"{prefab}: campo 'exposicao' não existe no ExposicaoAoAbater";

            int anterior = campo.intValue;
            campo.intValue = exposicao;
            soComp.ApplyModifiedPropertiesWithoutUndo();

            return novo
                ? $"{prefab}: ExposicaoAoAbater CRIADO com {exposicao} (concedia ZERO) — {razao}"
                : $"{prefab}: ExposicaoAoAbater {anterior} → {exposicao} — {razao}";
        }
    }
}
