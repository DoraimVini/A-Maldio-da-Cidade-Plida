using System.Collections.Generic;
using System.Diagnostics;
using UnityEngine;
using UnityEngine.InputSystem;
using Debug = UnityEngine.Debug;

namespace FavelaAmarela.Runtime.Diagnostico
{
    /// <summary>
    /// Desenha, por cima do jogo, <b>onde o combate realmente acontece</b>: as áreas que recebem
    /// dano, as que causam, e as zonas de gatilho.
    ///
    /// <para><b>Por que isto não é redundante com os Gizmos que já existem.</b> Sete scripts do
    /// projeto desenham gizmos, mas todos em <c>OnDrawGizmosSelected</c> — só aparecem para o
    /// objeto <b>selecionado no Inspector</b>, um de cada vez, e nunca durante uma luta. Este
    /// desenha <b>tudo ao mesmo tempo</b>, enquanto o jogo roda.</para>
    ///
    /// <para><b>E o mais importante: neste projeto a hitbox NÃO É UM COLISOR.</b> Ela é uma
    /// consulta (<c>Physics2D.OverlapCircle</c> com <c>ContactFilter2D</c>) rodada a cada
    /// <c>FixedUpdate</c> enquanto a janela do golpe está aberta — ver <see cref="Combat.Hitbox"/>
    /// e o motivo lá. Uma varredura de <c>Collider2D</c> acharia todas as hurtboxes e
    /// <b>nenhuma hitbox</b>. Por isso existe <see cref="RegistrarCirculo"/>: o código de combate
    /// avisa a geometria que acabou de consultar, e ela aparece com a forma, o tamanho e a
    /// posição <b>exatos</b> que a física usou.</para>
    ///
    /// <para><b>Ligar e desligar:</b> <see cref="Mostrar"/> é estático e público; a tecla
    /// <b>F11</b> alterna em jogo, e <b>Shift+F11</b> despeja no console a auditoria de
    /// colisores da cena (<see cref="AuditarColisoresDaCena"/>). (F1–F4 são as Relíquias e F12
    /// é o <see cref="ConsoleDeCarcosa"/>.)</para>
    ///
    /// <para><b>Custo em build de release: zero.</b> Os métodos de registro carregam
    /// <see cref="ConditionalAttribute"/>, então o <b>compilador apaga as chamadas</b> fora do
    /// Editor e de build de desenvolvimento — o código de combate não paga nem o teste do
    /// <c>if</c>.</para>
    ///
    /// <para><b>Roda em Edit mode também</b> (<see cref="ExecuteAlways"/>): em Play ele se
    /// auto-instancia e não precisa estar em cena nenhuma; em Edit mode basta pôr o componente
    /// num GameObject e o Scene view passa a desenhar as hurtboxes e os gatilhos sem entrar no
    /// jogo. As marcas de golpe, essas, só existem em Play — quem as registra é o código de
    /// combate.</para>
    /// </summary>
    [ExecuteAlways]
    [AddComponentMenu("Favela Amarela/Diagnóstico/Visualizador de Golpes")]
    public sealed class VisualizadorDeGolpes : MonoBehaviour
    {
        /// <summary>Liga o desenho. Estático para o código de combate consultar sem referência.</summary>
        public static bool Mostrar;

        // ── cores ────────────────────────────────────────────────────────────
        private static readonly Color Verde = new Color(0.30f, 1f, 0.40f, 0.90f);
        private static readonly Color Vermelho = new Color(1f, 0.25f, 0.20f, 0.95f);
        private static readonly Color Azul = new Color(0.35f, 0.65f, 1f, 0.75f);
        private static readonly Color Cinza = new Color(0.75f, 0.75f, 0.70f, 0.45f);
        private static readonly Color Apagada = new Color(1f, 0.85f, 0.20f, 0.90f);

