using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Tilemaps;
using FavelaAmarela.Runtime.Enemies;

namespace FavelaAmarela.EditorTools
{
    /// <summary>
    /// Povoa a arena da Byakhee, nos Portões das Ruínas.
    ///
    /// <para><b>O vazio que isto preenche (2026-09-09).</b> A cena inteira tinha <b>quatro</b>
    /// <c>SpriteRenderer</c> — numa luta de chefe. Sem referência visual, o jogador não sabe onde
    /// está na arena nem onde a arena acaba, e um chefe que voa fica ainda mais difícil de
    /// acompanhar.</para>
    ///
    /// <para><b>Por que NENHUM colisor, e isto é decisão e não esquecimento.</b> Medido, nada
    /// nesta luta é barrado por geometria:</para>
    ///
    /// <list type="bullet">
    ///   <item>O <c>GritoDirecionado</c> é <c>Vector2.Distance &lt;= alcanceDoGrito</c> — um
    ///   <b>raio</b>, sem ângulo e sem <i>raycast</i>. Parede não bloqueia.</item>
    ///   <item>O <c>MergulhoDeGarras</c> persegue o jogador; obstáculo não intercepta.</item>
    ///   <item>O <c>Rasante</c> é aéreo.</item>
    /// </list>
    ///
    /// <para>Num cenário assim, colisor <b>obstrui sem proteger</b>: o jogador esquivando de um
    /// mergulho encosta num pilar e morre por causa do pilar, não do chefe. É saldo negativo.
    /// Se um dia o grito ganhar cone e linha de visão, os mesmos objetos ganham colisor e viram
    /// cobertura de verdade — a colocação já está pensada para isso.</para>
    ///
    /// <para><b>Onde não se põe nada:</b> o norte (y &gt; 4) é dos portões, que são o fundo da
    /// luta; e o corredor sul (perto de x = 0, y &lt; −4) é por onde o jogador chega.</para>
    /// </summary>
    public static class CenarioDaArenaDaByakhee
    {
        private const string Marcador = "[ArenaByakhee]";
        private const string Cena = "Assets/Scenes/Portoes_Das_Ruinas.unity";
        private const string Raiz = "Cenario_DaArena";

        /// <summary>
        /// Semi-eixos do <b>anel de marcos</b> — só decoração. Até 2026-09-10 eram também a
        /// coleira do <c>ByakheeAI</c>; a coleira passou a ser o chão pintado da sala inteira
        /// (ver <c>ByakheeAI.chaoDaArena</c>), e estes números ficaram só para dizer onde os
        /// marcos de borda sentam, emoldurando o miolo onde a luta costuma acontecer.
        /// </summary>
        private const float SemiX = 9f, SemiY = 5f;

        private const string Objetos = "Assets/ThirdParty/CraftPix/Undead/Objetos/";

        /// <summary>
        /// Marcos de borda: dizem onde a arena acaba. Ângulos escolhidos para pular o norte
        /// (portões) e o sul (chegada do jogador).
        /// </summary>
        private static readonly (float Angulo, string Arte, float Escala)[] Borda =
        {
            (  0f, "Ruin_1", 1f),
            ( 40f, "Ruin_3", 1f),
            (140f, "Ruin_3", 1f),
            (180f, "Ruin_1", 1f),
            (215f, "Ruin_2", 1f),
            (325f, "Ruin_2", 1f),
        };

