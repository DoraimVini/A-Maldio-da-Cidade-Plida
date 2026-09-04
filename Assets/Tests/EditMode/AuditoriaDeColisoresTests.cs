using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using FavelaAmarela.Runtime.Diagnostico;

namespace FavelaAmarela.Tests.EditMode
{
    /// <summary>
    /// Guarda a <b>classificação</b> de <see cref="AuditoriaDeColisores"/> — a parte que já
    /// errou.
    ///
    /// <para><b>Por que este arquivo existe.</b> A primeira versão da auditoria chamava de
    /// "pegada" <b>todo colisor sólido</b> e comparava tudo com a pegada humana de 0,60 × 0,30.
    /// Rodou, saiu com código 0, escreveu o relatório — e acusou <b>57 de 141</b> colisores,
    /// entre eles as quatro paredes do Santuário, o Lago de Hali e os quatro limites do
    /// Deserto. Uma ferramenta de auditoria que acusa 40% do projeto não mede nada: ela só
    /// troca um limiar cego por outro.</para>
    ///
    /// <para>Nada na suíte pegou isso. Só a leitura do próprio relatório pegou. Estes testes
    /// são o que faltava para que a próxima versão não possa reintroduzi-lo em silêncio.</para>
    /// </summary>
    public sealed class AuditoriaDeColisoresTests
    {
        private readonly List<GameObject> _lixo = new List<GameObject>();

        [TearDown]
        public void TearDown()
        {
            foreach (var go in _lixo)
                if (go != null) Object.DestroyImmediate(go);

            _lixo.Clear();
        }

        private GameObject Novo(string nome)
        {
            var go = new GameObject(nome);
            _lixo.Add(go);
            return go;
        }

        /// <summary>
        /// O caso exato do falso positivo: um bloco sólido sem corpo e sem sprite é
        /// <b>cenário</b>, não a pegada de um ator.
        /// </summary>
        [Test]
        public void ParedeSemCorpoESemSprite_EhCenario_ENaoEhAcusada()
        {
            var parede = Novo("Parede_Norte");
            var col = parede.AddComponent<BoxCollider2D>();
            col.size = new Vector2(16f, 0.5f);   // uma das paredes reais do Santuário

            var m = AuditoriaDeColisores.Medir(col);

            Assert.AreEqual(AuditoriaDeColisores.Papel.Cenario, m.Funcao,
                "Uma parede foi classificada como pegada de ator. Foi assim que as quatro " +
                "paredes do Santuário, o Lago de Hali e os limites do Deserto entraram no " +
                "relatório acusados de ter o tamanho errado — comparados com a pegada humana " +
                "de 0,60 × 0,30, que nada tem a ver com geometria de cenário.");

            Assert.IsEmpty(m.Queixa,
                $"Cenário não tem tamanho esperado, mas a auditoria reclamou: '{m.Queixa}'.");
        }

        /// <summary>
        /// Um tilemap de colisão tem corpo <b>Static</b>. Corpo estático é cenário parado, não
        /// ator — os quatro `*FloorGrid/Colisao` do projeto são exatamente isto.
        /// </summary>
        [Test]
        public void CorpoEstaticoComSprite_EhCenario()
        {
            var chao = Novo("Colisao");
            chao.AddComponent<SpriteRenderer>();
            chao.AddComponent<Rigidbody2D>().bodyType = RigidbodyType2D.Static;
            var col = chao.AddComponent<BoxCollider2D>();
            col.size = new Vector2(68f, 34f);

            var m = AuditoriaDeColisores.Medir(col);

            Assert.AreEqual(AuditoriaDeColisores.Papel.Cenario, m.Funcao,
                "Um corpo Static com sprite foi tratado como ator. Os tilemaps de colisão do " +
                "projeto são Static de propósito (ver a auditoria de Rigidbody2D) e não se " +
                "movem — cobrá-los pela forma de uma pegada é ruído.");
        }

