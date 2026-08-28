using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using NUnit.Framework;
using UnityEditor;
using FavelaAmarela.Inventario;
using FavelaAmarela.Player;

namespace FavelaAmarela.Tests.EditMode
{
    /// <summary>
    /// Guarda as três travas que faziam a itemização a dado <b>não aparecer em jogo</b>.
    ///
    /// <para>Todas as três eram invisíveis do mesmo jeito: a peça existe, o Inspector mostra um
    /// valor plausível, nada dá erro, e o efeito nunca acontece. O afixo rolava certo, entrava
    /// no <c>ItemInstance</c> certo, e morria antes de virar número na tela.</para>
    /// </summary>
    public sealed class ItemizacaoDestravadaTests
    {
        // ── 1. O cache de bônus que congelava ─────────────────────────────────

        /// <summary>
        /// <b>O defeito mais caro dos três.</b> <c>GerenciadorEfeitosPassivos._cacheValido</c>
        /// era escrito em <b>um lugar só</b> — no fim de <c>Recalcular</c> — e nunca voltava
        /// para <c>false</c>. O bônus de equipamento era calculado na primeira leitura de
        /// <c>GetBonus</c> e <b>congelava para o resto da partida</b>: trocar de arma, vestir
        /// armadura, pegar item com afixo ou destravar um Eco deixavam de mudar qualquer número.
        ///
        /// <para>E o comentário logo acima do campo descrevia a invalidação por evento como se
        /// ela existisse — foi escrito junto com o cache, e a linha que o implementava nunca
        /// chegou. Documentação e código discordando <b>dentro do mesmo arquivo</b>.</para>
        ///
        /// <para>É guarda de fonte porque o caminho vivo é um <c>MonoBehaviour</c> singleton que
        /// depende do <c>InventoryManager</c> em cena — não há como instanciá-lo aqui.</para>
        /// </summary>
        [Test]
        public void OCacheDeBonus_EhInvalidadoQuandoOEquipamentoMuda()
        {
            string fonte = File.ReadAllText(
                "Assets/Scripts/Player/GerenciadorEfeitosPassivos.cs");

            var notificar = Regex.Match(
                fonte, @"private void NotificarMudanca\(\)\s*\{(?<corpo>[^}]*)\}");

            Assert.IsTrue(notificar.Success,
                "NotificarMudanca sumiu ou mudou de forma. Ela é o único ponto por onde os " +
                "quatro eventos de mudança passam.");

            StringAssert.Contains("_cacheValido = false", notificar.Groups["corpo"].Value,
                "NotificarMudanca voltou a só disparar o evento sem invalidar o cache. O bônus " +
                "de equipamento congela na primeira leitura e a itemização inteira vira " +
                "decoração — sem erro, sem log, sem nada.");
        }

        // ── 2. O nível do item nas tabelas de drop ────────────────────────────

        /// <summary>
        /// <c>TabelaDeDrop.nivelDoItem</c> existia no C# e <b>não estava serializado em nenhum
        /// dos três assets</b>. Todo item que caía nascia nível 1, e três dos oito afixos
        /// autorados — os que pedem nível 2 ou 3 — <b>nunca podiam rolar</b>.
        /// </summary>
        [Test]
        public void TodaTabelaDeDrop_DeclaraONivelDoItem()
        {
            var mudas = new List<string>();
            var vistas = 0;

            foreach (var caminho in Tabelas())
            {
                vistas++;
                string yaml = File.ReadAllText(caminho);

                if (!Regex.IsMatch(yaml, @"^\s*nivelDoItem:\s*\d+\s*$", RegexOptions.Multiline))
                    mudas.Add($"{Path.GetFileNameWithoutExtension(caminho)}: sem 'nivelDoItem' " +
                              "no YAML — cai no default do C# e ninguém percebe");
            }

            Assert.Greater(vistas, 0, "Nenhuma tabela de drop encontrada.");

            Assert.IsEmpty(mudas,
                "Tabela(s) de drop sem nível declarado:" + Environment.NewLine + "  " +
                string.Join(Environment.NewLine + "  ", mudas) + Environment.NewLine +
                "Conserto: 'Tools/FavelaAmarela/Itens: definir o nível das tabelas de drop'.");
        }

