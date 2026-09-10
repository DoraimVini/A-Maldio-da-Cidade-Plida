using System.Globalization;
using System.Text;
using UnityEngine;

namespace FavelaAmarela.Runtime.Diagnostico
{
    /// <summary>
    /// Diagnóstico: grava, por alguns segundos, tudo que pode fazer um sprite parecer "virar de um
    /// lado para outro" — <c>localScale.x</c>, <c>SpriteRenderer.flipX</c>,
    /// <c>Rigidbody2D.linearVelocity</c> e <c>position.x</c> — a cada quadro, e no fim conta as
    /// trocas de sinal e a amplitude.
    ///
    /// <para><b>Para que serve (pedido do Vini, 2026-09-10).</b> Separar, com números, as três
    /// famílias de causa de um sprite que oscila: (1) duas fontes escrevendo o <i>facing</i>
    /// (<c>flipX</c>/<c>localScale.x</c> piscam), (2) jitter de física (<c>position.x</c> inverte a
    /// direção a cada quadro com amplitude pequena), (3) nada disso — e então é a arte.
    /// Anexar ao objeto suspeito, dar Play, ler o Console.</para>
    ///
    /// <para><b>Sem dependências</b>: só <c>UnityEngine</c>. Os buffers nascem no <c>Awake</c>
    /// com capacidade fixa (Regra de Ouro 1: nada alocado no <c>Update</c>); se o quadro correr
    /// mais rápido que <see cref="amostrasPorSegundoMax"/>, a gravação para cedo e o relatório
    /// avisa.</para>
    ///
    /// <para>Não depende de <c>Time.timeScale</c>: o relógio é o tempo real, para o relatório sair
    /// mesmo com o jogo pausado — nesse caso ele só dirá que nada se mexeu.</para>
    /// </summary>
    [DisallowMultipleComponent]
    [AddComponentMenu("Favela Amarela/Diagnóstico/Detector de Oscilação")]
    public sealed class DetectorDeOscilacao : MonoBehaviour
    {
        [Header("Janela")]
        [Tooltip("Segundos de gravação (tempo real).")]
        [Min(0.1f)]
        [SerializeField] private float duracao = 2f;

        [Tooltip("Segundos de espera antes de começar a gravar — para deixar a luta começar, por exemplo.")]
        [Min(0f)]
        [SerializeField] private float atrasoInicial = 0f;

        [Tooltip("Começa sozinho ao ativar. Desligado, alguém chama Iniciar().")]
        [SerializeField] private bool comecarAoAtivar = true;

        [Header("Veredito")]
        [Tooltip("Acima de tantas trocas de flipX/localScale.x na janela, o facing está disputado.")]
        [Min(0)]
        [SerializeField] private int limiteDePiscadas = 2;

        [Tooltip("Teto de quadros por segundo que o buffer suporta. Acima disso a gravação para cedo.")]
        [Min(30)]
        [SerializeField] private int amostrasPorSegundoMax = 1000;

        private const string Marcador = "[DetectorDeOscilacao]";

        /// <summary>O aviso pedido: facing escrito por mais de uma fonte.</summary>
        public const string AvisoDeFacingDisputado = "Facing está sendo reescrito por múltiplas fontes.";

        private SpriteRenderer _sprite;
        private Rigidbody2D _rb;

        private float[] _escalaX;
        private bool[] _flipX;
        private float[] _velX;
        private float[] _velY;
        private float[] _posX;
        private int _n;

        private float _relogio;
        private bool _gravando;
        private bool _esperando;

        /// <summary>Se o relatório já saiu.</summary>
        public bool Concluido { get; private set; }

        /// <summary>O último relatório emitido, para quem prefere ler por código.</summary>
        public string UltimoRelatorio { get; private set; }

        /// <summary>Quantos quadros foram gravados.</summary>
        public int Amostras => _n;

        private void Awake()
        {
            _sprite = GetComponent<SpriteRenderer>();
            _rb = GetComponent<Rigidbody2D>();

            if (_sprite == null)
                Debug.LogWarning($"{Marcador} '{name}' sem SpriteRenderer — flipX não será gravado.", this);
            if (_rb == null)
                Debug.LogWarning($"{Marcador} '{name}' sem Rigidbody2D — a velocidade não será gravada.", this);

            int capacidade = Mathf.CeilToInt(duracao * amostrasPorSegundoMax) + 16;
            _escalaX = new float[capacidade];
            _flipX = new bool[capacidade];
            _velX = new float[capacidade];
            _velY = new float[capacidade];
            _posX = new float[capacidade];
        }

        private void OnEnable()
        {
            if (comecarAoAtivar) Iniciar();
        }

        /// <summary>(Re)começa a gravação do zero, respeitando o atraso inicial.</summary>
        public void Iniciar()
        {
            _n = 0;
            _relogio = 0f;
            Concluido = false;
            UltimoRelatorio = null;
            _esperando = atrasoInicial > 0f;
            _gravando = !_esperando;
        }

