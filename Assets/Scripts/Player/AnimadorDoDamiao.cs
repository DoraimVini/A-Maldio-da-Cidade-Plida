using UnityEngine;

namespace FavelaAmarela.Player
{
    /// <summary>
    /// Camada Runtime. Toca os quadros de Damião conforme <see cref="PlayerMovement"/> (parado
    /// vs. correndo, e a direção) e <see cref="MaoFisicaBridge"/> (golpe). Mesmo padrão dos
    /// animadores de inimigo (<c>AnimadorDoByakhee</c>, <c>AnimadorDoCultista</c>,
    /// <c>AnimadorDoEspectro</c>): lê o estado de outro componente e só escreve o
    /// <c>SpriteRenderer</c> — nenhuma regra de negócio aqui.
    ///
    /// <para><b>Direção por evento de subida, não por leitura contínua durante o golpe:</b> o
    /// golpe trava Damião no lugar (<c>PlayerMovement</c>, caso <c>PlayerState.Atacando</c>), e
    /// não há garantia de que <c>LookDirection</c> continue refletindo a intenção do jogador
    /// durante o gesto. Por isso a direção do golpe é <b>capturada no instante em que
    /// <c>IsAtacando</c> vira verdadeiro</b>, não recalculada quadro a quadro.</para>
    ///
    /// <para><b>Fora de escopo desta rodada</b> (arte inexistente ou custo desproporcional ao
    /// prazo): Esquiva, Congelado e reação a dano não têm ciclo próprio — o corpo continua no
    /// ciclo em que estava. Documentado aqui para não parecer esquecimento silencioso.</para>
    /// </summary>
    [RequireComponent(typeof(SpriteRenderer), typeof(PlayerMovement))]
    [AddComponentMenu("Favela Amarela/Player/Animador do Damião")]
    public sealed class AnimadorDoDamiao : MonoBehaviour
    {
        [Header("Parado")]
        [Tooltip("[ASSET]")]
        [SerializeField] private Sprite[] idle;

        [Header("Corrida, por direção")]
        [Tooltip("[ASSET]")] [SerializeField] private Sprite[] correrBaixo;
        [Tooltip("[ASSET]")] [SerializeField] private Sprite[] correrCima;
        [Tooltip("[ASSET]")] [SerializeField] private Sprite[] correrEsquerda;
        [Tooltip("[ASSET]")] [SerializeField] private Sprite[] correrDireita;

        [Header("Golpe da Mão Física, por direção (toca uma vez, segura o último quadro)")]
        [Tooltip("[ASSET]")] [SerializeField] private Sprite[] golpeBaixo;
        [Tooltip("[ASSET]")] [SerializeField] private Sprite[] golpeCima;
        [Tooltip("[ASSET]")] [SerializeField] private Sprite[] golpeEsquerda;
        [Tooltip("[ASSET]")] [SerializeField] private Sprite[] golpeDireita;

        [Header("Cadência")]
        [Min(1f)] [SerializeField] private float quadrosPorSegundoCorrida = 10f;
        [Min(1f)] [SerializeField] private float quadrosPorSegundoGolpe = 14f;

        private enum Direcao4 { Baixo, Cima, Esquerda, Direita }

        private SpriteRenderer _sprite;
        private PlayerMovement _movimento;
        private MaoFisicaBridge _mao;

        private Sprite[] _cicloAtual;
        private float _relogio;
        private int _quadro;

        private Direcao4 _direcaoDeCorrida = Direcao4.Baixo;
        private Direcao4 _direcaoDoGolpe = Direcao4.Baixo;
        private bool _atacandoNoQuadroAnterior;

        private void Awake()
        {
            _sprite = GetComponent<SpriteRenderer>();
            _movimento = GetComponent<PlayerMovement>();
            _mao = GetComponent<MaoFisicaBridge>();

            if (_mao == null)
                Debug.LogError($"[AnimadorDoDamiao] '{name}' sem MaoFisicaBridge — o golpe não " +
                               "vai animar.", this);
        }

        private void Start() => TrocarCiclo(idle);

        private void Update()
        {
            bool atacando = _mao != null && _mao.IsAtacando;

            // Borda de subida: o golpe começou agora. Captura a direção UMA vez — ver o porquê
            // no XML doc da classe.
            if (atacando && !_atacandoNoQuadroAnterior)
                _direcaoDoGolpe = BucketDeDirecao(_movimento.LookDirection);
            _atacandoNoQuadroAnterior = atacando;

            if (!atacando && _movimento.IsMoving)
                _direcaoDeCorrida = BucketDeDirecao(_movimento.LookDirection);

            Sprite[] alvo;
            float fps;
            bool seguraUltimoQuadro;

            if (atacando)
            {
                alvo = CicloDeGolpe(_direcaoDoGolpe);
                fps = quadrosPorSegundoGolpe;
                seguraUltimoQuadro = true;
            }
            else if (_movimento.IsMoving)
            {
                alvo = CicloDeCorrida(_direcaoDeCorrida);
                fps = quadrosPorSegundoCorrida;
                seguraUltimoQuadro = false;
            }
            else
            {
                alvo = idle;
                fps = quadrosPorSegundoCorrida; // reaproveita a cadência de passo p/ a respiração
                seguraUltimoQuadro = false;
            }

            TrocarCiclo(alvo);
            AvancarQuadro(fps, seguraUltimoQuadro);
        }

        /// <summary>
        /// Direção do jogador, dividida em 4 setores de 90°. Right=0°, Up=90°, Left=180°,
        /// Down=−90° (ou 270°) — a mesma convenção de <c>Mathf.Atan2</c>.
        /// </summary>
        private static Direcao4 BucketDeDirecao(Vector2 direcao)
        {
            if (direcao.sqrMagnitude < 0.0001f) return Direcao4.Baixo;

            float angulo = Mathf.Atan2(direcao.y, direcao.x) * Mathf.Rad2Deg;

            if (angulo > -45f && angulo <= 45f) return Direcao4.Direita;
            if (angulo > 45f && angulo <= 135f) return Direcao4.Cima;
            if (angulo > 135f || angulo <= -135f) return Direcao4.Esquerda;
            return Direcao4.Baixo;
        }

        private Sprite[] CicloDeCorrida(Direcao4 dir) => dir switch
        {
            Direcao4.Cima => correrCima,
            Direcao4.Esquerda => correrEsquerda,
            Direcao4.Direita => correrDireita,
            _ => correrBaixo,
        };

        private Sprite[] CicloDeGolpe(Direcao4 dir) => dir switch
        {
            Direcao4.Cima => golpeCima,
            Direcao4.Esquerda => golpeEsquerda,
            Direcao4.Direita => golpeDireita,
            _ => golpeBaixo,
        };

        private void TrocarCiclo(Sprite[] ciclo)
        {
            if (ciclo == null || ciclo.Length == 0 || ciclo == _cicloAtual) return;

            _cicloAtual = ciclo;
            _quadro = 0;
            _relogio = 0f;
            _sprite.sprite = ciclo[0];
        }

        private void AvancarQuadro(float fps, bool seguraUltimoQuadro)
        {
            if (_cicloAtual == null || _cicloAtual.Length == 0) return;

            _relogio += Time.deltaTime * fps;
            if (_relogio < 1f) return;
            _relogio -= 1f;

            if (_quadro + 1 >= _cicloAtual.Length)
            {
                if (seguraUltimoQuadro) return; // fica no último quadro até o ciclo trocar
                _quadro = 0;
            }
            else
            {
                _quadro++;
            }

            _sprite.sprite = _cicloAtual[_quadro];
        }
    }
}
