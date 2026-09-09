using System.Linq;
using UnityEngine;
using UnityEngine.U2D;
using FavelaAmarela.Core.Camera;
using FavelaAmarela.Runtime.Combat;

namespace FavelaAmarela.CameraSystem
{
    /// <summary>
    /// Orthographic 2D camera controller for isometric top-down view.
    /// Attaches to the Main Camera. Follows a target with smooth damping.
    /// 
    /// SETUP (Inspector):
    ///   1. Drag the player (Damiao) into the "Target" field.
    ///   2. Ensure Main Camera projection is set to "Orthographic".
    ///   3. Adjust "Orthographic Size" for zoom level (e.g., 8-12 for blockout).
    ///   4. Camera Z offset must be negative (default -10) so it renders the 2D scene.
    /// </summary>
    public class IsometricCameraController : MonoBehaviour
    {
        [Header("Target")]
        [SerializeField] private Transform target;

        [Header("Follow Settings")]
        [SerializeField] private float smoothTime = 0.15f;

        [Tooltip("Teto de velocidade da câmera, em unidades por segundo. Impede que ela " +
                 "atravesse a tela num salto quando o alvo teleporta (troca de cena, " +
                 "arremesso da Tempestade). 0 = sem teto.")]
        [SerializeField] private float velocidadeMaxima = 24f;

        [Tooltip("Zona morta como fração da meia-vista: o jogador se move dentro dela sem " +
                 "arrastar a câmera. 0,178 dá 1,33 x 0,75 un na cena padrão. 0 = desligada.")]
        [Range(0f, 0.5f)]
        [SerializeField] private float fracaoDaZonaMorta = 0.178f;

        [Tooltip("Quanto a câmera olha à frente, na direção em que o jogador se move.")]
        [SerializeField] private float antecipacao = 1.8f;

        [Tooltip("Velocidade do alvo em que a antecipação fica cheia. 7,5 é a corrida do Damião.")]
        [SerializeField] private float velocidadeDeAntecipacaoCheia = 7.5f;

        [Tooltip("Segundos para a antecipação alcançar a direção nova. Baixo demais e trocar " +
                 "de direção joga a câmera de um lado ao outro.")]
        [SerializeField] private float suavidadeDaAntecipacao = 0.35f;

        [Header("Camera Configuration")]
        [Tooltip("Z offset keeps camera behind the 2D plane. Must be negative.")]
        [SerializeField] private float zOffset = -10f;

        [Tooltip("Orthographic size controls the zoom level. Smaller = more zoomed in.")]
        [SerializeField] private float orthographicSize = 10f;

        [Header("Limites do mapa")]
        [Tooltip("Área que a câmera pode enquadrar. Vazio = derivada dos Limite_* da cena, " +
                 "como o Véu da Tempestade faz com os cantos.")]
        [SerializeField] private Collider2D limitesDoMapa;

        [Tooltip("Desliga o travamento. Só para cena sem limites, como a Arena de Testes.")]
        [SerializeField] private bool prenderNosLimites = true;

        [Header("Tremor")]
        [Tooltip("Deslocamento máximo, em unidades, com trauma cheio.")]
        [SerializeField] private float amplitudeDoTremor = 0.35f;

        [Tooltip("Quanto trauma se perde por segundo. 1 = o tremor cheio dura um segundo.")]
        [SerializeField] private float decaimentoDoTrauma = 1.6f;

        [Tooltip("Dano que gera trauma quase cheio. 40 é o 'golpe cheio' que o HitStop usa.")]
        [SerializeField] private float danoDeReferencia = 40f;

        [Tooltip("Teto de trauma que UM golpe pode gerar. Abaixo de 1 para dois golpes " +
                 "somarem em vez de saturarem no primeiro.")]
        [Range(0.1f, 1f)]
        [SerializeField] private float traumaPorGolpe = 0.45f;

        private Vector3 velocity = Vector3.zero;
        private UnityEngine.Camera cam;
        private PixelPerfectCamera _pixelPerfect;

        /// <summary>
        /// Onde a câmera está <b>sem</b> o tremor. É daqui que o amortecedor parte.
        ///
        /// <para><b>O defeito que isto conserta (2026-09-09).</b> A versão anterior fazia
        /// <c>transform.position += deslocamento</c> depois do <c>SmoothDamp</c> — e no quadro
        /// seguinte o <c>SmoothDamp</c> partia da posição <b>já sacudida</b>, com o
        /// <c>velocity</c> guardado. O tranco entrava no estado do amortecedor em vez de ficar
        /// por fora dele, e a câmera era arrastada pelo próprio tremor. Ninguém tinha visto
        /// porque o único chamador do tremor era uma cutscene.</para>
        /// </summary>
        private Vector3 _posicaoSeguida;
        private bool _seguindoIniciado;

