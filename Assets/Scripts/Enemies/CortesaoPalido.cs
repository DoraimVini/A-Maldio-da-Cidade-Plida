using UnityEngine;

namespace FavelaAmarela.Runtime.Enemies
{
    /// <summary>
    /// O Cortesão Pálido do Castelo: patrulha entre pontos e persegue Damião quando o vê.
    ///
    /// <para><b>SAIU de <c>Core/</c> em 2026-09-04, e a mudança foi forçada.</b> A nota antiga
    /// dizia que mover o arquivo daria "ganho nenhum em jogo" — deixou de ser verdade no dia em
    /// que o Vini pediu que o Cortesão levasse e causasse dano. <c>FavelaAmarela.Core</c> tem
    /// <c>references: []</c> e não pode enxergar <c>Hitbox</c> nem <c>EnemyBase</c>, que vivem
    /// no Runtime: de dentro de <c>Core/</c> este ator <b>não tinha como</b> ganhar combate.</para>
    ///
    /// <para><b>O que a mudança NÃO paga.</b> Ela põe um <c>MonoBehaviour</c> onde
    /// <c>MonoBehaviour</c> deve morar, e só. A dívida de verdade continua em aberto: separar
    /// uma <c>CortesaoPalidoFSM</c> (Core) de um adaptador (Runtime), como
    /// <c>CultistaFSM</c> + <c>CultistaAI</c>. Fica registrado aqui, com nome e endereço.</para>
    ///
    /// <para><b>Por que ele não virou a pilha padrão do elenco</b> (<c>EnemyMovement</c> +
    /// <c>EnemyPerception</c> + <c>EnemyStateMachine</c>): ele é o <b>único ator do jogo com
    /// detecção VISUAL</b> — cone de visão mais raycast de oclusão. O resto do elenco percebe
    /// 100% por som, e o <c>CLAUDE.md</c> registra que visão "é sistema, não ajuste, e não
    /// estava no prazo do edital". Trocá-lo pela pilha padrão apagaria a única visão que
    /// existe.</para>
    ///
    /// <para><b>O que MUDOU em 2026-08-27</b>, e não é dívida — é defeito:</para>
    ///
    /// <list type="number">
    ///   <item><b>Ele andava por <c>transform.position</c> tendo um <c>Rigidbody2D</c>
    ///   Dynamic.</b> Isso teleporta o corpo a cada quadro: a física não integra o movimento, ela
    ///   só descobre o resultado depois. O corpo entra dentro da parede e é expulso no quadro
    ///   seguinte (o tremor), e quando o Damião está no caminho, o Cortesão o <b>atravessa
    ///   empurrando</b> em vez de esbarrar. Passa a mover por <c>linearVelocity</c> em
    ///   <c>FixedUpdate</c>, que é a convenção do projeto (<c>CLAUDE.md</c> §5) e a do
    ///   <c>PlayerMovement</c>.</item>
    ///
    ///   <item><b><c>LayerMask.GetMask("Player")</c> era chamado todo <c>Update</c>.</b> É busca
    ///   por string no <c>TagManager</c> a 60 Hz, por Cortesão, para um valor que nunca muda —
    ///   Regra de Ouro 1. Resolvido uma vez no <c>Awake</c>.</item>
    /// </list>
    /// </summary>
    public class CortesaoPalido : MonoBehaviour
    {
        [Header("Status do Cortesão")]
        // Havia aqui um 'vida = 100f' que NINGUÉM lia -- o compilador denunciava
        // (CS0414) e o aviso estava certo. O Cortesão não implementa IDanificavel: não
        // existe caminho no jogo que tire vida dele. O campo prometia uma barra de vida
        // que não existe, e no Inspector era indistinguível de um número em uso.
        //
        // Se um dia ele for feito abatível, o caminho é o mesmo do resto do elenco --
        // EnemyBase + Vitalidade + Hurtbox --, não ressuscitar este float.
        [SerializeField] private float velocidadePatrulha = 1.5f;
        [SerializeField] private float campoDeVisao = 6f;

        [Header("Visão (Stealth)")]
        [SerializeField] private float anguloVisao = 90f;
        [SerializeField] private LayerMask layerObstaculos;

        [Header("Patrulha")]
        [SerializeField] private Transform[] pontosDePatrulha;
        private int indexPatrulhaAtual = 0;

        /// <summary>Quanto mais rápido ele vai atrás do Damião do que patrulhando.</summary>
        private const float FatorDePerseguicao = 1.5f;

        /// <summary>Distância que conta como "chegou" ao ponto de patrulha.</summary>
        private const float RaioDeChegada = 0.2f;

