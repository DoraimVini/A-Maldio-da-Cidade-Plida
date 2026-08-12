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

        // Golpe desarmado: POCO com dano 0 (a regra vive no Core, ver MaoVazia).
        // Instanciado uma vez — nunca por golpe (Regra de Ouro 1).
        private readonly IArma _maoVazia = new MaoVazia();

        private IArmaComHabilidade _armaEquipada;

        // Identificador serializável da arma empunhada. A instância de IArmaComHabilidade
        // não sobrevive a uma troca de cena; o enum sim, e a fábrica reconstrói a arma.
        private TipoArmaFisica? _idDaArmaEquipada;

        private float _lastUseTime = -999f;
        private float _lastAbilityUseTime = -999f;
        private PlayerStateMachine _fsm;

        // Buffer pré-alocado + filtro para resolver o golpe sem alocar lixo por golpe
        // (Regra de Ouro 1). 8 slots cobrem o alcance melee.
        private readonly Collider2D[] _hitBuffer = new Collider2D[8];
        private ContactFilter2D _filtroInimigos;

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
                camadaInimigos = LayerMask.GetMask("Enemy");

            _filtroInimigos = new ContactFilter2D();
            _filtroInimigos.useTriggers = true;
            _filtroInimigos.SetLayerMask(camadaInimigos);

            var armaTeste = CriarArmaDeTeste(armaInicialParaTeste);
            if (armaTeste != null) EquiparArma(armaTeste);
        }

        private void Start()
        {
            var inv = InventoryManager.Instance;
            if (inv != null)
            {
                inv.Equipment.OnSlotChanged += VerificarSlotDeArma;
            }
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
            // Slot 0 é Weapon (Mão Direita/Arma)
            if (slotIndex != 0) return;

            var inv = InventoryManager.Instance;
            if (inv == null) return;

            var slot = inv.Equipment.GetSlot(slotIndex);

            // Slot esvaziado (desequipou): Damião volta a lutar de mão vazia.
            if (slot == null || slot.Def == null)
            {
                EquiparArma(TipoArmaFisica.MaoVazia);
                return;
            }

            if (slot.Def.Tipo != ItemType.Arma) return;

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

            ArmaResult resultado;

            // A arma equipada é a fonte da verdade do golpe — a WeaponFactory já a
            // reconstruiu a partir do slot do inventário (ver VerificarSlotDeArma), então
            // não há por que reler o inventário a cada ataque.
            if (_armaEquipada != null)
            {
                resultado = _armaEquipada.Execute().ComBonus(
                    BonusPassivo(StatType.TraumaFisico),
                    BonusPassivo(StatType.TraumaAnomalia));
            }
            else
            {
                // Gesto de mão vazia: dano 0 por design — bônus passivos não se aplicam,
                // senão desarmado passaria a matar.
                resultado = _maoVazia.Execute();
            }

            // Fallback para cooldown baseado no ultimo ataque
            if (Time.time - _lastUseTime < resultado.DurationSeconds) return;

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

        private void ResolverGolpe(Vector2 direcao, ArmaResult resultado)
        {
            Vector2 centro = (Vector2)transform.position + direcao.normalized * (alcance * 0.5f);
            int total = Physics2D.OverlapCircle(centro, alcance * 0.5f, _filtroInimigos, _hitBuffer);

            int atingidos = 0;

            for (int i = 0; i < total; i++)
            {
                // Aliados (Yug-Neth e companheiros futuros) nunca são atingidos pelo golpe
                // do jogador — nem por acidente no meio de uma luta. Checado ANTES do
                // IDanificavel porque um aliado normalmente também é danificável: o que o
                // protege é este marcador, não a ausência de vitalidade. Ver `Aliado`.
                if (_hitBuffer[i].GetComponentInParent<Aliado>() != null) continue;

                // Mira qualquer IDanificavel (Cultista, Aparição Primordial/boss...), não
                // mais só o CultistaAI — é isto que permite as armas atingirem o Abdul.
                // GetComponentInParent, e não GetComponent: o colisor pode estar num filho
                // (sprite/hitbox separada) enquanto o script vive no objeto raiz — com
                // GetComponent o golpe simplesmente não encontrava o alvo.
                var alvo = _hitBuffer[i].GetComponentInParent<IDanificavel>();
                if (alvo != null)
                {
                    alvo.ReceberGolpe(resultado);
                    atingidos++;
                }
            }

            if (logarGolpes)
            {
                string arma = _armaEquipada != null ? _armaEquipada.NomeDaArma : "DESARMADO (mão vazia)";
                Debug.Log($"[Golpe] arma={arma} dano={resultado.Dano:0.##} " +
                          $"trauma={resultado.TraumaAnomalia:0.##} " +
                          $"colisores={total} alvosAtingidos={atingidos}", this);
            }
        }

        private void OnDrawGizmosSelected()
        {
            Gizmos.color = new Color(0.8f, 0.2f, 0.2f, 0.4f);
            Gizmos.DrawWireSphere(transform.position, alcance);
        }
    }
}
