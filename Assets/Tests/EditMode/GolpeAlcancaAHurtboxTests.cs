using System;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using FavelaAmarela.Core.Combat;

namespace FavelaAmarela.Tests.EditMode
{
    /// <summary>
    /// Guarda a única coisa que faz o golpe do jogador acertar: a <b>camada da hurtbox</b> estar
    /// na máscara consultada.
    ///
    /// <para><b>O defeito que este arquivo existe para nunca mais deixar passar (2026-08-27).</b>
    /// A cena da Tumba tinha um override antigo de <c>camadaInimigos</c> para <c>64</c> — só a
    /// camada <c>Enemy</c>, sem <c>EnemyHurtbox</c>. Isso era <b>inofensivo</b> enquanto o golpe
    /// resolvia por <c>GetComponentInParent&lt;IDanificavel&gt;()</c>, que encontra o
    /// <c>EnemyBase</c> na raiz do inimigo.</para>
    ///
    /// <para>A migração do golpe para <c>Hitbox</c> transformou esse override velho em
    /// <b>"nada é atingível na Tumba"</b>: a <c>Hitbox</c> procura uma <c>Hurtbox</c>, e a
    /// hurtbox é um GameObject <b>FILHO</b> na camada <c>EnemyHurtbox</c> —
    /// <c>GetComponentInParent</c> sobe, nunca desce. A consulta achava o colisor de movimento e
    /// devolvia nada.</para>
    ///
    /// <para><b>E o sintoma que o Vini viu não foi esse.</b> Foi "o Abdul não invoca mais as
    /// Pedras de Poder" — porque sem levar dano ele nunca troca de fase, e as Pedras nascem por
    /// fase. Um bug de máscara de camada apareceu como um bug de IA de chefe. É por isso que
    /// este guarda olha a <b>causa</b>, não o sintoma.</para>
    /// </summary>
    public sealed class GolpeAlcancaAHurtboxTests
    {
        /// <summary>Índice da camada onde as hurtboxes de inimigo vivem.</summary>
        private static int CamadaDaHurtboxDeInimigo => LayerMask.NameToLayer("EnemyHurtbox");

        [Test]
        public void ACamadaEnemyHurtbox_Existe()
        {
            Assert.AreNotEqual(-1, CamadaDaHurtboxDeInimigo,
                "A camada 'EnemyHurtbox' sumiu do projeto. Sem ela, Hurtbox.GarantirPara não " +
                "tem onde pôr a hurtbox e nenhum inimigo fica atingível.");
        }

        /// <summary>
        /// Toda máscara autorada — no prefab e em <b>override de cena</b> — precisa conter a
        /// camada da hurtbox. Foi um override de cena que passou, então olhar só o prefab não
        /// teria pego.
        /// </summary>
        [Test]
        public void NenhumaMascaraDeGolpe_ExcluiACamadaDaHurtbox()
        {
            int bitDaHurtbox = 1 << CamadaDaHurtboxDeInimigo;
            var faltando = new List<string>();

            foreach (var caminho in Arquivos())
            {
                string txt = File.ReadAllText(caminho);

                foreach (int bits in MascarasEm(txt))
                {
                    // 0 é o "não configurado", e o Awake da bridge cai num fallback seguro.
                    if (bits == 0) continue;

                    if ((bits & bitDaHurtbox) == 0)
                        faltando.Add($"{Path.GetFileName(caminho)}: m_Bits {bits} " +
                                     $"= camadas [{string.Join(", ", CamadasDe(bits))}] — " +
                                     $"sem EnemyHurtbox ({CamadaDaHurtboxDeInimigo})");
                }
            }

            Assert.IsEmpty(faltando,
                "Máscara(s) de golpe sem a camada da hurtbox:" + Environment.NewLine + "  " +
                string.Join(Environment.NewLine + "  ", faltando) + Environment.NewLine +
                "A Hitbox só acerta quem tem Hurtbox, e a hurtbox é um objeto FILHO nessa " +
                "camada. Sem ela na máscara, a consulta acha o colisor de movimento, não acha " +
                "hurtbox, e o golpe passa branco SEM ERRO NENHUM.");
        }

