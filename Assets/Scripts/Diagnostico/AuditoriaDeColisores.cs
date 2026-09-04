using System.Collections.Generic;
using UnityEngine;

namespace FavelaAmarela.Runtime.Diagnostico
{
    /// <summary>
    /// Mede todo <see cref="Collider2D"/> de uma hierarquia e o compara com o
    /// <b>corpo desenhado</b> do sprite.
    ///
    /// <para><b>Vive no Runtime, e não no Editor, de propósito.</b> A ferramenta de Editor
    /// (<c>Rigidbody2DAuditor</c>) precisa disto para varrer prefabs e cenas, e o jogo rodando
    /// precisa do mesmo para diagnóstico em Play. Código de Editor não pode ser referenciado
    /// pelo Runtime — se a medição morasse lá, existiriam duas cópias da mesma conta, e a que
    /// não fosse usada envelheceria calada.</para>
    ///
    /// <para><b>Por que NÃO comparar todo colisor com o sprite inteiro.</b> Seria o desenho
    /// óbvio e marcaria <b>100% do elenco</b>. Medido: a hurtbox nasce em 0,72 × 0,86 do sprite
    /// (fatores de <c>Hurtbox.GarantirPara</c>) e a pegada de movimento é 0,60 × 0,30 num corpo
    /// de 1,00 × 2,53 — ou seja, −40% de largura e −88% de altura. Os dois são <b>deliberados</b>
    /// e documentados. Um limiar cego de 20% acusaria os dois em todo ator e o relatório viraria
    /// ruído.</para>
    ///
    /// <para><b>E comparar "por papel" também não basta, se o papel for grosseiro.</b> A
    /// primeira versão disto chamava de pegada <b>todo colisor sólido</b> e comparava tudo com
    /// a pegada humana: marcou 57 de 141 colisores, entre eles as paredes do Santuário, o Lago
    /// de Hali e os limites do Deserto — geometria de cenário que não tem pegada nenhuma a
    /// respeitar. Trocar um limiar cego por outro não é medir.</para>
    ///
    /// <para><b>O que sobrou, e por quê.</b> Cada regra abaixo existe porque <b>nada mais no
    /// projeto a verifica</b>:</para>
    /// <list type="bullet">
    ///   <item><b>Hurtbox</b> contra a silhueta vezes os fatores de <c>GarantirPara</c>. É uma
    ///   conta fechada: a hurtbox é <i>derivada</i> desses fatores, então divergir significa
    ///   que o valor gravado na cena não é mais o que o código produziria.</item>
    ///   <item><b>Pegada de ator</b>: a <b>linha do pé</b> e a <b>proporção de chão</b>. Não o
    ///   tamanho absoluto — esse já é guardado por <c>ColisoresDoElencoTests</c>, que conhece
    ///   os quatro humanos pelo caminho do prefab. Repetir aquilo aqui, com uma identificação
    ///   pior, é criar a segunda fonte da verdade que o próprio doc daquele arquivo diz ser o
    ///   modo de falha mais repetido deste projeto.</item>
    ///   <item><b>Cenário e gatilho</b>: nada. Uma parede não tem tamanho esperado, e um portal
    ///   não tem relação com sprite.</item>
    /// </list>
    /// </summary>
    public static class AuditoriaDeColisores
    {
        /// <summary>Fatores com que <c>Hurtbox.GarantirPara</c> deriva a área do sprite.</summary>
        private const float FatorLarguraDaHurtbox = 0.72f;

        /// <inheritdoc cref="FatorLarguraDaHurtbox"/>
        private const float FatorAlturaDaHurtbox = 0.86f;

        /// <summary>
        /// Proporção de chão da célula isométrica: uma pegada é <b>duas vezes mais larga que
        /// alta</b>, porque o chão é visto de viés. É a razão que a pegada calibrada do elenco
        /// (0,60 × 0,30) obedece, e é o que se pode conferir sem saber a espécie do ator.
        /// </summary>
        private const float RazaoDeChao = 2f;

        /// <summary>Quanto a proporção pode fugir de <see cref="RazaoDeChao"/> sem queixa.</summary>
        private const float ToleranciaDeRazao = 0.5f;

        /// <summary>
        /// Quanto os eixos da escala podem diferir antes de virar queixa. Pixel art a 32 PPU
        /// esticada por fator não inteiro sai do grid de pixel — é o que a skill
        /// <c>favela-pixelart-standards</c> existe para proteger.
        /// </summary>
        private const float ToleranciaDeEscala = 0.01f;