        /// <summary>
        /// Um ator de verdade — corpo dinâmico mais sprite — é pegada, e uma pegada deitada na
        /// razão 2:1 do chão isométrico passa sem queixa.
        /// </summary>
        [Test]
        public void PegadaDeitadaNaRazaoDoChao_PassaSemQueixa()
        {
            var ator = Novo("Ator");
            ator.AddComponent<SpriteRenderer>();          // sem sprite: só a razão é conferida
            ator.AddComponent<Rigidbody2D>().bodyType = RigidbodyType2D.Dynamic;
            var col = ator.AddComponent<BoxCollider2D>();
            col.size = new Vector2(0.60f, 0.30f);          // a pegada calibrada do elenco

            var m = AuditoriaDeColisores.Medir(col);

            Assert.AreEqual(AuditoriaDeColisores.Papel.Pegada, m.Funcao,
                "Corpo dinâmico com sprite tem de ser classificado como pegada de ator.");

            Assert.IsEmpty(m.Queixa,
                $"A pegada calibrada do elenco (0,60 × 0,30) foi acusada: '{m.Queixa}'. Ela é " +
                "exatamente a razão 2:1 da célula isométrica — se ela não passa, o limiar está " +
                "errado.");
        }

        /// <summary>
        /// O achado que a razão existe para pegar: a instância do Abdul na
        /// <c>Tumba_De_Alhazred</c> media <b>0,3 × 2,54</b> — uma parede em pé no lugar de
        /// uma área de chão, enquanto o prefab dele media 0,60 × 0,30.
        /// </summary>
        [Test]
        public void PegadaEmPe_EhAcusadaPelaProporcao()
        {
            var ator = Novo("Abdul_Alhazred");
            ator.AddComponent<SpriteRenderer>();
            ator.AddComponent<Rigidbody2D>().bodyType = RigidbodyType2D.Dynamic;
            var col = ator.AddComponent<BoxCollider2D>();
            col.size = new Vector2(0.30f, 2.54f);          // o valor medido na cena

            var m = AuditoriaDeColisores.Medir(col);

            Assert.IsNotEmpty(m.Queixa,
                "Uma 'pegada' de 0,30 × 2,54 é mais alta que larga — é uma parede em pé, não " +
                "uma área de chão. A auditoria tem de acusar isso sem precisar saber a " +
                "espécie do ator, porque o tamanho absoluto varia legitimamente entre " +
                "criaturas e a proporção do chão não.");

            StringAssert.Contains("proporção", m.Queixa,
                $"A queixa saiu, mas não foi a da proporção: '{m.Queixa}'.");
        }

        /// <summary>
        /// O tamanho é lido em <b>mundo</b>, não em local. Um relatório em unidades locais
        /// mentiria em todo ator escalado — e o elenco inteiro é escalado.
        /// </summary>
        [Test]
        public void OTamanhoEhEmMundo_NaoEmLocal()
        {
            var ator = Novo("Escalado");
            ator.transform.localScale = new Vector3(2f, 2f, 1f);
            var col = ator.AddComponent<BoxCollider2D>();
            col.size = new Vector2(0.60f, 0.30f);

            var m = AuditoriaDeColisores.Medir(col);

            Assert.AreEqual(1.20f, m.Tamanho.x, 0.001f,
                $"Largura em mundo deveria ser 0,60 × 2 = 1,20, e veio {m.Tamanho.x:0.###}. " +
                "Ler o campo cru do colisor ignora a escala do transform.");
            Assert.AreEqual(0.60f, m.Tamanho.y, 0.001f,
                $"Altura em mundo deveria ser 0,30 × 2 = 0,60, e veio {m.Tamanho.y:0.###}.");
        }