        [Header("Golpe")]
        [Tooltip("Distância em que ele para de avançar e bate.")]
        [Min(0.1f)]
        [SerializeField] private float alcanceDoGolpe = 1.1f;

        [Tooltip("Raio da área atingida, em unidades de mundo.")]
        [Min(0.1f)]
        [SerializeField] private float raioDoGolpe = 0.6f;

        [Tooltip("Segundos que a janela de acerto fica aberta. Não é o intervalo entre golpes: " +
                 "é quanto tempo o golpe EXISTE, e portanto quanto tempo há para esquivar.")]
        [Min(0.05f)]
        [SerializeField] private float janelaDoGolpe = 0.15f;

        [Tooltip("Segundos entre um golpe e o seguinte.")]
        [Min(0.1f)]
        [SerializeField] private float recargaDoGolpe = 1.4f;

        [Header("Coleira")]
        [Tooltip("Distância máxima do ponto onde a caça começou. Passando disso ele desiste e " +
                 "volta a patrulhar. 20 é o mesmo valor que EnemyStateMachine.maxChaseDistance " +
                 "usa para o resto do elenco.")]
        [Min(1f)]
        [SerializeField] private float distanciaMaximaDeCaca = 20f;

        private Transform alvo;
        private bool jogadorDetectado = false;

        /// <summary>
        /// Onde a caça começou. É a referência da coleira — e é o ponto de partida, não a
        /// posição atual, pelo mesmo motivo que a <c>EnemyStateMachine</c> usa
        /// <c>_chaseOrigin</c>: medir contra a posição atual nunca dispara, porque ela é sempre
        /// zero.
        /// </summary>
        private Vector2 origemDaCaca;

        private Combat.Hitbox _hitbox;
        private EnemyBase _corpoDeCombate;
        private float _proximoGolpe;

        private Rigidbody2D _corpo;
        private int _mascaraDoJogador;

        private void Awake()
        {
            _corpo = GetComponent<Rigidbody2D>();

            // Uma busca por nome, não uma por quadro.
            _mascaraDoJogador = LayerMask.GetMask("Player");
            if (_mascaraDoJogador == 0)
                Debug.LogError($"[CortesaoPalido] '{name}': a camada 'Player' não existe no " +
                               "TagManager. A varredura de visão nunca vai achar ninguém e ele " +
                               "patrulha para sempre.", this);

            if (layerObstaculos.value == 0)
                Debug.LogWarning($"[CortesaoPalido] '{name}': 'layerObstaculos' está vazia. O " +
                                 "raycast de linha de visão nunca acerta nada, então ele " +
                                 "enxerga ATRAVÉS de parede.", this);
        }

        /// <summary>
        /// Percepção roda por quadro (é leitura); movimento roda em <c>FixedUpdate</c> (é
        /// física). Misturar os dois é o que fazia o corpo ser teleportado.
        /// </summary>
        private void Update()
        {
            if (jogadorDetectado && alvo != null)
            {
                // A COLEIRA. Sem ela, `jogadorDetectado` NUNCA voltava a false: este `return`
                // saía cedo para sempre, e o Cortesão perseguia o Damião pelo mapa inteiro. O
                // Vini relatou o sintoma jogando o Castelo -- "ele invade a luta contra o Rei",
                // que é a última luta do Vertical Slice, três zonas adiante do Salão do
                // Banquete onde ele patrulha.
                //
                // Mede contra ONDE A CAÇA COMEÇOU, e não contra a posição atual: distância até
                // si mesmo é sempre zero e nunca dispararia. Mesma semântica de
                // EnemyStateMachine._chaseOrigin, e o mesmo valor padrão (20).
                if (Vector2.Distance(transform.position, origemDaCaca) <= distanciaMaximaDeCaca)
                    return;

                DesistirDaCaca();
                return;
            }

            ProcurarJogador();
        }

        /// <summary>
        /// Larga a perseguição e volta à patrulha. Público não: quem decide é a coleira.
        /// </summary>
        private void DesistirDaCaca()
        {
            jogadorDetectado = false;
            alvo = null;
        }

        /// <summary>
        /// Arma o golpe se o Damião estiver ao alcance e a recarga tiver passado.
        /// </summary>
        /// <returns>true se golpeou — quem chama para de avançar.</returns>
        private bool TentarGolpear()
        {
            if (alvo == null || Time.time < _proximoGolpe) return false;

            if (Vector2.Distance(transform.position, alvo.position) > alcanceDoGolpe)
                return false;

            GarantirHitbox();
            if (_hitbox == null) return false;

            _proximoGolpe = Time.time + recargaDoGolpe;

            // O dano sai da FICHA, e não de um número solto aqui: é o EnemyBase que carrega a
            // ficha do Cortesão, e é dela que o resto do elenco tira ataque. Um float no
            // Inspector deste script seria uma segunda fonte da verdade para o mesmo número.
            float dano = _corpoDeCombate != null ? _corpoDeCombate.Atributos.Ataque : 0f;

            var golpe = new Core.Abilities.ArmaResult(
                success: true, durationSeconds: 0f, cooldownSeconds: 0f, dano: dano);

            Vector2 direcao = ((Vector2)alvo.position - (Vector2)transform.position).normalized;
            _hitbox.Armar(golpe, janelaDoGolpe, direcao);
            return true;
        }

