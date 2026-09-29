using System.Collections.Generic;
using UnityEngine;
using FavelaAmarela.Core.Enemies;
using FavelaAmarela.Player;
using FavelaAmarela.Runtime.Itens;

namespace FavelaAmarela.Runtime.Enemies
{
    /// <summary>
    /// Camada Runtime (MonoBehaviour). Adaptador do <see cref="RitoDoReiFSM"/> — o <b>Rito do
    /// Olhar</b>, a luta final, no Trono de Aldebaran. Plano completo em
    /// <c>Docs/KnowledgeBundle/systems/dossie_luta_do_rei.md</c>.
    ///
    /// <para><b>A regra:</b> o Rei vê o Damião. Estar na linha de visão dele drena Resiliência
    /// Mental; estar visto <b>dentro do Altar de Selamento</b> avança o selo; atrás de um Nobre
    /// Fossilizado a mente se ancora. Cada fase muda a sala, não o controle.</para>
    ///
    /// <para><b>O que este adaptador faz, e só isso:</b> mede o mundo a cada quadro (a linha Rei→
    /// Damião contra as pegadas dos Nobres, o ângulo, se está no Altar ou sobre um fragmento),
    /// entrega a leitura ao Core, aplica na mente o que o Core devolve e traduz os eventos do
    /// rito em cena — animação, fala, feixe, Nobre desfeito, Eco. A regra inteira mora no
    /// <see cref="RitoDoReiFSM"/>.</para>
    ///
    /// <para><b>O que saiu (2026-09-28), com a luta dos escudos:</b> <c>IDanificavel</c>,
    /// <c>Vitalidade</c>, a hurtbox, a barra de vida e o <c>IFonteDeEspolio</c>. O Rei não tem
    /// carne nem espólio: é selado, e o jogo termina (decisão D3 do plano).</para>
    /// </summary>
    [AddComponentMenu("Favela Amarela/Enemies/Rei em Amarelo AI")]
    public sealed class ReiEmAmareloAI : MonoBehaviour
    {
        [Header("Relíquias")]
        [Tooltip("Ids dos ItemDef de Artefato que pesam no rito (ex.: 'necronomicon'). Nenhuma é " +
                 "chave: cada uma é um modificador, e sem elas o rito continua vencível.")]
        [SerializeField] private string[] idsDasReliquiasExigidas =
        {
            RitoDoRei.Necronomicon,
            RitoDoRei.Patua,
            RitoDoRei.Anel,
        };

        [Header("O rito")]
        [SerializeField] private ConfiguracaoDoRito configuracao = new ConfiguracaoDoRito();

        [Header("A sala [CENA]")]
        [Tooltip("O Altar de Selamento, no centro do Trono.")]
        [SerializeField] private AltarDeSelamento altar;

        [Tooltip("Os Nobres Fossilizados que dão sombra. O Verbo os desfaz do mais perto do Altar " +
                 "para o mais longe — a ordem é calculada aqui, não a da lista.")]
        [SerializeField] private CoberturaDoTrono[] coberturas;

        [Tooltip("Os altares de relíquia onde arde um fragmento da Peça (Fase 3).")]
        [SerializeField] private PontoFocalDeReliquia[] altaresDeFragmento;

        [Tooltip("O Eco de Carcosa que se manifesta na Queda. Fica inativo até lá.")]
        [SerializeField] private EcoDeCarcosa eco;

        [Tooltip("Meia-largura e meia-altura da elipse, em volta do Altar, em que a entrada do " +
                 "Damião começa o rito.")]
        [SerializeField] private Vector2 areaDeInicio = new Vector2(12f, 6f);

        [Header("O que o jogador vê")]
        [Tooltip("Material dos traços do olhar. Vazio: o do próprio sprite do Rei. [ASSET]")]
        [SerializeField] private Material materialDosTracos;

        [Tooltip("Ordem de desenho do cone da Máscara: acima do chão, abaixo dos atores.")]
        [SerializeField] private int ordemDoFarol = -2000;

        [Tooltip("Fonte do rótulo do selo. Vazia: a da caixa de fala do HUD. [ASSET]")]
        [SerializeField] private Font fonteDaTela;

