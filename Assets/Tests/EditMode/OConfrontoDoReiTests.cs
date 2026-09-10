using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using FavelaAmarela.Core.Combat;
using FavelaAmarela.Core.Enemies;
using FavelaAmarela.Core.Progression;
using FavelaAmarela.Inventario;
using FavelaAmarela.Runtime.Enemies;
using FavelaAmarela.Runtime.Itens;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace FavelaAmarela.Tests.EditMode
{
    /// <summary>
    /// A quarta fase do rito — o Confronto — medida contra o que o jogador tem nas mãos quando
    /// chega ao Trono.
    ///
    /// <para><b>O pedido (Vini, 2026-09-10):</b> <i>"depois dos três escudos, abre-se uma fase
    /// de combate contra ele"</i>. O Rei não tem clipe de ataque (só idle, selar, desvelo, dano,
    /// queda), então a fase mantém o ritmo das três primeiras — escudo, calmaria, desvelo — e
    /// acrescenta uma coisa: <b>entre os desvelos ele sangra</b>. O jogador sai do abrigo, corre
    /// até ele, fere, e volta antes do próximo desvelo.</para>
    ///
    /// <para><b>Três coisas precisam bater, e este arquivo as amarra:</b> a geometria da sala
    /// (dá para ir, ferir e voltar dentro da calmaria?), a ficha do Rei contra a arma que o
    /// jogador carrega (quantos ciclos a luta dura?), e o nível em que ele chega (somado das
    /// cenas do caminho crítico, não estimado). É a mesma régua do Byakhee: <i>"9 por 9 no
    /// nível 3"</i> saiu daqui, não do gosto.</para>
    /// </summary>
    public sealed class OConfrontoDoReiTests
    {
        private const string Cena = "Assets/Scenes/Castelo_Carcosa.unity";
        private const string Prefab = "Assets/FavelaAmarela/Art/Enemies/ReiEmAmarelo.prefab";
        private const string Enemies = "Assets/FavelaAmarela/Art/Enemies";
        private const string Cenas = "Assets/Scenes";

        /// <summary>Damião correndo — no Confronto correr é o jogo esperado.</summary>
        private const float VelocidadeCorrendo = 7.5f;

        /// <summary>
        /// Golpes que um jogador de verdade dá por ciclo: chega, bate, vai embora. O máximo
        /// geométrico é maior; este é o número que se joga.
        /// </summary>
        private const int GolpesPorCicloRealista = 3;

        /// <summary>
        /// A mesma curva do <c>ProgressionBridge</c>, copiada como o
        /// <c>EconomiaDeExposicaoTests</c> já faz — se ela mudar lá, os dois testes avisam.
        /// </summary>
        private static readonly int[] Curva =
        {
            0, 100, 300, 600, 1000, 1500, 2100, 2800, 3600, 4500, 5500, 6600
        };

        /// <summary>
        /// A arma do baú da Tumba chega ao jogador no nível 2 (medido no Byakhee: 50,5 × 1,25
        /// = 63 brutos, "9 por 9"). É o piso do que ele carrega ao Trono.
        /// </summary>
        private const int NivelDaArmaDoBau = 2;

        // ── infraestrutura ───────────────────────────────────────────────────

        private static T Carregar<T>(string nome) where T : UnityEngine.Object
        {
            string caminho = AssetDatabase.FindAssets($"t:{typeof(T).Name} {nome}")
                .Select(AssetDatabase.GUIDToAssetPath)
                .FirstOrDefault(p => Path.GetFileNameWithoutExtension(p) == nome);

            Assert.NotNull(caminho, $"Asset '{nome}' não encontrado.");
            return AssetDatabase.LoadAssetAtPath<T>(caminho);
        }

        private static FichaAtributosConfig FichaDoRei()
        {
            var rei = AssetDatabase.LoadAssetAtPath<GameObject>(Prefab).GetComponent<ReiEmAmareloAI>();
            Assert.NotNull(rei, "O prefab do Rei perdeu o ReiEmAmareloAI.");

            var ficha = new SerializedObject(rei).FindProperty("ficha").objectReferenceValue
                        as FichaAtributosConfig;

            Assert.NotNull(ficha, "O prefab do Rei está sem ficha — o Confronto rodaria na de " +
                                  "emergência. Rode Tools/FavelaAmarela/Trono: dar carne ao Rei.");
            return ficha;
        }

        private static float DanoMedioBruto(BaseDeArma arma, int nivelDoItem)
            => (arma.DanoMinBase + arma.DanoMaxBase) * 0.5f * EscalaDeNivel.FatorDeDano(nivelDoItem);

        private static float GolpesParaDerrubar(FichaAtributosConfig rei, float danoBruto)
        {
            float porGolpe = MitigacaoDeDano.Aplicar(danoBruto, rei.Defesa);
            return rei.VitalidadeMax / porGolpe;
        }

        /// <summary>
        /// O nível em que Damião chega ao Trono, somado das cenas do caminho crítico — o mesmo
        /// método do <c>EconomiaDeExposicaoTests</c>, esticado até o Castelo.
        /// </summary>
        private static int NivelAoChegarNoTrono(out string memoria)
        {
            var linhas = new List<string>();
            int total = 0;

            foreach (var cena in new[] { "Deserto_Hali", "Tumba_De_Alhazred", "Portoes_Das_Ruinas",
                                         "Santuario_Yhtill", "Castelo_Carcosa" })
            {
                string yamlDaCena = File.ReadAllText($"{Cenas}/{cena}.unity");

                foreach (var prefab in Directory.GetFiles(Enemies, "*.prefab"))
                {
                    string nome = Path.GetFileNameWithoutExtension(prefab);
                    if (nome == "ReiEmAmarelo") continue; // é quem se enfrenta

                    var guid = Regex.Match(File.ReadAllText(prefab + ".meta"), @"guid:\s*(\w+)");
                    if (!guid.Success) continue;

                    int quantos = Regex.Matches(yamlDaCena,
                        @"m_SourcePrefab: \{fileID: \d+, guid: " + guid.Groups[1].Value).Count;
                    if (quantos == 0) continue;

                    int valor = LerInteiro(File.ReadAllText(prefab), "exposicaoAoAbater");
                    if (valor == 0) valor = LerInteiro(File.ReadAllText(prefab), "exposicao");
                    if (valor == 0) continue;

                    total += quantos * valor;
                    linhas.Add($"  {cena}: {quantos}× {nome} × {valor} = {quantos * valor}");
                }
            }

            var progressao = new Progressao(Curva);
            progressao.AdicionarExposicao(total);

            linhas.Add($"  TOTAL: {total} de Exposição → nível {progressao.NivelAtual}");
            memoria = string.Join(Environment.NewLine, linhas);
            return progressao.NivelAtual;
        }

        private static int LerInteiro(string yaml, string campo)
        {
            var m = Regex.Match(yaml, campo + @":\s*(\d+)");
            return m.Success ? int.Parse(m.Groups[1].Value) : 0;
        }

        // ── o prefab tem carne ───────────────────────────────────────────────

        [Test]
        public void ORei_EhUmaAparicaoPrimordial_QueSeFere()
        {
            var rei = AssetDatabase.LoadAssetAtPath<GameObject>(Prefab).GetComponent<ReiEmAmareloAI>();

            Assert.IsInstanceOf<IDanificavel>(rei,
                "O Rei precisa implementar IDanificavel — sem isso a Hurtbox não entrega o golpe.");
            Assert.IsTrue(((IDanificavel)rei).EhAparicaoPrimordial,
                "Aparição Primordial: imune ao crítico de furtividade, como todo chefe.");
        }

        [Test]
        public void ORei_TemFichaAutorada()
        {
            var ficha = FichaDoRei();

            Assert.Greater(ficha.VitalidadeMax, 0f, "Sem Vitalidade não há o que ferir.");
            Assert.AreEqual(0f, ficha.Ataque,
                "O Rei não golpeia — o desvelo é o ataque. Ataque > 0 na ficha seria promessa " +
                "de um moveset que não tem arte.");
        }

        // ── a geometria: ir, ferir, voltar ───────────────────────────────────

        /// <summary>
        /// De <b>todo</b> abrigo dá para correr até o Rei, dar os golpes de um ciclo e voltar
        /// antes do desvelo. Se alguém afastar um altar ou encurtar a calmaria, este teste
        /// falha em vez de o jogador descobrir que a fase é impossível.
        /// </summary>
        [Test]
        public void DeTodoAbrigo_DaParaIrFerirEVoltar_DentroDaCalmaria()
        {
            EditorSceneManager.OpenScene(Cena, OpenSceneMode.Single);

            var rei = UnityEngine.Object.FindAnyObjectByType<ReiEmAmareloAI>();
            var abrigos = UnityEngine.Object.FindObjectsByType<EscudoDeReliquia>();
            Assert.IsNotEmpty(abrigos, "Sem abrigos na cena.");

            var so = new SerializedObject(rei);
            float calmaria = so.FindProperty("intervaloNoConfronto").floatValue;
            if (calmaria <= 0f) calmaria = so.FindProperty("intervaloEntreCiclos").floatValue;

            var alfanje = Carregar<BaseDeArma>("BaseArma_Alfanje");
            float golpes = GolpesPorCicloRealista * alfanje.DuracaoTotalDoGolpe;

            // Onde se bate nele: um passo ao sul dos pés, dentro do alcance da hurtbox.
            Vector2 pontoDeGolpe = (Vector2)rei.transform.position + Vector2.down;

            foreach (var abrigo in abrigos)
            {
                float ida = AbrigoDeReliquia.SegundosParaAlcancar(
                    pontoDeGolpe, abrigo.transform.position, VelocidadeCorrendo,
                    abrigo.SemiEixoX, abrigo.SemiEixoY);

                float idaEVolta = ida * 2f;
                float total = idaEVolta + golpes;

                Assert.Less(total, calmaria,
                    $"Do abrigo de '{abrigo.ArtefatoId}': {idaEVolta:F2} s de ida e volta " +
                    $"correndo + {golpes:F2} s de {GolpesPorCicloRealista} golpes = {total:F2} s, " +
                    $"e a calmaria do Confronto é {calmaria:F2} s. Não dá para ferir e voltar.");
            }
        }

        // ── a duração da luta ────────────────────────────────────────────────

        /// <summary>
        /// Com a arma do baú (o piso), a fase dura entre 4 e 9 ciclos a 3 golpes por ciclo —
        /// nem um empurrão, nem uma maratona. Abaixo de 4 o Confronto não chega a ser uma
        /// fase; acima de 9 são mais de um minuto correndo entre abrigo e trono.
        /// </summary>
        [Test]
        public void ComAArmaDoBau_OConfrontoDuraEntreQuatroENoveCiclos()
        {
            var rei = FichaDoRei();
            var alfanje = Carregar<BaseDeArma>("BaseArma_Alfanje");

            float bruto = DanoMedioBruto(alfanje, NivelDaArmaDoBau);
            float golpes = GolpesParaDerrubar(rei, bruto);
            float ciclos = Mathf.Ceil(golpes / GolpesPorCicloRealista);

            string memoria = $"Alfanje do baú @ nível {NivelDaArmaDoBau}: {bruto:F1} brutos, " +
                             $"{MitigacaoDeDano.Aplicar(bruto, rei.Defesa):F1} após Defesa {rei.Defesa} → " +
                             $"{golpes:F1} golpes → {ciclos:0} ciclos a {GolpesPorCicloRealista}/ciclo.";

            Assert.GreaterOrEqual(ciclos, 4f, "Curto demais para ser uma fase.\n" + memoria);
            Assert.LessOrEqual(ciclos, 9f, "Longo demais — vira maratona.\n" + memoria);
        }

        /// <summary>
        /// Com uma arma T2 no nível em que se chega ao Trono, o Rei <b>não cai num ciclo só</b>
        /// nem para o jogador perfeito. O nível é somado das cenas, não estimado.
        ///
        /// <para><b>O que "jogo perfeito" quer dizer aqui (afinado em 2026-09-10).</b> A primeira
        /// versão media o máximo geométrico — cada segundo da calmaria batendo, sem sair do
        /// lugar. Mas quem não sai do lugar não volta ao abrigo, e morre. O máximo real é o que
        /// sobra <b>depois de ir e voltar</b> do abrigo mais próximo, correndo, sem um quadro de
        /// hesitação. Foi esta conta que decidiu a calmaria do Confronto em <b>5 s</b>, e não 6:
        /// a 6 s a T2 matava o Rei num ciclo; a 5 s cabem 5 golpes e ela precisa de 8,6.</para>
        /// </summary>
        [Test]
        public void ComUmaArmaT2NoNivelDeChegada_ORei_NaoCaiNumCicloSo()
        {
            var rei = FichaDoRei();
            int nivel = NivelAoChegarNoTrono(out string memoria);

            var t2 = Carregar<BaseDeArma>("BaseArma_Alfanje_T2");
            float bruto = DanoMedioBruto(t2, nivel);
            float golpes = GolpesParaDerrubar(rei, bruto);

            EditorSceneManager.OpenScene(Cena, OpenSceneMode.Single);
            var ai = UnityEngine.Object.FindAnyObjectByType<ReiEmAmareloAI>();
            var so = new SerializedObject(ai);
            float calmaria = so.FindProperty("intervaloNoConfronto").floatValue;
            if (calmaria <= 0f) calmaria = so.FindProperty("intervaloEntreCiclos").floatValue;

            Vector2 pontoDeGolpe = (Vector2)ai.transform.position + Vector2.down;
            float menorIdaEVolta = UnityEngine.Object
                .FindObjectsByType<EscudoDeReliquia>()
                .Min(e => 2f * AbrigoDeReliquia.SegundosParaAlcancar(
                    pontoDeGolpe, e.transform.position, VelocidadeCorrendo, e.SemiEixoX, e.SemiEixoY));

            float golpesMaximosPorCiclo = Mathf.Floor((calmaria - menorIdaEVolta) / t2.DuracaoTotalDoGolpe);

            Assert.Greater(golpes, golpesMaximosPorCiclo,
                $"Alfanje T2 @ nível {nivel}: {bruto:F1} brutos → {golpes:F1} golpes. Do abrigo " +
                $"mais próximo sobram {calmaria - menorIdaEVolta:F2} s da calmaria de {calmaria:F1} s, " +
                $"que comportam {golpesMaximosPorCiclo:0} golpes. O Rei cairia num ciclo só.\n" + memoria);
        }

        /// <summary>
        /// A calmaria do Confronto é <b>mais curta</b> que a do selamento: as três primeiras
        /// fases ensinam o relógio, a quarta aperta. É a alavanca que impede a arma T2 de
        /// resolver a luta num ciclo sem punir quem chega com a do baú.
        /// </summary>
        [Test]
        public void ACalmariaDoConfronto_EhMaisCurtaQueADoSelamento()
        {
            EditorSceneManager.OpenScene(Cena, OpenSceneMode.Single);
            var so = new SerializedObject(UnityEngine.Object.FindAnyObjectByType<ReiEmAmareloAI>());

            float selamento = so.FindProperty("intervaloEntreCiclos").floatValue;
            float confronto = so.FindProperty("intervaloNoConfronto").floatValue;

            Assert.Greater(confronto, 0f, "intervaloNoConfronto zerado herda os 6 s do selamento.");
            Assert.Less(confronto, selamento,
                $"Confronto a {confronto:F1} s não aperta nada contra os {selamento:F1} s do selamento.");
        }

        /// <summary>
        /// A conta de nível ficou visível: se a economia de Exposição mudar e Damião passar a
        /// chegar no Trono num nível muito diferente, esta asserção diz em qual.
        /// </summary>
        [Test]
        public void DamiaoChegaAoTrono_NumNivelDeMeioDeJogo()
        {
            int nivel = NivelAoChegarNoTrono(out string memoria);

            Assert.GreaterOrEqual(nivel, 3, "Chega fraco demais para o desfecho.\n" + memoria);
            Assert.LessOrEqual(nivel, 6, "Chega forte demais — o loot deixa de importar.\n" + memoria);
        }
    }
}
