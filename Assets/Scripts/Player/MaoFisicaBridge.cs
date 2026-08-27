using System;
using UnityEngine;
using FavelaAmarela.Core.Abilities;
using FavelaAmarela.Core.Combat;
using FavelaAmarela.Core.Player;
using FavelaAmarela.Runtime.Combat;
using FavelaAmarela.Core.Factories;
using FavelaAmarela.Inventario;

namespace FavelaAmarela.Player
{
    /// <summary>
    /// Bridge da Mão Física: conecta a arma equipada (um <see cref="IArmaComHabilidade"/>)
    /// à Unity. A arma <b>não é mais fixa</b> — Damião começa desarmado e equipa uma
    /// arma da Tumba em runtime (o baú sorteia entre Cravo de Aklo, Estilete de Irem e
    /// Alfanje de Alhazred). Expõe <see cref="TryAtacar"/> (ataque básico) e
    /// <see cref="TryUsarHabilidade"/> (habilidade, botão separado) para o
    /// <c>PlayerMovement</c>, e resolve o golpe contra qualquer <see cref="IDanificavel"/>
    /// (Cultista, Aparição Primordial/boss) — não mais só o Cultista.
    /// </summary>
    [AddComponentMenu("Favela Amarela/Mao Fisica Bridge")]
    public class MaoFisicaBridge : MonoBehaviour
    {
        /// <summary>Armas da Tumba para equipar em teste isolado (no jogo real vem do baú).</summary>
        public enum ArmaDeTeste { Nenhuma, CravoDeAklo, EstileteDeIrem, AlfanjeDeAlhazred }

        [Header("Arma (equipada pelo baú no jogo; aqui só para teste isolado)")]
        [Tooltip("No jogo real Damião começa DESARMADO — a arma vem do baú da Tumba. Escolha uma aqui só para testar o combate.")]
        [SerializeField] private ArmaDeTeste armaInicialParaTeste = ArmaDeTeste.Nenhuma;

        [Header("Alcance do Golpe")]
        [SerializeField] private float alcance = 1.2f;
        [SerializeField] private LayerMask camadaInimigos;

        [Header("Diagnóstico")]
        [Tooltip("Loga cada golpe: arma empunhada, dano e o que foi atingido. Serve para " +
                 "distinguir 'estou desarmado' de 'o golpe não alcança' de 'o alvo ignorou'. " +
                 "Desligue quando o combate estiver estável.")]
        [SerializeField] private bool logarGolpes = true;

        /// <summary>
        /// Índice do slot de Arma no <c>EquipmentInventory</c> (Mão Direita). Era um <c>0</c>
        /// solto em dois pontos; virou constante porque o <see cref="Start"/> passou a ser um
        /// terceiro leitor, e um deles divergir silenciosamente equivale a Damião ficar desarmado.
        /// </summary>
        private const int SlotDeArma = 0;

        // Golpe desarmado: POCO com dano 0 (a regra vive no Core, ver MaoVazia).
        // Instanciado uma vez — nunca por golpe (Regra de Ouro 1).
        private readonly IArma _maoVazia = new MaoVazia();

        private IArmaComHabilidade _armaEquipada;

        /// <summary>
        /// A <b>família</b> da arma empunhada — alcance, raio e janela do golpe. Nula quando o
        /// item não tem base ligada (armas ainda não migradas) ou quando Damião está desarmado;
        /// nesses casos vale a geometria padrão, idêntica à de antes da migração.
        /// </summary>
        private BaseDeArma _baseDaArma;

        /// <summary>
        /// A área que causa dano. Antes de 2026-08-27 o golpe do jogador era uma consulta
        /// instantânea que falava direto com <c>IDanificavel</c>, <b>pulando a Hurtbox</b> —
        /// enquanto os inimigos usavam <c>Hitbox</c> com janela ativa. Eram dois modelos de
        /// dano no mesmo jogo, e só um deles permitia esquivar no tempo certo.
        /// </summary>
        private Hitbox _hitbox;

        // Identificador serializável da arma empunhada. A instância de IArmaComHabilidade
        // não sobrevive a uma troca de cena; o enum sim, e a fábrica reconstrói a arma.
        private TipoArmaFisica? _idDaArmaEquipada;

        private float _lastUseTime = -999f;
        private float _lastAbilityUseTime = -999f;
        private PlayerStateMachine _fsm;