        /// <summary>
        /// Os muros que ladeiam os portões.
        ///
        /// <para><b>Por que existem (pedido do Vini, 2026-09-09).</b> O colisor
        /// <c>Os_Portoes</c> tem <b>18 unidades</b> de largura e a arte do portão tem <b>8</b> —
        /// sobravam <b>5 unidades de parede invisível de cada lado</b>. Um portão sem muro não é
        /// portão: é um arco solto que dá para contornar. Agora a barreira que já existia tem
        /// corpo.</para>
        ///
        /// <para><c>Ruin_1</c> são colunas quebradas de pé, 4 × 4 un — pedra, no tom dos
        /// Portões. As <c>Ruins_*</c> do <i>CursedLand</i> ficaram de fora: são arcos orgânicos
        /// avermelhados, do bioma errado.</para>
        /// </summary>
        /// <summary>
        /// A <b>muralha</b> que os Muros viraram. Os Muros (4 peças em ±6,5 e ±9,2, de 09/09,
        /// pedido <i>"seria bom se os portões tivessem paredes de cada lado"</i>) fechavam só os
        /// 18 do colisor; a muralha fecha a sala. Pedido do Vini, 2026-09-10: <i>"quero
        /// integrar os portões à cena, não tem um jeito mais bonito de deixar esse portão como
        /// fundo?"</i>).
        ///
        /// <para><b>Por que frontal, e não a muralha modular da Kenney.</b> O pacote
        /// <c>kenney_isometric-miniature-dungeon</c> tem <c>stoneWall*</c> em quatro orientações,
        /// mas medido: a face corre na <b>diagonal isométrica</b> (colunas 95..255 de 256), como
        /// toda parede iso 2:1. Enfileirá-las numa linha de y constante — que é a linha do
        /// portão — dá zigue-zague, não muro. O próprio portão é <b>frontal e simétrico</b>: um
        /// cheat que funciona por ser um prop só, centrado. O fundo tem de falar essa língua:
        /// colunas frontais, na mesma pixel art do chão e dos props.</para>
        ///
        /// <para><b>Por que <c>Ruin_1</c>:</b> colunas negras quebradas, 4 × 4 un, no tom do
        /// portão (pedra quase preta) e do chão (cinza). A cada 4 unidades de ±6 a ±18 — o
        /// portão ocupa ±4 — com bases <c>Ruin_3</c> nos vãos, um pouco atrás. É um colonato em
        /// ruínas: <i>Portões das Ruínas</i>.</para>
        /// </summary>
        private static readonly (float X, string Arte, float Recuo)[] Muralha =
        {
            ( -6f, "Ruin_1", 0f), (  6f, "Ruin_1", 0f),
            (-10f, "Ruin_1", 0f), ( 10f, "Ruin_1", 0f),
            (-14f, "Ruin_1", 0f), ( 14f, "Ruin_1", 0f),
            (-18f, "Ruin_1", 0f), ( 18f, "Ruin_1", 0f),
            (-22f, "Ruin_1", 0f), ( 22f, "Ruin_1", 0f),
            ( -8f, "Ruin_3", 0.6f), (  8f, "Ruin_3", 0.6f),   // bases nos vãos, meio passo atrás
            (-12f, "Ruin_3", 0.6f), ( 12f, "Ruin_3", 0.6f),
            (-16f, "Ruin_3", 0.6f), ( 16f, "Ruin_3", 0.6f),
            (-20f, "Ruin_3", 0.6f), ( 20f, "Ruin_3", 0.6f),
        };

        /// <summary>
        /// Meia-largura do colisor da muralha. A sala é um losango 2:1 de meia-largura 31,5 no
        /// centro: em y = 3 ela tem <b>25,5</b> de meia-largura. O colisor vai até 26 para
        /// encostar na parede da sala — parede que se contorna não é parede.
        /// </summary>
        private const float MeiaLarguraDaMuralha = 26f;

        /// <summary>
        /// <b>Além dos portões, Carcosa.</b> Um gradiente escuro cobre o chão ao norte da linha
        /// do portão, do transparente na linha ao quase-opaco no fundo. Faz três coisas de uma
        /// vez: a arena passa a <i>acabar</i> em algum lugar (antes o chão flutuava no cinza até
        /// a borda da tela); a luz dourada do portão ganha contra o que brilhar; e o Refúgio,
        /// atrás dele, vira um poste aceso no escuro — que é o que um Poste de Luz é.
        ///
        /// <para>Camada <c>Chao</c>, ordem 1: acima do piso (ordem 0), abaixo de tudo em
        /// <c>Default</c> — pilares, portão, atores e o Poste continuam por cima.</para>
        /// </summary>
        private const string Escuridao =
            "Assets/FavelaAmarela/Art/Vfx/escuridao_alem_dos_portoes.png";

        private const float AlturaDaEscuridao = 14f;