        private void Update()
        {
            if (Concluido) return;

            float dt = Time.unscaledDeltaTime;

            if (_esperando)
            {
                _relogio += dt;
                if (_relogio < atrasoInicial) return;
                _esperando = false;
                _gravando = true;
                _relogio = 0f;
            }

            if (!_gravando) return;

            if (_n < _escalaX.Length)
            {
                _escalaX[_n] = transform.localScale.x;
                _flipX[_n] = _sprite != null && _sprite.flipX;
                var v = _rb != null ? _rb.linearVelocity : Vector2.zero;
                _velX[_n] = v.x;
                _velY[_n] = v.y;
                _posX[_n] = transform.position.x;
                _n++;
            }

            _relogio += dt;
            if (_relogio >= duracao || _n >= _escalaX.Length) Concluir();
        }

        private void Concluir()
        {
            _gravando = false;
            Concluido = true;

            var r = Analisar(_escalaX, _flipX, _velX, _velY, _posX, _n);
            UltimoRelatorio = Relatorio(r);

            Debug.Log(UltimoRelatorio, this);

            if (r.FacingDisputado(limiteDePiscadas))
                Debug.LogWarning($"{Marcador} {name}: {AvisoDeFacingDisputado} " +
                                 $"(flipX piscou {r.TrocasFlipX}×, localScale.x trocou de sinal " +
                                 $"{r.TrocasSinalEscalaX}× em {_relogio:0.00} s)", this);
        }

        private string Relatorio(Resultado r)
        {
            var sb = new StringBuilder(512);
            var c = CultureInfo.InvariantCulture;

            sb.Append(Marcador).Append(' ').Append(name).Append(" — ")
              .Append(_n).Append(" quadros em ").Append(_relogio.ToString("0.00", c)).Append(" s");
            if (_n >= _escalaX.Length)
                sb.Append(" (BUFFER CHEIO: gravação encerrada antes da janela; suba amostrasPorSegundoMax)");
            sb.AppendLine();

            sb.Append("  localScale.x  trocas de sinal: ").Append(r.TrocasSinalEscalaX)
              .Append("  | ").Append(r.EscalaXMin.ToString("0.###", c)).Append(" .. ").Append(r.EscalaXMax.ToString("0.###", c)).AppendLine();
            sb.Append("  flipX         piscadas: ").Append(r.TrocasFlipX)
              .Append(r.TrocasFlipX == 0 ? "  (constante)" : "")
              .Append("  | sem inversão de velocity.x por perto: ").Append(r.PiscadasOrfas)
              .Append(r.TrocasFlipX > 0 && r.PiscadasOrfas == 0 ? "  (toda piscada acompanha uma virada real)" : "")
              .AppendLine();
            sb.Append("  velocity.x    trocas de sinal: ").Append(r.TrocasSinalVelX)
              .Append("  | velocity.y: ").Append(r.TrocasSinalVelY).AppendLine();
            sb.Append("  position.x    trocas de sinal: ").Append(r.TrocasSinalPosX)
              .Append("  | inversões de direção (Δx): ").Append(r.InversoesDeDirecaoX)
              .Append("  | amplitude: ").Append(r.AmplitudePosX.ToString("0.###", c)).Append(" un")
              .Append("  | maior |Δx| numa inversão: ").Append(r.MaiorPassoNumaInversao.ToString("0.###", c)).Append(" un").AppendLine();

            sb.Append("  veredito: ");
            if (r.FacingDisputado(limiteDePiscadas)) sb.Append("FACING DISPUTADO — ").Append(AvisoDeFacingDisputado);
            else if (r.InversoesDeDirecaoX > _n / 4 && r.AmplitudePosX < 1f) sb.Append("JITTER de posição (inverte a direção quadro sim, quadro não, com amplitude pequena) — física ou dois escritores de velocidade.");
            else sb.Append("facing e posição estáveis — se ainda parece virar, é a arte (quadros com lados ou eixos diferentes).");

            return sb.ToString();
        }

        // ── Análise pura (estática, testável sem cena) ────────────────────────