        [Tooltip("Quanto a câmera sobe enquanto o rito corre, para os pés do Rei entrarem no " +
                 "quadro com o Damião no Altar (a vista do Castelo tem 8,4 un de altura).")]
        [SerializeField] private float subidaDaCamera = 2.2f;

        [Tooltip("Segundos para o Rei se apagar depois de se curvar, na Queda.")]
        [Min(0.1f)]
        [SerializeField] private float duracaoDoApagar = 3f;

        [Header("Animação")]
        [Tooltip("Animator com o ReiEmAmarelo_AC. Vazio: o Rei desenha o quadro parado.")]
        [SerializeField] private Animator animator;

        [Header("Fala")]
        [Tooltip("Caixa onde o rito fala com Damião. Se vazia, usa a do HUD. [CENA]")]
        [SerializeField] private FavelaAmarela.Runtime.UI.TutorialHintUI caixaDeTexto;

        private RitoDoReiFSM _rito;
        private SpriteRenderer _sprite;
        private Collider2D _corpo;
        private Transform _jogador;
        private FavelaAmarela.Runtime.Combat.ResilienciaBridge _mente;
        private ArtefatosBridge _artefatos;
        private CoberturaDoTrono[] _ordemDoVerbo = System.Array.Empty<CoberturaDoTrono>();
        private SinaisDoOlhar _sinais;
        private TelaDoRito _tela;
        private float _apagando = -1f;
        private FavelaAmarela.CameraSystem.IsometricCameraController _camera;
        private bool _primeiraExposicaoDita;

        /// <summary>
        /// Os ids de Artefato que o rito lê — o Rei é a <b>fonte da verdade</b> do que vale trazer
        /// ao Trono. O Carcosa Debugger e o montador da cena leem daqui, sem segunda cópia.
        /// </summary>
        public IReadOnlyList<string> ReliquiasExigidas => idsDasReliquiasExigidas;

        /// <summary>O rito em curso, ou <c>null</c> antes de o Damião entrar no Trono.</summary>
        public RitoDoReiFSM Rito => _rito;

        /// <summary>O Altar de Selamento desta sala.</summary>
        public AltarDeSelamento Altar => altar;

        /// <summary>Os Nobres, na ordem em que o Verbo os desfaz.</summary>
        public IReadOnlyList<CoberturaDoTrono> CoberturasNaOrdemDoVerbo => _ordemDoVerbo;

        /// <summary>A leitura do último quadro — para testes e o Carcosa Debugger.</summary>
        public LeituraDoRito UltimaLeitura { get; private set; }

        /// <summary>Se o Rei via o Damião no último quadro.</summary>
        public bool VendoODamiao { get; private set; }

        /// <summary>Disparado no selamento — a <c>SequenciaDeSelamento</c> fecha o jogo.</summary>
        public event System.Action OnVitoria;

        /// <summary>
        /// De onde sai o olhar: o <b>chão sob o Rei</b>, o centro do colisor do corpo.
        ///
        /// <para>Não é o <c>transform.position</c>: o pivô do sprite fica no canto inferior do
        /// quadro de 88 × 129 px, e a figura desenhada ocupa só a faixa esquerda dele — com a
        /// escala 3,7, o pivô está 3,9 un à direita e 4,3 un abaixo dos pés que se veem. O
        /// colisor foi posto nos pés de verdade; é dele que a linha sai.</para>
        /// </summary>
        public Vector2 OrigemDoOlhar
        {
            get
            {
                var corpo = _corpo != null ? _corpo : GetComponent<Collider2D>();
                if (corpo == null) return transform.position;
                return (Vector2)transform.position
                       + Vector2.Scale(corpo.offset, transform.lossyScale);
            }
        }

        private void Awake()
        {
            _sprite = GetComponent<SpriteRenderer>();
            _corpo = GetComponent<Collider2D>();
            if (animator == null) animator = GetComponent<Animator>();

            if (altar == null)
                Debug.LogError("[ReiEmAmarelo] Sem Altar de Selamento — o selo nunca avança. " +
                               "Rode 'Tools/FavelaAmarela/Trono: montar o Rito do Olhar'.", this);
            if (coberturas == null || coberturas.Length == 0)
                Debug.LogError("[ReiEmAmarelo] Sem Nobres de cobertura — não há onde a mente se " +
                               "ancore, e o rito vira contagem regressiva.", this);
            if (altaresDeFragmento == null) altaresDeFragmento = System.Array.Empty<PontoFocalDeReliquia>();
            if (coberturas == null) coberturas = System.Array.Empty<CoberturaDoTrono>();
        }