        /// <summary>
        /// A máscara também não pode incluir a camada do <b>colisor de movimento</b>.
        ///
        /// <para>A Hitbox pergunta "o que pode ser ferido?" e a resposta é hurtbox. Incluir
        /// <c>Enemy</c> responde a outra pergunta — "o que É um inimigo?" — e traz o colisor da
        /// raiz, que não carrega <c>Hurtbox</c>. Em 2026-08-27 isso fez <b>todo golpe que
        /// conectava</b> emitir um <c>LogError</c>: o diagnóstico, escrito para um caso raro,
        /// virou ruído constante e teria escondido o caso real.</para>
        /// </summary>
        [Test]
        public void AMascaraDoGolpe_NaoIncluiOColisorDeMovimento()
        {
            int camadaEnemy = LayerMask.NameToLayer("Enemy");
            Assert.AreNotEqual(-1, camadaEnemy, "A camada 'Enemy' sumiu do projeto.");

            int bitEnemy = 1 << camadaEnemy;
            var comExcesso = new List<string>();

            foreach (var caminho in Arquivos())
            {
                string txt = File.ReadAllText(caminho);

                foreach (int bits in MascarasEm(txt))
                {
                    if (bits == 0) continue;

                    if ((bits & bitEnemy) != 0)
                        comExcesso.Add($"{Path.GetFileName(caminho)}: m_Bits {bits} inclui " +
                                       $"Enemy ({camadaEnemy})");
                }
            }

            Assert.IsEmpty(comExcesso,
                "Máscara(s) de golpe incluindo a camada do colisor de movimento:" +
                Environment.NewLine + "  " +
                string.Join(Environment.NewLine + "  ", comExcesso) + Environment.NewLine +
                "A query passa a encontrar o colisor da raiz, que não tem Hurtbox — e todo " +
                "acerto volta a emitir LogError.");
        }

        /// <summary>
        /// Cinto e suspensório de propósito: mesmo que uma máscara errada volte a ser autorada,
        /// o código força a camada da hurtbox. O valor do Inspector não pode ser capaz de
        /// quebrar o combate.
        /// </summary>
        [Test]
        public void OCodigo_ForcaACamadaDaHurtboxNaMascara()
        {
            string bridge = File.ReadAllText("Assets/Scripts/Player/MaoFisicaBridge.cs");

            // Casa a CONSTANTE e o uso dela, não um literal solto. A primeira versão deste
            // guarda exigia a string "EnemyHurtbox" escrita na chamada — e falhou no momento
            // em que o código MELHOROU, extraindo a camada para uma constante nomeada. Guarda
            // que reprova refatoração correta é guarda que ensina a não refatorar.
            StringAssert.Contains("CamadaDeHurtboxAlvo = \"EnemyHurtbox\"", bridge,
                "A constante da camada de hurtbox mudou de nome ou de valor. Ela é o que o " +
                "golpe procura, e a única coisa que ele deve procurar.");

            StringAssert.Contains("LayerMask.GetMask(CamadaDeHurtboxAlvo)", bridge,
                "A MaoFisicaBridge parou de forçar a camada da hurtbox na máscara. Um override " +
                "esquecido numa cena volta a poder tornar um mapa inteiro intocável — foi " +
                "exatamente assim que a Tumba ficou sem nada atingível.");
        }

        /// <summary>
        /// O golpe que acha colisor e não acha hurtbox tem de <b>gritar</b>. Foi o silêncio
        /// dessa situação que fez a Tumba ficar intocável por uma noite inteira sem nada no
        /// console.
        /// </summary>
        [Test]
        public void AHitbox_DenunciaAlvoSemHurtbox()
        {
            string hitbox = File.ReadAllText("Assets/Scripts/Combat/Hitbox.cs");

            StringAssert.Contains("não achou Hurtbox", hitbox,
                "A Hitbox voltou a engolir em silêncio o caso 'achei colisor na camada alvo e " +
                "não achei hurtbox'. Esse silêncio é indistinguível de 'errei a mira'.");
        }

