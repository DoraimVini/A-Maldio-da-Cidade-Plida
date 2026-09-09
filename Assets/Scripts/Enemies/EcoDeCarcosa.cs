using UnityEngine;
using FavelaAmarela.Player;
using FavelaAmarela.Runtime.GameLoop;

namespace FavelaAmarela.Runtime.Enemies
{
    /// <summary>
    /// O <b>Eco de Carcosa</b>: a pressão que o Castelo faz contra ficar parado. Quando o
    /// jogador para por <c>tempoMaximoImovel</c> segundos, ele se manifesta <b>nas costas</b>
    /// dele e passa a drenar Resiliência Mental por segundo, até o jogador voltar a andar.
    ///
    /// <para><b>O defeito que a aparência conserta (2026-09-04).</b> Este componente sempre
    /// mostrou o Eco ligando os <b>filhos</b> do próprio objeto. Medido na cena:
    /// <c>Eco_De_Carcosa_0</c> e <c>Eco_De_Carcosa_1</c>, os dois da Biblioteca (Z3), tinham
    /// <b>zero filhos e nenhum <c>SpriteRenderer</c></b>. Ou seja: o Eco se manifestava, drenava
    /// 3 de Resiliência por segundo e <b>nada aparecia na tela</b>. Num jogo em que Resiliência
    /// zerada é derrota, isso é sanidade caindo sem causa visível — o jogador não tem como
    /// aprender a regra, e a única pista era um <c>Debug.Log</c> no console.</para>
    ///
    /// <para><b>Por que a garantia mora no código.</b> Um "não esqueça de pôr um filho com
    /// sprite" é uma lista para alguém manter à mão, que é o modo de falha mais repetido deste
    /// projeto — foi exatamente ele que produziu este bug. <see cref="GarantirVisual"/> segue o
    /// padrão de <c>Hurtbox.GarantirPara</c>: a cena pode chegar sem nada e o objeto se monta.
    /// O que ela <b>não</b> pode inventar são os quadros; sem eles, reclama alto.</para>
    /// </summary>
    [AddComponentMenu("Favela Amarela/Enemies/Eco de Carcosa")]
    public class EcoDeCarcosa : MonoBehaviour
    {
        [Header("Aparência")]
        [Tooltip("Quadros do vulto. [ASSET pixel art] Art/Enemies/EcoDeCarcosa/Eco_0..3.")]
        [SerializeField] private Sprite[] quadros;

        [Tooltip("Velocidade do ciclo. 6 é lento de propósito: o Eco respira, não corre.")]
        [SerializeField] private float quadrosPorSegundo = 6f;

        [Header("Anti-Camping (Eco de Carcosa)")]
        [Tooltip("Tempo em segundos que o jogador pode ficar completamente parado antes do Eco se manifestar.")]
        [SerializeField] private float tempoMaximoImovel = 5f;
        
        [Tooltip("Dreno de RM por segundo enquanto o Eco estiver ativo assombrando o jogador.")]
        [SerializeField] private float taxaDrenoRMPorSegundo = 3f;

        private PlayerMovement playerMovement;
        private float tempoParado = 0f;
        private bool ativo = false;

        private SpriteRenderer _visual;
        private float _relogioDoQuadro;
        private int _quadro;

        private void Start()
        {
            GameObject player = GameObject.FindGameObjectWithTag("Player");
            if (player != null)
            {
                playerMovement = player.GetComponent<PlayerMovement>();
                _mente = player.GetComponentInChildren<FavelaAmarela.Runtime.Combat.ResilienciaBridge>();

                if (_mente == null)
                    Debug.LogWarning("[EcoDeCarcosa] Damião sem ResilienciaBridge — o Eco não vai " +
                                     "drenar nada.", this);
            }
            
            GarantirVisual();

            // Oculta inicialmente
            DesativarEco(true);
        }

        private void Update()
        {
            if (playerMovement == null) return;

            // Fica checando o movimento do jogador (ficar parado atrai o Eco)
            if (!playerMovement.IsMoving)
            {
                tempoParado += Time.deltaTime;
                
                if (tempoParado >= tempoMaximoImovel)
                {
                    if (!ativo)
                    {
                        ativo = true;
                        AtivarEco();
                    }
                    DrenarResiliencia();
                    Animar();
                }
            }
            else
            {
                // Se voltar a se mover, reseta o timer e afasta o Eco
                if (ativo)
                {
                    ativo = false;
                    DesativarEco(false);
                }
                tempoParado = 0f;
            }
        }