        /// <summary>
        /// A linha dos portões — a mesma do <c>Batente</c> e do colisor.
        ///
        /// <para><b>Era 8, virou 5,5, e a planta é que disse.</b> A câmera mostra 11,25 de
        /// altura: centrada na arena, ela alcança <b>y = 5,6</b>. Com os portões em 8 eles
        /// ficavam <i>acima do quadro</i> e só apareciam quando o jogador empurrava para o
        /// norte — não eram fundo da luta, eram um lugar aonde se ia.</para>
        ///
        /// <para>A leitura certa é mais simples: <b>os portões SÃO a parede norte da arena</b>.
        /// A elipse termina em y = +5; eles ficam meio passo além, e o jogador os vê sempre que
        /// está na metade norte.</para>
        ///
        /// <para><b>E virou 3 em 2026-09-10, pela captura.</b> Com a base em 5,5 e o topo da
        /// câmera em 5,6, o portão era <b>0,1 unidade</b> de borda: da posição em que a luta
        /// acontece, ele não existia. "Fundo da luta" é o que se vê <i>durante</i> a luta — com
        /// a base em 3, quem está no centro vê 2,6 unidades do portão, a parte das portas. A
        /// arena fica com 10 de altura entre o gatilho (−7) e ele; o Refúgio (y = 8) e a
        /// passagem (y = 11,8) continuam atrás.</para>
        ///
        /// <para>Esta constante manda no <c>Os_Portoes</c> (colisor), no <c>Batente</c> (arte,
        /// filho dele, local zero) e nos muros — uma fonte só para a linha do portão.</para>
        /// </summary>
        private const float YDosPortoes = 3f;

        /// <summary>
        /// Quanto a <b>base</b> da arte do portão fica abaixo da linha do colisor.
        ///
        /// <para>A <c>Entrada_PortoesDeCarcosa</c> é um <b>diorama</b>: uma plataforma de pedra
        /// em losango embaixo, e os pilares com a grade em cima. Medido no PNG (128 × 132 px,
        /// pivô na base, escala 2): a plataforma vai da base até <b>2,7 unidades</b> acima dela
        /// — é a linha mais larga da imagem, onde os pilares nascem. Com a base na linha do
        /// colisor, o que se via da arena era só a plataforma: uma faixa bege. Com a base 2,7
        /// abaixo, os <b>pilares</b> ficam na linha do colisor, a plataforma vira chão de pedra
        /// dentro da arena, e a grade com a luz dourada entra no quadro.</para>
        /// </summary>
        private const float DeslocamentoDaBaseDoPortao = -2.7f;

        /// <summary>Textura de chão, mais para dentro. Nada no miolo, onde a luta acontece.</summary>
        private static readonly (float Angulo, float Fracao, string Arte)[] Interior =
        {
            ( 20f, 0.72f, "Bones_1"),
            ( 65f, 0.66f, "Rock_1"),
            (115f, 0.70f, "Bones_2"),
            (160f, 0.64f, "Rock_2"),
            (200f, 0.72f, "Bones_3"),
            (250f, 0.62f, "Rock_1"),
            (300f, 0.70f, "Bones_1"),
            (340f, 0.66f, "Pile_sculls"),
        };

