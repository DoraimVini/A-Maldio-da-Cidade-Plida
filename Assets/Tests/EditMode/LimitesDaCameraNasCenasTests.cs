using System.Collections.Generic;
using System.IO;
using System.Linq;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Tilemaps;

namespace FavelaAmarela.Tests.EditMode
{
    /// <summary>
    /// Guarda que <b>toda cena de gameplay oferece à câmera de onde tirar os limites do mapa</b>.
    ///
    /// <para><b>O defeito que isto fecha (2026-09-09), e ele era meu.</b> O travamento da câmera
    /// foi escrito olhando o Deserto de Hali, onde as paredes de borda se chamam
    /// <c>Limite_Norte</c>, <c>Limite_Sul</c>, <c>Limite_Leste</c> e <c>Limite_Oeste</c>. Medido
    /// depois nas seis cenas: <b>só o Deserto usa esse nome</b>. O Santuário tem as quatro
    /// paredes equivalentes chamadas <c>Parede_*</c>; Castelo, Tumba e Portões não têm parede
    /// nomeada nenhuma.</para>
    ///
    /// <para><b>O travamento funcionava em uma cena de seis</b>, com a suíte verde e 22 testes
    /// EditMode confirmando que a aritmética estava certa — porque a aritmética <i>estava</i>
    /// certa. O que faltava era o elo. É o modo de falha dominante deste projeto: código que
    /// existe, compila, tem teste, e não está ligado.</para>
    ///
    /// <para><b>Por que ler a cena e não confiar no componente.</b> O <c>ResolverLimites</c> roda
    /// em <c>Awake</c>, que não existe em EditMode. Este teste refaz a mesma pergunta contra o
    /// disco: há parede nomeada? há Tilemap? Se as duas respostas forem não, a câmera daquela
    /// cena vai seguir o jogador sem travar e mostrar o vazio além da borda.</para>
    /// </summary>
    public sealed class LimitesDaCameraNasCenasTests
    {
        /// <summary>Os mesmos prefixos que o <c>IsometricCameraController</c> procura.</summary>
        private static readonly string[] PrefixosDeParede = { "Limite_", "Parede_", "Muro_" };

        /// <summary>
        /// Cenas sem área jogável, que legitimamente não travam a câmera.
        /// </summary>
        private static readonly string[] SemLimitesDePropósito = { "Cena_Menu" };

        /// <summary>
        /// Cenas que hoje caem no <b>último recurso</b> — limites derivados do Tilemap, porque
        /// não têm parede de borda nomeada.
        ///
        /// <para>É dívida registrada, não aprovação: o Tilemap é a extensão do que foi
        /// <i>pintado</i>, e chão costuma ser pintado além da área jogável de propósito (no
        /// Deserto, 228 × 114 de areia para 88 × 64 de área jogável). Travar por ele é melhor
        /// que não travar, e pior que uma parede nomeada. Quando estas cenas ganharem
        /// <c>Limite_*</c> ou <c>Parede_*</c>, tire o nome daqui.</para>
        /// </summary>
        private static readonly string[] ApoiadasNoTilemap =
        {
            "Tumba_De_Alhazred", "Castelo_Carcosa", "Portoes_Das_Ruinas",
        };

        private sealed class Estado
        {
            public string Cena;
            public int Paredes;
            public int Tilemaps;
        }

        private static List<Estado> Levantar()
        {
            var estados = new List<Estado>();

            foreach (var entrada in EditorBuildSettings.scenes)
            {
                if (!entrada.enabled || !File.Exists(entrada.path)) continue;

                Scene cena = EditorSceneManager.OpenScene(entrada.path, OpenSceneMode.Single);
                var raizes = cena.GetRootGameObjects();

                estados.Add(new Estado
                {
                    Cena = Path.GetFileNameWithoutExtension(entrada.path),
                    Paredes = raizes
                        .SelectMany(r => r.GetComponentsInChildren<Collider2D>(true))
                        .Count(c => PrefixosDeParede.Any(
                            p => c.name.StartsWith(p, System.StringComparison.Ordinal))),
                    Tilemaps = raizes
                        .SelectMany(r => r.GetComponentsInChildren<Tilemap>(true))
                        .Count(),
                });
            }

            return estados;
        }

        [Test]
        public void TodaCenaDeGameplay_OfereceDeOndeTirarOsLimites()
        {
            var orfas = Levantar()
                .Where(e => !SemLimitesDePropósito.Contains(e.Cena))
                .Where(e => e.Paredes == 0 && e.Tilemaps == 0)
                .Select(e => e.Cena)
                .ToArray();

            Assert.IsEmpty(orfas,
                "Estas cenas não têm parede de borda nomeada NEM Tilemap: a câmera vai seguir o " +
                "jogador sem travar e mostrar o vazio além da borda do mapa — sem erro, só um " +
                "aviso no console.\n  " + string.Join("\n  ", orfas));
        }

        /// <summary>
        /// As cenas que <b>têm</b> parede nomeada precisam continuar tendo. Renomear
        /// <c>Limite_Norte</c> para <c>Borda_Norte</c> não quebra compilação nem cena — só
        /// desliga o travamento, em silêncio. Foi assim que o defeito nasceu.
        /// </summary>
        [Test]
        public void AsCenasComParedeNomeada_ContinuamComElas()
        {
            var porCena = Levantar().ToDictionary(e => e.Cena, e => e.Paredes);

            foreach (var cena in new[] { "Deserto_Hali", "Santuario_Yhtill" })
            {
                Assert.That(porCena.ContainsKey(cena), Is.True,
                    $"A cena {cena} sumiu do Build Settings.");

                Assert.GreaterOrEqual(porCena[cena], 4,
                    $"{cena} tinha 4 paredes de borda nomeadas e agora tem {porCena[cena]}. " +
                    "Um rename desliga o travamento da câmera sem quebrar nada mais.");
            }
        }

        /// <summary>
        /// Congela a lista de cenas que dependem do Tilemap. Se uma cena <b>sair</b> da lista
        /// porque ganhou paredes, o teste avisa para tirá-la daqui — assim a dívida não fica
        /// registrada depois de paga.
        /// </summary>
        [Test]
        public void ADividaDoTilemap_NaoCresceEEncolheComAviso()
        {
            var semParede = Levantar()
                .Where(e => !SemLimitesDePropósito.Contains(e.Cena))
                .Where(e => e.Paredes == 0)
                .Select(e => e.Cena)
                .OrderBy(n => n)
                .ToArray();

            var esperado = ApoiadasNoTilemap.OrderBy(n => n).ToArray();

            CollectionAssert.AreEqual(esperado, semParede,
                "A lista de cenas sem parede de borda mudou.\n" +
                $"  esperado: {string.Join(", ", esperado)}\n" +
                $"  medido:   {string.Join(", ", semParede)}\n" +
                "Se uma cena GANHOU paredes, tire o nome de ApoiadasNoTilemap. Se uma cena " +
                "PERDEU, a câmera dela acabou de ficar menos precisa.");
        }
    }
}
