using System.Linq;
using FavelaAmarela.Core.Enemies;
using FavelaAmarela.Runtime.Enemies;
using FavelaAmarela.Runtime.Itens;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Tilemaps;

namespace FavelaAmarela.Tests.EditMode
{
    /// <summary>
    /// Mede a sala do Trono de Aldebaran contra o que se joga dentro dela.
    ///
    /// <para><b>A queixa que originou (Vini, 2026-09-10):</b> <i>"O rei se encontra fora da
    /// arena."</i> Estava. O <c>ReiEmAmarelo.prefab</c> tem escala 1 e o sprite
    /// <c>rei_idle</c> é 88 × 129 px a PPU 32 — 2,75 × 4,03 unidades, pivô no pé. Mas a
    /// <b>instância na cena</b> carregava um override de escala <c>2,9041092</c> (número de
    /// arrastar a alça, não de digitar), que o punha em <b>7,99 × 11,71 un</b>: mais alto que a
    /// tela inteira (a câmera mostra 11,25) e, de pé em y = 67, com o corpo indo até y = 78,7
    /// quando a parede norte acaba em 70,5. <b>Oito unidades dele estavam desenhadas sobre o
    /// vazio.</b></para>
    ///
    /// <para><b>Por que a sala engana.</b> Ela é um <b>losango</b>, não um retângulo — o
    /// <c>Castelo_Grid</c> tem <c>m_CellLayout: 2</c> (Isometric), então célula não é
    /// coordenada de mundo. Larga 30 un em y = 62 e afunilando até um ponto em y = 69,5. O
    /// gatilho <c>Z5_TronoDeAldebaran</c>, esse sim, é uma caixa de 30 × 15, e os cantos dela
    /// <b>não têm chão nenhum</b>. Medir a arena pelo gatilho dá a resposta errada; é preciso
    /// medir pelo chão pintado.</para>
    /// </summary>
    public sealed class OTronoCabeNaSalaTests
    {
        private const string Cena = "Assets/Scenes/Castelo_Carcosa.unity";

        /// <summary>Altura visível da câmera nas arenas: referência 640 × 360 a PPU 32.</summary>
        private const float AlturaDaTela = 11.25f;

        /// <summary>Damião andando, do <c>PlayerStealthState</c> — a velocidade forgiving.</summary>
        private const float VelocidadeAndando = 4.5f;

        private static Scene Abrir() => EditorSceneManager.OpenScene(Cena, OpenSceneMode.Single);

        private static Tilemap Chao()
        {
            var mapa = Object.FindObjectsByType<Tilemap>(FindObjectsSortMode.None)
                             .FirstOrDefault(t => t.name == "Piso_Castelo");

            Assert.NotNull(mapa, "Tilemap 'Piso_Castelo' não encontrado — sem ele não há como " +
                                 "saber onde existe chão.");
            return mapa;
        }

        private static bool TemChao(Tilemap chao, Vector2 mundo)
            => chao.HasTile(chao.WorldToCell(mundo));

        // ── O Rei ────────────────────────────────────────────────────────────

        [Test]
        public void ORei_TemChaoDebaixoDosPes()
        {
            Abrir();
            var rei = Object.FindFirstObjectByType<ReiEmAmareloAI>();
            Assert.NotNull(rei, "Nenhum ReiEmAmareloAI na cena do Castelo.");

            Assert.IsTrue(TemChao(Chao(), rei.transform.position),
                $"O Rei está em {(Vector2)rei.transform.position}, onde não há chão pintado.");
        }