        // O buffer, o ContactFilter2D e o HashSet<IDanificavel> que viviam aqui saíram em
        // 2026-08-27: a resolução do golpe passou para a Hitbox, que tem os seus próprios
        // (e de-duplica por Hurtbox, não por IDanificavel). Manter cópias mortas aqui seria
        // a mesma podridão que este repositório já catalogou nove vezes.

        /// <summary>Direção e duração do ataque básico executado.</summary>
        public event Action<Vector2, float> OnAtaqueExecutado;

        /// <summary>Direção e duração da habilidade da arma executada.</summary>
        public event Action<Vector2, float> OnHabilidadeExecutada;

        /// <summary>
        /// Disparado quando a arma da Mão Física muda (baú da Tumba equipando, ou troca
        /// num Refúgio). A UI da barra de ações observa isto para se redesenhar, em vez de
        /// fazer polling do nome da arma a cada frame.
        /// </summary>
        public event Action OnArmaTrocada;

        // Cooldown da habilidade da arma equipada, capturado do último ArmaResult —
        // é o que permite a UI desenhar o preenchimento de recarga sem que a interface
        // IArmaComHabilidade precise expor a duração do cooldown.
        private float _cooldownHabilidadeAtual;

        /// <summary>
        /// Progresso de recarga da habilidade, de 0 (acabou de usar) a 1 (pronta).
        /// Vale 1 quando não há arma equipada ou quando a habilidade nunca foi usada.
        /// </summary>
        public float ProgressoCooldownHabilidade
        {
            get
            {
                if (_armaEquipada == null || _cooldownHabilidadeAtual <= 0f) return 1f;
                float decorrido = Time.time - _lastAbilityUseTime;
                return Mathf.Clamp01(decorrido / _cooldownHabilidadeAtual);
            }
        }

        /// <summary>Se a habilidade da arma está pronta para uso (cooldown completo).</summary>
        public bool HabilidadePronta =>
            _armaEquipada != null && _armaEquipada.CanActivateHabilidade(Time.time - _lastAbilityUseTime);

        /// <summary>true enquanto a FSM do jogador estiver Atacando (fonte única de verdade).</summary>
        public bool IsAtacando => _fsm != null && _fsm.CurrentState == PlayerState.Atacando;

        /// <summary>Injeta a FSM de estado do jogador (chamado por <c>PlayerMovement</c> no Awake).</summary>
        public void BindStateMachine(PlayerStateMachine fsm) => _fsm = fsm;

        /// <summary>Se há uma arma equipada na Mão Física.</summary>
        public bool TemArmaEquipada => _armaEquipada != null;

        /// <summary>Nome diegético da arma equipada, ou vazio se desarmado.</summary>
        public string NomeDaArmaEquipada => _armaEquipada?.NomeDaArma ?? "";

        /// <summary>Nome da habilidade da arma equipada, ou vazio se desarmado.</summary>
        public string NomeDaHabilidade => _armaEquipada?.NomeHabilidade ?? "";

        /// <summary>
        /// Equipa uma arma na Mão Física (chamado pelo baú da Tumba). Substitui a arma
        /// anterior — o slot de Mão Física é único (troca só sob a luz de um Refúgio, no design).
        /// </summary>
        public void EquiparArma(IArmaComHabilidade arma)
        {
            _armaEquipada = arma;
            _idDaArmaEquipada = null; // via genérica: identidade desconhecida para o save

            // Arma nova entra com a habilidade pronta (não herda a recarga da anterior).
            _cooldownHabilidadeAtual = 0f;
            _lastAbilityUseTime = -999f;

            OnArmaTrocada?.Invoke();
        }

        /// <summary>
        /// Equipa uma das armas da Tumba <b>guardando qual é</b>. Preferir esta sobrecarga:
        /// só ela deixa o save saber o que reequipar depois de uma troca de cena — a
        /// instância de <see cref="IArmaComHabilidade"/> sozinha não é serializável.
        /// </summary>
        public void EquiparArma(TipoArmaFisica qual)
        {
            EquiparArma(WeaponFactory.Criar(qual));
            _idDaArmaEquipada = qual; // depois do Equipar: a sobrecarga base limpa o id
        }

        /// <summary>
        /// Qual arma da Tumba está empunhada, ou null se desarmado (ou se a arma foi
        /// equipada por uma via que não informou o identificador).
        /// </summary>
        public TipoArmaFisica? IdDaArmaEquipada => _idDaArmaEquipada;