        /// <summary>
        /// Acha (ou cria) o corpo visível do Eco: um filho <c>Visual_Eco</c> com
        /// <c>SpriteRenderer</c>.
        ///
        /// <para><b>Filho, e não um <c>SpriteRenderer</c> no próprio objeto</b>, por dois
        /// motivos: <see cref="AtivarEco"/> e <see cref="DesativarEco"/> já ligam e desligam
        /// filhos — então o visual criado aqui obedece ao mesmo interruptor, sem caso especial —,
        /// e o objeto do Eco carrega também o <c>CorpoImpregnado</c>, cujo ciclo não deve ser
        /// desligado junto com o desenho.</para>
        /// </summary>
        private void GarantirVisual()
        {
            var existente = transform.Find("Visual_Eco");
            if (existente != null)
            {
                _visual = existente.GetComponent<SpriteRenderer>();
                if (_visual == null) _visual = existente.gameObject.AddComponent<SpriteRenderer>();
            }
            else
            {
                var go = new GameObject("Visual_Eco");
                go.transform.SetParent(transform, false);
                _visual = go.AddComponent<SpriteRenderer>();

                // Ordena por profundidade como o resto do elenco, senão o vulto aparece por
                // baixo do piso do Salão.
                go.AddComponent<FavelaAmarela.Runtime.Rendering.DynamicYSort>();
            }

            if (quadros != null && quadros.Length > 0 && quadros[0] != null)
            {
                _visual.sprite = quadros[0];
                return;
            }

            // Sem quadros o Eco volta a ser exatamente o bug que esta classe documenta: dreno
            // de Resiliência sem nada na tela. Falha alto -- é barato de consertar e caríssimo
            // de descobrir jogando.
            Debug.LogError($"[EcoDeCarcosa] '{name}' está sem quadros. Ele vai drenar " +
                           "Resiliência Mental sem aparecer na tela, que é indistinguível de " +
                           "perder sanidade sem motivo. Conserto: " +
                           "'Tools/FavelaAmarela/Cena: vestir os Ecos de Carcosa'.", this);
        }

        private void AtivarEco()
        {
            // O Eco deve se manifestar sempre "nas costas" do Damião, para criar tensão
            Vector3 costas = -(Vector3)playerMovement.LookDirection;
            transform.position = playerMovement.transform.position + (costas * 1.5f);
            
            // Ativa renderers ou efeitos visuais (fumaça negra, som bizarro)
            foreach (Transform child in transform)
            {
                child.gameObject.SetActive(true);
            }

            // O ciclo recomeça do primeiro quadro: o Eco tem de aparecer sempre com a mesma
            // pose, senão a manifestação parece continuar de onde parou em vez de acontecer.
            _quadro = 0;
            _relogioDoQuadro = 0f;
            if (_visual != null && quadros != null && quadros.Length > 0)
                _visual.sprite = quadros[0];
            
            Debug.Log("[Eco de Carcosa] Manifestou-se! O jogador está acampando.");
        }

        private void DesativarEco(bool instantaneo)
        {
            // Oculta o Eco
            foreach (Transform child in transform)
            {
                child.gameObject.SetActive(false);
            }
            
            if (!instantaneo)
            {
                Debug.Log("[Eco de Carcosa] Dissipou-se. O jogador voltou a se mover.");
            }
        }

        /// <summary>
        /// Avança o ciclo do vulto. Só roda enquanto o Eco está manifesto — parado ele não
        /// existe na tela, e animar um objeto desligado seria trabalho por quadro sem efeito.
        /// </summary>
        private void Animar()
        {
            if (_visual == null || quadros == null || quadros.Length == 0) return;

            _relogioDoQuadro += Time.deltaTime * quadrosPorSegundo;
            if (_relogioDoQuadro < 1f) return;

            _relogioDoQuadro -= 1f;
            _quadro = (_quadro + 1) % quadros.Length;
            _visual.sprite = quadros[_quadro];
        }

        // Resolvida uma vez no Start, junto com o playerMovement — não por frame.
        private FavelaAmarela.Runtime.Combat.ResilienciaBridge _mente;

        private void DrenarResiliencia()
        {
            // Drena a sanidade do Damião pela presença opressiva do Eco.
            _mente?.SofrerTrauma(taxaDrenoRMPorSegundo * Time.deltaTime);
        }
    }
}
