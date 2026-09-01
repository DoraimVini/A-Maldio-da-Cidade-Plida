using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using UnityEditor;
using FavelaAmarela.Core.Loot;
using FavelaAmarela.Inventario;

namespace FavelaAmarela.Tests.EditMode
{
    /// <summary>
    /// Guarda a <b>escada de armas</b> e a escala de afixo por nível — as duas metades de
    /// "todos os drops fracos".
    ///
    /// <para><b>O que foi medido em 2026-09-01.</b> O jogo tinha <b>três armas</b>: as três do
    /// baú da Tumba, entregues no começo. Depois dele não existia no jogo uma arma que o jogador
    /// já não tivesse. Somado à curva de grau, que no nível 1 dá 80,6% de Inerte — sem
    /// modificadores —, <b>oito em cada dez drops eram uma arma repetida sem afixo nenhum</b>.
    /// Nenhuma das duas queixas do Vini era matemática: era <b>catálogo</b>.</para>
    /// </summary>
    public sealed class EscadaDeArmasTests
    {
        private const string PastaDeBases = "Assets/FavelaAmarela/Config/Armas";

        private sealed class FonteFixa : IFonteDeAleatoriedade
        {
            private readonly float _v;
            public FonteFixa(float v) => _v = v;
            public float ProximoValor() => _v;
            public int ProximoInteiro(int min, int max) => min;
        }

        private static BaseDeArma Base(string nome) =>
            AssetDatabase.LoadAssetAtPath<BaseDeArma>($"{PastaDeBases}/{nome}.asset");

        /// <summary>As três famílias, com os nomes dos três degraus de cada.</summary>
        private static readonly (string Familia, string[] Degraus)[] Escada =
        {
            ("Alfanje", new[] { "BaseArma_Alfanje", "BaseArma_Alfanje_T2", "BaseArma_Alfanje_T3" }),
            ("Cravo", new[] { "BaseArma_Cravo", "BaseArma_Cravo_T2", "BaseArma_Cravo_T3" }),
            ("Lâmina fina", new[] { "BaseArma_LaminaFina", "BaseArma_LaminaFina_T2",
                                    "BaseArma_LaminaFina_T3" }),
        };

        // ── A escada existe ───────────────────────────────────────────────────

        [Test]
        public void CadaFamilia_TemTresDegraus()
        {
            var faltando = new List<string>();

            foreach (var (familia, degraus) in Escada)
                foreach (var d in degraus)
                    if (Base(d) == null) faltando.Add($"{familia}: {d}");

            Assert.IsEmpty(faltando,
                "Degrau(s) de arma ausentes:" + Environment.NewLine + "  " +
                string.Join(Environment.NewLine + "  ", faltando) + Environment.NewLine +
                "Conserto: 'Tools/FavelaAmarela/Itens: montar a escada de armas'.");
        }

        [Test]
        public void ODano_SobeACadaDegrau()
        {
            foreach (var (familia, degraus) in Escada)
            {
                for (int i = 1; i < degraus.Length; i++)
                {
                    var anterior = Base(degraus[i - 1]);
                    var atual = Base(degraus[i]);

                    if (anterior == null || atual == null) continue;

                    Assert.Greater(atual.DanoMaxBase, anterior.DanoMaxBase,
                        $"{familia}: o degrau {i + 1} não bate mais forte que o {i}. Um tier que " +
                        "não sobe o dano não é um degrau, é uma cópia com outro nome.");
                }
            }
        }

        /// <summary>
        /// <b>A decisão de design que este teste protege.</b> O tier muda <b>só a faixa de
        /// dano</b>. Crítico, precisão, alcance, raio e cadência são <b>identidade da
        /// família</b>: escalá-los junto faria as três convergirem para "a mais forte, com
        /// números maiores", e a escolha entre elas morreria no tier 2.
        ///
        /// <para>O Alfanje continua sendo o que erra e explode; o Estilete, o que quase nunca
        /// erra e quase nunca dói — <b>em qualquer degrau</b>.</para>
        /// </summary>
        [Test]
        public void OTier_NaoMudaAIdentidadeDaFamilia()
        {
            var borrados = new List<string>();

            foreach (var (familia, degraus) in Escada)
            {
                var t1 = Base(degraus[0]);
                if (t1 == null) continue;

                foreach (var nome in degraus.Skip(1))
                {
                    var d = Base(nome);
                    if (d == null) continue;

                    void Conferir(string campo, float esperado, float achado)
                    {
                        if (Math.Abs(esperado - achado) > 0.001f)
                            borrados.Add($"{familia}/{nome}: {campo} {achado} " +
                                         $"(o T1 tem {esperado})");
                    }

                    Conferir("ChanceCritica", t1.ChanceCriticaBase, d.ChanceCriticaBase);
                    Conferir("MultiplicadorCritico", t1.MultiplicadorCritico, d.MultiplicadorCritico);
                    Conferir("Precisao", t1.PrecisaoBase, d.PrecisaoBase);
                    Conferir("Alcance", t1.Alcance, d.Alcance);
                    Conferir("Raio", t1.Raio, d.Raio);
                }
            }

            Assert.IsEmpty(borrados,
                "Degrau(s) que mudaram a identidade da família:" + Environment.NewLine + "  " +
                string.Join(Environment.NewLine + "  ", borrados));
        }