        private float _trauma;
        private float _relogioDoTremor;
        private Vector2 _limiteMin;
        private Vector2 _limiteMax;
        private bool _temLimites;

        private Vector2 _antecipacaoAtual;
        private Vector2 _velocidadeDaAntecipacao;
        private Rigidbody2D _corpoDoAlvo;

        private void Awake()
        {
            cam = GetComponent<UnityEngine.Camera>();
            if (cam == null)
            {
                Debug.LogError("[IsometricCameraController] No Camera component found!", this);
                return;
            }

            // Force orthographic projection for 2D isometric
            cam.orthographic = true;

            // ── Quem manda no zoom (2026-08-27) ───────────────────────────────
            // Com um PixelPerfectCamera presente, o TAMANHO É DELE: ele recalcula
            // orthographicSize a cada OnPreCull a partir da resolução de referência e da tela
            // real. Escrever aqui não muda nada em jogo e mente no Inspector — o número que
            // este componente mostra deixaria de ser o número que se vê.
            //
            // A própria doc do pacote descreve esse conflito na seção do Cinemachine: dois
            // sistemas disputando orthographicSize "would cause them to fight for control over
            // the Camera and likely produce unwanted results".
            _pixelPerfect = GetComponent<PixelPerfectCamera>();

            if (_pixelPerfect == null) cam.orthographicSize = orthographicSize;

            if (target == null)
                Debug.LogWarning("[IsometricCameraController] No target assigned. Camera will not follow.", this);

            ResolverLimites();
            ResolverCorpoDoAlvo();
        }

        /// <summary>
        /// O corpo do alvo, para ler a velocidade dele. Resolvido uma vez — <c>GetComponent</c>
        /// em <c>LateUpdate</c> seria alocação em hot path (Regra de Ouro §1).
        /// </summary>
        private void ResolverCorpoDoAlvo()
            => _corpoDoAlvo = target != null ? target.GetComponentInParent<Rigidbody2D>() : null;

        private void OnEnable() => HitStop.OnImpacto += AoImpacto;

        private void OnDisable() => HitStop.OnImpacto -= AoImpacto;

        /// <summary>Todo golpe que aterrissa sacode a tela, na medida do dano.</summary>
        private void AoImpacto(float dano)
            => _trauma = EnquadramentoDaCamera.Acumular(
                _trauma,
                EnquadramentoDaCamera.TraumaDeUmGolpe(dano, danoDeReferencia, traumaPorGolpe));

        /// <summary>
        /// A área que a câmera pode enquadrar: a autorada, ou a união dos <c>Limite_*</c> da
        /// cena.
        ///
        /// <para>Derivar é o padrão deste projeto, e pelo mesmo motivo que o
        /// <c>VeuDaTempestade</c> registra: <b>o mapa dobrou de tamanho em 2026-09-01</b>, e
        /// quatro números escritos à mão teriam ficado no meio do mapa novo, em silêncio.</para>
        /// </summary>
        private void ResolverLimites()
        {
            if (!prenderNosLimites) return;

            if (limitesDoMapa != null)
            {
                var b = limitesDoMapa.bounds;
                _limiteMin = b.min;
                _limiteMax = b.max;
                _temLimites = true;
                return;
            }

            var paredes = FindObjectsByType<Collider2D>(FindObjectsInactive.Include,
                                                        FindObjectsSortMode.None)
                .Where(c => c.name.StartsWith("Limite_"))
                .ToArray();

            if (paredes.Length == 0)
            {
                // Sem erro alto: a Arena de Testes e o Menu não têm limites de propósito. O
                // aviso existe para a cena que DEVERIA ter e não tem.
                Debug.LogWarning("[IsometricCameraController] Nenhum 'Limite_*' na cena — a " +
                                 "câmera vai seguir o alvo sem travar, e pode mostrar o vazio " +
                                 "além da borda do mapa.", this);
                return;
            }

            Bounds uniao = paredes[0].bounds;
            for (int i = 1; i < paredes.Length; i++) uniao.Encapsulate(paredes[i].bounds);

            _limiteMin = uniao.min;
            _limiteMax = uniao.max;
            _temLimites = true;
        }