        [Header("O que desenhar")]
        [Tooltip("Áreas que RECEBEM dano (componente Hurtbox, ou camadas PlayerHurtbox/EnemyHurtbox).")]
        [SerializeField] private bool hurtboxes = true;

        [Tooltip("Áreas que CAUSAM dano — só aparecem quando o código de combate as registra.")]
        [SerializeField] private bool hitboxes = true;

        [Tooltip("Gatilhos que não são hurtbox: zonas, portais, coletáveis, interação.")]
        [SerializeField] private bool zonasDeGatilho = true;

        [Tooltip("Colisores SÓLIDOS — a pegada de movimento. Foi olhando para isto que se " +
                 "descobriu a pegada do Esqueleto com metade abaixo do chão.")]
        [SerializeField] private bool pegadasDeMovimento;

        [Header("Comportamento")]
        [Tooltip("Liga o desenho. Em Play define o estado inicial (F11 alterna depois); em " +
                 "Edit mode é o próprio interruptor, porque não há tecla para ler.")]
        [SerializeField] private bool ligadoAoIniciar;

        /// <summary>
        /// Em Edit mode a caixa do Inspector É o interruptor: sem Play não há
        /// <c>Keyboard.current</c> nem árbitro de foco, e marcar a caixa tem de acender na hora.
        /// </summary>
        private void OnValidate()
        {
            if (!Application.isPlaying) Mostrar = ligadoAoIniciar;
        }

        [Tooltip("Segundos que um golpe registrado fica na tela, no MÍNIMO. Ver o doc de " +
                 "RegistrarCirculo: com 0 os golpes instantâneos piscam por um quadro e não " +
                 "dá para ver nenhum deles.")]
        [Min(0f)]
        [SerializeField] private float permanenciaMinima = 0.25f;

        /// <summary>Uma geometria de golpe avisada pelo código de combate.</summary>
        private struct Marca
        {
            public Vector2 Centro;
            public float Raio;        // > 0: círculo
            public Vector2 Tamanho;   // usado quando Raio <= 0
            public Color Cor;
            public float Expira;
        }

        // Lista compartilhada: o registro é estático porque quem chama não tem (nem deve ter)
        // referência a este componente. Capacidade inicial generosa para não realocar em luta.
        private static readonly List<Marca> _marcas = new List<Marca>(64);
        private static float _permanencia = 0.25f;

        private static VisualizadorDeGolpes _instancia;

        /// <summary>
        /// O relógio da expiração, <b>por modo</b>.
        ///
        /// <para><c>Time.time</c> é escalado por <c>Time.timeScale</c> e conta a partir do
        /// início da aplicação — em Edit mode ele não avança de forma útil, e uma marca
        /// registrada fora do Play ou nunca expiraria ou expiraria no mesmo quadro.
        /// <c>EditorApplication.timeSinceStartup</c> é o relógio do Editor e, segundo a doc da
        /// 6000.4, <b>não é zerado ao entrar em Play</b>.</para>
        ///
        /// <para>Registro e poda passam os dois por aqui, então dentro de um mesmo modo as
        /// contas fecham. Na troca de modo a lista é esvaziada (ver <c>Awake</c>), porque as
        /// duas origens de tempo não são comparáveis entre si.</para>
        /// </summary>
        private static float Agora
        {
            get
            {
#if UNITY_EDITOR
                if (!Application.isPlaying) return (float)UnityEditor.EditorApplication.timeSinceStartup;
#endif
                return Time.time;
            }
        }

#if UNITY_EDITOR || DEVELOPMENT_BUILD
        // Cache da varredura de colisores: OnDrawGizmos roda todo quadro, e um
        // FindObjectsByType por quadro numa cena do tamanho do Castelo pesa até no Editor.
        //
        // A VARREDURA INTEIRA VIVE ATRÁS DESTE #if de propósito: Assets/Scripts/CLAUDE.md
        // proíbe Find* em código de produção, e com razão. Aqui ela é ferramenta de
        // diagnóstico, e some do binário de release junto com o desenho.
        private Collider2D[] _colisores = System.Array.Empty<Collider2D>();
        private float _proximaVarredura;
        private const float IntervaloDeVarredura = 0.5f;
#endif

