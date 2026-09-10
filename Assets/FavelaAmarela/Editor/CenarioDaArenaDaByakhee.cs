using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

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

        /// <summary>Semi-eixos da arena, iguais aos do <c>ByakheeAI</c>.</summary>
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
        private static readonly (float X, string Arte)[] Muros =
        {
            (-6.5f, "Ruin_1"), (6.5f, "Ruin_1"),   // o corpo do muro, encostando na arte do portão
            (-9.2f, "Ruin_3"), (9.2f, "Ruin_3"),   // as pontas, fechando os 18 do colisor
        };

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
        /// </summary>
        private const float YDosPortoes = 5.5f;

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

            foreach (var (x, arte) in Muros)
                if (Criar(raiz.transform, arte, new Vector2(x, YDosPortoes), 1f, "Muro")) postos++;
                else faltando++;

            foreach (var (ang, frac, arte) in Interior)
            {
                var p = NaElipse(ang, frac);
                if (Criar(raiz.transform, arte, p, 1f, "Chao")) postos++; else faltando++;
            }

            EditorSceneManager.MarkSceneDirty(cena);
            EditorSceneManager.SaveScene(cena);

            Debug.Log($"{Marcador} {postos} objeto(s) postos, {faltando} sem arte. " +
                      "Nenhum com colisor — ver o resumo da classe.");
        }

        private static Vector2 NaElipse(float grausl, float fator)
        {
            float r = grausl * Mathf.Deg2Rad;
            return new Vector2(SemiX * fator * Mathf.Cos(r), SemiY * fator * Mathf.Sin(r));
        }

        private static bool Criar(Transform pai, string arte, Vector2 pos, float escala, string tipo)
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
            sr.sortingOrder = Mathf.RoundToInt(-pos.y * 10f);

            return true;
        }
    }
}