        /// <summary>
        /// <b>O outro lado da máscara estreita.</b> Reduzir a consulta do golpe à camada de
        /// hurtbox só é seguro enquanto <b>todo</b> alvo danificável tiver uma. Quem implementa
        /// <c>IDanificavel</c> e não chama <c>Hurtbox.GarantirPara</c> fica <b>intocável</b> —
        /// sem erro, sem log, sem nada: o golpe simplesmente atravessa.
        ///
        /// <para>Hoje passam todos, e três deles só passam porque a chamada foi acrescentada à
        /// mão em cada um (<c>EnemyBase</c>, <c>AbdulAlhazredAI</c>, <c>EsqueletoInvocado</c>,
        /// <c>PedraDePoder</c>, <c>VitalidadeBridge</c>). É exatamente o tipo de coisa que a
        /// próxima classe esquece.</para>
        ///
        /// <para>Nota do levantamento de 2026-08-27: <c>CoisaDoCemiterio</c>,
        /// <c>EspectroHali</c> e <c>ReiEmAmarelo</c> <b>não</b> aparecem aqui porque não
        /// implementam <c>IDanificavel</c> — nunca foram feríveis pelo golpe do jogador. Não é
        /// regressão da máscara; é desenho a confirmar com o Vini.</para>
        /// </summary>
        [Test]
        public void TodoDanificavel_GaranteASuaHurtbox()
        {
            var semGarantia = new List<string>();
            var vistos = 0;

            foreach (var tipo in TypeCache.GetTypesDerivedFrom<IDanificavel>())
            {
                if (tipo.IsAbstract || tipo.IsInterface) continue;
                if (!typeof(MonoBehaviour).IsAssignableFrom(tipo)) continue;   // POCO não tem corpo

                vistos++;
                if (GarantePelaHierarquia(tipo)) continue;

                semGarantia.Add($"{tipo.Name}: implementa IDanificavel e nunca chama " +
                                "Hurtbox.GarantirPara");
            }

            Assert.Greater(vistos, 0,
                "Nenhum MonoBehaviour IDanificavel foi encontrado — este guarda parou de olhar " +
                "para o jogo.");

            Assert.IsEmpty(semGarantia,
                "Alvo(s) danificável(is) sem hurtbox garantida:" + Environment.NewLine + "  " +
                string.Join(Environment.NewLine + "  ", semGarantia) + Environment.NewLine +
                "A Hitbox do jogador só consulta a camada EnemyHurtbox. Sem hurtbox eles são " +
                "INTOCÁVEIS, e o sintoma em jogo é 'meu golpe não acerta esse inimigo'.");
        }

        /// <summary>
        /// Se o tipo — ou alguma base dele — chama <c>Hurtbox.GarantirPara</c> no próprio
        /// código-fonte. Subir a hierarquia é o que deixa os herdeiros de <c>EnemyBase</c>
        /// passarem sem repetir a chamada.
        /// </summary>
        private static bool GarantePelaHierarquia(Type tipo)
        {
            for (var t = tipo; t != null && t != typeof(MonoBehaviour); t = t.BaseType)
            {
                foreach (var guid in AssetDatabase.FindAssets($"{t.Name} t:MonoScript"))
                {
                    string caminho = AssetDatabase.GUIDToAssetPath(guid);
                    if (Path.GetFileNameWithoutExtension(caminho) != t.Name) continue;
                    if (!File.Exists(caminho)) continue;

                    if (File.ReadAllText(caminho).Contains("Hurtbox.GarantirPara")) return true;
                }
            }

            return false;
        }

        // ── Apoio ─────────────────────────────────────────────────────────────

        private static IEnumerable<string> Arquivos()
        {
            foreach (var c in Directory.GetFiles("Assets/Scenes", "*.unity", SearchOption.AllDirectories))
                yield return c;

            foreach (var c in Directory.GetFiles("Assets", "*.prefab", SearchOption.AllDirectories))
            {
                // Só os que realmente carregam a máscara do golpe do jogador.
                if (File.ReadAllText(c).Contains("camadaInimigos")) yield return c;
            }
        }

        /// <summary>
        /// Acha os valores de <c>camadaInimigos</c> tanto no formato de componente
        /// (<c>camadaInimigos:</c> + <c>m_Bits:</c> nas linhas seguintes) quanto no de override
        /// de instância de prefab (<c>propertyPath: camadaInimigos.m_Bits</c> + <c>value:</c>).
        /// Os dois formatos importam: foi o SEGUNDO que passou.
        /// </summary>
        private static IEnumerable<int> MascarasEm(string yaml)
        {
            foreach (Match m in Regex.Matches(yaml,
                         @"camadaInimigos:\s*\r?\n\s*serializedVersion:\s*\d+\s*\r?\n\s*m_Bits:\s*(\d+)"))
            {
                if (int.TryParse(m.Groups[1].Value, out int bits)) yield return bits;
            }

            foreach (Match m in Regex.Matches(yaml,
                         @"propertyPath:\s*camadaInimigos\.m_Bits\s*\r?\n\s*value:\s*(\d+)"))
            {
                if (int.TryParse(m.Groups[1].Value, out int bits)) yield return bits;
            }
        }

        private static IEnumerable<int> CamadasDe(int bits)
        {
            for (int i = 0; i < 32; i++)
                if ((bits & (1 << i)) != 0) yield return i;
        }
    }
}