        /// <summary>
        /// O corpo do Rei tem de estar <b>majoritariamente dentro</b> da sala.
        ///
        /// <para><b>Por que uma fração e não "nada pode passar" (afinado em 2026-09-10).</b> A
        /// primeira versão deste guarda exigia que o topo do sprite não passasse do chão
        /// pintado. Era mais rígido do que o defeito que ele protege: o Vini reposicionou o Rei
        /// em y = 67 depois de jogar e vencer, e ali <b>meia unidade de chapéu</b> passa da
        /// ponta da parede — num isométrico isso lê como estar de pé na frente do fundo, não
        /// como estar fora.</para>
        ///
        /// <para>O defeito real era de outra ordem de grandeza: com o override de escala
        /// 2,9041092 o Rei tinha 11,71 un e <b>só 30% do corpo</b> ficava na sala. 80% separa
        /// os dois casos com folga larga dos dois lados — 87% hoje, 30% no defeito.</para>
        ///
        /// <para>E mede contra a <b>parede</b> (o Tilemap de colisão), não contra o chão: é a
        /// parede que fecha a sala, e ela vai meia unidade além do último piso.</para>
        /// </summary>
        [Test]
        public void OCorpoDoRei_FicaMajoritariamenteDentroDaSala()
        {
            Abrir();
            var rei = Object.FindFirstObjectByType<ReiEmAmareloAI>();
            var sprite = rei.GetComponent<SpriteRenderer>();
            Assert.NotNull(sprite, "O Rei perdeu o SpriteRenderer.");

            var parede = Object.FindObjectsByType<Tilemap>(FindObjectsSortMode.None)
                               .FirstOrDefault(m => m.name == "Colisao");
            Assert.NotNull(parede, "Tilemap 'Colisao' não encontrado — é ele que fecha a sala.");

            parede.CompressBounds();
            float topoDaSala = parede.localBounds.max.y + parede.transform.position.y;

            float pe = sprite.bounds.min.y;
            float topo = sprite.bounds.max.y;
            float altura = topo - pe;

            float dentro = Mathf.Clamp(topoDaSala - pe, 0f, altura);
            float fracao = altura > 0f ? dentro / altura : 0f;

            Assert.GreaterOrEqual(fracao, FracaoMinimaDentroDaSala,
                $"Só {fracao:P0} do corpo do Rei está dentro da sala. Ele vai de y={pe:F2} a " +
                $"y={topo:F2} e a parede acaba em y={topoDaSala:F2}. Causa provável: override " +
                "de escala na instância de cena (o prefab é escala 1).");
        }

        /// <summary>Ver <see cref="OCorpoDoRei_FicaMajoritariamenteDentroDaSala"/>.</summary>
        private const float FracaoMinimaDentroDaSala = 0.8f;

        /// <summary>
        /// Um chefe mais alto que a tela não pode ser lido. A câmera das arenas mostra 11,25
        /// unidades de altura; com o override de 2,9 o Rei tinha 11,71.
        /// </summary>
        [Test]
        public void ORei_CabeNaTela()
        {
            Abrir();
            var sprite = Object.FindFirstObjectByType<ReiEmAmareloAI>()
                               .GetComponent<SpriteRenderer>();

            Assert.Less(sprite.bounds.size.y, AlturaDaTela,
                $"O Rei tem {sprite.bounds.size.y:F2} un de altura e a câmera mostra " +
                $"{AlturaDaTela} — ele não cabe no quadro em que se luta contra ele.");
        }

        // ── Os abrigos ───────────────────────────────────────────────────────

        private static EscudoDeReliquia[] Abrigos()
        {
            var escudos = Object.FindObjectsByType<EscudoDeReliquia>(FindObjectsSortMode.None);
            Assert.IsNotEmpty(escudos,
                "Nenhum EscudoDeReliquia na cena. Rode " +
                "Tools/FavelaAmarela/Trono: montar os abrigos das relíquias.");
            return escudos;
        }