        /// <summary>
        /// Cada degrau tem <b>habilidade própria</b>, porque o <c>HabilidadeDef</c> carrega o
        /// <c>NomeDaArma</c> — texto que o jogador lê. Reusar a do T1 faria o "Alfanje do Rei"
        /// anunciar a habilidade do "Alfanje de Alhazred".
        /// </summary>
        [Test]
        public void CadaDegrau_TemHabilidadePropria()
        {
            var compartilhadas = new List<string>();

            foreach (var (familia, degraus) in Escada)
            {
                var vistas = new Dictionary<HabilidadeDef, string>();

                foreach (var nome in degraus)
                {
                    var d = Base(nome);
                    if (d?.Habilidade == null) continue;

                    if (vistas.TryGetValue(d.Habilidade, out string outro))
                        compartilhadas.Add($"{familia}: {nome} e {outro} usam a MESMA " +
                                           $"HabilidadeDef ('{d.Habilidade.name}')");
                    else
                        vistas[d.Habilidade] = nome;
                }
            }

            Assert.IsEmpty(compartilhadas,
                "Degrau(s) compartilhando habilidade:" + Environment.NewLine + "  " +
                string.Join(Environment.NewLine + "  ", compartilhadas));
        }

        // ── A escala de afixo ─────────────────────────────────────────────────

        /// <summary>
        /// <b>O defeito que fazia todo drop parecer igual.</b> A base escalava +25% por nível e
        /// o afixo <b>não</b>: o <c>afixo_cravado</c> (+2 a 5 de dano) valia de 4% a 11% num
        /// Alfanje de nível 1, e <b>1% a 3%</b> num de nível 12. O afixo saía de marginal para
        /// invisível conforme o jogador subia — e isso ataca a razão de existir de um ARPG.
        /// </summary>
        [Test]
        public void AfixoAbsoluto_CresceComONivelDoItem()
        {
            var afixo = Todos().FirstOrDefault(a => a.EscalaComONivelDoItem);

            Assert.IsNotNull(afixo, "Nenhum afixo com escala ligada — o pool inteiro voltou a ser " +
                                    "plano.");

            var fonte = new FonteFixa(0.5f);

            float n1 = afixo.Rolar(fonte, 1);
            float n6 = afixo.Rolar(fonte, 6);

            Assert.Greater(n6, n1 * 1.5f,
                $"'{afixo.name}' rolou {n1:0.##} no nível 1 e {n6:0.##} no nível 6 — a escala " +
                "não está sendo aplicada, e o afixo volta a ser decorativo em nível alto.");
        }

        /// <summary>
        /// E o contrário: taxa por segundo <b>não</b> pode escalar. Multiplicar
        /// <c>RegenRM</c> por 3,75 no nível 12 anularia a Resiliência como recurso.
        /// </summary>
        [Test]
        public void AfixoDeTaxa_NaoEscala()
        {
            var fonte = new FonteFixa(0.5f);
            var planos = new List<string>();

            foreach (var a in Todos().Where(a => !a.EscalaComONivelDoItem))
            {
                float n1 = a.Rolar(fonte, 1);
                float n12 = a.Rolar(fonte, 12);

                if (Math.Abs(n1 - n12) > 0.0001f)
                    planos.Add($"{a.name}: {n1:0.###} → {n12:0.###}");
            }

            Assert.IsEmpty(planos,
                "Afixo(s) marcados como planos que escalaram mesmo assim:" +
                Environment.NewLine + "  " + string.Join(Environment.NewLine + "  ", planos));
        }

        /// <summary>
        /// O pool precisa cobrir os eixos que o combate ganhou. <c>ChanceCritica</c>,
        /// <c>DanoCritico</c>, <c>Precisao</c> e <c>AumentoDeDanoFisico</c> existiam no enum
        /// desde 2026-08-28 e <b>nenhum afixo os rolava</b> — quatro eixos de itemização
        /// inteiros sem conteúdo.
        /// </summary>
        [Test]
        public void OPool_CobreOsEixosDeCombate()
        {
            var rolados = Todos().Select(a => a.Stat).ToHashSet();

            foreach (var eixo in new[] { StatType.ChanceCritica, StatType.DanoCritico,
                                         StatType.Precisao, StatType.AumentoDeDanoFisico })
            {
                Assert.Contains(eixo, rolados.ToList(),
                    $"Nenhum afixo rola {eixo}. O atributo existe, o combate o lê, e o loot não " +
                    "tem como entregá-lo — eixo de itemização sem conteúdo.");
            }
        }

        private static AfixoDef[] Todos() =>
            AssetDatabase.FindAssets("t:AfixoDef")
                .Select(AssetDatabase.GUIDToAssetPath)
                .Select(AssetDatabase.LoadAssetAtPath<AfixoDef>)
                .Where(a => a != null)
                .OrderBy(a => a.name)
                .ToArray();
    }
}