        /// <summary>
        /// Sobe sozinho, como o <see cref="ConsoleDeCarcosa"/> e o <c>GerenciadorDeSave</c> —
        /// nenhuma cena precisa hospedá-lo, então ele não pode ser esquecido numa delas.
        /// </summary>
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void GarantirInstancia()
        {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            if (_instancia != null) return;

            var go = new GameObject("[Visualizador de Golpes]");
            go.AddComponent<VisualizadorDeGolpes>();
#endif
        }

        private void Awake()
        {
            if (_instancia != null && _instancia != this)
            {
                Destroy(gameObject);
                return;
            }

            _instancia = this;

            // DontDestroyOnLoad só existe em Play: chamá-lo em Edit mode reclama no console e
            // não faz nada. Em Edit mode o componente vive no GameObject onde foi posto.
            if (Application.isPlaying) DontDestroyOnLoad(gameObject);

            // As marcas de um modo não valem no outro: os dois relógios (Time.time e
            // EditorApplication.timeSinceStartup) têm origens diferentes, então um Expira
            // gravado em Edit mode leria como passado remoto ou futuro distante em Play.
            _marcas.Clear();

            Mostrar = ligadoAoIniciar;
            _permanencia = permanenciaMinima;
        }

        private void OnDestroy()
        {
            if (_instancia == this) _instancia = null;
        }

        private void Update()
        {
            _permanencia = permanenciaMinima;

            // A PODA VEM PRIMEIRO, antes de qualquer return. Ela vive aqui e não no desenho
            // porque OnDrawGizmos não roda com os Gizmos desligados na Game view — e se ela
            // ficasse depois do árbitro, um painel aberto a desligaria e a lista cresceria
            // calada, que é exatamente o que este comentário existe para impedir.
            PodarExpiradas();

            // Daqui para baixo é só Play. Em Edit mode não há teclado de jogo nem árbitro de
            // foco para consultar, e o toggle é a caixa no Inspector.
            if (!Application.isPlaying) return;

            // O ÁRBITRO VEM ANTES DA TECLA. Leitura crua de teclado ignora mapa de ação,
            // painel aberto e Time.timeScale — foi assim que digitar "3" no console consumia o
            // item do slot 3. O ConsoleDeCarcosa TOMA o foco porque congela o jogo; este aqui
            // só alterna um bool, então basta consultar. Guardado por
            // LeitorDeTeclaRespeitaOFocoTests.
            if (!Entrada.ArbitroDeFoco.JogoNoComando) return;

            // Input System novo: o projeto está em activeInputHandler 1, então Input.GetKeyDown
            // e KeyCode nem existem em runtime aqui.
            var teclado = Keyboard.current;
            if (teclado == null || !teclado.f11Key.wasPressedThisFrame) return;

            // Shift+F11 audita em vez de alternar: a mesma tecla, porque quem quer conferir
            // geometria de colisor e quem quer ver a hitbox desenhada é a mesma pessoa no
            // mesmo momento.
            bool shift = teclado.leftShiftKey.isPressed || teclado.rightShiftKey.isPressed;
            if (shift) AuditarColisoresDaCena();
            else Alternar();
        }

