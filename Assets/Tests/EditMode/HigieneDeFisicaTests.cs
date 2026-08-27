using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using FavelaAmarela.Runtime.Combat;

namespace FavelaAmarela.Tests.EditMode
{
    /// <summary>
    /// Guarda a classe de defeito mais silenciosa deste projeto: <b>a máscara de camada
    /// vazia</b>.
    ///
    /// <para>Um <c>LayerMask</c> em zero não dá erro, não loga, e não aparece em teste nenhum —
    /// ele só faz a checagem que depende dele <b>nunca acontecer</b>. O sistema continua
    /// rodando e passa a mentir. Foram três casos, todos achados na varredura de 2026-08-27 e
    /// todos com o mesmo sintoma em jogo: <i>"atravessa parede"</i>.</para>
    /// </summary>
    public sealed class HigieneDeFisicaTests
    {
        private const string PastaDosInimigos = "Assets/FavelaAmarela/Art/Enemies";

        // ── Máscaras que precisam ter conteúdo ────────────────────────────────

        /// <summary>
        /// Cada linha é uma máscara autorada cuja ausência tem consequência de jogo, com o
        /// sintoma que ela produz quando fica em zero.
        ///
        /// <para>Isto é lista escrita à mão, e é de propósito: derivar "toda LayerMask precisa
        /// de conteúdo" seria falso — várias existem legitimamente vazias, com fallback no
        /// código. O que se guarda aqui são as que <b>não</b> têm fallback.</para>
        /// </summary>
        private static readonly (string Arquivo, string Campo, string Sintoma)[] MascarasVitais =
        {
            (PastaDosInimigos + "/ConeDeGelo.prefab", "camadasQueBloqueiam",
             "o projétil do Abdul atravessa parede e nunca se desfaz"),

            ("Assets/Scenes/Castelo_Carcosa.unity", "layerObstaculos",
             "o Cortesão Pálido enxerga através de parede — a linha de visão dele sempre passa"),
        };

        [Test]
        public void NenhumaMascaraVital_EstaVazia()
        {
            var vazias = new List<string>();

            foreach (var (arquivo, campo, sintoma) in MascarasVitais)
            {
                Assert.IsTrue(File.Exists(arquivo), $"Arquivo ausente: {arquivo}");

                string yaml = File.ReadAllText(arquivo);
                var achou = false;

                foreach (Match m in Regex.Matches(yaml,
                             Regex.Escape(campo) + @":\s*\r?\n\s*serializedVersion:\s*\d+\s*\r?\n\s*m_Bits:\s*(\d+)"))
                {
                    achou = true;
                    if (m.Groups[1].Value == "0")
                        vazias.Add($"{Path.GetFileName(arquivo)} · {campo} = 0 → {sintoma}");
                }

                if (!achou)
                    vazias.Add($"{Path.GetFileName(arquivo)}: campo '{campo}' não encontrado — " +
                               "renomeado? Este guarda parou de olhar para o jogo.");
            }

            Assert.IsEmpty(vazias,
                "Máscara(s) vital(is) em zero:" + Environment.NewLine + "  " +
                string.Join(Environment.NewLine + "  ", vazias) + Environment.NewLine +
                "Máscara vazia não dá erro e não loga: ela só faz a checagem nunca acontecer.");
        }

        // ── Corpo sólido vs. corpo de dano ────────────────────────────────────

