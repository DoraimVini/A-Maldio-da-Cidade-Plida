using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;
using FavelaAmarela.Core.Combat;

namespace FavelaAmarela.EditorTools
{
    /// <summary>
    /// Autora as fichas do <b>Templo do Povo Serpente</b> — a Dungeon 2, item 14 da lista de
    /// produção.
    ///
    /// <para><b>O que a auditoria encontrou (2026-08-29).</b> Os três atores do Templo —
    /// Sseth Farejador, Nagaraja e Avatar de Set — carregavam o dano num campo do próprio
    /// script, como Cultista e Byakhee carregavam até a véspera. Mas o defeito era maior que
    /// isso: <b>nenhum dos três tem Vitalidade</b>. São <c>MonoBehaviour</c> puros, sem
    /// <c>EnemyBase</c> e sem <c>IDanificavel</c> — <b>causam dano e não podem receber</b>.
    /// Como não há prefab nem cena do Templo ainda, isso nunca apareceu em jogo.</para>
    ///
    /// <para><b>De onde vêm os números.</b> O <c>Ataque</c> dos três já estava autorado nos
    /// scripts (35, 20, 80) — aqui ele é <b>preservado</b>, não inventado; é a mesma disciplina
    /// que corrigiu a ficha do Cultista para 20 em vez de deixar a unificação enfraquecê-lo.
    /// <b>Vitalidade e Defesa são novas</b>, e foram derivadas do elenco que já existe em vez de
    /// escolhidas no vácuo:</para>
    ///
    /// <list type="bullet">
    ///   <item>Cultista — tropa da Fase 1: Vit 100, Atk 20, Def 5</item>
    ///   <item>Abdul — primeiro chefe: Vit 300, Atk 8, Conj 25, Def 5, RA 20</item>
    ///   <item>Byakhee — chefe que fecha a Fase 1: Vit 500, RM 120, Atk 26, Def 8, RA 12</item>
    /// </list>
    ///
    /// <para><b>Isto é proposta, não decreto.</b> Vitalidade e Defesa são decisão de design do
    /// Vini; ficam no asset justamente para ele mexer sem tocar em código. O que <b>não</b> é
    /// negociável é existirem: sem ficha, o ator não tem como ser abatido.</para>
    ///
    /// <para><b>O que ainda falta, e por que não está aqui.</b> Os três precisam de
    /// <c>EnemyBase</c> nos prefabs, e <b>não existe prefab de nenhum deles</b> — nem cena do
    /// Templo. Quando os prefabs nascerem: acrescentar <c>EnemyBase</c>, apontar a ficha, e
    /// definir o <c>nivelDaUnidade</c> (sugestão: <b>3</b>, que é o nível em que o jogador sai
    /// da Fase 1).</para>
    /// </summary>
    public static class MontarFichasDoTemplo
    {
        private const string Marcador = "[FichasDoTemplo]";
        private const string Pasta = "Assets/FavelaAmarela/Config";

        private readonly struct Ficha
        {
            public readonly string Nome, Papel;
            public readonly float Vitalidade, Ataque, Defesa, Conjuracao, ResistenciaAnomala, Resiliencia;

            public Ficha(string nome, float vit, float atk, float def, float conj, float ra,
                         float rm, string papel)
            {
                Nome = nome; Vitalidade = vit; Ataque = atk; Defesa = def;
                Conjuracao = conj; ResistenciaAnomala = ra; Resiliencia = rm; Papel = papel;
            }
        }

        private static readonly Ficha[] Fichas =
        {
            new Ficha("Ficha_Sseth", vit: 120f, atk: 20f, def: 6f, conj: 0f, ra: 0f, rm: 0f,
                "TROPA do Templo. Ataque 20 é o do script, e é o mesmo do Cultista de propósito: " +
                "ele não bate mais forte, ele CAÇA — segue por faro, o que muda o jogo de " +
                "furtividade, não a conta de dano. Vitalidade 120 contra 100 do Cultista marca " +
                "que já é a Dungeon 2"),

            new Ficha("Ficha_Nagaraja", vit: 220f, atk: 35f, def: 7f, conj: 0f, ra: 10f, rm: 60f,
                "ELITE nomeado, que fala Aklo e é IInteragivel — tem conversa antes da luta, " +
                "como o Abdul. Ataque 35 é o do script. Vitalidade entre a tropa e um chefe. " +
                "Tem MENTE (Resiliência 60): é o que permite derrotá-lo pelo canal anômalo, " +
                "coerente com uma criatura que argumenta. Larga a Coroa de Ossos"),

            new Ficha("Ficha_AvatarDeSet", vit: 450f, atk: 80f, def: 10f, conj: 0f, ra: 25f, rm: 0f,
                "CHEFE do Templo. Ataque 80 é o do script e é o maior do jogo — com cadência " +
                "2,0s, é o oposto do Byakhee: poucos golpes, cada um devastador. Vitalidade 450 " +
                "fica abaixo das 500 do Byakhee de propósito (o Templo é conteúdo opcional; " +
                "punir quem explora seria punir a curiosidade). SEM mente: é um avatar de deus, " +
                "não há o que argumentar — só carne. Resistência Anômala 25, a maior do elenco"),
        };

        [MenuItem("Tools/FavelaAmarela/Fichas: montar as do Templo do Povo Serpente")]
        public static void Executar()
        {
            var resumo = new List<string>();

            foreach (var f in Fichas) resumo.Add(Aplicar(f));

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            string quebra = System.Environment.NewLine + "  ";
            Debug.Log($"{Marcador} Concluído:" + quebra + string.Join(quebra, resumo));
        }

        private static string Aplicar(Ficha f)
        {
            string caminho = $"{Pasta}/{f.Nome}.asset";

            var config = AssetDatabase.LoadAssetAtPath<FichaAtributosConfig>(caminho);
            bool criada = config == null;

            if (criada)
            {
                config = ScriptableObject.CreateInstance<FichaAtributosConfig>();
                AssetDatabase.CreateAsset(config, caminho);
            }

            var so = new SerializedObject(config);

            foreach (var (campo, valor) in new (string, float)[]
                     {
                         // PascalCase: os campos do FichaAtributosConfig foram renomeados e
                         // carregam [FormerlySerializedAs] com a grafia antiga. Os assets
                         // antigos ainda mostram 'vitalidadeMax' no YAML porque nunca foram
                         // regravados -- mas FindProperty só conhece o nome ATUAL.
                         ("VitalidadeMax", f.Vitalidade),
                         ("Ataque", f.Ataque),
                         ("Defesa", f.Defesa),
                         ("Conjuracao", f.Conjuracao),
                         ("ResistenciaAnomala", f.ResistenciaAnomala),
                         ("ResilienciaMax", f.Resiliencia),
                     })
            {
                var prop = so.FindProperty(campo);
                if (prop == null) return $"{f.Nome}: campo '{campo}' não existe mais na ficha";

                // Sempre escreve, mesmo quando o valor já bate: um campo ausente no YAML vale o
                // inicializador do C# e passaria por "já calibrado" -- foi assim que a
                // ferramenta de nível de drop deixou o Cultista para trás.
                prop.floatValue = valor;
            }

            so.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(config);
            AssetDatabase.SaveAssetIfDirty(config);

            string estado = criada ? "CRIADA" : "atualizada";

            return $"{f.Nome} [{estado}]: Vit {f.Vitalidade:0} · Atk {f.Ataque:0} · " +
                   $"Def {f.Defesa:0} · RA {f.ResistenciaAnomala:0} · RM {f.Resiliencia:0} — {f.Papel}";
        }
    }
}