        /// <summary>
        /// Mede todo colisor da cena carregada e joga o resultado no console — a metade
        /// <b>runtime</b> da auditoria de colisores, sendo a outra o
        /// <c>Rigidbody2DAuditor</c> do Editor.
        ///
        /// <para><b>Por que as duas existem.</b> A do Editor vê prefabs e cenas fechadas, mas vê
        /// o objeto <b>como está no disco</b>. Esta aqui vê o que o jogo montou: hurtbox criada
        /// em <c>Awake</c> por <c>Hurtbox.GarantirPara</c>, colisor desligado por i-frame,
        /// inimigo instanciado por spawner. A conta é a mesma classe
        /// (<see cref="AuditoriaDeColisores"/>) nas duas — de propósito, para não haver duas
        /// versões da mesma medida divergindo em silêncio.</para>
        /// </summary>
        [Conditional("UNITY_EDITOR"), Conditional("DEVELOPMENT_BUILD")]
        public static void AuditarColisoresDaCena()
        {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            var colisores = FindObjectsByType<Collider2D>(
                FindObjectsInactive.Include, FindObjectsSortMode.None);

            var texto = new System.Text.StringBuilder();
            texto.AppendLine($"[AuditoriaDeColisores] {colisores.Length} colisor(es) na cena.");

            int queixosos = 0;
            for (int i = 0; i < colisores.Length; i++)
            {
                var m = AuditoriaDeColisores.Medir(colisores[i]);
                if (string.IsNullOrEmpty(m.Queixa)) continue;

                queixosos++;
                texto.AppendLine($"  {m.Caminho} [{m.Tipo}, {m.Funcao}] — {m.Queixa}");
            }

            texto.Append(queixosos == 0
                ? "  nada fora do esperado para o papel de cada colisor."
                : $"  {queixosos} fora do esperado.");

            Debug.Log(texto.ToString());
#endif
        }

        /// <summary>Liga/desliga o desenho. Exposto para o Console e para atalhos futuros.</summary>
        public static void Alternar()
        {
            Mostrar = !Mostrar;
            Debug.Log($"[VisualizadorDeGolpes] {(Mostrar ? "LIGADO" : "desligado")} — " +
                      "verde recebe dano, vermelho causa, azul é gatilho.");
        }

        // ── registro, chamado pelo código de combate ─────────────────────────

        /// <summary>
        /// Avisa que um golpe consultou um <b>círculo</b> — a forma que
        /// <c>Physics2D.OverlapCircle</c> realmente usa neste projeto.
        ///
        /// <para><b>Por que existe:</b> a hitbox daqui não é um <c>Collider2D</c>, é uma
        /// consulta. Varrer colisores acharia zero hitboxes. Só o próprio código de combate sabe
        /// o centro e o raio que passou para a física — então é ele quem avisa.</para>
        ///
        /// <para><b>Por que NÃO se limpa a lista todo quadro.</b> Era o desenho óbvio, e ele
        /// esconde justamente o que interessa: um golpe instantâneo (Cultista, Esqueleto,
        /// Sseth — ver a auditoria de hitbox) existe por <b>um</b> quadro, e um quadro a 60 fps
        /// é invisível a olho nu. As marcas expiram por tempo, com um piso de
        /// <see cref="permanenciaMinima"/>, para que um golpe de janela zero apareça na tela
        /// tanto quanto um de janela longa — e a diferença entre eles fique legível.</para>
        ///
        /// <para>Some por completo em build de release: o <see cref="ConditionalAttribute"/>
        /// faz o compilador apagar a chamada.</para>
        /// </summary>
        /// <param name="centro">Centro em coordenadas de mundo, o mesmo passado à física.</param>
        /// <param name="raio">Raio em unidades de mundo, o mesmo passado à física.</param>
        /// <param name="cor">Cor da marca. Use <see cref="CorDeGolpe"/> para o padrão.</param>
        /// <param name="duracao">Quanto fica na tela. Menor que o piso, usa o piso.</param>
        [Conditional("UNITY_EDITOR"), Conditional("DEVELOPMENT_BUILD")]
        public static void RegistrarCirculo(Vector2 centro, float raio, Color cor,
                                            float duracao = 0f)
        {
            if (!Mostrar) return;

            _marcas.Add(new Marca
            {
                Centro = centro,
                Raio = Mathf.Max(0.01f, raio),
                Cor = cor,
                Expira = Agora + Mathf.Max(duracao, _permanencia),
            });
        }