        private const int CamadaHurtboxJogador = 13;
        private const int CamadaHurtboxInimigo = 14;

        /// <summary>O que um colisor é, para saber contra o que compará-lo.</summary>
        public enum Papel
        {
            /// <summary>Recebe dano. Esperado: silhueta × os fatores da Hurtbox.</summary>
            Hurtbox,

            /// <summary>
            /// Área de chão que barra o movimento de um <b>ator</b> — algo com corpo físico e
            /// sprite. Conferida pela linha do pé e pela proporção, não pelo tamanho.
            /// </summary>
            Pegada,

            /// <summary>
            /// Colisor sólido que não é ator: parede, tilemap, limite de mapa, portão, lago.
            /// <b>Nada é conferido</b> — cenário não tem tamanho esperado.
            /// </summary>
            Cenario,

            /// <summary>Zona, portal, coletável. Sem relação com sprite.</summary>
            Gatilho,
        }

        /// <summary>Uma medição, pronta para virar linha de tabela ou de log.</summary>
        public struct Medida
        {
            public string Caminho;
            public string Tipo;          // Box, Circle, Capsule, Polygon, Edge, Composite
            public Papel Funcao;
            public Vector2 Tamanho;      // mundo; para círculo, (diâmetro, diâmetro)
            public float Raio;           // > 0 só para círculo
            public Vector2 Offset;       // local, como no Inspector
            public Vector2 CentroMundo;
            public bool EhTrigger;

            /// <summary>
            /// Operação de composição, como <c>Collider2D.compositeOperation</c>.
            ///
            /// <para>O pedido descrevia "Used By Composite", que era o <b>bool</b> das versões
            /// antigas. Na 6000.4 ele não existe mais: o sucessor é <c>compositeOperation</c>,
            /// um enum (None / Merge / Intersect / Difference / Flip). Conferido na Script
            /// Reference offline da versão exata — não há sequer página para
            /// <c>usedByComposite</c>. O enum diz mais: <c>None</c> é o antigo "false", e os
            /// outros quatro dizem <b>qual</b> booleana o composite aplica.</para>
            /// </summary>
            public string Composicao;

            public string Material;
            public bool TemSprite;
            public Vector2 TamanhoDoSprite;
            public Vector2 CentroDoSprite;

            /// <summary>Vazio quando nada destoa.</summary>
            public string Queixa;
        }

        /// <summary>Diferença relativa que passa a ser reportada.</summary>
        public const float LimiarDeTamanho = 0.20f;

        /// <summary>Deslocamento de centro, em unidades de mundo, que passa a ser reportado.</summary>
        public const float LimiarDeCentro = 0.2f;

        /// <summary>
        /// Mede a hierarquia inteira, inclusive objetos desativados.
        /// </summary>
        /// <param name="raiz">Prefab carregado ou objeto de cena.</param>
        /// <param name="saida">Lista que recebe as medidas. Não é limpa.</param>
        public static void Medir(GameObject raiz, List<Medida> saida)
        {
            if (raiz == null || saida == null) return;

            foreach (var col in raiz.GetComponentsInChildren<Collider2D>(includeInactive: true))
                saida.Add(Medir(col));
        }