        private void Start()
        {
            var jogadorGo = GameObject.FindGameObjectWithTag("Player");
            if (jogadorGo == null)
            {
                Debug.LogError("[ReiEmAmarelo] Nenhum objeto com a tag Player — o rito não tem " +
                               "quem observar.", this);
                return;
            }

            _jogador = jogadorGo.transform;
            _mente = jogadorGo.GetComponentInChildren<FavelaAmarela.Runtime.Combat.ResilienciaBridge>();
            _artefatos = jogadorGo.GetComponent<ArtefatosBridge>();
            if (_mente == null)
                Debug.LogError("[ReiEmAmarelo] Damião sem ResilienciaBridge — o olhar não custaria " +
                               "nada e o Colapso não teria efeito.", this);

            _ordemDoVerbo = OrdenarParaOVerbo(coberturas, altar != null ? (Vector2)altar.transform.position : OrigemDoOlhar);
            if (eco != null) eco.gameObject.SetActive(false);
        }

        /// <summary>
        /// A ordem em que o Verbo desfaz os Nobres: do mais perto do Altar ao mais longe, com
        /// empate decidido pela esquerda. O último é o que resiste.
        /// </summary>
        public static CoberturaDoTrono[] OrdenarParaOVerbo(IEnumerable<CoberturaDoTrono> nobres, Vector2 centroDoAltar)
        {
            var lista = new List<CoberturaDoTrono>();
            foreach (var n in nobres) if (n != null) lista.Add(n);
            lista.Sort((a, b) =>
            {
                float da = Vector2.Distance(a.transform.position, centroDoAltar);
                float db = Vector2.Distance(b.transform.position, centroDoAltar);
                int porDistancia = da.CompareTo(db);
                return porDistancia != 0 ? porDistancia : a.transform.position.x.CompareTo(b.transform.position.x);
            });
            return lista.ToArray();
        }

        private void OnDestroy()
        {
            if (_rito == null) return;
            if (!_rito.Encerrado) Enquadrar(false);
            _rito.OnFaseMudou -= HandleFaseMudou;
            _rito.OnSelado -= HandleSelado;
            _rito.OnColapso -= HandleColapso;
            _rito.Olhar.OnMascaraVaiAbrir -= HandleMascaraVaiAbrir;
            _rito.Fragmentos.OnApareceu -= HandleFragmentoApareceu;
            _rito.Fragmentos.OnLido -= HandleFragmentoLido;
            _rito.Verbo.OnPulso -= HandlePulsoDoVerbo;
        }

        // ── O começo ─────────────────────────────────────────────────────────

        /// <summary>
        /// Começa o rito: lê as relíquias <b>equipadas agora</b>, monta a FSM e liga a tela.
        /// Idempotente. Chamado pelo próprio Rei quando o Damião entra no Trono (e pelo Carcosa
        /// Debugger).
        ///
        /// <para>As relíquias são lidas na entrada, não por quadro: trocar de Artefato no meio da
        /// luta não muda o rito. É a leitura mais simples de explicar ("o que você trouxe").</para>
        /// </summary>
        public void IniciarRitual()
        {
            if (_rito != null) return;

            var p = configuracao.Criar();
            var mods = ModificadoresDoRito.DasReliquias(Equipada(RitoDoRei.Necronomicon),
                                                        Equipada(RitoDoRei.Patua),
                                                        Equipada(RitoDoRei.Anel), p);
            _rito = new RitoDoReiFSM(p, mods, _ordemDoVerbo.Length, altaresDeFragmento.Length);

            _rito.OnFaseMudou += HandleFaseMudou;
            _rito.OnSelado += HandleSelado;
            _rito.OnColapso += HandleColapso;
            _rito.Olhar.OnMascaraVaiAbrir += HandleMascaraVaiAbrir;
            _rito.Fragmentos.OnApareceu += HandleFragmentoApareceu;
            _rito.Fragmentos.OnLido += HandleFragmentoLido;
            _rito.Verbo.OnPulso += HandlePulsoDoVerbo;

            CriarSinais(p);
            Enquadrar(true);
            _rito.Iniciar();
        }