        private void Awake()
        {
            // Fallback seguro: se "Camada Inimigos" ficou sem valor no Inspector, usa "Enemy".
            if (camadaInimigos.value == 0)
                // Inclui EnemyHurtbox: com a separação, o corpo atingível do inimigo é a
                // hurtbox, não o colisor de movimento (que agora é só a pegada no chão,
                // 0,60 × 0,30 — acertar só isso seria acertar os pés).
                camadaInimigos = LayerMask.GetMask("Enemy", "EnemyHurtbox");

            GarantirHitbox();

            var armaTeste = CriarArmaDeTeste(armaInicialParaTeste);
            if (armaTeste != null) EquiparArma(armaTeste);
        }

        // ── Geometria do golpe, vinda da arma ─────────────────────────────────

        /// <summary>Alcance da arma empunhada, ou o padrão quando não há base ligada.</summary>
        private float AlcanceAtual => _baseDaArma != null ? _baseDaArma.Alcance : alcance;

        /// <summary>Raio da área atingida.</summary>
        private float RaioAtual =>
            _baseDaArma != null ? _baseDaArma.Raio : BaseDeArma.RaioPadrao;

        /// <summary>Quanto tempo a área fica ativa.</summary>
        private float JanelaAtual =>
            _baseDaArma != null ? _baseDaArma.JanelaAtiva : BaseDeArma.JanelaPadrao;

        /// <summary>
        /// Cria (ou reconfigura) a hitbox com a geometria da arma atual. Chamada de novo a cada
        /// troca de arma: é o que faz um estilete e um alfanje terem pegadas diferentes.
        ///
        /// <para><c>pouparAliados: true</c> preserva a proteção que a consulta antiga fazia à
        /// mão — Yug-Neth e companheiros futuros nunca são atingidos pelo golpe do jogador. A
        /// taxonomia de layers do projeto é fechada, então quem protege é o marcador
        /// <c>Aliado</c>, não uma camada própria.</para>
        /// </summary>
        /// <summary>Troca a família da arma e reconfigura a área do golpe.</summary>
        private void AplicarBase(BaseDeArma nova)
        {
            _baseDaArma = nova;
            GarantirHitbox();
        }

        private void GarantirHitbox()
        {
            _hitbox = Hitbox.GarantirPara(gameObject, "Hitbox_MaoFisica", camadaInimigos,
                                          RaioAtual, AlcanceAtual, pouparAliados: true);
        }

        /// <summary>
        /// Assina o inventário <b>e aplica o slot corrente na hora</b>.
        ///
        /// <para>Aplicar aqui não é redundância — é o que faz a arma sobreviver à troca de cena.
        /// <c>OnSlotChanged</c> só dispara em <b>mudança</b>, e trocar de cena não muda nada: o
        /// <c>InventoryManager</c> é <c>DontDestroyOnLoad</c> e continua com a arma no slot, mas
        /// quem é destruído e recriado é <b>esta bridge</b>. A instância nova assinava e ficava
        /// esperando um evento que nunca vinha, então Damião chegava desarmado na cena seguinte
        /// com a arma ainda no inventário (relatado em playtest: "quando saio da Tumba a arma
        /// some").</para>
        ///
        /// <para>Mesma classe de bug que o <c>GameStatePresenter</c> tinha antes da Fase 2:
        /// observar um evento de mudança sem aplicar o estado corrente no momento da inscrição.</para>
        /// </summary>
        private void Start()
        {
            var inv = InventoryManager.Instance;
            if (inv == null)
            {
                Debug.LogError("[MaoFisicaBridge] InventoryManager.Instance nulo no Start — a " +
                               "Mão Física não vai reagir ao inventário nem restaurar a arma " +
                               "equipada.", this);
                return;
            }

            inv.Equipment.OnSlotChanged += VerificarSlotDeArma;

            // Só sobrescreve se o inventário tiver mesmo uma arma: slot vazio deixa de pé o que
            // o Awake equipou (o override de teste 'armaInicialParaTeste').
            var slot = inv.Equipment.GetSlot(SlotDeArma);
            if (slot != null && slot.Def != null && slot.Def.Tipo == ItemType.Arma)
                EquiparArma(slot.Def.ArmaFisica);
        }

        private void OnDestroy()
        {
            var inv = InventoryManager.Instance;
            if (inv != null)
            {
                inv.Equipment.OnSlotChanged -= VerificarSlotDeArma;
            }
        }