        /// <summary>
        /// Um colisor cujo <c>bounds</c> veio vazio não foi <b>medido</b> — e acusar −100% de
        /// um número que não foi medido é ruído. Os quatro <c>*FloorGrid/Colisao</c> saíam
        /// assim no primeiro relatório.
        /// </summary>
        [Test]
        public void ColisorNaoMedido_NaoViraQueixa()
        {
            var vazio = Novo("Poligono_Vazio");
            vazio.AddComponent<SpriteRenderer>();
            vazio.AddComponent<Rigidbody2D>().bodyType = RigidbodyType2D.Dynamic;

            // PolygonCollider2D sem pontos cai no ramo do bounds, que vem vazio.
            var col = vazio.AddComponent<PolygonCollider2D>();
            col.pathCount = 0;

            var m = AuditoriaDeColisores.Medir(col);

            Assert.IsEmpty(m.Queixa,
                $"Um colisor sem geometria medível gerou queixa: '{m.Queixa}'. Zero aqui " +
                "significa 'não medido', não 'tem tamanho zero' — a doc da 6000.4 diz que " +
                "bounds fica vazio com o colisor desligado ou o objeto inativo.");
        }

        /// <summary>
        /// A queixa que só existe porque a ferramenta a encontrou: <b>nenhum</b> ator
        /// instanciado numa cena deste projeto tem escala uniforme.
        ///
        /// <para>Medido em 2026-09-04: Abdul em (1,162 × 2,671) — esticado 2,3× em Y — os dez
        /// Cultistas do Deserto em (0,630 × 0,804), a Cassilda em (1,478 × 1,925). Isso
        /// falsificou um comentário que dois arquivos deste projeto carregavam afirmando o
        /// contrário; a medição antiga tinha olhado a raiz dos prefabs, e é na <b>instância</b>
        /// que a escala é sobrescrita.</para>
        /// </summary>
        [Test]
        public void EscalaNaoUniformeNumAtor_EhAcusada()
        {
            var ator = Novo("Abdul_Alhazred");
            ator.transform.localScale = new Vector3(1.1621f, 2.6705f, 1f);  // o valor da cena
            ator.AddComponent<SpriteRenderer>();
            ator.AddComponent<Rigidbody2D>().bodyType = RigidbodyType2D.Dynamic;
            var col = ator.AddComponent<BoxCollider2D>();
            col.size = new Vector2(0.60f, 0.30f);

            var m = AuditoriaDeColisores.Medir(col);

            StringAssert.Contains("escala não uniforme", m.Queixa,
                $"Escala (1,162 × 2,671) num ator não foi acusada. Queixa veio: '{m.Queixa}'. " +
                "Esticar pixel art de 32 PPU por fator não inteiro tira o sprite do grid de " +
                "pixel, e num colisor circular a física usa o maior eixo enquanto o desenho " +
                "vira elipse — os dois passam a discordar.");
        }

        /// <summary>
        /// Cenário esticado é legítimo: uma parede <b>é</b> uma caixa alongada. A queixa de
        /// escala não pode voltar a marcar as paredes do Santuário.
        /// </summary>
        [Test]
        public void EscalaNaoUniformeNumCenario_NaoEhAcusada()
        {
            var parede = Novo("Parede_Leste");
            parede.transform.localScale = new Vector3(1f, 11f, 1f);
            var col = parede.AddComponent<BoxCollider2D>();

            var m = AuditoriaDeColisores.Medir(col);

            Assert.IsEmpty(m.Queixa,
                $"Uma parede esticada foi acusada: '{m.Queixa}'. Alongar cenário é como se " +
                "constrói uma parede — a queixa de escala vale para ator e para círculo, " +
                "onde física e desenho passam a discordar.");
        }

        /// <summary>
        /// A composição é lida de <c>compositeOperation</c>, o enum da 6000.4 — e não do bool
        /// <c>usedByComposite</c>, que não existe mais nesta versão.
        /// </summary>
        [Test]
        public void AComposicao_EhOEnumDa6000_4()
        {
            var go = Novo("Qualquer");
            var col = go.AddComponent<BoxCollider2D>();

            var m = AuditoriaDeColisores.Medir(col);

            Assert.AreEqual(Collider2D.CompositeOperation.None.ToString(), m.Composicao,
                $"A composição veio como '{m.Composicao}'. Ela tem de ser o enum " +
                "Collider2D.CompositeOperation — 'None' é o equivalente ao antigo " +
                "usedByComposite = false, e a Script Reference da 6000.4 não tem sequer " +
                "página para usedByComposite.");
        }
    }
}