        private bool Equipada(string id) => _artefatos != null && _artefatos.Inventario.Contem(id);

        private void CriarSinais(ParametrosDoRito p)
        {
            var material = materialDosTracos != null ? materialDosTracos
                         : _sprite != null ? _sprite.sharedMaterial : null;
            int camada = _sprite != null ? _sprite.sortingLayerID : 0;
            var pai = transform.parent != null ? transform.parent : transform;

            _sinais = new SinaisDoOlhar(pai, material, camada, ordemDoFarol);
            _tela = new TelaDoRito(pai, FonteDaTela(),
                                   new[] { p.MarcoMascara, p.MarcoPeca, p.MarcoVerbo, p.MarcoQueda });
            _tela.Mostrar(true);
        }

        private Font FonteDaTela()
        {
            if (fonteDaTela != null) return fonteDaTela;
            var caixa = CaixaDeFala;
            var texto = caixa != null ? caixa.GetComponentInChildren<UnityEngine.UI.Text>(true) : null;
            return texto != null ? texto.font : null;
        }

        // ── Cada quadro ──────────────────────────────────────────────────────

        private void Update()
        {
            if (_jogador == null) return;

            if (_rito == null)
            {
                if (DentroDoTrono(_jogador.position)) IniciarRitual();
                return;
            }

            if (_rito.Encerrado)
            {
                Apagar(Time.deltaTime);
                return;
            }

            var leitura = Ler();
            UltimaLeitura = leitura;

            var resultado = _rito.Tick(Time.deltaTime, leitura);
            VendoODamiao = resultado.Exposto;
            AplicarNaMente(resultado.DeltaResiliencia);
            if (_rito.Encerrado) return;

            Mostrar(leitura, resultado.Exposto);
            if (_rito.Fase == FaseDoRito.Queda) Apagar(Time.deltaTime);
        }

        private bool DentroDoTrono(Vector2 p)
            => altar != null && AbrigoDeReliquia.EstaAbrigado(p, altar.transform.position, areaDeInicio.x, areaDeInicio.y);

        /// <summary>Mede o mundo: linha de visão, ângulo, Altar e fragmento sob o pé.</summary>
        private LeituraDoRito Ler()
        {
            Vector2 olho = OrigemDoOlhar;
            Vector2 pe = _jogador.position;

            bool linhaLivre = true;
            for (int i = 0; i < _ordemDoVerbo.Length; i++)
            {
                var n = _ordemDoVerbo[i];
                if (n == null || !n.DePe) continue;
                var caixa = n.Caixa;
                if (LinhaDeVisao.Cruza(olho, pe, caixa)) { linhaLivre = false; break; }
            }

            int sobOPe = -1;
            for (int i = 0; i < altaresDeFragmento.Length; i++)
                if (altaresDeFragmento[i] != null && altaresDeFragmento[i].Sob(pe)) { sobOPe = i; break; }

            bool noAltar = altar != null && altar.Contem(pe);
            return new LeituraDoRito(linhaLivre, LinhaDeVisao.AnguloAPartirDeBaixo(olho, pe), noAltar, sobOPe);
        }

        /// <summary>
        /// Aplica na mente o que o rito devolveu. O dreno vai por
        /// <c>SofrerDrenoContinuo</c> (sem mitigação por fatia — ver a doc dele); a Ancoragem, pelo
        /// caminho de sempre. Mente em zero é o Colapso do jogo; aqui só se encerra o rito.
        /// </summary>
        private void AplicarNaMente(float delta)
        {
            if (_mente == null) return;

            if (delta < 0f) _mente.SofrerDrenoContinuo(-delta);
            else if (delta > 0f) _mente.Ancorar(delta);

            if (_mente.Ligada && _mente.Atual <= 0f) _rito.Colapsar();
        }