        /// <summary>
        /// Reage ao inventário: quando o slot de Arma muda, reconstrói o POCO da arma pela
        /// <see cref="WeaponFactory"/>. É o que liga o baú da Tumba à Mão Física sem que o
        /// baú precise conhecer esta bridge.
        /// </summary>
        private void VerificarSlotDeArma(int slotIndex)
        {
            if (slotIndex != SlotDeArma) return;

            var inv = InventoryManager.Instance;
            if (inv == null) return;

            var slot = inv.Equipment.GetSlot(slotIndex);

            // Slot esvaziado (desequipou): Damião volta a lutar de mão vazia.
            if (slot == null || slot.Def == null)
            {
                AplicarBase(null);
                EquiparArma(TipoArmaFisica.MaoVazia);
                return;
            }

            if (slot.Def.Tipo != ItemType.Arma) return;

            // A FAMÍLIA vem junto com a arma: é ela que muda alcance, raio e janela do golpe.
            // Sem esta linha, trocar de arma trocaria só os números de dano e o golpe
            // continuaria com a mesma pegada -- que era o estado até 2026-08-27.
            AplicarBase(slot.Def.Base);

            // Sobrecarga com o identificador: só ela deixa o save reequipar depois da troca de cena.
            EquiparArma(slot.Def.ArmaFisica);
        }


        private static IArmaComHabilidade CriarArmaDeTeste(ArmaDeTeste escolha) => escolha switch
        {
            ArmaDeTeste.CravoDeAklo => new CravoDeAklo(),
            ArmaDeTeste.EstileteDeIrem => new EstileteDeIrem(),
            ArmaDeTeste.AlfanjeDeAlhazred => new AlfanjeDeAlhazred(),
            _ => null,
        };

        /// <summary>
        /// Ataque básico na direção dada. Com arma equipada, usa a arma; <b>desarmado</b>,
        /// executa o gesto de mão vazia — entra no estado Atacando e faz barulho, mas com
        /// <b>dano zero</b> (decisão de design: bater de mão vazia não mata). É o que
        /// ensina o verbo de combate antes do baú da Tumba entregar uma arma.
        /// </summary>
        public void TryAtacar(Vector2 direcao)
        {
            if (direcao == Vector2.zero) return;
            if (_fsm == null || !_fsm.EstaLivre) return;

            // A arma equipada é a fonte da verdade do golpe — a WeaponFactory já a
            // reconstruiu a partir do slot do inventário (ver VerificarSlotDeArma), então
            // não há por que reler o inventário a cada ataque.
            IArma arma = _armaEquipada != null ? (IArma)_armaEquipada : _maoVazia;

            // A CADÊNCIA É PERGUNTADA À ARMA, e antes de executar. Dois defeitos consertados
            // aqui em 2026-08-27:
            //
            // 1. `IArma.CanActivate` NÃO ERA CHAMADO POR NINGUÉM. O gate usava
            //    `resultado.DurationSeconds`, então o `cooldownBasico` de toda arma era dado
            //    morto -- autorar uma arma pesada de cadência lenta não fazia efeito nenhum.
            //    Os POCOs sempre tiveram o contrato testado (`ArmasDaTumbaTests`); era a
            //    bridge que não o consultava. Consequência de jogo: as três armas batiam
            //    praticamente na mesma velocidade, e o Alfanje (0,7 s) não pesava mais que o
            //    Estilete (0,3 s).
            //
            // 2. `Execute()` rodava ANTES do gate, ou seja, a arma executava mesmo quando o
            //    ataque era recusado. Inofensivo enquanto as armas são sem estado; vira bug
            //    silencioso no instante em que uma habilidade contar cargas.
            if (!arma.CanActivate(Time.time - _lastUseTime)) return;

            ArmaResult resultado = _armaEquipada != null
                ? _armaEquipada.Execute().ComBonus(
                    BonusPassivo(StatType.TraumaFisico),
                    BonusPassivo(StatType.TraumaAnomalia))
                // Gesto de mão vazia: dano 0 por design — bônus passivos não se aplicam,
                // senão desarmado passaria a matar.
                : _maoVazia.Execute();

            if (!_fsm.TryEntrarAcao(PlayerState.Atacando, resultado.DurationSeconds)) return;

            _lastUseTime = Time.time;
            ResolverGolpe(direcao, resultado);
            OnAtaqueExecutado?.Invoke(direcao, resultado.DurationSeconds);
        }

