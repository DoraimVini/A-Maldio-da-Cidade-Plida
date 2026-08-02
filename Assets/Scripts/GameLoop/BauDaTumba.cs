using UnityEngine;
using FavelaAmarela.Core.Abilities;
using FavelaAmarela.Core.Persistencia;
using FavelaAmarela.Player;
using FavelaAmarela.Runtime.Interaction;
using FavelaAmarela.Runtime.Persistencia;
using FavelaAmarela.Runtime.UI;

namespace FavelaAmarela.Runtime.GameLoop
{
    /// <summary>
    /// Camada Runtime (MonoBehaviour). Baú da Tumba de Alhazred: quando Damião o
    /// <b>abre</b>, o baú <b>sorteia</b> uma das três armas seladas (Cravo de Aklo,
    /// Estilete de Irem, Alfanje de Alhazred) e a equipa na Mão Física. Não é escolha —
    /// é RNG, e é o que faz a build variar entre partidas.
    ///
    /// <para>Abre por <b>interação deliberada</b> (botão E), não por encostar: é um baú,
    /// o jogador decide abrir. Implementa <see cref="IInteragivel"/>; o
    /// <c>DetectorDeInteracao</c> no Damião cuida da mira e do prompt. A regra do sorteio
    /// vive no Core (<see cref="SorteioDeArmaDaTumba"/>); aqui só há o gatilho e o
    /// feedback visual.</para>
    /// </summary>
    [RequireComponent(typeof(Collider2D))]
    [AddComponentMenu("Favela Amarela/GameLoop/Baú da Tumba")]
    public sealed class BauDaTumba : MonoBehaviour, IInteragivel
    {
        [Header("Sorteio")]
        [Tooltip("Se marcado, sempre entrega a arma escolhida abaixo (para testar uma build específica).")]
        [SerializeField] private bool forcarArma = false;

        [Tooltip("Arma entregue quando 'Forçar Arma' está marcado.")]
        [SerializeField] private ArmaDaTumba armaForcada = ArmaDaTumba.CravoDeAklo;

        [Header("Visual")]
        [Tooltip("Sprite do baú fechado → trocado pelo aberto ao coletar. [ASSET pixel art]")]
        [SerializeField] private SpriteRenderer spriteDoBau;

        [Tooltip("Sprite do baú já aberto. [ASSET pixel art]")]
        [SerializeField] private Sprite spriteAberto;

        [Header("Feedback")]
        [Tooltip("Dica mostrada ao abrir o baú (reaproveita a UI do tutorial).")]
        [SerializeField] private TutorialHintUI hintUI;

        private readonly SorteioDeArmaDaTumba _sorteio = new SorteioDeArmaDaTumba();
        private bool _aberto;

        /// <summary>Se o baú já foi aberto (um baú entrega uma arma só).</summary>
        public bool Aberto => _aberto;

        // ── IInteragivel ─────────────────────────────────────────────────────

        /// <inheritdoc />
        public string RotuloDeInteracao => "Abrir o baú";

        /// <inheritdoc />
        public bool PodeInteragir => !_aberto;

        /// <summary>
        /// Prioridade alta: o baú é o objetivo da sala, deve ganhar de qualquer
        /// cenário interagível que por acaso esteja ao lado.
        /// </summary>
        public int PrioridadeDeInteracao => 10;

        /// <inheritdoc />
        public Vector2 PosicaoDeInteracao => transform.position;

        /// <inheritdoc />
        public void Interagir(GameObject quemInterage)
        {
            if (_aberto) return;

            var maoFisica = quemInterage.GetComponent<MaoFisicaBridge>();
            if (maoFisica == null)
            {
                Debug.LogError("[BauDaTumba] Quem abriu o baú não tem MaoFisicaBridge — " +
                               "nenhuma arma pôde ser equipada.", this);
                return;
            }

            _aberto = true;
            GerenciadorDeSave.MarcarAconteceu(ChavesDeSave.BauDaTumbaAberto);

            var qual = forcarArma ? armaForcada : _sorteio.Sortear();
            var arma = SorteioDeArmaDaTumba.Criar(qual);

            // Equipa pelo identificador (não pela instância): é o que permite ao save
            // reequipar a mesma arma depois de uma troca de cena.
            maoFisica.EquiparArma(qual);

            // Guarda também no inventário: armas são itens (decisão de 2026-08-01), e é o
            // inventário que permite voltar a uma arma anterior num Refúgio. Se não couber,
            // ela continua empunhada — perder a arma do baú por inventário cheio seria pior
            // que a inconsistência.
            GuardarNoInventario(quemInterage, qual, arma.NomeDaArma);

            MostrarComoAberto();

            if (hintUI != null)
                hintUI.Mostrar($"A Tumba te entregou: {arma.NomeDaArma}. " +
                               $"Habilidade: {arma.NomeHabilidade}.");
        }

        /// <summary>
        /// Coloca a arma sorteada no inventário, se houver um. Falha em silêncio quando não
        /// há inventário na cena: a arma já foi empunhada, e o baú não pode travar por causa
        /// de um sistema que talvez ainda não esteja montado naquela cena.
        /// </summary>
        private void GuardarNoInventario(GameObject quemInterage, ArmaDaTumba qual, string nomeDaArma)
        {
            var inventario = quemInterage.GetComponent<Runtime.Itens.InventarioBridge>();
            if (inventario?.Inventario == null) return;

            var definicao = new Core.Itens.DefinicaoDeItem(
                id: $"arma_{qual}", nome: nomeDaArma, armaEquipavel: qual);

            if (inventario.Inventario.Adicionar(definicao) > 0)
                Debug.LogWarning($"[BauDaTumba] Inventário cheio — '{nomeDaArma}' foi empunhada " +
                                 "mas não guardada.", this);
        }

        /// <summary>
        /// Restaura um baú já aberto numa visita anterior à dungeon.
        ///
        /// <para><b>Não reequipa nada de propósito.</b> A arma empunhada atravessa a troca
        /// de cena por conta própria (<c>EstadoPersistenteDoJogador</c> +
        /// <c>ChavesDeSave.ArmaEquipada</c>); sortear ou equipar de novo aqui entregaria uma
        /// segunda arma — possivelmente diferente da que o jogador está carregando.</para>
        /// </summary>
        private void Start()
        {
            if (!GerenciadorDeSave.JaAconteceu(ChavesDeSave.BauDaTumbaAberto)) return;

            _aberto = true;
            MostrarComoAberto();
        }

        /// <summary>Troca o sprite para o baú aberto. Sem animação — só o estado final.</summary>
        private void MostrarComoAberto()
        {
            if (spriteDoBau != null && spriteAberto != null)
                spriteDoBau.sprite = spriteAberto;
        }
    }
}
