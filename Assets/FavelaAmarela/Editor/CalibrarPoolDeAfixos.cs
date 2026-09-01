using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;
using FavelaAmarela.Inventario;

namespace FavelaAmarela.EditorTools
{
    /// <summary>
    /// Calibra e amplia o <b>pool de afixos</b> — a outra metade de "todos os drops fracos".
    ///
    /// <para><b>O que estava errado.</b> Oito afixos no jogo inteiro, e <b>três deles de
    /// Vigor</b>. Com no máximo 2 prefixos + 2 sufixos por item, a variedade real era curta: dois
    /// drops seguidos saíam parecidos porque o pool não tem de onde variar. Um ARPG funciona por
    /// multiplicação — <c>bases × afixos × graus</c> —, e 3×8×3 é pouco por construção.</para>
    ///
    /// <para><b>E dois afixos não podem escalar.</b> <c>RegenRM</c> e <c>RegeneracaoVigor</c> são
    /// <b>taxas por segundo</b>: multiplicá-las pelo fator do nível 12 (×3,75) daria regeneração
    /// que anula o recurso. <c>Furtividade</c> é uma <b>fração</b> de redução de ruído —
    /// escalá-la a estouraria. Estes ficam planos, e a decisão fica gravada no asset.</para>
    /// </summary>
    public static class CalibrarPoolDeAfixos
    {
        private const string Marcador = "[Afixos]";
        private const string Pasta = "Assets/FavelaAmarela/Config/Resources/Afixos";

        /// <summary>
        /// Afixos que <b>não</b> escalam com o nível, com a razão. Estar aqui é decisão
        /// registrada; o padrão é escalar.
        /// </summary>
        private static readonly (string Nome, string Razao)[] Planos =
        {
            ("afixo_da_vigilia",
             "RegenRM é taxa POR SEGUNDO — ×3,75 no nível 12 anularia a Resiliência como recurso"),

            ("afixo_do_peregrino",
             "RegeneracaoVigor é taxa por segundo — mesma razão"),
        };

        /// <summary>
        /// Afixos novos. O critério é <b>cobrir os eixos que o combate ganhou</b> e que o pool
        /// ignorava: crítico, precisão e aumento percentual de dano existem como
        /// <c>StatType</c> desde 2026-08-28 e <b>nenhum afixo os rolava</b> — três eixos de
        /// itemização inteiros sem conteúdo.
        ///
        /// <para><b>Nenhum passa do nível 3</b>, e isso é imposição do guarda
        /// <c>ItemizacaoDestravadaTests</c>: a melhor fonte de drop do jogo entrega nível 3, e
        /// afixo que pede mais é <b>conteúdo morto</b> — existe no asset, ocupa peso no sorteio
        /// e nunca cai. Quando as Fases 2–4 existirem e as tabelas subirem de nível, aqui é o
        /// lugar de abrir os degraus seguintes.</para>
        /// </summary>
        private static readonly Afixo[] Novos =
        {
            new Afixo("afixo_afiado", "Afiado", TipoDeAfixo.Prefixo,
                      StatType.ChanceCritica, 0.02f, 0.06f, nivel: 1, peso: 1f, escala: false,
                      "chance de crítico é FRAÇÃO: 0,06 escalado por 3,75 daria 22% num afixo só"),

            new Afixo("afixo_do_augurio", "do Augúrio", TipoDeAfixo.Sufixo,
                      StatType.DanoCritico, 0.1f, 0.35f, nivel: 3, peso: 0.7f, escala: false,
                      "multiplicador de crítico é fração somada ao 1,5 base"),

            new Afixo("afixo_certeiro", "Certeiro", TipoDeAfixo.Prefixo,
                      StatType.Precisao, 0.03f, 0.08f, nivel: 1, peso: 1f, escala: false,
                      "precisão é fração e tem teto em 1,0 — escalar a desperdiçaria"),

            new Afixo("afixo_da_furia", "da Fúria", TipoDeAfixo.Sufixo,
                      StatType.AumentoDeDanoFisico, 0.08f, 0.20f, nivel: 2, peso: 0.8f,
                      escala: false,
                      "já É percentual: multiplica o dano inteiro, então escalar seria dobrar"),

            new Afixo("afixo_do_abismo", "do Abismo", TipoDeAfixo.Sufixo,
                      StatType.TraumaAnomalia, 3f, 9f, nivel: 3, peso: 0.6f, escala: true,
                      "dano anômalo absoluto — acompanha a base"),

            new Afixo("afixo_couracado", "Couraçado", TipoDeAfixo.Prefixo,
                      StatType.DefesaAnomalia, 2f, 6f, nivel: 3, peso: 0.8f, escala: true,
                      "mitigação anômala absoluta; o canal existe e quase nada o alimentava"),

            new Afixo("afixo_do_peregrino_firme", "do Peregrino Firme", TipoDeAfixo.Prefixo,
                      StatType.VitMaxima, 14f, 32f, nivel: 3, peso: 0.7f, escala: true,
                      "faixa de Vitalidade mais alta que o afixo_encorpado, travada em nível 4"),
        };