        /// <summary>
        /// <b>O teste que mede o buraco, não o campo.</b> De nada adianta o nível estar
        /// serializado se todos ficarem em 1: o pool de afixos continuaria com 37% do conteúdo
        /// inalcançável. Este guarda cruza os níveis autorados nas tabelas com os
        /// <c>NivelMinimoDoItem</c> dos afixos e exige que <b>todo afixo seja alcançável por
        /// alguma fonte</b>.
        /// </summary>
        [Test]
        public void TodoAfixoAutorado_EhAlcancavelPorAlgumaFonteDeDrop()
        {
            int melhorNivel = Tabelas()
                .Select(c => NivelDe(File.ReadAllText(c)))
                .DefaultIfEmpty(0)
                .Max();

            Assert.Greater(melhorNivel, 0, "Nenhuma tabela de drop declara nível.");

            var inalcancaveis = new List<string>();
            var vistos = 0;

            foreach (var guid in AssetDatabase.FindAssets("t:AfixoDef"))
            {
                var afixo = AssetDatabase.LoadAssetAtPath<AfixoDef>(
                    AssetDatabase.GUIDToAssetPath(guid));
                if (afixo == null) continue;

                vistos++;
                if (afixo.NivelMinimoDoItem > melhorNivel)
                    inalcancaveis.Add($"{afixo.name}: pede nível {afixo.NivelMinimoDoItem}, e a " +
                                      $"melhor fonte do jogo entrega {melhorNivel}");
            }

            Assert.Greater(vistos, 0, "Nenhum AfixoDef encontrado — o pool sumiu?");

            Assert.IsEmpty(inalcancaveis,
                "Afixo(s) que nenhuma fonte consegue rolar:" + Environment.NewLine + "  " +
                string.Join(Environment.NewLine + "  ", inalcancaveis) + Environment.NewLine +
                "Ou o afixo baixa de nível, ou alguma tabela sobe. Conteúdo autorado e " +
                "inalcançável é pior que conteúdo ausente: ele parece pronto.");
        }

        // ── 3. Os dois atributos que dois artefatos prometiam e ninguém lia ───

        /// <summary>
        /// <c>Artefato_AnelDoSinalAmarelo</c> rola <c>Furtividade</c> e
        /// <c>Artefato_CoroaDeOssos</c> rola <c>DefesaAnomalia</c>. Os dois eram
        /// <b>decorativos</b> — nenhuma linha do jogo os lia —, então os dois artefatos
        /// prometiam um efeito que não existia.
        ///
        /// <para>Num jogo cujo pilar é a furtividade, deixar <c>Furtividade</c> sem consumidor
        /// era o mais caro dos dois. E <c>DefesaAnomalia</c> escondia uma assimetria: todo
        /// inimigo mitiga o canal anômalo pela ficha, e o Damião não mitigava nada.</para>
        /// </summary>
        [Test]
        public void AFurtividade_ReduzORuidoDoJogador()
        {
            var furtivo = new PlayerStealthState();
            furtivo.SetMode(MovementMode.Running);

            float semAnel = furtivo.GetCurrentNoiseEmission(true, 0f, 0f);
            float comAnel = furtivo.GetCurrentNoiseEmission(true, 0f, 3f);

            Assert.Less(comAnel, semAnel,
                "Furtividade voltou a não fazer nada. O Anel do Sinal Amarelo promete discrição " +
                "e o pilar do jogo é justamente esse.");

            Assert.GreaterOrEqual(comAnel, 0f, "Ruído não pode ficar negativo.");
        }

        [Test]
        public void AFurtividade_NaoConcedeInvisibilidade()
        {
            var furtivo = new PlayerStealthState();
            furtivo.SetMode(MovementMode.Sneaking);

            float absurdo = furtivo.GetCurrentNoiseEmission(true, 0f, 999f);

            Assert.GreaterOrEqual(absurdo, PlayerStealthState.PisoDeRuidoEmMovimento,
                "Furtividade alta demais zerou o ruído. Quem se move nunca pode ser " +
                "literalmente inaudível — é a mesma regra que a tempestade já respeita.");
        }

        [Test]
        public void ADefesaAnomala_MitigaOTraumaDoJogador()
        {
            string fonte = File.ReadAllText("Assets/Scripts/Combat/ResilienciaBridge.cs");

            StringAssert.Contains("MitigacaoDeDano.Aplicar", fonte,
                "O Trauma voltou a chegar cru à Resiliência Mental. Todo inimigo mitiga o canal " +
                "anômalo pela ficha; o Damião ficava sem defesa nenhuma nesse canal, e a " +
                "DefesaAnomalia da Coroa de Ossos não fazia efeito.");

            StringAssert.Contains("StatType.DefesaAnomalia", fonte,
                "A ResilienciaBridge parou de consultar DefesaAnomalia.");
        }

        // ── Apoio ─────────────────────────────────────────────────────────────

        private static IEnumerable<string> Tabelas() =>
            AssetDatabase.FindAssets("t:TabelaDeDrop")
                         .Select(AssetDatabase.GUIDToAssetPath)
                         .Where(c => !string.IsNullOrEmpty(c) && File.Exists(c))
                         .OrderBy(c => c);

        private static int NivelDe(string yaml)
        {
            var m = Regex.Match(yaml, @"^\s*nivelDoItem:\s*(\d+)\s*$", RegexOptions.Multiline);
            return m.Success && int.TryParse(m.Groups[1].Value, out int v) ? v : 0;
        }
    }
}