        /// <summary>
        /// Mede um colisor só.
        ///
        /// <para>Pública porque a varredura de cena do <see cref="VisualizadorDeGolpes"/> devolve
        /// <b>colisores</b>, não raízes de hierarquia — com só o overload de <c>GameObject</c>
        /// ele teria de reagrupar os colisores por raiz e mediria os mesmos objetos várias
        /// vezes.</para>
        /// </summary>
        public static Medida Medir(Collider2D col)
        {
            var m = new Medida
            {
                Caminho = Hierarquia(col.transform),
                Tipo = Rotulo(col),
                EhTrigger = col.isTrigger,
                Composicao = col.compositeOperation.ToString(),
                Material = col.sharedMaterial != null ? col.sharedMaterial.name : "—",
            };

            Vector3 escala = col.transform.lossyScale;
            Vector2 offsetLocal = col.offset;

            switch (col)
            {
                case CircleCollider2D c:
                    // A Unity escala o raio pelo MAIOR eixo -- um círculo continua círculo por
                    // mais que o transform seja esticado.
                    //
                    // CORREÇÃO DE 2026-09-04. Este comentário afirmava que "todo o elenco deste
                    // projeto está em escala uniforme (medido em 2026-09-03)". É FALSO, e foi
                    // esta própria ferramenta que derrubou a afirmação: NENHUM ator colocado em
                    // cena tem escala uniforme. Abdul está em (1,162 × 2,671), os dez Cultistas
                    // do Deserto em (0,630 × 0,804), a Cassilda em (1,478 × 1,925), e o prefab
                    // do YugNeth em (4,113 × 0,563). A medição anterior olhou só a raiz dos
                    // prefabs e não as instâncias de cena, que é onde a escala é sobrescrita.
                    //
                    // Consequência prática: para estes, o desenho do gizmo (que segue a matriz
                    // e vira elipse) e a física (que trata como círculo do maior eixo) NÃO
                    // coincidem. A queixa "escala não uniforme" em Conferir existe por isso.
                    m.Raio = c.radius * Mathf.Max(Mathf.Abs(escala.x), Mathf.Abs(escala.y));
                    m.Tamanho = new Vector2(m.Raio * 2f, m.Raio * 2f);
                    break;

                case BoxCollider2D b:
                    m.Tamanho = new Vector2(b.size.x * Mathf.Abs(escala.x),
                                            b.size.y * Mathf.Abs(escala.y));
                    break;

                case CapsuleCollider2D k:
                    m.Tamanho = new Vector2(k.size.x * Mathf.Abs(escala.x),
                                            k.size.y * Mathf.Abs(escala.y));
                    break;

                default:
                    // Polygon, Edge, Composite: a forma não cabe em dois números. O bounds só
                    // vale com o colisor ligado e o objeto ativo (doc da 6000.4), então fora
                    // disso fica zero -- e zero aqui significa "não medido", não "vazio".
                    m.Tamanho = col.bounds.size;
                    break;
            }

            m.Offset = offsetLocal;
            m.CentroMundo = (Vector2)col.transform.position
                          + new Vector2(offsetLocal.x * escala.x, offsetLocal.y * escala.y);

            m.Funcao = PapelDe(col);

            var sr = col.GetComponentInParent<SpriteRenderer>();
            if (sr != null && sr.sprite != null)
            {
                m.TemSprite = true;
                var b = sr.sprite.bounds;   // local, já dividido pela PPU e com o pivô aplicado
                m.TamanhoDoSprite = new Vector2(b.size.x * Mathf.Abs(escala.x),
                                                b.size.y * Mathf.Abs(escala.y));
                m.CentroDoSprite = (Vector2)sr.transform.position
                                 + new Vector2(b.center.x * escala.x, b.center.y * escala.y);
            }

            m.Queixa = Conferir(m, escala);
            return m;
        }

        /// <summary>
        /// Classifica o colisor. <b>Ator e cenário são coisas diferentes</b> e a primeira versão
        /// disto não os separava — foi assim que as paredes do Santuário apareceram acusadas de
        /// ter a pegada errada.
        ///
        /// <para>O que separa é ter <b>corpo físico e sprite</b>: um ator se move e é desenhado;
        /// parede, tilemap de colisão e limite de mapa não têm as duas coisas.</para>
        /// </summary>
        private static Papel PapelDe(Collider2D col)
        {
            int camada = col.gameObject.layer;
            bool ehHurtbox = camada == CamadaHurtboxJogador || camada == CamadaHurtboxInimigo
                             || col.GetComponent<Combat.Hurtbox>() != null;

            if (ehHurtbox) return Papel.Hurtbox;
            if (col.isTrigger) return Papel.Gatilho;

            var corpo = col.GetComponentInParent<Rigidbody2D>();
            bool ehAtor = corpo != null
                          && corpo.bodyType != RigidbodyType2D.Static
                          && col.GetComponentInParent<SpriteRenderer>() != null;

            return ehAtor ? Papel.Pegada : Papel.Cenario;
        }

