using System;
using UnityEngine;
using FavelaAmarela.Core.Combat;
using FavelaAmarela.Core.GameLoop;
using FavelaAmarela.Core.Stealth;
using FavelaAmarela.Core.Environment;
using FavelaAmarela.Player;
using FavelaAmarela.Runtime.Combat;
using FavelaAmarela.Runtime.Enemies;
using FavelaAmarela.Runtime.Environment;

namespace FavelaAmarela.Runtime.GameLoop
{
    /// <summary>
    /// <b>Casca de compatibilidade.</b> Não faz mais nada por conta própria: encaminha para o
    /// <see cref="GameLoopBootstrap"/> e para os componentes focados irmãos.
    ///
    /// <para><b>Por que ainda existe:</b> 18 arquivos alcançam este singleton. Removê-lo junto
    /// com a extração transformaria uma refatoração verificável em 18 mudanças simultâneas sem
    /// rede. Os encaminhamentos estão marcados <c>[Obsolete]</c> de propósito — o compilador
    /// passa a emitir a lista exata de call-sites, e essa lista <b>é</b> o roteiro da rodada de
    /// migração seguinte (Fase 5 do plano).</para>
    ///
    /// <para><b>Ordem de execução −100</b>, atrás do <see cref="GameLoopBootstrap"/> (−200): os
    /// POCOs precisam existir antes de esta casca responder qualquer consulta.</para>
    ///
    /// <para><b>O que morreu na extração de 2026-08-14</b>, e não foi migrado:</para>
    /// <list type="bullet">
    ///   <item><c>telaTransicaoDeFase</c> e <c>gameplayRoot</c> — <c>fileID: 0</c> nas 5 cenas,
    ///   restos da cena anterior à Tumba.</item>
    ///   <item>O ramo <c>if (atual == GameState.Menu)</c> do antigo <c>HandleStateChanged</c>, que
    ///   restaurava Resiliência e Vigor. <b>Nenhum código de produção transiciona para
    ///   <c>GameState.Menu</c></b> (verificado por busca): desde que o menu virou cena própria em
    ///   2026-08-11, trocar de cena já recria tudo do zero. Era código inalcançável.</item>
    /// </list>
    /// </summary>
    [AddComponentMenu("Favela Amarela/Game Manager (legado)")]
    [DefaultExecutionOrder(-100)]
    public class GameManager : MonoBehaviour
    {
        /// <summary>
        /// Ponto de acesso legado. Prefira injeção: peça a dependência por <c>Bind()</c> a partir
        /// do <see cref="GameLoopBootstrap"/> em vez de alcançar este singleton.
        /// </summary>
        public static GameManager Instance { get; private set; }

        // Os campos serializados saíram daqui em 2026-08-14, depois de
        // 'Tools/FavelaAmarela/Migrar para GameLoopBootstrap' copiar os valores das 4 cenas:
        // maxResiliencia e fracaoPanico foram para o GameLoopBootstrap, telaPause para o
        // GameStatePresenter e sequenciaColapso para o PlayerDeathController. A ordem importava —
        // a Unity descarta o valor serializado de um campo que não existe mais na classe, então
        // remover antes de migrar teria apagado as referências de cena.

        private GameLoopBootstrap _bootstrap;
        private CutsceneController _cutscene;
        private CompanionManager _companheiro;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;

            _bootstrap = GetComponent<GameLoopBootstrap>();
            _cutscene = GetComponent<CutsceneController>();
            _companheiro = GetComponent<CompanionManager>();

            if (_bootstrap == null)
                Debug.LogError("[GameManager] Sem GameLoopBootstrap no mesmo GameObject. Esta " +
                               "casca só encaminha — sem ele, tudo que passa por " +
                               "GameManager.Instance devolve nulo.", this);
        }

        private void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        // ── O que sobrou, e por quê ──────────────────────────────────────────
        //
        // A Fase 5 (2026-08-18) removeu SETE encaminhamentos cujos consumidores migraram para
        // injeção: StateMachine, TriggerTransicaoDeFase, RegistrarYugNeth, TempestadeAmbiente,
        // DefinirInvulneravel, SoundBroadcaster e Environment. Os dois últimos nunca tiveram
        // consumidor externo — nasceram encaminhamento morto.
        //
        // Os quatro abaixo ficam, e a razão é a mesma para todos: seus consumidores TAMBÉM usam
        // `.Resiliencia`, que tem rodada própria (19 usos em 11 arquivos, dois deles chamando
        // dentro do Update). Migrar só metade deixaria os mesmos arquivos dependendo desta casca
        // — trabalho sem redução de acoplamento.

        /// <summary>Resiliência Mental de Damião.</summary>
        [Obsolete("Injete ResilienciaMental via Bind(). Migração própria — 19 call-sites.")]
        public ResilienciaMental Resiliencia => _bootstrap != null ? _bootstrap.Resiliencia : null;

        /// <summary>Vitalidade corpórea de Damião, ou <c>null</c> se não houver bridge na cena.</summary>
        [Obsolete("Injete Vitalidade via Bind(). Sai junto com .Resiliencia (mesmo consumidor).")]
        public Vitalidade VitalidadeDoJogador => _bootstrap != null ? _bootstrap.VitalidadeDoJogador : null;

        /// <summary>
        /// Verdadeiro enquanto Damião está preso numa sequência roteirizada e não pode agir.
        /// Fontes de morte instantânea por toque devem respeitar isto.
        /// </summary>
        [Obsolete("Injete CutsceneController via Bind(). Sai junto com .Resiliencia.")]
        public bool JogadorInvulneravel => _cutscene != null && _cutscene.JogadorInvulneravel;

        /// <summary>O companheiro Yug-Neth, se já libertado (<c>null</c> antes disso).</summary>
        [Obsolete("Injete CompanionManager via Bind(). Sai junto com .Resiliencia.")]
        public YugNethAI YugNeth => _companheiro != null ? _companheiro.YugNeth : null;

    }
}