        private readonly struct Afixo
        {
            public readonly string Id, Nome, Razao;
            public readonly TipoDeAfixo Tipo;
            public readonly StatType Stat;
            public readonly float Min, Max, Peso;
            public readonly int Nivel;
            public readonly bool Escala;

            public Afixo(string id, string nome, TipoDeAfixo tipo, StatType stat,
                         float min, float max, int nivel, float peso, bool escala, string razao)
            {
                Id = id; Nome = nome; Tipo = tipo; Stat = stat;
                Min = min; Max = max; Nivel = nivel; Peso = peso;
                Escala = escala; Razao = razao;
            }
        }

        [MenuItem("Tools/FavelaAmarela/Itens: calibrar e ampliar o pool de afixos")]
        public static void Executar()
        {
            Directory.CreateDirectory(Pasta);

            var resumo = new List<string>();

            foreach (var (nome, razao) in Planos) resumo.Add(DesligarEscala(nome, razao));
            foreach (var a in Novos) resumo.Add(Criar(a));

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            string quebra = System.Environment.NewLine + "  ";
            Debug.Log($"{Marcador} Concluído:" + quebra + string.Join(quebra, resumo));
        }

        private static string DesligarEscala(string nome, string razao)
        {
            string caminho = $"{Pasta}/{nome}.asset";
            var def = AssetDatabase.LoadAssetAtPath<AfixoDef>(caminho);

            if (def == null) return $"{nome}: AUSENTE";

            def.EscalaComONivelDoItem = false;
            EditorUtility.SetDirty(def);
            AssetDatabase.SaveAssetIfDirty(def);

            return $"{nome}: escala DESLIGADA — {razao}";
        }

        private static string Criar(Afixo a)
        {
            string caminho = $"{Pasta}/{a.Id}.asset";

            var def = AssetDatabase.LoadAssetAtPath<AfixoDef>(caminho);
            bool novo = def == null;

            if (novo)
            {
                def = ScriptableObject.CreateInstance<AfixoDef>();
                AssetDatabase.CreateAsset(def, caminho);
            }

            def.Id = a.Id;

            // 'Rotulo' e nao 'Nome': e o texto VISIVEL ao jogador, que aparece colado ao nome
            // do item ("Alfanje Afiado", "Cravo do Augurio"). Segue o lore-enforcer.
            def.Rotulo = a.Nome;
            def.Tipo = a.Tipo;
            def.Stat = a.Stat;
            def.ValorMin = a.Min;
            def.ValorMax = a.Max;
            def.NivelMinimoDoItem = a.Nivel;
            def.Peso = a.Peso;
            def.EscalaComONivelDoItem = a.Escala;

            EditorUtility.SetDirty(def);
            AssetDatabase.SaveAssetIfDirty(def);

            return $"{a.Id} [{(novo ? "CRIADO" : "atualizado")}]: {a.Nome}, {a.Stat} " +
                   $"{a.Min:0.##}–{a.Max:0.##}, nível {a.Nivel}, " +
                   $"escala={(a.Escala ? "sim" : "não")} — {a.Razao}";
        }
    }
}