        /// <summary>Habilidade da arma equipada (botão separado, cooldown próprio), na direção dada.</summary>
        public void TryUsarHabilidade(Vector2 direcao)
        {
            if (_armaEquipada == null) return;
            if (direcao == Vector2.zero) return;
            if (_fsm == null || !_fsm.EstaLivre) return;
            if (!_armaEquipada.CanActivateHabilidade(Time.time - _lastAbilityUseTime)) return;

            var resultado = _armaEquipada.ExecuteHabilidade().ComBonus(
                BonusPassivo(StatType.TraumaFisico),
                BonusPassivo(StatType.TraumaAnomalia));

            if (!_fsm.TryEntrarAcao(PlayerState.Atacando, resultado.DurationSeconds)) return;

            _lastAbilityUseTime = Time.time;
            _cooldownHabilidadeAtual = resultado.CooldownSeconds;
            ResolverGolpe(direcao, resultado);
            OnHabilidadeExecutada?.Invoke(direcao, resultado.DurationSeconds);
        }

        /// <summary>
        /// Bônus passivo agregado (equipamentos + relíquias + Ecos) para um atributo, ou 0
        /// se o gerenciador ainda não existe na cena. Existe para que os dois canais de dano
        /// sejam consultados do mesmo jeito, num lugar só.
        /// </summary>
        private static float BonusPassivo(StatType atributo)
            => GerenciadorEfeitosPassivos.Instance?.GetBonus(atributo) ?? 0f;

        /// <summary>
        /// Arma a área do golpe. <b>Não resolve dano aqui</b> — quem resolve é a
        /// <see cref="Hitbox"/>, durante a janela ativa.
        ///
        /// <para><b>O que mudou em 2026-08-27.</b> Este método fazia um
        /// <c>Physics2D.OverlapCircle</c> instantâneo e chamava <c>IDanificavel.ReceberGolpe</c>
        /// direto, <b>pulando a Hurtbox</b> — enquanto o Byakhee já usava <c>Hitbox</c> com
        /// janela ativa. Eram <b>dois modelos de dano no mesmo jogo</b>, e só o do inimigo
        /// permitia esquivar no tempo certo: contra o Damião, o golpe era um teste de posição
        /// num quadro, sem nada para ler nem quando reagir.</para>
        ///
        /// <para>Agora os dois lados usam a mesma peça. A de-duplicação por alvo, a proteção
        /// aos aliados, a repulsão e o hit-stop passaram todos a viver na <c>Hitbox</c> — um
        /// lugar só, em vez de dois que divergem.</para>
        /// </summary>
        private void ResolverGolpe(Vector2 direcao, ArmaResult resultado)
        {
            if (_hitbox == null) GarantirHitbox();

            if (_hitbox == null)
            {
                Debug.LogError("[MaoFisicaBridge] Sem hitbox: o golpe não pode acertar nada.", this);
                return;
            }

            _hitbox.Armar(resultado, JanelaAtual, direcao);

            if (logarGolpes)
            {
                string arma = _armaEquipada != null ? _armaEquipada.NomeDaArma : "DESARMADO (mão vazia)";
                string familia = _baseDaArma != null ? _baseDaArma.NomeDaFamilia : "geometria padrão";

                Debug.Log($"[Golpe] arma={arma} familia={familia} dano={resultado.Dano:0.##} " +
                          $"trauma={resultado.TraumaAnomalia:0.##} " +
                          $"alcance={AlcanceAtual:0.##} raio={RaioAtual:0.##} " +
                          $"janela={JanelaAtual:0.###}s", this);
            }
        }

        /// <summary>
        /// Desenha <b>o volume que o golpe realmente consulta</b>.
        ///
        /// <para>Até 2026-08-27 este gizmo mentia: desenhava um círculo de raio <c>alcance</c>
        /// centrado no jogador, enquanto <see cref="ResolverGolpe"/> consulta um círculo de raio
        /// <c>alcance/2</c> deslocado <c>alcance/2</c> à frente. Ou seja, a área desenhada tinha
        /// <b>quatro vezes</b> a do teste real e ficava no lugar errado — calibrar alcance
        /// olhando para ela levava à conclusão oposta da verdadeira.</para>
        ///
        /// <para>Sem direção de input no Editor, desenha para a direita e marca o centro do
        /// jogador, para a assimetria ficar evidente.</para>
        /// </summary>
        private void OnDrawGizmosSelected()
        {
            Vector2 frente = transform.right;
            Vector2 centro = (Vector2)transform.position + frente * (alcance * 0.5f);

            Gizmos.color = new Color(0.8f, 0.2f, 0.2f, 0.9f);
            Gizmos.DrawWireSphere(centro, alcance * 0.5f);

            Gizmos.color = new Color(0.8f, 0.8f, 0.2f, 0.5f);
            Gizmos.DrawLine(transform.position, centro);
        }
    }
}