        private void Mostrar(in LeituraDoRito leitura, bool exposto)
        {
            Vector2 olho = OrigemDoOlhar;
            _sinais.Fio(exposto, olho, _jogador.position);
            _sinais.Farol(_rito.Fase == FaseDoRito.Mascara, olho, _rito.Olhar.Centro,
                          _rito.Olhar.Largura, _rito.MascaraAberta);
            _tela.Atualizar(_rito.Selo, exposto, Time.deltaTime);
            if (altar != null) altar.MostrarAvanco(exposto && leitura.NoAltar);

            if (exposto && !_primeiraExposicaoDita && _rito.Fase == FaseDoRito.Chegada)
            {
                _primeiraExposicaoDita = true;
                Dizer(Falas.PrimeiraExposicao, 4.5f);
            }
        }

        /// <summary>Na Queda o Rei se curva e se apaga; o sprite some em <c>duracaoDoApagar</c>.</summary>
        private void Apagar(float dt)
        {
            if (_apagando < 0f || _sprite == null) return;
            _apagando += dt;
            var c = _sprite.color;
            c.a = Mathf.Clamp01(1f - _apagando / duracaoDoApagar);
            _sprite.color = c;
        }

        // ── Eventos do rito ──────────────────────────────────────────────────

        private void HandleFaseMudou(FaseDoRito anterior, FaseDoRito nova)
        {
            TocarAnimacaoDa(nova);

            switch (nova)
            {
                case FaseDoRito.Chegada:
                    Dizer(Falas.Chegada + FaltaDeReliquias(), 4.5f);
                    break;
                case FaseDoRito.Mascara:
                    Dizer(Falas.Mascara, 3.5f);
                    break;
                case FaseDoRito.Peca:
                    Dizer(Falas.Peca, 3.5f);
                    break;
                case FaseDoRito.Verbo:
                    Dizer(Falas.Verbo, 3.5f);
                    break;
                case FaseDoRito.Queda:
                    Dizer(Falas.Queda, 4f);
                    _apagando = 0f;
                    _sinais?.Esconder();
                    if (altar != null) altar.MostrarAvanco(false);
                    if (eco != null) eco.gameObject.SetActive(true);
                    break;
            }

            if (anterior == FaseDoRito.Peca)
                foreach (var a in altaresDeFragmento) if (a != null) a.MostrarFragmento(false);
        }

        private void HandleSelado()
        {
            Encerrar();
            OnVitoria?.Invoke();
        }

        private void HandleColapso() => Encerrar();

        /// <summary>Sobe a vista durante o rito (ver <c>IsometricCameraController.DeslocamentoDeEnquadramento</c>).</summary>
        private void Enquadrar(bool noRito)
        {
            if (_camera == null)
            {
                var cam = Camera.main;
                if (cam != null) _camera = cam.GetComponent<FavelaAmarela.CameraSystem.IsometricCameraController>();
            }
            if (_camera != null)
                _camera.DeslocamentoDeEnquadramento = noRito ? new Vector2(0f, subidaDaCamera) : Vector2.zero;
        }

        private void Encerrar()
        {
            Enquadrar(false);
            _sinais?.Esconder();
            _tela?.Mostrar(false);
            if (altar != null) altar.MostrarAvanco(false);
            if (eco != null) eco.gameObject.SetActive(false);
            foreach (var a in altaresDeFragmento) if (a != null) a.MostrarFragmento(false);
        }

        /// <summary>
        /// A Máscara vai abrir em meio segundo. Som, e não só imagem: quem está jogando certo
        /// está olhando para o próprio Damião, não para o topo da tela onde o Rei está.
        /// </summary>
        private void HandleMascaraVaiAbrir() => Tocar(FavelaAmarela.Runtime.Audio.SomDoJogo.EntrouEmPanico);

        private void HandleFragmentoApareceu(int i)
        {
            if (i >= 0 && i < altaresDeFragmento.Length && altaresDeFragmento[i] != null)
                altaresDeFragmento[i].MostrarFragmento(true);
        }

        private void HandleFragmentoLido(int i)
        {
            if (i >= 0 && i < altaresDeFragmento.Length && altaresDeFragmento[i] != null)
                altaresDeFragmento[i].MostrarFragmento(false);
            Tocar(FavelaAmarela.Runtime.Audio.SomDoJogo.ItemRecolhido);
        }