        private void LateUpdate()
        {
            if (target == null) return;

            if (!_seguindoIniciado)
            {
                _posicaoSeguida = transform.position;
                _seguindoIniciado = true;
            }

            // ── antecipação: olhar para onde ele vai, não para onde ele está ──
            Vector2 desejada = EnquadramentoDaCamera.AntecipacaoDesejada(
                _corpoDoAlvo != null ? _corpoDoAlvo.linearVelocity : Vector2.zero,
                antecipacao, velocidadeDeAntecipacaoCheia);

            _antecipacaoAtual = Vector2.SmoothDamp(
                _antecipacaoAtual, desejada, ref _velocidadeDaAntecipacao,
                suavidadeDaAntecipacao);

            // A meia-vista serve a DUAS contas — a zona morta, que é uma fração dela, e o
            // travamento nos limites. Calculada uma vez só.
            float meiaAltura = cam != null ? cam.orthographicSize : 0f;
            float meiaLargura = cam != null ? meiaAltura * cam.aspect : 0f;

            Vector2 alvo = (Vector2)target.position + _antecipacaoAtual;

            // ── zona morta: passo curto não arrasta a câmera ──
            alvo = EnquadramentoDaCamera.AlvoComZonaMorta(
                alvo, _posicaoSeguida,
                EnquadramentoDaCamera.MeiaExtensaoDaZonaMorta(
                    meiaLargura, meiaAltura, fracaoDaZonaMorta));

            var targetPosition = new Vector3(alvo.x, alvo.y, zOffset);

            _posicaoSeguida = velocidadeMaxima > 0f
                ? Vector3.SmoothDamp(_posicaoSeguida, targetPosition, ref velocity,
                                     smoothTime, velocidadeMaxima)
                : Vector3.SmoothDamp(_posicaoSeguida, targetPosition, ref velocity, smoothTime);

            if (_temLimites && cam != null)
            {
                Vector2 presa = EnquadramentoDaCamera.Prender(
                    _posicaoSeguida, _limiteMin, _limiteMax, meiaLargura, meiaAltura);

                _posicaoSeguida = new Vector3(presa.x, presa.y, zOffset);
            }

            transform.position = _posicaoSeguida + (Vector3)DeslocamentoDoTremor();
        }

        /// <summary>
        /// O tremor deste quadro, e o decaimento do trauma.
        ///
        /// <para><b>Em tempo REAL, não escalado.</b> O <c>HitStop</c> põe o <c>timeScale</c> em
        /// 0,05 no impacto — que é exatamente quando o tremor acontece. Medido em
        /// <c>Time.deltaTime</c>, um tremor de 0,3 s duraria <b>seis segundos</b> de relógio.
        /// Em tempo real ele atravessa o congelamento na velocidade certa, que é o efeito que se
        /// quer: o mundo para e a tela treme.</para>
        ///
        /// <para>Mas <b>a pausa continua congelando</b>: com o <c>timeScale</c> no chão por menu
        /// ou Colapso, o trauma nem decai nem desloca. Tremer atrás de um menu de pausa não é
        /// dramático, é defeito.</para>
        /// </summary>
        private Vector2 DeslocamentoDoTremor()
        {
            if (_trauma <= 0f) return Vector2.zero;
            if (Time.timeScale <= LimiarDePausa) return Vector2.zero;

            _trauma = EnquadramentoDaCamera.Decair(
                _trauma, Time.unscaledDeltaTime, decaimentoDoTrauma);

            // Normaliza pelo zoom: as cenas rodam em orthographicSize 4,21875 e 5,625, e um
            // deslocamento fixo em unidades apareceria 33% menor na mais afastada.
            float fator = cam != null && cam.orthographicSize > 0f
                ? cam.orthographicSize / TamanhoDeReferencia
                : 1f;

            _relogioDoTremor += Time.unscaledDeltaTime;

            var ruido = EnquadramentoDaCamera.Ruido(_relogioDoTremor, FrequenciaDoTremor);

            return EnquadramentoDaCamera.Deslocamento(
                _trauma, amplitudeDoTremor, ruido, fator);
        }

        /// <summary>Mesmo limiar do <c>HitStop</c>: abaixo disto o jogo está pausado por outro.</summary>
        private const float LimiarDePausa = 0.01f;

        /// <summary>O <c>orthographicSize</c> mais comum do projeto (4 cenas de 6).</summary>
        private const float TamanhoDeReferencia = 4.21875f;