        /// <summary>
        /// Cada relíquia que o rito exige tem um abrigo. Uma sem escudo seria um ciclo sem
        /// resposta possível — morte certa, do jeito que o Vini relatou na mecânica antiga.
        /// </summary>
        [Test]
        public void CadaReliquiaExigida_TemOSeuAbrigo()
        {
            Abrir();
            var rei = Object.FindFirstObjectByType<ReiEmAmareloAI>();
            var abrigos = Abrigos();

            foreach (var id in rei.ReliquiasExigidas)
                Assert.IsTrue(abrigos.Any(e => e.ArtefatoId == id),
                    $"O rito exige '{id}' e nenhum ponto focal ergue o abrigo dessa relíquia.");
        }

        /// <summary>
        /// <b>Todo o abrigo tem de estar sobre chão pintado.</b> Um escudo que transborda a
        /// borda do losango convida o jogador a ficar de pé no vazio — e a cúpula desenhada diz
        /// que ali é seguro.
        /// </summary>
        [Test]
        public void TodoOAbrigo_FicaSobreOChao()
        {
            Abrir();
            var chao = Chao();

            foreach (var escudo in Abrigos())
            {
                Vector2 centro = escudo.transform.position;

                for (int i = 0; i < 16; i++)
                {
                    float t = i / 16f * Mathf.PI * 2f;
                    var p = centro + new Vector2(escudo.SemiEixoX * Mathf.Cos(t),
                                                 escudo.SemiEixoY * Mathf.Sin(t));

                    Assert.IsTrue(TemChao(chao, p),
                        $"O abrigo de '{escudo.ArtefatoId}' em {centro} chega a {p}, onde não " +
                        "há chão. O jogador seria mandado ficar de pé sobre o vazio.");
                }
            }
        }

        /// <summary>
        /// <b>A travessia mais longa tem de caber na calmaria.</b> O escudo acende no começo de
        /// <c>Selando</c>, então a calmaria inteira é o tempo de corrida; se um altar for
        /// afastado, este teste falha em vez de o jogador descobrir sozinho que não dava para
        /// chegar. Mede <b>andando</b> (4,5 u/s), não correndo — a luta não pode exigir correr.
        /// </summary>
        [Test]
        public void ATravessiaMaisLonga_CabeNaCalmaria()
        {
            Abrir();
            var rei = Object.FindFirstObjectByType<ReiEmAmareloAI>();
            var abrigos = Abrigos();

            float calmaria = new SerializedObject(rei)
                .FindProperty("intervaloEntreCiclos").floatValue;

            float pior = 0f;
            string trajeto = "";

            foreach (var origem in abrigos)
            foreach (var destino in abrigos)
            {
                if (origem == destino) continue;

                float t = AbrigoDeReliquia.SegundosParaAlcancar(
                    origem.transform.position, destino.transform.position,
                    VelocidadeAndando, destino.SemiEixoX, destino.SemiEixoY);

                if (t <= pior) continue;

                pior = t;
                trajeto = $"{origem.ArtefatoId} -> {destino.ArtefatoId}";
            }

            Assert.Less(pior, calmaria,
                $"A travessia mais longa ({trajeto}) leva {pior:F2} s andando e a calmaria " +
                $"entre ciclos é de {calmaria:F2} s. O jogador não teria como chegar ao abrigo " +
                "antes do desvelo.");
        }

        /// <summary>
        /// Com três relíquias e três ciclos, cada artefato abriga exatamente uma vez — que é o
        /// que <i>"cada artefato gera um escudo por vez"</i> quer dizer.
        /// </summary>
        [Test]
        public void OsCiclos_SaoUmPorReliquia()
        {
            Abrir();
            var rei = Object.FindFirstObjectByType<ReiEmAmareloAI>();

            int ciclos = new SerializedObject(rei).FindProperty("ciclosDeSelamento").intValue;

            Assert.AreEqual(rei.ReliquiasExigidas.Count, ciclos,
                $"O rito exige {rei.ReliquiasExigidas.Count} relíquia(s) e roda {ciclos} " +
                "ciclo(s). Com números diferentes, alguma relíquia abriga duas vezes e outra " +
                "nenhuma — o revezamento deixa de ler como 'um escudo por artefato'.");
        }
    }
}
