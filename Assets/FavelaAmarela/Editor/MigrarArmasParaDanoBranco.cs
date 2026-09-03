using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;
using FavelaAmarela.Inventario;

namespace FavelaAmarela.EditorTools
{
    /// <summary>
    /// Move o dano das três armas da Tumba do <c>HabilidadeDef</c> para a <c>BaseDeArma</c> —
    /// de número fixo por família para <b>faixa de dano branco por arma</b>.
    ///
    /// <para><b>A calibragem tem uma regra só: o valor esperado não muda.</b> Com faixa, erro e
    /// crítico, o dano de um golpe deixa de ser um número e vira uma distribuição — e o que se
    /// preserva é a <i>média</i>. Cada faixa abaixo foi escolhida para que</para>
    ///
    /// <code>
    ///   média × precisão × (1 + chanceCrítica × (multiplicador − 1))
    /// </code>
    ///
    /// <para>caia em cima do dano fixo que a arma causa hoje. O Alfanje sai de 45 fixos para
    /// 40–61 com 85% de precisão e 5% de chance de dobrar: <b>44,6 esperados</b>. A Fase 1 muda
    /// a <b>textura</b> do combate — variância, crítico, erro —, não a dificuldade. Rebalancear
    /// é a Fase 4, e é decisão consciente, não efeito colateral de refatoração.</para>
    ///
    /// <para><b>Crítico e precisão são a identidade da família</b>, e é aqui que as três armas
    /// deixam de ser "a mesma coisa com números diferentes":</para>
    ///
    /// <list type="bullet">
    ///   <item><b>Estilete de Irem</b> — lâmina fina: erra pouco (95%), critica muito (12%),
    ///   multiplica pouco (1,6). Dano confiável, morte por mil cortes.</item>
    ///   <item><b>Maça de Aklo</b> — perfurante: o meio-termo em tudo.</item>
    ///   <item><b>Alfanje de Alhazred</b> — peso e arco: erra bastante (85%), critica pouco
    ///   (5%), mas <b>dobra</b> quando critica. Alto risco, alto retorno.</item>
    /// </list>
    ///
    /// <para><b>O básico é sempre 100% do dano da arma</b> — é a definição de dano branco. A
    /// habilidade é um percentual dele, calculado sobre o dano fixo que ela causava antes.</para>
    /// </summary>
    public static class MigrarArmasParaDanoBranco
    {
        private const string PastaDasBases = "Assets/FavelaAmarela/Config/Armas";
        private const string PastaDasHabilidades = "Assets/FavelaAmarela/Config/Habilidades";

        /// <summary>
        /// Uma linha por arma. <c>DanoAntigo</c> e <c>HabilidadeAntiga</c> ficam registrados
        /// para a conta ser auditável — é a partir deles que a faixa foi derivada, e sem eles a
        /// calibragem viraria número mágico na primeira vez que alguém reler isto.
        /// </summary>
        private static readonly Arma[] Armas =
        {
            new Arma("BaseArma_Alfanje", "Habilidade_AlfanjeDeAlhazred",
                     danoMin: 40f, danoMax: 61f,
                     chanceCritica: 0.05f, multiplicador: 2.0f, precisao: 0.85f,
                     danoAntigo: 45f, habilidadeAntiga: 40f,
                     leitura: "peso e arco: erra, mas quando pega, dobra"),

            new Arma("BaseArma_Maca", "Habilidade_MacaDeAklo",
                     danoMin: 33f, danoMax: 49f,
                     chanceCritica: 0.08f, multiplicador: 1.7f, precisao: 0.92f,
                     danoAntigo: 40f, habilidadeAntiga: 30f,
                     leitura: "perfurante: o meio-termo honesto das três"),

            new Arma("BaseArma_LaminaFina", "Habilidade_EstileteDeIrem",
                     danoMin: 24f, danoMax: 35f,
                     chanceCritica: 0.12f, multiplicador: 1.6f, precisao: 0.95f,
                     danoAntigo: 30f, habilidadeAntiga: 15f,
                     leitura: "lâmina fina: quase nunca erra, sangra sempre"),
        };

        [MenuItem("Tools/FavelaAmarela/Armas: migrar para dano branco em faixa")]
        public static void Executar()
        {
            var resumo = new List<string>();

            foreach (var arma in Armas)
                resumo.AddRange(Migrar(arma));

            AssetDatabase.SaveAssets();
            Debug.Log("[DanoBranco] Concluído:\n  " + string.Join("\n  ", resumo));
        }