        /// <summary>
        /// Igual a <see cref="RegistrarCirculo"/>, para golpes que consultam uma
        /// <b>caixa</b> (<c>Physics2D.OverlapBox</c>).
        /// </summary>
        /// <param name="centro">Centro em coordenadas de mundo.</param>
        /// <param name="tamanho">Largura e altura totais, em unidades de mundo.</param>
        /// <param name="cor">Cor da marca.</param>
        /// <param name="duracao">Quanto fica na tela. Menor que o piso, usa o piso.</param>
        [Conditional("UNITY_EDITOR"), Conditional("DEVELOPMENT_BUILD")]
        public static void RegistrarCaixa(Vector2 centro, Vector2 tamanho, Color cor,
                                          float duracao = 0f)
        {
            if (!Mostrar) return;

            _marcas.Add(new Marca
            {
                Centro = centro,
                Raio = 0f,
                Tamanho = tamanho,
                Cor = cor,
                Expira = Agora + Mathf.Max(duracao, _permanencia),
            });
        }

        /// <summary>Vermelho padrão de "isto causa dano", para quem registra não escolher cor.</summary>
        public static Color CorDeGolpe => Vermelho;

        private static void PodarExpiradas()
        {
            float agora = Agora;
            for (int i = _marcas.Count - 1; i >= 0; i--)
                if (_marcas[i].Expira <= agora) _marcas.RemoveAt(i);
        }

        // ── desenho ──────────────────────────────────────────────────────────
        //
        // Tudo daqui para baixo some do binário de release: OnDrawGizmos só roda no
        // Editor, e a varredura de colisores usa Find*, proibido em produção.
#if UNITY_EDITOR || DEVELOPMENT_BUILD

        private void OnDrawGizmos()
        {
            if (!Mostrar) return;

            if (Application.isPlaying && Agora >= _proximaVarredura)
            {
                _proximaVarredura = Agora + IntervaloDeVarredura;
                _colisores = FindObjectsByType<Collider2D>(
                    FindObjectsInactive.Exclude, FindObjectsSortMode.None);
            }
            else if (!Application.isPlaying)
            {
                _colisores = FindObjectsByType<Collider2D>(
                    FindObjectsInactive.Exclude, FindObjectsSortMode.None);
            }

            // Poda TAMBÉM aqui. Em Edit mode o Update de um [ExecuteAlways] só roda quando o
            // Editor resolve repintar — pode ficar minutos parado com o mouse fora da janela.
            // Sem esta segunda poda, uma marca registrada fora do Play ficaria na tela até
            // alguém mexer em alguma coisa.
            PodarExpiradas();

            DesenharColisores();
            DesenharMarcas();

            Gizmos.matrix = Matrix4x4.identity;
        }

        private void DesenharColisores()
        {
            for (int i = 0; i < _colisores.Length; i++)
            {
                var col = _colisores[i];
                if (col == null) continue;

                bool ehHurtbox = col.GetComponent<Combat.Hurtbox>() != null
                                 || col.gameObject.layer == CamadaHurtboxJogador
                                 || col.gameObject.layer == CamadaHurtboxInimigo;

                Color cor;
                if (ehHurtbox)
                {
                    if (!hurtboxes) continue;
                    // Colisor desligado numa hurtbox é i-frame acontecendo (EsquivaBridge
                    // desliga o colisor por 0,15 s). Ver isso é metade do valor da ferramenta.
                    cor = col.enabled ? Verde : Apagada;
                }
                else if (col.isTrigger)
                {
                    if (!zonasDeGatilho) continue;
                    cor = Azul;
                }
                else
                {
                    if (!pegadasDeMovimento) continue;
                    cor = Cinza;
                }

                Gizmos.color = cor;
                DesenharForma(col);
            }
        }

