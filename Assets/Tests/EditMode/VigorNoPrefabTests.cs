using System.IO;
using System.Text.RegularExpressions;
using NUnit.Framework;

namespace FavelaAmarela.Tests.EditMode
{
    /// <summary>
    /// Guarda o <c>GerenciadorDeVigor</c> no prefab de Damião.
    ///
    /// <para><b>O bug que motivou (2026-08-18):</b> o componente não estava no prefab — tinha
    /// sido adicionado como override de instância <b>só na Arena de Testes</b>. Nas três cenas
    /// jogáveis <c>GetComponent&lt;GerenciadorDeVigor&gt;()</c> devolvia null, e todos os
    /// consumidores degradam em silêncio com o padrão
    /// <c>if (_vigor != null &amp;&amp; !_vigor.TentarConsumir…())</c>: a condição
    /// curto-circuita e a ação passa. Ou seja, <b>esquiva grátis e corrida infinita</b> em todo o
    /// jogo, sem erro no console.</para>
    ///
    /// <para>Um guarda de cena não pegaria isto: o Damião vem de prefab, e componentes herdados
    /// não aparecem no YAML da cena. Por isso este olha o <b>prefab</b>.</para>
    /// </summary>
    public sealed class VigorNoPrefabTests
    {
        private const string PrefabDoDamiao =
            "Assets/FavelaAmarela/Art/Characters/Damiao/Player_Damiao.prefab";

        [Test]
        public void PlayerDamiao_TemGerenciadorDeVigor()
        {
            Assert.IsTrue(File.Exists(PrefabDoDamiao), $"Prefab ausente: {PrefabDoDamiao}");

            string guid = GuidDoScript("GerenciadorDeVigor.cs");
            Assert.IsNotNull(guid, "GerenciadorDeVigor.cs sem .meta — nada a procurar.");

            string conteudo = File.ReadAllText(PrefabDoDamiao);

            Assert.IsTrue(conteudo.Contains(guid),
                "Player_Damiao.prefab está sem GerenciadorDeVigor. Sem ele, EsquivaBridge e " +
                "PlayerMovement acham null e liberam a ação de graça — esquiva sem custo e " +
                "corrida infinita, em silêncio. Rode " +
                "'Tools/FavelaAmarela/Ligar Vigor no prefab do Damião'.");
        }

        /// <summary>
        /// Nenhuma cena pode acrescentar um <c>GerenciadorDeVigor</c> por cima da instância: com
        /// o componente já no prefab, o override produziria <b>dois</b> no mesmo GameObject, e
        /// dois gerenciadores concorrendo pelo mesmo recurso é bug difícil de enxergar.
        /// </summary>
        [Test]
        public void NenhumaCena_AcrescentaVigorPorCimaDaInstancia()
        {
            string guid = GuidDoScript("GerenciadorDeVigor.cs");
            Assert.IsNotNull(guid);

            var comOverride = new System.Collections.Generic.List<string>();

            foreach (var cena in Directory.GetFiles("Assets/Scenes", "*.unity"))
            {
                string txt = File.ReadAllText(cena);

                // Um bloco MonoBehaviour com este script DENTRO de uma cena, apontando para um
                // GameObject 'stripped', é componente adicionado a instância de prefab.
                foreach (var bloco in Regex.Split(txt, @"\n(?=--- !u!)"))
                {
                    if (!bloco.Contains("m_Script:") || !bloco.Contains(guid)) continue;
                    comOverride.Add(Path.GetFileName(cena));
                    break;
                }
            }

            Assert.IsEmpty(comOverride,
                "Cena(s) acrescentando GerenciadorDeVigor por cima do prefab, o que resultaria " +
                "em dois componentes no mesmo Damião: " + string.Join(", ", comOverride));
        }

        private static string GuidDoScript(string nomeDoArquivo)
        {
            var metas = Directory.GetFiles("Assets/Scripts", nomeDoArquivo + ".meta",
                SearchOption.AllDirectories);
            if (metas.Length == 0) return null;

            var m = Regex.Match(File.ReadAllText(metas[0]), @"guid:\s*([0-9a-f]{32})");
            return m.Success ? m.Groups[1].Value : null;
        }
    }
}
