using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using NUnit.Framework;
using UnityEngine;

namespace FavelaAmarela.Tests.EditMode
{
    /// <summary>
    /// Guarda a parede: onde ela está, em que camada, e quantas formas de física ela custa.
    ///
    /// <para><b>Os dois defeitos que este arquivo existe para não deixar voltar (2026-08-27).</b></para>
    ///
    /// <para><b>1. A parede fora da camada.</b> Quatro ferramentas do Editor constroem o mesmo
    /// tilemap "Colisao"; três punham em <c>Obstacle</c> e a da Arena <b>nunca setou camada
    /// nenhuma</b> — e os Portões herdaram o trecho copiado. As paredes das duas cenas ficaram na
    /// <c>Default</c>. Elas ainda barram o Damião (a matriz deixa <c>Default × Player</c>
    /// colidir), então <i>parece</i> certo em jogo. O que quebra é toda consulta que pergunta por
    /// <c>Obstacle</c>: o Cone de Gelo do Abdul atravessaria, e a linha de visão do Cortesão
    /// passaria através da parede. É a mesma família da máscara vazia — a peça existe, não dá
    /// erro, e a checagem simplesmente não acontece.</para>
    ///
    /// <para><b>2. O tilemap vazio que promete colisão.</b> O 'Batente' dos Portões é um sprite
    /// decorativo que tinha um <c>Tilemap</c> sem uma única célula e um
    /// <c>TilemapCollider2D</c> trigger em cima. Zero célula = zero forma: o gatilho nunca podia
    /// disparar. No Inspector era indistinguível de um gatilho de porta funcionando.</para>
    ///
    /// <para>A leitura é do <b>YAML</b>, não de cena aberta: abrir seis cenas num teste EditMode
    /// custa mais que o resto da suíte inteira, e o que precisa ser afirmado aqui está todo no
    /// disco.</para>
    /// </summary>
    public sealed class ColisaoDeCenarioTests
    {
        private const string PastaDeCenas = "Assets/Scenes";
        private const string CamadaDeParede = "Obstacle";

        /// <summary><c>RigidbodyType2D.Static</c> na serialização.</summary>
        private const int BodyTypeStatic = 2;

        /// <summary><c>Collider2D.CompositeOperation.Merge</c> na serialização.</summary>
        private const int CompositeMerge = 1;

        // ── Camada ────────────────────────────────────────────────────────────

        [Test]
        public void TodaParedeDeTilemap_EstaNaCamadaObstacle()
        {
            int esperada = LayerMask.NameToLayer(CamadaDeParede);
            Assert.AreNotEqual(-1, esperada,
                $"A camada '{CamadaDeParede}' sumiu do TagManager. Sem ela, nenhuma consulta de " +
                "linha de visão ou de projétil tem o que perguntar.");

            var fora = new List<string>();

            foreach (var (cena, parede) in Paredes())
            {
                if (parede.EhTrigger) continue;   // gatilho de área não é parede

                if (parede.Camada != esperada)
                    fora.Add($"{cena} · '{parede.Nome}' na camada {parede.Camada} " +
                             $"({LayerMask.LayerToName(parede.Camada)}) — devia ser {esperada} " +
                             $"({CamadaDeParede})");
            }

            Assert.IsEmpty(fora,
                "Parede(s) de tilemap fora da camada de obstáculo:" + Environment.NewLine + "  " +
                string.Join(Environment.NewLine + "  ", fora) + Environment.NewLine +
                "Elas continuam barrando o jogador, então o erro NÃO aparece andando pelo mapa. " +
                "O que some é o projétil que devia se desfazer na parede e a linha de visão que " +
                "devia ser cortada por ela. Conserto: 'Tools/FavelaAmarela/Física: consolidar a " +
                "colisão dos tilemaps'.");
        }

        // ── Composite ─────────────────────────────────────────────────────────