        /// <summary>O que o relatório conta. Público para o teste e para quem lê por código.</summary>
        public readonly struct Resultado
        {
            public readonly int TrocasSinalEscalaX;
            public readonly int TrocasFlipX;

            /// <summary>
            /// Piscadas de flipX sem troca de sinal de velocity.x a até dois quadros de distância.
            /// Uma piscada acompanhada de virada real é um escritor só fazendo o trabalho dele;
            /// a órfã é o que denuncia uma segunda fonte.
            /// </summary>
            public readonly int PiscadasOrfas;
            public readonly int TrocasSinalVelX;
            public readonly int TrocasSinalVelY;
            public readonly int TrocasSinalPosX;
            public readonly int InversoesDeDirecaoX;
            public readonly float AmplitudePosX;
            public readonly float MaiorPassoNumaInversao;
            public readonly float EscalaXMin;
            public readonly float EscalaXMax;

            public Resultado(int trocasEscala, int trocasFlip, int piscadasOrfas, int trocasVelX, int trocasVelY,
                             int trocasPosX, int inversoes, float amplitude, float maiorPasso,
                             float escalaMin, float escalaMax)
            {
                TrocasSinalEscalaX = trocasEscala;
                TrocasFlipX = trocasFlip;
                PiscadasOrfas = piscadasOrfas;
                TrocasSinalVelX = trocasVelX;
                TrocasSinalVelY = trocasVelY;
                TrocasSinalPosX = trocasPosX;
                InversoesDeDirecaoX = inversoes;
                AmplitudePosX = amplitude;
                MaiorPassoNumaInversao = maiorPasso;
                EscalaXMin = escalaMin;
                EscalaXMax = escalaMax;
            }

            /// <summary>flipX ou localScale.x trocaram mais vezes que o limite.</summary>
            public bool FacingDisputado(int limite) => TrocasFlipX > limite || TrocasSinalEscalaX > limite;
        }

        /// <summary>
        /// Conta trocas de sinal e amplitude nas séries gravadas. Zeros não contam como sinal:
        /// uma troca é um valor não nulo com sinal diferente do último não nulo — senão um
        /// corpo que para (velocidade 0) entre dois movimentos para o mesmo lado contaria duas.
        /// </summary>
        public static Resultado Analisar(float[] escalaX, bool[] flipX, float[] velX, float[] velY, float[] posX, int n)
        {
            int trocasEscala = TrocasDeSinal(escalaX, n);
            int trocasVelX = TrocasDeSinal(velX, n);
            int trocasVelY = TrocasDeSinal(velY, n);
            int trocasPosX = TrocasDeSinal(posX, n);

            int trocasFlip = 0, orfas = 0;
            for (int i = 1; i < n; i++)
            {
                if (flipX[i] == flipX[i - 1]) continue;
                trocasFlip++;
                if (!HaInversaoDeVelocidadePerto(velX, n, i, 2)) orfas++;
            }

            float min = float.MaxValue, max = float.MinValue, escMin = float.MaxValue, escMax = float.MinValue;
            for (int i = 0; i < n; i++)
            {
                if (posX[i] < min) min = posX[i];
                if (posX[i] > max) max = posX[i];
                if (escalaX[i] < escMin) escMin = escalaX[i];
                if (escalaX[i] > escMax) escMax = escalaX[i];
            }

            // Inversões de direção: o sinal de Δx muda. É isto que denuncia jitter — position.x
            // em si quase nunca troca de sinal (o objeto raramente cruza x = 0).
            int inversoes = 0; float maiorPasso = 0f; int ultimoSinal = 0;
            for (int i = 1; i < n; i++)
            {
                float dx = posX[i] - posX[i - 1];
                if (Mathf.Abs(dx) < 1e-4f) continue;
                int sinal = dx > 0f ? 1 : -1;
                if (ultimoSinal != 0 && sinal != ultimoSinal)
                {
                    inversoes++;
                    if (Mathf.Abs(dx) > maiorPasso) maiorPasso = Mathf.Abs(dx);
                }
                ultimoSinal = sinal;
            }

            return new Resultado(trocasEscala, trocasFlip, orfas, trocasVelX, trocasVelY, trocasPosX, inversoes,
                                 n > 0 ? max - min : 0f, maiorPasso,
                                 n > 0 ? escMin : 0f, n > 0 ? escMax : 0f);
        }

        /// <summary>Se velocity.x troca de sinal em algum par de quadros consecutivos a até <paramref name="raio"/> de <paramref name="i"/>.</summary>
        private static bool HaInversaoDeVelocidadePerto(float[] velX, int n, int i, int raio)
        {
            int ini = Mathf.Max(1, i - raio), fim = Mathf.Min(n - 1, i + raio);
            for (int k = ini; k <= fim; k++)
            {
                float a = velX[k - 1], b = velX[k];
                if (Mathf.Abs(a) < 1e-4f || Mathf.Abs(b) < 1e-4f) continue;
                if ((a > 0f) != (b > 0f)) return true;
            }
            return false;
        }

        private static int TrocasDeSinal(float[] v, int n)
        {
            int trocas = 0, ultimo = 0;
            for (int i = 0; i < n; i++)
            {
                if (Mathf.Abs(v[i]) < 1e-4f) continue;
                int sinal = v[i] > 0f ? 1 : -1;
                if (ultimo != 0 && sinal != ultimo) trocas++;
                ultimo = sinal;
            }
            return trocas;
        }
    }
}