        private static IEnumerable<string> Migrar(Arma arma)
        {
            var notas = new List<string>();

            // ── A base ganha o bloco de combate ───────────────────────────────
            var baseDeArma = AssetDatabase.LoadAssetAtPath<BaseDeArma>(
                $"{PastaDasBases}/{arma.Base}.asset");

            if (baseDeArma == null)
            {
                notas.Add($"{arma.Base}: ASSET AUSENTE");
                return notas;
            }

            var so = new SerializedObject(baseDeArma);
            Escrever(so, "DanoMinBase", arma.DanoMin);
            Escrever(so, "DanoMaxBase", arma.DanoMax);
            Escrever(so, "ChanceCriticaBase", arma.ChanceCritica);
            Escrever(so, "MultiplicadorCritico", arma.Multiplicador);
            Escrever(so, "PrecisaoBase", arma.Precisao);
            so.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(baseDeArma);

            notas.Add($"{arma.Base}: {arma.DanoMin}–{arma.DanoMax} branco, " +
                      $"crítico {arma.ChanceCritica:P0}×{arma.Multiplicador}, " +
                      $"precisão {arma.Precisao:P0} → esperado {arma.Esperado:0.0} " +
                      $"(era {arma.DanoAntigo} fixo) — {arma.Leitura}");

            // ── A habilidade troca dano fixo por percentual ───────────────────
            var habilidade = AssetDatabase.LoadAssetAtPath<HabilidadeDef>(
                $"{PastaDasHabilidades}/{arma.Habilidade}.asset");

            if (habilidade == null)
            {
                notas.Add($"{arma.Habilidade}: ASSET AUSENTE");
                return notas;
            }

            var soHab = new SerializedObject(habilidade);

            // O básico É o dano branco, por definição.
            TrocarDanoPorPercentual(soHab, "EfeitosDoBasico", 1.0f, notas, arma.Habilidade,
                                    "básico");

            // A habilidade é uma fração dele, derivada do que ela causava antes -- e a fração
            // sai do valor ESPERADO do básico, não da média da faixa. Calibrar pela média
            // ignora precisão e crítico, e foi o que fez o Golpe do Deserto cair de 40 para
            // 36,1 na primeira rodada: 80% da média são 40,4, mas 80% do ESPERADO são 36,1.
            float percentual = Mathf.Round(arma.HabilidadeAntiga / arma.Esperado * 20f) / 20f;
            TrocarDanoPorPercentual(soHab, "EfeitosDaHabilidade", percentual, notas,
                                    arma.Habilidade, "habilidade");

            soHab.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(habilidade);

            return notas;
        }

        /// <summary>
        /// Troca o efeito de <c>Dano</c> plano da lista por um <c>DanoDaArma</c> percentual.
        /// Os outros efeitos (sangramento, atordoamento, repulsão, interrupção) ficam intactos —
        /// são o que dá verbo próprio a cada arma e nada neles depende do dano.
        /// </summary>
        private static void TrocarDanoPorPercentual(SerializedObject so, string lista,
                                                    float percentual, List<string> notas,
                                                    string nome, string rotulo)
        {
            var efeitos = so.FindProperty(lista);
            if (efeitos == null || !efeitos.isArray)
            {
                notas.Add($"{nome}: lista '{lista}' não encontrada");
                return;
            }

            for (int i = 0; i < efeitos.arraySize; i++)
            {
                var efeito = efeitos.GetArrayElementAtIndex(i);
                var tipo = efeito.FindPropertyRelative("Tipo");
                var valor = efeito.FindPropertyRelative("Valor");

                if (tipo == null || valor == null) continue;

                bool plano = tipo.enumValueIndex == (int)TipoDeEfeito.Dano;
                bool jaMigrado = tipo.enumValueIndex == (int)TipoDeEfeito.DanoDaArma;

                // Aceita os dois: converter o plano é a migração, e reescrever o percentual
                // torna a ferramenta REEXECUTÁVEL. Sem isto, recalibrar exigiria desfazer a
                // migração à mão primeiro -- e uma ferramenta que só roda uma vez é uma
                // ferramenta que ninguém confere depois.
                if (!plano && !jaMigrado) continue;

                float antes = valor.floatValue;
                tipo.enumValueIndex = (int)TipoDeEfeito.DanoDaArma;
                valor.floatValue = percentual;

                notas.Add(plano
                    ? $"  {nome} · {rotulo}: Dano {antes} → {percentual:P0} do dano da arma"
                    : $"  {nome} · {rotulo}: {antes:P0} → {percentual:P0} do dano da arma");
                return;
            }

            notas.Add($"  {nome} · {rotulo}: nenhum efeito de Dano plano — nada a migrar");
        }

        private static void Escrever(SerializedObject so, string campo, float valor)
        {
            var prop = so.FindProperty(campo);
            if (prop != null) prop.floatValue = valor;
        }

        private readonly struct Arma
        {
            public readonly string Base, Habilidade, Leitura;
            public readonly float DanoMin, DanoMax, ChanceCritica, Multiplicador, Precisao;
            public readonly float DanoAntigo, HabilidadeAntiga;

            public Arma(string @base, string habilidade, float danoMin, float danoMax,
                        float chanceCritica, float multiplicador, float precisao,
                        float danoAntigo, float habilidadeAntiga, string leitura)
            {
                Base = @base; Habilidade = habilidade; Leitura = leitura;
                DanoMin = danoMin; DanoMax = danoMax;
                ChanceCritica = chanceCritica; Multiplicador = multiplicador; Precisao = precisao;
                DanoAntigo = danoAntigo; HabilidadeAntiga = habilidadeAntiga;
            }

            public float Media => (DanoMin + DanoMax) * 0.5f;

            /// <summary>O valor esperado de um golpe — o número que tem de bater com o antigo.</summary>
            public float Esperado =>
                Media * Precisao * (1f + ChanceCritica * (Multiplicador - 1f));
        }
    }
}