        /// <summary>
        /// Oscilações por segundo do ruído do tremor. 22 fica no ponto em que ele lê como
        /// pancada; muito acima disso o Perlin volta a parecer sorteio por quadro, que é o
        /// defeito que ele veio consertar.
        /// </summary>
        private const float FrequenciaDoTremor = 22f;

        /// <summary>
        /// Sacode a câmera por <paramref name="duration"/> segundos, com deslocamento
        /// aleatório de até <paramref name="magnitude"/> unidades por frame. Reaproveitável
        /// por qualquer evento de impacto (ex.: chão desmoronando na queda Z4→Z5).
        /// </summary>
        public void Shake(float duration, float magnitude)
        {
            // A assinatura fica: o QuedaZ4Z5Trigger a usa e mudá-la seria retrabalho sem ganho.
            // O que mudou é o modelo por baixo -- de "duração + magnitude constante" para
            // trauma que decai. `duration` vira quanto trauma acumular, considerando o
            // decaimento.
            if (duration <= 0f || magnitude <= 0f) return;

            // `magnitude` NÃO escreve mais em amplitudeDoTremor. A versão anterior fazia
            // `amplitudeDoTremor = Max(amplitudeDoTremor, magnitude)` -- mutação PERMANENTE de
            // um campo serializado a partir do argumento de uma chamada. Um Shake alto deixaria
            // todos os golpes do resto da cena tremendo mais forte, para sempre e em silêncio.
            // Hoje isso não acontece por sorte (o único chamador pede 0,15 contra 0,35 de
            // amplitude, então o Max é no-op), e é exatamente esse tipo de armadilha que espera
            // o segundo chamador.
            if (magnitude > amplitudeDoTremor)
                Debug.LogWarning($"[IsometricCameraController] Shake pediu magnitude " +
                                 $"{magnitude}, acima da amplitude do tremor " +
                                 $"({amplitudeDoTremor}). O tremor sai no teto. Para um evento " +
                                 "mais violento, suba 'Amplitude Do Tremor' no Inspector.", this);

            AcrescentarTrauma(Mathf.Clamp01(duration * decaimentoDoTrauma));
        }

        /// <summary>
        /// Acrescenta trauma à câmera: a entrada pública do tremor, para eventos que não são
        /// golpe.
        ///
        /// <para>Golpe já entra sozinho por <c>HitStop.OnImpacto</c>. Isto é para o resto —
        /// a aterrissagem do Byakhee, um portão batendo, o rugido do Rei em Amarelo. Sem ela, o
        /// único caminho era o <c>Shake</c> legado, que fala em duração e magnitude e não em
        /// trauma.</para>
        ///
        /// <para>Acumula com teto: dois eventos próximos somam em vez de o segundo reiniciar o
        /// primeiro.</para>
        /// </summary>
        /// <param name="quantidade">De 0 a 1. 1 é o tremor cheio.</param>
        public void AcrescentarTrauma(float quantidade)
            => _trauma = EnquadramentoDaCamera.Acumular(_trauma, quantidade);

        /// <summary>
        /// Muda o zoom em tempo de execução (a ideia original era o efeito do Salto Dimensional).
        ///
        /// <para><b>Não faz efeito com o <c>PixelPerfectCamera</c> ligado</b>, e o aviso é
        /// deliberado: o componente reescreve <c>orthographicSize</c> a cada quadro, então um
        /// zoom por aqui seria desfeito no mesmo frame e o efeito simplesmente não aconteceria —
        /// sem erro nenhum. Quando o Salto Dimensional for ganhar zoom de verdade, o caminho é a
        /// resolução de referência do <c>PixelPerfectCamera</c>, não este método. Hoje ele não
        /// tem um único chamador em produção.</para>
        /// </summary>
        public void SetZoom(float newSize)
        {
            orthographicSize = Mathf.Max(1f, newSize);

            if (_pixelPerfect != null)
            {
                Debug.LogWarning($"[IsometricCameraController] SetZoom({newSize}) ignorado: o " +
                                 "PixelPerfectCamera reescreve o tamanho a cada quadro. Mude a " +
                                 "resolução de referência dele em vez disso.", this);
                return;
            }

            if (cam != null) cam.orthographicSize = orthographicSize;
        }

        /// <summary>
        /// Sets a new target for the camera to follow.
        /// </summary>
        public void SetTarget(Transform newTarget)
        {
            target = newTarget;
            ResolverCorpoDoAlvo();
        }
    }
}