        [MenuItem("Tools/FavelaAmarela/Arena: povoar a arena da Byakhee")]
        public static void Executar()
        {
            Scene cena = EditorSceneManager.OpenScene(Cena, OpenSceneMode.Single);

            var antiga = cena.GetRootGameObjects().FirstOrDefault(g => g.name == Raiz);
            if (antiga != null)
            {
                Object.DestroyImmediate(antiga);
                Debug.Log($"{Marcador} cenário anterior removido — a ferramenta é idempotente.");
            }

            var raiz = new GameObject(Raiz);
            int postos = 0, faltando = 0;

            foreach (var (ang, arte, escala) in Borda)
            {
                // 1,08 do semi-eixo: logo FORA da coleira, marcando o limite sem estorvar.
                var p = NaElipse(ang, 1.08f);
                if (Criar(raiz.transform, arte, p, escala, "Borda")) postos++; else faltando++;
            }

            // Os Muros (4 peças) deram lugar à Muralha: a mesma ideia, de ponta a ponta.
            foreach (var (x, arte, recuo) in Muralha)
            {
                // Pivô central: a base do sprite fica na linha do portão quando o centro está
                // meia altura acima dela. Ruin_1 tem 4 un, Ruin_3 tem 2.
                float meiaAltura = arte == "Ruin_1" ? 2f : 1f;
                var p = new Vector2(x, YDosPortoes + recuo + meiaAltura);
                if (Criar(raiz.transform, arte, p, 1f, "Muralha", ordemY: YDosPortoes + recuo)) postos++;
                else faltando++;
            }

            if (CriarEscuridao(raiz.transform)) postos++; else faltando++;
            AlargarOColisorDosPortoes();

            foreach (var (ang, frac, arte) in Interior)
            {
                var p = NaElipse(ang, frac);
                if (Criar(raiz.transform, arte, p, 1f, "Chao")) postos++; else faltando++;
            }

            PorOPortaoOndeEstaOColisor();
            LigarAColeiraNaSala();

            EditorSceneManager.MarkSceneDirty(cena);
            EditorSceneManager.SaveScene(cena);

            Debug.Log($"{Marcador} {postos} objeto(s) postos, {faltando} sem arte. " +
                      "Nenhum com colisor — ver o resumo da classe.");
        }

        /// <summary>
        /// A arte dos Portões (<c>Batente</c>) é <b>filha</b> do colisor (<c>Os_Portoes</c>). Em
        /// 09/09 eu escrevi nela a posição <b>local</b> 5,5 achando que era mundo — e o mundo
        /// virou 5,5 + 5,5 = <b>11</b>: a arte ficou 5,5 unidades acima da tela, invisível da
        /// arena, e os muros que pus em 5,5 ladeavam um colisor sem arte. É o <i>"a cena continua
        /// toda errada"</i> do Vini. Local zero é a base do portão exatamente na linha do
        /// colisor.
        /// </summary>
        private static void PorOPortaoOndeEstaOColisor()
        {
            var colisor = GameObject.Find("Os_Portoes");
            if (colisor == null)
            {
                Debug.LogWarning($"{Marcador} 'Os_Portoes' não encontrado — a arte do portão " +
                                 "não foi reposicionada.");
                return;
            }

            var batente = colisor.transform.Find("Batente");
            if (batente == null)
            {
                Debug.LogWarning($"{Marcador} 'Batente' não é filho de 'Os_Portoes' — nada " +
                                 "reposicionado.");
                return;
            }

            Vector3 antesColisor = colisor.transform.position;
            colisor.transform.position = new Vector3(0f, YDosPortoes, 0f);

            Vector3 antes = batente.position;
            batente.localPosition = new Vector3(0f, DeslocamentoDaBaseDoPortao, 0f);

            // Y-sort pela linha dos PILARES (o colisor), não pela base da plataforma: quem está
            // na plataforma, ao sul dos pilares, tem de ser desenhado na frente deles.
            var sr = batente.GetComponent<SpriteRenderer>();
            if (sr != null) sr.sortingOrder = Mathf.RoundToInt(-YDosPortoes * 10f);

            Debug.Log($"{Marcador} Os_Portoes: y {antesColisor.y} -> {YDosPortoes}; Batente: mundo " +
                      $"{(Vector2)antes} -> {(Vector2)batente.position} (os pilares na linha do " +
                      "colisor; a plataforma do diorama entra na arena).");
        }

        /// <summary>
        /// Liga a coleira do Byakhee à sala: o Tilemap do chão e a linha dos Portões. São
        /// objetos de cena, então a ligação vive na <b>instância</b>, como override — o prefab
        /// não pode apontar para eles.
        /// </summary>
        private static void LigarAColeiraNaSala()
        {
            var byakhee = Object.FindAnyObjectByType<ByakheeAI>();
            if (byakhee == null)
            {
                Debug.LogWarning($"{Marcador} nenhum ByakheeAI na cena — coleira não ligada.");
                return;
            }

            // O mesmo critério da IA e do teste: mais células PINTADAS, não a maior caixa —
            // pela caixa, o anel de paredes ganhava do chão.
            var chao = ByakheeAI.ChaoComMaisTiles(
                Object.FindObjectsByType<Tilemap>());
            int maisCelulas = chao != null ? CelulasPintadas(chao) : 0;

            var muralha = GameObject.Find("Os_Portoes");

            var so = new SerializedObject(byakhee);
            so.FindProperty("chaoDaArena").objectReferenceValue = chao;
            so.FindProperty("muralhaNorte").objectReferenceValue = muralha != null ? muralha.transform : null;
            so.ApplyModifiedPropertiesWithoutUndo();

            Debug.Log($"{Marcador} coleira do Byakhee: chão = {(chao != null ? chao.name : "NENHUM")} " +
                      $"({maisCelulas} células), muralha norte = " +
                      $"{(muralha != null ? muralha.name + " em y=" + muralha.transform.position.y : "NENHUMA")}.");
        }

