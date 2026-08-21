using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using NUnit.Framework;

namespace FavelaAmarela.Tests.EditMode
{
    /// <summary>
    /// Guarda a separação hitbox/hurtbox e — o que mais importa — a <b>janela ativa</b> do golpe.
    ///
    /// <para><b>Por que existe (2026-08-21):</b> o Vini relatou que a luta contra o Byakhee "não
    /// tem feel bom". Três causas foram achadas e corrigidas antes (chefe sem colisor, golpe sem
    /// som, física girando). A quarta era a mais estrutural: <c>ByakheeAI.GolpearComGarras</c>
    /// rodava <b>uma vez</b>, na entrada do estado <c>Pousado</c>, fazendo
    /// <c>Vector2.Distance &lt;= alcance</c>. Sem janela, não existe esquivar no tempo — só
    /// estar longe naquele quadro exato; e sendo radial, estar <b>atrás</b> do chefe também
    /// levava dano.</para>
    ///
    /// <para><b>O teste central aqui é <see cref="AJanelaDasGarras_NaoEInstantanea"/>.</b> Um
    /// guarda que só verificasse "a hitbox existe" passaria mesmo com a janela em zero — que é
    /// exatamente o defeito de origem, com outro nome.</para>
    ///
    /// <para><b>Nota de método:</b> o regex de documento aceita sufixo depois do fileID
    /// (<c>&amp;123 stripped</c>). Um padrão que exigisse quebra de linha imediata falha em
    /// referências herdadas de prefab — erro que gerou um falso alarme grave nesta mesma
    /// sessão.</para>
    /// </summary>
    public sealed class HitboxEHurtboxTests
    {
        private const string PrefabDamiao =
            "Assets/FavelaAmarela/Art/Characters/Damiao/Player_Damiao.prefab";

        private const string PrefabByakhee = "Assets/FavelaAmarela/Art/Enemies/Byakhee.prefab";

        /// <summary>Quebra de linha. Constante para não depender de escape ao gerar código.</summary>
        private static readonly string NovaLinha = System.Environment.NewLine;

        private const string TagManager = "ProjectSettings/TagManager.asset";

        [Test]
        public void ODamiao_TemHurtboxNaCamadaCerta()
        {
            Assert.IsTrue(File.Exists(PrefabDamiao), $"{PrefabDamiao} ausente.");

            int camada = CamadaPorNome("PlayerHurtbox");
            Assert.GreaterOrEqual(camada, 0, "Camada 'PlayerHurtbox' não existe no TagManager.");

            var docs = Documentos(File.ReadAllText(PrefabDamiao)).ToList();

            var hurtbox = docs.FirstOrDefault(d =>
                Regex.IsMatch(d, @"(?m)^\s*m_Name:\s*Hurtbox\s*$"));

            Assert.IsNotNull(hurtbox,
                "Damião está sem GameObject 'Hurtbox'. Sem hurtbox, o colisor de movimento " +
                "volta a acumular a função de receber dano — e um número só não serve para " +
                "andar e para apanhar ao mesmo tempo. " +
                "Conserto: 'Tools/FavelaAmarela/Combate: montar hitbox e hurtbox'.");

            var layer = Regex.Match(hurtbox, @"(?m)^\s*m_Layer:\s*(\d+)");
            Assert.IsTrue(layer.Success, "Hurtbox sem m_Layer.");
            Assert.AreEqual(camada, int.Parse(layer.Groups[1].Value),
                $"A Hurtbox do Damião deveria estar na camada PlayerHurtbox ({camada}).");
        }

        [Test]
        public void AHurtboxDoDamiao_ETrigger()
        {
            string yaml = File.ReadAllText(PrefabDamiao);

            var capsula = Documentos(yaml).FirstOrDefault(d => Regex.IsMatch(d, @"!u!70\b"));

            Assert.IsNotNull(capsula, "Damião sem CapsuleCollider2D de hurtbox.");
            Assert.IsTrue(Regex.IsMatch(capsula, @"m_IsTrigger:\s*1"),
                "A hurtbox precisa ser trigger — sólida, ela empurraria o Damião pelo cenário " +
                "e viraria um segundo colisor de movimento.");
        }

        [Test]
        public void OByakhee_TemHitboxLigadaNaIA()
        {
            Assert.IsTrue(File.Exists(PrefabByakhee), $"{PrefabByakhee} ausente.");

            string yaml = File.ReadAllText(PrefabByakhee);

            var campo = Regex.Match(yaml, @"hitboxDasGarras:\s*\{fileID:\s*(-?\d+)");

            Assert.IsTrue(campo.Success,
                "ByakheeAI não tem o campo 'hitboxDasGarras'.");

            Assert.AreNotEqual("0", campo.Groups[1].Value,
                "A hitbox das garras não está ligada no ByakheeAI. Sem ela o golpe cai para o " +
                "teste instantâneo de distância — não esquivável no tempo. " +
                "Conserto: 'Tools/FavelaAmarela/Combate: montar hitbox e hurtbox'.");
        }

        /// <summary>
        /// <b>O teste que realmente importa.</b> A hitbox existir não conserta nada se a janela
        /// for zero: um golpe de duração nula é o mesmo teste de um quadro só que existia antes,
        /// com outro nome.
        /// </summary>
        [Test]
        public void AJanelaDasGarras_NaoEInstantanea()
        {
            string yaml = File.ReadAllText(PrefabByakhee);

            var janela = Regex.Match(yaml, @"janelaDasGarras:\s*([\d.eE+-]+)");

            Assert.IsTrue(janela.Success, "ByakheeAI sem 'janelaDasGarras'.");

            float valor = float.Parse(janela.Groups[1].Value, CultureInfo.InvariantCulture);

            Assert.Greater(valor, 0.05f,
                $"A janela das garras está em {valor:0.###}s — curta demais para ser lida e " +
                "esquivada. É ela que transforma a esquiva numa decisão de tempo em vez de um " +
                "teste de posição; zerá-la reintroduz o defeito original.");
        }