        /// <summary>
        /// Um ator que se move precisa de <b>um colisor sólido</b> para o cenário barrá-lo. Se
        /// todos os colisores dele forem trigger, ele atravessa parede e tilemap.
        ///
        /// <para>Era o caso do <b>Byakhee</b>: os dois colisores dele eram trigger — inclusive a
        /// cápsula de corpo 2×3 que eu mesmo acrescentei em 2026-08-21 para torná-lo
        /// atingível. Resolveu "o chefe é impossível de acertar" e criou "o chefe voa através
        /// das paredes da arena".</para>
        /// </summary>
        [Test]
        public void AtorQueSeMove_TemUmColisorSolido()
        {
            var atravessaParede = new List<string>();

            foreach (var caminho in Directory.GetFiles(PastaDosInimigos, "*.prefab"))
            {
                var go = AssetDatabase.LoadAssetAtPath<GameObject>(caminho);
                if (go == null) continue;

                // Só quem tem corpo físico se move por física; o resto é cenário ou é movido
                // por transform, e não é este guarda que cobre isso.
                if (go.GetComponentInChildren<Rigidbody2D>(true) == null) continue;

                var colisores = go.GetComponentsInChildren<Collider2D>(true);
                if (colisores.Length == 0) continue;   // coberto por ColisoresDoElencoTests

                // Quem age POR CONTATO é todo-trigger de propósito, e a exceção é DERIVADA do
                // código, não escrita à mão.
                //
                // A Coisa do Cemitério mata ao encostar; o Cone de Gelo precisa atravessar o
                // gatilho da parede para se desfazer. Os dois resolvem por OnTriggerEnter2D --
                // um corpo sólido faria a Coisa empurrar o jogador em vez de alcançá-lo, e o
                // Cone quicar na parede em vez de sumir.
                //
                // A primeira versão deste guarda excetuava "CoisaDoCemiterio" PELO NOME. O Cone
                // reprovou na primeira execução, o que é a lista escrita à mão anunciando que
                // ia envelhecer -- na estreia.
                if (AgePorContato(go)) continue;

                if (colisores.All(c => c.isTrigger))
                    atravessaParede.Add($"{go.name}: todos os {colisores.Length} colisores são " +
                                        "trigger");
            }

            Assert.IsEmpty(atravessaParede,
                "Ator(es) com corpo físico e nenhum colisor sólido:" + Environment.NewLine + "  " +
                string.Join(Environment.NewLine + "  ", atravessaParede) + Environment.NewLine +
                "Eles atravessam parede, tilemap e os limites de arena.");
        }

        /// <summary>
        /// Se algum script deste ator implementa <c>OnTriggerEnter2D</c> — ou seja, se o
        /// <b>contato é o mecanismo dele</b>, e não um acidente de configuração.
        /// </summary>
        private static bool AgePorContato(GameObject go)
        {
            foreach (var mb in go.GetComponentsInChildren<MonoBehaviour>(true))
            {
                if (mb == null) continue;

                var script = MonoScript.FromMonoBehaviour(mb);
                if (script == null) continue;

                string caminho = AssetDatabase.GetAssetPath(script);
                if (string.IsNullOrEmpty(caminho) || !File.Exists(caminho)) continue;

                if (File.ReadAllText(caminho).Contains("OnTriggerEnter2D")) return true;
            }

            return false;
        }

        // ── Imobilidade: decisão, não acidente ────────────────────────────────

        /// <summary>
        /// Todo ator do elenco declara <b>quanto ainda obedece à física</b>.
        ///
        /// <para>Três deles — Abdul, Rei em Amarelo e Pedra de Poder — não têm
        /// <c>Rigidbody2D</c>, então <c>RepulsaoDeImpacto.GarantirPara</c> devolve <c>null</c>
        /// <b>sem log</b> e eles nunca cedem a um golpe. O comportamento está certo; o que
        /// faltava era estar <b>escrito</b>.</para>
        ///
        /// <para>Olhando o prefab, <i>"não cede porque falta um componente"</i> e <i>"não cede
        /// porque decidimos assim"</i> eram indistinguíveis. Com <c>CorpoImpregnado</c> em 1,00
        /// a imobilidade vira decisão legível — e o dia em que alguém der um corpo a eles, este
        /// guarda obriga a revisar o número em vez de deixar acontecer por acaso.</para>
        /// </summary>
        [Test]
        public void TodoAtorSemCorpo_DeclaraSuaImobilidade()
        {
            var mudos = new List<string>();

            foreach (var caminho in Directory.GetFiles(PastaDosInimigos, "*.prefab"))
            {
                var go = AssetDatabase.LoadAssetAtPath<GameObject>(caminho);
                if (go == null) continue;
                if (go.GetComponentInChildren<Collider2D>(true) == null) continue;

                bool temCorpo = go.GetComponentInChildren<Rigidbody2D>(true) != null;
                if (temCorpo) continue;

                var impregnado = go.GetComponent<CorpoImpregnado>();

                if (impregnado == null)
                    mudos.Add($"{go.name}: sem Rigidbody2D e sem CorpoImpregnado — inamovível " +
                              "por acidente");
                else if (impregnado.ResistenciaAImpulso < 0.999f)
                    mudos.Add($"{go.name}: sem Rigidbody2D mas declara resistência " +
                              $"{impregnado.ResistenciaAImpulso:0.00} — o dado promete que ele " +
                              "cede, e ele não pode ceder");
            }

            Assert.IsEmpty(mudos,
                "Ator(es) cuja imobilidade não está declarada:" + Environment.NewLine + "  " +
                string.Join(Environment.NewLine + "  ", mudos) + Environment.NewLine +
                "Conserto: 'Tools/FavelaAmarela/Física: marcar corpos impregnados'.");
        }
    }
}