        private void GarantirHitbox()
        {
            if (_hitbox != null) return;

            int camadaDoJogador = LayerMask.GetMask("PlayerHurtbox");

            // Profundidade de uma célula, igual à do golpe do Damião. Simetria de propósito: um
            // inimigo que alcança três células de profundidade enquanto o jogador alcança uma
            // lê como injustiça, porque contradiz o que se vê.
            _hitbox = Combat.Hitbox.GarantirPara(
                gameObject, "Hitbox_Cortesao", camadaDoJogador,
                raioDoGolpe, alcanceDoGolpe, pouparAliados: false,
                profundidade: Combat.Hitbox.ProfundidadeDeUmaCelula);
        }

        private void FixedUpdate()
        {
            Vector2 destino;
            float velocidade;

            if (jogadorDetectado && alvo != null)
            {
                // Golpear vem ANTES de mover: quem já está ao alcance para e bate, em vez de
                // continuar empurrando o Damião enquanto o acerta.
                if (TentarGolpear()) { Parar(); return; }

                destino = alvo.position;
                velocidade = velocidadePatrulha * FatorDePerseguicao;
            }
            else if (pontosDePatrulha != null && pontosDePatrulha.Length > 0)
            {
                var ponto = pontosDePatrulha[indexPatrulhaAtual];
                if (ponto == null) { Parar(); return; }

                destino = ponto.position;
                velocidade = velocidadePatrulha;

                if (Vector2.Distance(transform.position, destino) < RaioDeChegada)
                    indexPatrulhaAtual = (indexPatrulhaAtual + 1) % pontosDePatrulha.Length;
            }
            else
            {
                Parar();
                return;
            }

            Mover(destino, velocidade);
        }

        /// <summary>
        /// Reproduz o passo do <c>Vector2.MoveTowards</c> antigo — inclusive o freio ao chegar —
        /// mas entrega o resultado como <b>velocidade</b>, para o solver resolver a colisão em
        /// vez de descobrir o atravessamento depois do fato.
        /// </summary>
        private void Mover(Vector2 destino, float velocidade)
        {
            Vector2 posicao = transform.position;
            Vector2 passo = Vector2.MoveTowards(posicao, destino, velocidade * Time.fixedDeltaTime)
                            - posicao;

            if (_corpo != null) _corpo.linearVelocity = passo / Time.fixedDeltaTime;
            else transform.position = posicao + passo;   // sem corpo, o jeito antigo ainda serve
        }

        private void Parar()
        {
            if (_corpo != null) _corpo.linearVelocity = Vector2.zero;
        }

        private void ProcurarJogador()
        {
            Collider2D col = Physics2D.OverlapCircle(transform.position, campoDeVisao,
                                                     _mascaraDoJogador);
            if (col == null) return;

            Vector2 direcaoAoJogador = (col.transform.position - transform.position).normalized;

            // O Cortesão olha para a direção que está patrulhando
            Vector2 direcaoOlhar = transform.right;
            if (pontosDePatrulha != null && pontosDePatrulha.Length > 0 &&
                pontosDePatrulha[indexPatrulhaAtual] != null)
            {
                direcaoOlhar = ((Vector2)pontosDePatrulha[indexPatrulhaAtual].position
                                - (Vector2)transform.position).normalized;
            }
            if (direcaoOlhar == Vector2.zero) direcaoOlhar = transform.right;

            if (Vector2.Angle(direcaoOlhar, direcaoAoJogador) >= anguloVisao / 2f) return;

            // Raycast para garantir que não tem estátuas/paredes na frente
            float distancia = Vector2.Distance(transform.position, col.transform.position);
            RaycastHit2D hit = Physics2D.Raycast(transform.position, direcaoAoJogador, distancia,
                                                 layerObstaculos);

            if (hit.collider != null) return;

            // A origem da caça é gravada AQUI, no instante em que ele passa a caçar -- não no
            // Awake. Gravar no Awake mediria a coleira contra o ponto de nascimento, e um
            // Cortesão que patrulhasse para longe começaria a caça já esticado.
            origemDaCaca = transform.position;
            jogadorDetectado = true;
            alvo = col.transform;
        }
    }
}