        [Test]
        public void AHitboxDoByakhee_MiraAHurtboxDoJogador()
        {
            int camada = CamadaPorNome("PlayerHurtbox");
            Assert.GreaterOrEqual(camada, 0, "Camada 'PlayerHurtbox' não existe.");

            string yaml = File.ReadAllText(PrefabByakhee);

            var bits = Regex.Match(yaml, @"camadasAlvo:\s*\r?\n\s*serializedVersion:\s*\d+\s*\r?\n\s*m_Bits:\s*(\d+)");

            Assert.IsTrue(bits.Success, "Hitbox do Byakhee sem 'camadasAlvo'.");

            int mascara = int.Parse(bits.Groups[1].Value);

            Assert.AreNotEqual(0, mascara,
                "A hitbox do Byakhee está sem camada alvo — o golpe não pode acertar nada.");

            Assert.IsTrue((mascara & (1 << camada)) != 0,
                $"A hitbox do Byakhee não mira PlayerHurtbox (bit {camada}); máscara={mascara}.");
        }

        /// <summary>
        /// As quatro camadas de combate precisam continuar existindo com estes nomes — os
        /// componentes as resolvem por <c>LayerMask.NameToLayer</c>, que devolve -1 em silêncio
        /// se alguém as renomear.
        /// </summary>
        [Test]
        public void AsQuatroCamadasDeCombate_Existem()
        {
            var faltando = new List<string>();

            foreach (var nome in new[] { "PlayerHitbox", "EnemyHitbox", "PlayerHurtbox", "EnemyHurtbox" })
                if (CamadaPorNome(nome) < 0) faltando.Add(nome);

            Assert.IsEmpty(faltando,
                "Camadas de combate ausentes no TagManager: " + string.Join(", ", faltando));
        }

        /// <summary>
        /// <b>O guarda do contrato novo.</b> A hurtbox deixou de ser montada prefab a prefab
        /// (lista escrita à mão no Editor) e passou a se construir sozinha em runtime, via
        /// <c>Hurtbox.GarantirPara</c>, chamada do <c>Awake</c> de quem implementa
        /// <c>IDanificavel</c>.
        ///
        /// <para>Este teste existe porque a mudança troca <i>onde</i> a coisa pode quebrar: não
        /// há mais lista para envelhecer, mas passa a haver a possibilidade de alguém escrever
        /// um <c>IDanificavel</c> novo e esquecer a chamada. Aqui é onde isso aparece.</para>
        ///
        /// <para><c>EnemyBase</c> cobre a família de inimigos por herança — um inimigo novo que
        /// herde dele já vem servido. Os outros quatro implementam <c>IDanificavel</c> direto e
        /// precisam da chamada explícita.</para>
        /// </summary>
        [Test]
        public void TodoDanificavel_GaranteHurtboxNoAwake()
        {
            var esperados = new Dictionary<string, string>
            {
                ["Assets/Scripts/Enemies/EnemyBase.cs"] = "EnemyHurtbox",
                ["Assets/Scripts/Enemies/AbdulAlhazredAI.cs"] = "EnemyHurtbox",
                ["Assets/Scripts/Enemies/EsqueletoInvocado.cs"] = "EnemyHurtbox",
                ["Assets/Scripts/Enemies/PedraDePoder.cs"] = "EnemyHurtbox",
                ["Assets/Scripts/Combat/VitalidadeBridge.cs"] = "PlayerHurtbox",
            };

            var falhas = new List<string>();

            foreach (var par in esperados)
            {
                if (!File.Exists(par.Key)) { falhas.Add($"{par.Key}: ausente"); continue; }

                string codigo = File.ReadAllText(par.Key);

                if (!codigo.Contains("Hurtbox.GarantirPara"))
                {
                    falhas.Add($"{Path.GetFileName(par.Key)}: não chama Hurtbox.GarantirPara — " +
                               "este danificável nasce sem área atingível, e o golpe do jogador " +
                               "só o encontra pela pegada dos pés.");
                    continue;
                }

                if (!codigo.Contains($"\"{par.Value}\""))
                    falhas.Add($"{Path.GetFileName(par.Key)}: chama GarantirPara mas não com a " +
                               $"camada '{par.Value}'.");
            }

            Assert.IsEmpty(falhas,
                "Danificáveis sem garantia de hurtbox:" + NovaLinha + "  " +
                string.Join(NovaLinha + "  ", falhas));
        }

        // ── auxiliares ────────────────────────────────────────────────────────

        private static IEnumerable<string> Documentos(string yaml)
            => Regex.Split(yaml, @"(?m)^--- ").Where(d => d.Contains("!u!"));

        private static int CamadaPorNome(string nome)
        {
            var m = Regex.Match(File.ReadAllText(TagManager), @"layers:\r?\n((?:\s*-.*\r?\n)+)");
            if (!m.Success) return -1;

            var linhas = m.Groups[1].Value
                .Split('\n')
                .Select(l => l.Trim().TrimStart('-').Trim())
                .ToList();

            return linhas.FindIndex(l => l == nome);
        }
    }
}