        /// <summary>
        /// Compara contra o <b>esperado do papel</b>, e devolve a queixa ou string vazia.
        /// </summary>
        private static string Conferir(Medida m, Vector3 escala)
        {
            var queixas = new List<string>();

            // ESCALA NÃO UNIFORME. Vale para qualquer papel de ator, e para todo círculo:
            //   - num círculo, a física usa o MAIOR eixo e o desenho vira elipse: os dois
            //     discordam, e a área atingível é maior do que parece no eixo curto;
            //   - num ator, o sprite é esticado junto -- pixel art a 32 PPU por fator não
            //     inteiro perde o alinhamento do grid, que é o que a skill de pixel art
            //     protege.
            // Cenário esticado é legítimo (uma parede é uma caixa), então ali não se cobra.
            bool importaEscala = m.Funcao == Papel.Hurtbox || m.Funcao == Papel.Pegada
                                 || m.Raio > 0f;

            if (importaEscala &&
                Mathf.Abs(Mathf.Abs(escala.x) - Mathf.Abs(escala.y)) > ToleranciaDeEscala)
            {
                queixas.Add($"escala não uniforme {escala.x:0.###} × {escala.y:0.###} " +
                            $"(esticado {escala.y / escala.x:0.##}× em Y)");
            }

            // Polygon / Edge / Composite desligado devolve bounds vazio (doc da 6000.4). Zero
            // aqui é "não medido", e acusar -100% de um número que não foi medido é ruído.
            // Devolve o que JÁ se juntou (a escala não depende de ter medido a forma) em vez
            // de string vazia, que descartaria a queixa de escala em silêncio.
            if (m.Tamanho.sqrMagnitude <= 0.0001f)
                return queixas.Count == 0 ? "" : string.Join("; ", queixas);

            if (m.Funcao == Papel.Hurtbox && m.TemSprite)
            {
                var esperado = new Vector2(m.TamanhoDoSprite.x * FatorLarguraDaHurtbox,
                                           m.TamanhoDoSprite.y * FatorAlturaDaHurtbox);

                Comparar(queixas, "largura", m.Tamanho.x, esperado.x);
                Comparar(queixas, "altura", m.Tamanho.y, esperado.y);

                float desvio = Vector2.Distance(m.CentroMundo, m.CentroDoSprite);
                if (desvio > LimiarDeCentro)
                    queixas.Add($"centro a {desvio:0.##} do centro desenhado " +
                                $"(limiar {LimiarDeCentro})");
            }
            else if (m.Funcao == Papel.Pegada)
            {
                // NÃO se compara o tamanho absoluto: ColisoresDoElencoTests já guarda os quatro
                // humanos pelo caminho do prefab, e um chefe legitimamente não tem a pegada de
                // um humano. O que se conforme aqui é a FORMA, que vale para qualquer espécie.

                // 1. A proporção. Uma pegada é área de CHÃO, na razão 2:1 da célula isométrica.
                //    Uma "pegada" mais alta que larga é uma parede em pé no lugar de um chão --
                //    foi assim que a instância do Abdul apareceu com 0,3 × 2,54.
                if (m.Tamanho.x > 0.0001f && m.Tamanho.y > 0.0001f)
                {
                    float razao = m.Tamanho.x / m.Tamanho.y;
                    if (Mathf.Abs(razao - RazaoDeChao) > ToleranciaDeRazao)
                        queixas.Add($"proporção {razao:0.##}:1 — chão isométrico é " +
                                    $"{RazaoDeChao:0}:1 (±{ToleranciaDeRazao:0.#})");
                }

                // 2. A linha do pé. A pegada DEITA no chão; longe do pé ela flutua -- foi assim
                //    que a do Esqueleto ficou com metade abaixo do piso.
                if (m.TemSprite)
                {
                    float pe = m.CentroDoSprite.y - m.TamanhoDoSprite.y * 0.5f;
                    float acima = m.CentroMundo.y - pe;
                    if (Mathf.Abs(acima) > LimiarDeCentro)
                        queixas.Add($"a {acima:+0.##;-0.##} do pé — pegada é área de chão");
                }
            }

            return queixas.Count == 0 ? "" : string.Join("; ", queixas);
        }

        private static void Comparar(List<string> queixas, string eixo, float medido, float esperado)
        {
            if (esperado <= 0.0001f) return;

            float razao = medido / esperado - 1f;
            if (Mathf.Abs(razao) > LimiarDeTamanho)
                queixas.Add($"{eixo} {razao * 100:+0;-0}% do esperado " +
                            $"({medido:0.##} contra {esperado:0.##})");
        }

        private static string Rotulo(Collider2D col) => col switch
        {
            BoxCollider2D => "Box",
            CircleCollider2D => "Circle",
            CapsuleCollider2D => "Capsule",
            PolygonCollider2D => "Polygon",
            EdgeCollider2D => "Edge",
            CompositeCollider2D => "Composite",
            _ => col.GetType().Name,
        };

        private static string Hierarquia(Transform t)
        {
            var partes = new List<string>();
            for (var a = t; a != null; a = a.parent) partes.Add(a.name);
            partes.Reverse();
            return string.Join("/", partes);
        }
    }
}