        /// <summary>
        /// Desenha em espaço LOCAL, com a matriz do transform.
        ///
        /// <para>Deliberadamente <b>não</b> usa <c>Collider2D.bounds</c>, apesar de ele ser
        /// mundo e exato: a doc da 6000.4 diz que <c>bounds</c> fica <b>vazio quando o colisor
        /// está desligado</b> — e é exatamente durante os i-frames da Esquiva que a hurtbox do
        /// Damião fica desligada. Pela matriz, ela continua desenhável.</para>
        ///
        /// <para><b>Ressalva, e ela MORDE aqui.</b> Sob escala não uniforme um círculo
        /// desenhado pela matriz vira elipse, enquanto a física continua tratando como círculo
        /// do maior eixo. Este doc dizia que "todo o elenco deste projeto está em escala
        /// uniforme (medido em 2026-09-03)" — <b>é falso</b>. A auditoria de colisores de
        /// 2026-09-04 mediu o contrário: <b>nenhum</b> ator instanciado em cena tem escala
        /// uniforme (Abdul em 1,162 × 2,671; os Cultistas em 0,630 × 0,804; Cassilda em
        /// 1,478 × 1,925). A medição anterior olhou a raiz dos prefabs, não as instâncias — e
        /// é na instância que a escala é sobrescrita. Ou seja: para os colisores circulares
        /// destes atores, <b>o desenho mente hoje</b>, e mente para menos no eixo curto.</para>
        /// </summary>
        private static void DesenharForma(Collider2D col)
        {
            Gizmos.matrix = col.transform.localToWorldMatrix;

            switch (col)
            {
                case BoxCollider2D caixa:
                    Gizmos.DrawWireCube(caixa.offset, caixa.size);
                    break;

                case CircleCollider2D circulo:
                    DesenharCirculoLocal(circulo.offset, circulo.radius);
                    break;

                case CapsuleCollider2D capsula:
                    // Aproximação por caixa: a cápsula do projeto é sempre bem mais alta que
                    // larga (Damião, Byakhee), e a caixa comunica altura e largura sem custar
                    // um desenho de arco.
                    Gizmos.DrawWireCube(capsula.offset, capsula.size);
                    break;

                default:
                    // Polygon, Edge, Composite: a caixa envolvente diz onde está, que é o que
                    // esta ferramenta precisa responder.
                    Gizmos.matrix = Matrix4x4.identity;
                    var b = col.bounds;
                    if (b.size.sqrMagnitude > 0f) Gizmos.DrawWireCube(b.center, b.size);
                    break;
            }

            Gizmos.matrix = Matrix4x4.identity;
        }

        /// <summary>Círculo no plano XY. <c>DrawWireSphere</c> desenha três anéis e polui a tela.</summary>
        private static void DesenharCirculoLocal(Vector2 centro, float raio, int lados = 28)
        {
            Vector3 anterior = centro + new Vector2(raio, 0f);
            for (int i = 1; i <= lados; i++)
            {
                float a = i / (float)lados * Mathf.PI * 2f;
                Vector3 atual = centro + new Vector2(Mathf.Cos(a) * raio, Mathf.Sin(a) * raio);
                Gizmos.DrawLine(anterior, atual);
                anterior = atual;
            }
        }

        private void DesenharMarcas()
        {
            if (!hitboxes) return;

            Gizmos.matrix = Matrix4x4.identity;

            for (int i = 0; i < _marcas.Count; i++)
            {
                var m = _marcas[i];
                Gizmos.color = m.Cor;

                if (m.Raio > 0f) DesenharCirculoLocal(m.Centro, m.Raio);
                else Gizmos.DrawWireCube(m.Centro, m.Tamanho);
            }
        }

        // As camadas foram conferidas no TagManager.asset em 2026-09-03. As camadas 11 e 12
        // estão VAZIAS — o doc do Hurtbox afirma que PlayerHitbox e EnemyHitbox moram lá, e
        // não é verdade. Não faz falta: a hitbox daqui é consulta, não colisor.
        private const int CamadaHurtboxJogador = 13;
        private const int CamadaHurtboxInimigo = 14;

#endif
    }
}