        /// <summary>
        /// Cada célula pintada vira uma forma de física própria. As cinco cenas somavam 2 622
        /// delas; o <c>CompositeCollider2D</c> funde tudo no contorno, que é o que a doc da 6.4
        /// prescreve para tilemap.
        /// </summary>
        [Test]
        public void TodaParedeDeTilemap_MesclaAsCelulasNumComposite()
        {
            var soltas = new List<string>();

            foreach (var (cena, parede) in Paredes())
            {
                if (parede.OperacaoComposta != CompositeMerge)
                    soltas.Add($"{cena} · '{parede.Nome}': compositeOperation = " +
                               $"{parede.OperacaoComposta} (Merge = {CompositeMerge})");

                else if (!parede.TemComposite)
                    soltas.Add($"{cena} · '{parede.Nome}': pede Merge e não há " +
                               "CompositeCollider2D no mesmo objeto — o Merge não mescla em nada");
            }

            Assert.IsEmpty(soltas,
                "Parede(s) de tilemap sem mesclagem:" + Environment.NewLine + "  " +
                string.Join(Environment.NewLine + "  ", soltas) + Environment.NewLine +
                "Conserto: 'Tools/FavelaAmarela/Física: consolidar a colisão dos tilemaps'.");
        }

        /// <summary>
        /// <b>O passo que fez a tentativa anterior ser abandonada.</b>
        /// <c>CompositeCollider2D</c> tem <c>[RequireComponent(typeof(Rigidbody2D))]</c>, e o
        /// corpo que a Unity cria junto nasce <b>Dynamic</b>. Parede Dynamic com
        /// <c>gravityScale 0</c> não cai — mas é <b>empurrada</b>, e o primeiro esbarrão do
        /// Damião leva o mapa embora.
        /// </summary>
        [Test]
        public void OCorpoDaParede_EhEstatico()
        {
            var empurraveis = new List<string>();

            foreach (var (cena, parede) in Paredes())
            {
                if (!parede.TemComposite) continue;   // coberto pelo teste acima

                if (!parede.TemCorpo)
                    empurraveis.Add($"{cena} · '{parede.Nome}': CompositeCollider2D sem " +
                                    "Rigidbody2D — a Unity vai criar um Dynamic ao carregar");

                else if (parede.TipoDeCorpo != BodyTypeStatic)
                    empurraveis.Add($"{cena} · '{parede.Nome}': bodyType {parede.TipoDeCorpo} " +
                                    $"(Static = {BodyTypeStatic}) — a parede é EMPURRÁVEL");
            }

            Assert.IsEmpty(empurraveis,
                "Parede(s) com corpo não-estático:" + Environment.NewLine + "  " +
                string.Join(Environment.NewLine + "  ", empurraveis) + Environment.NewLine +
                "Uma parede Dynamic sai do lugar no primeiro encostão e não volta.");
        }

        // ── Colisor que não colide ────────────────────────────────────────────

        [Test]
        public void NenhumTilemapCollider_EstaSobreUmTilemapVazio()
        {
            var mentirosos = new List<string>();

            foreach (var (cena, parede) in Paredes())
                if (parede.TilemapVazio)
                    mentirosos.Add($"{cena} · '{parede.Nome}'" +
                                   (parede.EhTrigger ? " (trigger)" : ""));

            Assert.IsEmpty(mentirosos,
                "TilemapCollider2D sobre tilemap sem uma única célula:" + Environment.NewLine +
                "  " + string.Join(Environment.NewLine + "  ", mentirosos) + Environment.NewLine +
                "Zero célula = zero forma de colisão. O componente aparece no Inspector, promete " +
                "colisão ou gatilho, e não pode disparar nunca.");
        }

        // ── Leitura do YAML ───────────────────────────────────────────────────

        /// <summary>
        /// Campos públicos e não <c>init</c> de propósito: <c>init</c> exige
        /// <c>IsExternalInit</c>, que o runtime da Unity não traz, e o teste não compilaria.
        /// </summary>
        private sealed class Parede
        {
            public string Nome;
            public int Camada;
            public bool EhTrigger;
            public int OperacaoComposta;
            public bool TemComposite;
            public bool TemCorpo;
            public int TipoDeCorpo;
            public bool TilemapVazio;
        }