        /// <summary>O Verbo: o pulso soa, e um Nobre se desfaz (o índice é a ordem do Verbo).</summary>
        private void HandlePulsoDoVerbo(int desfeita)
        {
            Tocar(FavelaAmarela.Runtime.Audio.SomDoJogo.EntrouEmPanico);
            if (desfeita >= 0 && desfeita < _ordemDoVerbo.Length && _ordemDoVerbo[desfeita] != null)
                _ordemDoVerbo[desfeita].Desfazer();
        }

        private void Tocar(FavelaAmarela.Runtime.Audio.SomDoJogo som)
            => FavelaAmarela.Runtime.Audio.MixerDeAudio.Instancia?.Tocar(som, transform.position);

        // ── Fala ─────────────────────────────────────────────────────────────

        /// <summary>
        /// As falas do rito. <b>Rascunho</b> — o texto final é do Vini (plano, §7.3); vocabulário
        /// da skill <c>favela-lore-enforcer</c>.
        ///
        /// <para>Ficam pouco na tela (3,5 a 4,5 s) de propósito: a caixa de fala do HUD cobre a
        /// metade de baixo da vista, que é onde o Damião luta com a câmera subida.</para>
        /// </summary>
        private static class Falas
        {
            internal const string Chegada = "Ele te vê. Tudo o que te vê te desfaz.";
            internal const string PrimeiraExposicao =
                "Fica no altar, e o selo aperta. Esconde-te na sombra dos nobres, e a tua mente volta.";
            internal const string Mascara = "A Máscara se volta. Não estejas onde ela olha.";
            internal const string Peca = "Páginas da Peça ardem nos altares. Lê-las custa o que resta de ti.";
            internal const string Verbo = "Ele fala. A pedra não resiste à voz.";
            internal const string Queda = "Ele se vai — e o que fica não descansa. Não pares.";
        }

        /// <summary>
        /// " Sem o Necronomicon e o Patuá, o selo pesa mais." — uma frase para todas as que
        /// faltam, pelo nome diegético. Vazio quando o Damião trouxe as três.
        /// </summary>
        private string FaltaDeReliquias()
        {
            var nomes = new List<string>();
            foreach (var id in idsDasReliquiasExigidas)
                if (!Equipada(id)) nomes.Add(_artefatos?.Def(id)?.Nome ?? "uma relíquia");

            if (nomes.Count == 0) return "";
            string lista = nomes.Count == 1 ? nomes[0]
                         : string.Join(", ", nomes.GetRange(0, nomes.Count - 1)) + " e " + nomes[nomes.Count - 1];
            return $" Sem {lista}, o selo pesa mais.";
        }

        private FavelaAmarela.Runtime.UI.TutorialHintUI CaixaDeFala
            => caixaDeTexto != null ? caixaDeTexto : FavelaAmarela.Runtime.UI.TutorialHintUI.Instancia;

        private void Dizer(string texto, float duracao)
        {
            var caixa = CaixaDeFala;
            if (caixa != null) caixa.Mostrar(texto, duracao);
        }

        // ── Animação ─────────────────────────────────────────────────────────

        /// <summary>
        /// Um clipe por transição (plano, §7.4). Quem manda é o rito — o <c>ReiEmAmarelo_AC</c>
        /// não tem teia de transições, para não haver segunda fonte de verdade.
        /// </summary>
        private void TocarAnimacaoDa(FaseDoRito fase)
        {
            if (animator == null || animator.runtimeAnimatorController == null) return;
            if (fase == FaseDoRito.Selado) return;   // já caiu na Queda; não recomeçar o clipe

            int clipe = fase switch
            {
                FaseDoRito.Mascara => Anim.Selar,
                FaseDoRito.Peca => Anim.Dano,
                FaseDoRito.Verbo => Anim.Desvelo,
                FaseDoRito.Queda => Anim.Queda,
                _ => Anim.Idle,
            };
            animator.Play(clipe, 0, 0f);
        }

        /// <summary>Hashes do <c>ReiEmAmarelo_AC</c>, resolvidos uma vez (sem string em caminho quente).</summary>
        private static class Anim
        {
            internal static readonly int Idle = Animator.StringToHash("idle");
            internal static readonly int Selar = Animator.StringToHash("selar");
            internal static readonly int Desvelo = Animator.StringToHash("desvelo");
            internal static readonly int Dano = Animator.StringToHash("dano");
            internal static readonly int Queda = Animator.StringToHash("queda");
        }
    }
}