        private static int CelulasPintadas(Tilemap mapa)
        {
            int n = 0;
            foreach (var t in mapa.GetTilesBlock(mapa.cellBounds)) if (t != null) n++;
            return n;
        }

        private static Vector2 NaElipse(float grausl, float fator)
        {
            float r = grausl * Mathf.Deg2Rad;
            return new Vector2(SemiX * fator * Mathf.Cos(r), SemiY * fator * Mathf.Sin(r));
        }

        private static bool Criar(Transform pai, string arte, Vector2 pos, float escala, string tipo,
                                  float? ordemY = null)
        {
            var sprite = AssetDatabase.LoadAssetAtPath<Sprite>(Objetos + arte + ".png");

            if (sprite == null)
            {
                Debug.LogWarning($"{Marcador} sprite '{arte}' não encontrada — pulada.");
                return false;
            }

            var go = new GameObject($"{tipo}_{arte}");
            go.transform.SetParent(pai, false);
            go.transform.position = pos;
            go.transform.localScale = new Vector3(escala, escala, 1f);

            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = sprite;

            // A MESMA regra da geometria estática do projeto: sortingOrder = -y * 10. Sem isto
            // o objeto empata com os atores e os dois piscam entre si.
            // Para sprites de pivô central postos "de pé" na linha do portão, o Y de ordenação
            // é a BASE (a linha), não o centro — senão o pilar empataria com quem está 2 un ao
            // norte dele.
            sr.sortingOrder = Mathf.RoundToInt(-(ordemY ?? pos.y) * 10f);

            return true;
        }

        private static bool CriarEscuridao(Transform pai)
        {
            var sprite = AssetDatabase.LoadAssetAtPath<Sprite>(Escuridao);
            if (sprite == null)
            {
                Debug.LogWarning($"{Marcador} '{Escuridao}' não encontrado — sem escuridão além dos portões.");
                return false;
            }

            var go = new GameObject("Escuridao_AlemDosPortoes");
            go.transform.SetParent(pai, false);
            go.transform.position = new Vector3(0f, YDosPortoes, 0f);

            // O sprite tem 4 × 64 px a PPU 32 = 0,125 × 2 un, pivô na base. Esticado para cobrir
            // a sala de lado a lado (a câmera nunca mostra mais de 20) e AlturaDaEscuridao para
            // cima. 64 degraus de alpha em 14 un = 3,5/255 por degrau: imperceptível com Point.
            go.transform.localScale = new Vector3(70f / 0.125f, AlturaDaEscuridao / 2f, 1f);

            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = sprite;
            sr.sortingLayerName = "Chao";
            sr.sortingOrder = 1;
            return true;
        }

        /// <summary>
        /// O colisor <c>Os_Portoes</c> passa a ter a largura da muralha: parede que se contorna
        /// não é parede. Antes eram 18 un numa linha em que a sala tem ~41 — dava para dar a
        /// volta por fora e chegar ao Refúgio sem vencer a Byakhee.
        /// </summary>
        private static void AlargarOColisorDosPortoes()
        {
            var colisor = GameObject.Find("Os_Portoes");
            var caixa = colisor != null ? colisor.GetComponent<BoxCollider2D>() : null;
            if (caixa == null) return;

            var antes = caixa.size;
            caixa.size = new Vector2(MeiaLarguraDaMuralha * 2f, caixa.size.y);
            Debug.Log($"{Marcador} Os_Portoes: colisor {antes.x} -> {caixa.size.x} de largura (a muralha inteira).");
        }
    }
}