        private static IEnumerable<(string Cena, Parede Parede)> Paredes()
        {
            foreach (var caminho in Directory.GetFiles(PastaDeCenas, "*.unity",
                                                       SearchOption.AllDirectories).OrderBy(c => c))
            {
                string cena = Path.GetFileName(caminho);
                var docs = Documentos(File.ReadAllText(caminho));

                foreach (var col in docs.Where(d => d.Tipo == "TilemapCollider2D"))
                {
                    string dono = Referencia(col.Corpo, "m_GameObject");
                    if (dono == null) continue;

                    var go = docs.FirstOrDefault(d => d.Tipo == "GameObject" && d.Id == dono);
                    var comp = docs.FirstOrDefault(d => d.Tipo == "CompositeCollider2D" &&
                                                        Referencia(d.Corpo, "m_GameObject") == dono);
                    var corpo = docs.FirstOrDefault(d => d.Tipo == "Rigidbody2D" &&
                                                        Referencia(d.Corpo, "m_GameObject") == dono);
                    var mapa = docs.FirstOrDefault(d => d.Tipo == "Tilemap" &&
                                                        Referencia(d.Corpo, "m_GameObject") == dono);

                    yield return (cena, new Parede
                    {
                        Nome = go == null ? "?" : Campo(go.Corpo, "m_Name", "?"),
                        Camada = go == null ? -1 : Numero(go.Corpo, "m_Layer", -1),
                        EhTrigger = Numero(col.Corpo, "m_IsTrigger", 0) != 0,
                        OperacaoComposta = Numero(col.Corpo, "m_CompositeOperation", 0),
                        TemComposite = comp != null,
                        TemCorpo = corpo != null,
                        TipoDeCorpo = corpo == null ? -1 : Numero(corpo.Corpo, "m_BodyType", -1),

                        // Tilemap sem célula serializa 'm_Tiles: {}'. Ausência do próprio
                        // Tilemap conta como vazio: colisor de tilemap sem tilemap não gera nada.
                        TilemapVazio = mapa == null ||
                                       Regex.IsMatch(mapa.Corpo, @"^\s*m_Tiles:\s*\{\}\s*$",
                                                     RegexOptions.Multiline),
                    });
                }
            }
        }

        private sealed class Documento
        {
            public string Tipo;
            public string Id;
            public string Corpo;
        }

        /// <summary>
        /// Quebra o YAML da cena nos seus documentos. A chave é o <b>nome do tipo</b> (a linha
        /// logo após o marcador), não o número de classe: número de classe é coisa que se
        /// decora errado, e <c>TilemapCollider2D</c> é <c>!u!19719996</c>.
        /// </summary>
        private static List<Documento> Documentos(string yaml)
        {
            var docs = new List<Documento>();
            var marcadores = Regex.Matches(yaml, @"^--- !u!\d+ &(-?\d+).*$", RegexOptions.Multiline);

            for (int i = 0; i < marcadores.Count; i++)
            {
                int inicio = marcadores[i].Index + marcadores[i].Length;
                int fim = i + 1 < marcadores.Count ? marcadores[i + 1].Index : yaml.Length;

                string corpo = yaml.Substring(inicio, fim - inicio);
                var tipo = Regex.Match(corpo, @"^(\w+):\s*$", RegexOptions.Multiline);

                docs.Add(new Documento
                {
                    Tipo = tipo.Success ? tipo.Groups[1].Value : "?",
                    Id = marcadores[i].Groups[1].Value,
                    Corpo = corpo,
                });
            }

            return docs;
        }

        private static string Referencia(string corpo, string campo)
        {
            var m = Regex.Match(corpo, Regex.Escape(campo) + @":\s*\{fileID:\s*(-?\d+)\}");
            return m.Success ? m.Groups[1].Value : null;
        }

        private static string Campo(string corpo, string campo, string padrao)
        {
            var m = Regex.Match(corpo, @"^\s*" + Regex.Escape(campo) + @":\s*(.*)$",
                                RegexOptions.Multiline);
            return m.Success ? m.Groups[1].Value.Trim() : padrao;
        }

        private static int Numero(string corpo, string campo, int padrao)
        {
            var m = Regex.Match(corpo, @"^\s*" + Regex.Escape(campo) + @":\s*(-?\d+)\s*$",
                                RegexOptions.Multiline);
            return m.Success && int.TryParse(m.Groups[1].Value, out int v) ? v : padrao;
        }
    }
}
