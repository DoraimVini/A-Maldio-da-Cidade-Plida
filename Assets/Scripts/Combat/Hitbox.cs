using System.Collections.Generic;
using UnityEngine;
using FavelaAmarela.Core.Abilities;

namespace FavelaAmarela.Runtime.Combat
{
    /// <summary>
    /// A área que <b>causa</b> dano, ligada só durante os <b>quadros ativos</b> do golpe.
    ///
    /// <para><b>O defeito que isto conserta (2026-08-21).</b> O Vini relatou que a luta contra o
    /// Byakhee "não tem feel bom". Achados os motivos um a um — o chefe sem colisor (era
    /// inacertável), o golpe sem som, a física girando — sobrou o mais importante: <b>o dano dos
    /// inimigos era um teste de distância instantâneo</b>. O <c>ByakheeAI.GolpearComGarras</c>
    /// rodava <b>uma vez</b>, na entrada do estado <c>Pousado</c>, e fazia
    /// <c>Vector2.Distance &lt;= alcance</c>.</para>
    ///
    /// <para>Isso quebra o combate de ARPG em dois pontos:</para>
    /// <list type="number">
    ///   <item><b>Não há janela.</b> Sendo um teste de um quadro só, não existe "esquivar no
    ///   tempo certo" — só "estar longe naquele instante exato". O mergulho telegrafado vira
    ///   decoração: o jogador não tem o que ler nem quando reagir.</item>
    ///   <item><b>Não há direção.</b> Distância é radial. Estar <b>atrás</b> do Byakhee, a 1,4
    ///   unidade, levava garrada igual. Lê como injustiça, porque contradiz o que se vê.</item>
    /// </list>
    ///
    /// <para><b>Como esta classe resolve:</b> <see cref="Armar"/> abre uma janela de
    /// <c>duracaoAtiva</c> segundos. Enquanto ela dura, a área é consultada a cada
    /// <c>FixedUpdate</c> — então atravessar a área <i>durante</i> o golpe acerta, e sair antes
    /// da janela abrir (ou entrar depois de fechar) não acerta. É isso que torna a esquiva uma
    /// decisão de tempo em vez de um teste de posição.</para>
    ///
    /// <para><b>Por que consulta e não colisor com trigger:</b> um trigger só dispara
    /// <c>OnTriggerEnter2D</c> em quem <b>entra</b>. Se a janela abre com o alvo <b>já</b>
    /// sobreposto — o caso mais comum, porque o inimigo mira em quem está perto — o evento nunca
    /// vem e o golpe passa branco. A consulta explícita pega ambos os casos. Usa
    /// <c>Physics2D.OverlapCircle</c> com <c>ContactFilter2D</c>, o mesmo padrão que
    /// <c>MaoFisicaBridge.ResolverGolpe</c> já usa neste projeto.</para>
    ///
    /// <para><b>Uma vez por ativação:</b> cada alvo atingido entra num conjunto que só é limpo
    /// na próxima <see cref="Armar"/>. Sem isso, uma janela de 0,2 s a 50 fps aplicaria o dano
    /// dez vezes.</para>
    /// </summary>
    [AddComponentMenu("Favela Amarela/Combate/Hitbox (área que causa dano)")]
    public sealed class Hitbox : MonoBehaviour
    {
        [Header("Forma")]
        [Tooltip("Raio da área de acerto, em unidades de mundo.")]
        [Min(0.05f)]
        [SerializeField] private float raio = 1f;

        [Tooltip("Deslocamento a partir deste objeto. Aponta para a frente do golpe.")]
        [SerializeField] private Vector2 deslocamento = Vector2.zero;

        [Tooltip("Quanta PROFUNDIDADE de chão o golpe abrange, em unidades de mundo, para " +
                 "cada lado do ponto onde ele cai. 0,5 = uma célula isométrica. Zero ou " +
                 "negativo desliga o portão.")]
        [SerializeField] private float profundidadeMaxima;

        /// <summary>
        /// Uma célula isométrica de profundidade para cada lado — o valor que o Vini escolheu
        /// em 2026-09-04 para o golpe do Damião.
        /// </summary>
        public const float ProfundidadeDeUmaCelula = Core.Player.BaseIsometrica.AlturaDeCelulaPadrao;

        [Header("Alvo")]
        [Tooltip("Camadas de hurtbox que este golpe pode atingir " +
                 "(PlayerHurtbox para inimigos, EnemyHurtbox para o jogador).")]
        [SerializeField] private LayerMask camadasAlvo;

        [Tooltip("Golpe do jogador: nunca acerta quem carrega o marcador Aliado. " +
                 "Golpe de inimigo: DESLIGADO — o Byakhee pode e deve derrubar o companheiro.")]
        [SerializeField] private bool pouparAliados;

        // Buffer reusado: alocar por golpe seria lixo em hot path (Regra de Ouro 1).
        private readonly Collider2D[] _buffer = new Collider2D[16];
        private readonly HashSet<Hurtbox> _jaAtingidos = new HashSet<Hurtbox>();

        private ContactFilter2D _filtro;
        private ArmaResult _resultado;
        private float _fimDaJanela;
        private bool _ativa;

        /// <summary>Se a janela de acerto está aberta agora.</summary>
        public bool Ativa => _ativa;

        /// <summary>Centro da área em coordenadas de mundo.</summary>
        public Vector2 Centro => (Vector2)transform.position + deslocamento;

        /// <summary>
        /// Profundidade de chão que o golpe abrange para cada lado, em unidades de mundo.
        /// <c>&lt;= 0</c> significa portão desligado.
        /// </summary>
        public float ProfundidadeMaxima => profundidadeMaxima;

        /// <summary>
        /// Onde o golpe cai <b>no plano do chão</b> — a raiz de quem golpeia mais o
        /// deslocamento direcional.
        ///
        /// <para><b>Não é o Y de <see cref="Centro"/>, e a diferença é o ponto todo.</b> O
        /// círculo da consulta sai da altura do TORSO (ver <see cref="AlturaDoTorso"/>), porque
        /// é lá que moram as hurtboxes — corpos são altos. Mas altura de corpo não é
        /// profundidade de chão: neste isométrico quem diz profundidade é o Y da raiz, o mesmo
        /// que o <c>DynamicYSort</c> usa para <c>sortingOrder</c>. Medir o portão pelo centro
        /// do círculo puniria o golpe por o alvo ser alto.</para>
        /// </summary>
        public float AlturaDeChaoDoGolpe => AlturaDeChaoDe(_corpoDoDono, transform.parent)
                                            + deslocamento.y;

        /// <summary>
        /// O Y de chão de um ator. <b>Não use <c>transform.root</c> para isto.</b>
        ///
        /// <para>Foi exatamente o que quebrou o combate em 2026-09-04: numa cena real os atores
        /// são filhos de contêineres de organização (<c>Inimigos_Playtest</c>,
        /// <c>TumbaDeAbdul_Conteudo</c>), e <c>transform.root</c> devolve o CONTÊINER, que está
        /// em y = 0. Com o Damião por volta de y = -14, a diferença dava 14 e o portão rejeitava
        /// TODO golpe. O teste não pegou porque montava jogador e inimigo soltos na raiz, onde
        /// <c>transform.root</c> é o próprio ator.</para>
        ///
        /// <para>Quem marca o ator é o <see cref="Rigidbody2D"/>: ele mora no objeto do ator,
        /// nunca no contêiner de organização, que é um <c>GameObject</c> vazio.</para>
        /// </summary>
        private static float AlturaDeChaoDe(Rigidbody2D corpo, Transform reserva)
        {
            if (corpo != null) return corpo.position.y;
            return reserva != null ? reserva.position.y : 0f;
        }

        /// <summary>Corpo do dono, resolvido uma vez. Ver <see cref="AlturaDeChaoDe"/>.</summary>
        private Rigidbody2D _corpoDoDono;

        private void Awake()
        {
            ReconstruirFiltro();
            _corpoDoDono = GetComponentInParent<Rigidbody2D>();
        }

        private void ReconstruirFiltro()
        {
            _filtro = new ContactFilter2D();
            // Hurtboxes são triggers, então sem isto a consulta não acharia nenhuma.
            _filtro.useTriggers = true;
            _filtro.SetLayerMask(camadasAlvo);
        }

        /// <summary>
        /// Altura do <b>meio do corpo</b> do dono, em unidades locais.
        ///
        /// <para><b>O defeito que isto conserta (2026-09-03).</b> A hitbox nascia com
        /// <c>SetParent(dono.transform, false)</c> e mais nada — ou seja, no <b>pé</b>, porque
        /// o pivô de todo o elenco é BottomCenter (o jogo ordena profundidade por
        /// <c>-worldCenter.y</c>). Com raio 0,6, o círculo do golpe cobria de y −0,60 a +0,60:
        /// <b>metade dele debaixo do chão</b>.</para>
        ///
        /// <para>E as hurtboxes ficam no <b>corpo</b>, não no pé: a do alvo padrão vai de
        /// y 0,14 a 1,86. Medido, a sobreposição vertical era de <b>0,46 de 1,72 — 27% do
        /// corpo</b>, só a canela. Daqui sai a queixa de "meu golpe passa por baixo" e a
        /// sensação de acerto que não vem. Com a origem no meio do corpo a sobreposição vai
        /// para 70%.</para>
        ///
        /// <para><b>Derivada da arte, e não um 0,5 fixo</b>, pela mesma fonte que
        /// <c>Hurtbox.GarantirPara</c> usa (<c>sprite.bounds.center</c>): assim a garra de um
        /// Byakhee de 4,6 unidades sai do corpo dele, e não da altura do peito do Damião.</para>
        /// </summary>
        private static float AlturaDoTorso(GameObject dono)
        {
            var sr = dono.GetComponentInChildren<SpriteRenderer>(true);

            if (sr == null || sr.sprite == null)
            {
                Debug.LogWarning($"[Hitbox] '{dono.name}' não tem sprite — o golpe sai do PÉ, " +
                                 "e a hurtbox de quem ele tenta acertar está no corpo. Metade " +
                                 "do círculo fica debaixo do chão.", dono);
                return 0f;
            }

            return sr.sprite.bounds.center.y;
        }

        /// <summary>
        /// Define a geometria do golpe a partir de dado — o alcance, o raio e a camada que
        /// esta hitbox pode acertar.
        ///
        /// <para>Existe porque a geometria deixou de ser propriedade do <b>ator</b> e passou a
        /// ser propriedade da <b>arma</b> (ver <c>BaseDeArma</c>). Antes de 2026-08-27 havia um
        /// <c>alcance = 1.2f</c> no <c>MaoFisicaBridge</c> valendo para todas as armas, e por
        /// isso um estilete e um alfanje tinham a mesma pegada.</para>
        /// </summary>
        /// <param name="raioDoGolpe">Raio da área atingida.</param>
        /// <param name="distanciaAFrente">Do corpo até o centro da área.</param>
        /// <param name="camadas">Camadas de hurtbox que este golpe alcança.</param>
        public void Configurar(float raioDoGolpe, float distanciaAFrente, LayerMask camadas)
        {
            raio = Mathf.Max(0.05f, raioDoGolpe);

            // Preserva a direção corrente e troca só a distância: `Armar` gira o deslocamento
            // mantendo a magnitude, então uma magnitude zero aqui faria a direção do golpe ser
            // ignorada para sempre — e o golpe sairia sempre em cima do próprio ator.
            Vector2 direcao = deslocamento.sqrMagnitude > 0.0001f
                ? deslocamento.normalized
                : Vector2.right;

            deslocamento = direcao * Mathf.Max(0.01f, distanciaAFrente);

            camadasAlvo = camadas;
            ReconstruirFiltro();
        }

        /// <summary>
        /// Acha (ou cria) a hitbox de um ator, já configurada.
        ///
        /// <para>Nasce <b>inativa</b> e só é ligada depois de configurada — mesmo padrão de
        /// <c>CarcosaDebuggerWindow.CriarCorpoDoChefe</c> —, para nenhum <c>Awake</c> rodar
        /// antes de a camada alvo existir.</para>
        /// </summary>
        /// <param name="profundidade">
        /// Profundidade de chão do portão, em unidades de mundo. O padrão é
        /// <see cref="ProfundidadeDeUmaCelula"/>.
        ///
        /// <para><b>Por que o portão nasce LIGADO aqui e DESLIGADO no campo serializado.</b>
        /// Este método só é chamado pelo <c>MaoFisicaBridge</c> — o golpe do Damião, que é
        /// onde a profundidade de três células foi medida e reclamada. As outras dez hitboxes
        /// do projeto são autoradas em prefab, e uma delas é a garra do Byakhee, um chefe que
        /// ataca <b>mergulhando</b>: ligar o portão nela mudaria o comportamento de um chefe
        /// sem que ninguém tivesse medido o efeito. Campo novo com padrão ligado teria feito
        /// exatamente isso, em silêncio, nos dez prefabs de uma vez.</para>
        /// </param>
        public static Hitbox GarantirPara(GameObject dono, string nome, LayerMask camadas,
                                          float raioDoGolpe, float distanciaAFrente,
                                          bool pouparAliados = false,
                                          float profundidade = ProfundidadeDeUmaCelula)
        {
            if (dono == null) return null;

            float altura = AlturaDoTorso(dono);

            foreach (var existente in dono.GetComponentsInChildren<Hitbox>(true))
            {
                if (existente.gameObject.name != nome) continue;
                existente.pouparAliados = pouparAliados;
                existente.profundidadeMaxima = profundidade;
                existente.Configurar(raioDoGolpe, distanciaAFrente, camadas);
                existente.transform.localPosition = new Vector3(0f, altura, 0f);
                return existente;
            }

            var go = new GameObject(nome);
            go.SetActive(false);
            go.transform.SetParent(dono.transform, false);
            go.transform.localPosition = new Vector3(0f, altura, 0f);

            var hitbox = go.AddComponent<Hitbox>();
            hitbox.pouparAliados = pouparAliados;
            hitbox.profundidadeMaxima = profundidade;
            hitbox.Configurar(raioDoGolpe, distanciaAFrente, camadas);

            go.SetActive(true);
            return hitbox;
        }

        /// <summary>
        /// Abre a janela de acerto por <paramref name="duracaoAtiva"/> segundos, carregando o
        /// <paramref name="resultado"/> que será aplicado em quem for atingido.
        /// </summary>
        /// <param name="direcao">
        /// Para onde o golpe aponta. Gira o <see cref="deslocamento"/> para essa direção, que é
        /// o que dá <b>frente</b> ao ataque — atrás do atacante deixa de ser zona de dano.
        /// Passe <c>Vector2.zero</c> para manter o deslocamento como está (golpe radial, ex.: um
        /// baque de pouso).
        /// </param>
        public void Armar(ArmaResult resultado, float duracaoAtiva, Vector2 direcao = default)
        {
            // A checagem mora AQUI, e não no Awake, desde 2026-08-27: uma hitbox construída em
            // runtime é configurada depois de existir, então reclamar no Awake acusaria toda
            // hitbox nova por um estado que dura microssegundos. Aqui a reclamação é verdadeira:
            // é o instante em que um golpe sai sem poder acertar nada.
            if (camadasAlvo.value == 0)
                Debug.LogError($"[Hitbox] '{name}' foi armada sem camada alvo — este golpe não " +
                               "pode acertar nada.", this);

            _resultado = resultado;
            _fimDaJanela = Time.time + Mathf.Max(0f, duracaoAtiva);
            _ativa = true;
            _jaAtingidos.Clear();
            _jaReclamouDeAlvoSurdo = false;

            if (direcao.sqrMagnitude > 0.0001f)
            {
                float distancia = deslocamento.magnitude;
                if (distancia > 0.0001f) deslocamento = direcao.normalized * distancia;
            }

            // Consulta já neste quadro: uma janela curta poderia fechar antes do próximo
            // FixedUpdate e o golpe sairia sem nunca ter sido testado.
            Consultar();
        }

        /// <summary>Fecha a janela antes do tempo (interrupção, atordoamento, morte).</summary>
        public void Desarmar() => _ativa = false;

        private void FixedUpdate()
        {
            if (!_ativa) return;

            if (Time.time >= _fimDaJanela)
            {
                _ativa = false;
                return;
            }

            Consultar();
        }

        /// <summary>
        /// Já reclamou de "achei colisor e não achei hurtbox" nesta ativação? Uma reclamação
        /// por golpe, não uma por <c>FixedUpdate</c> — a janela roda várias vezes.
        /// </summary>
        private bool _jaReclamouDeAlvoSurdo;

        private void Consultar()
        {
            // Mostra a geometria EXATA que a física acabou de receber. A chamada some do
            // binário em build de release (ConditionalAttribute), então não custa nada aqui.
            Diagnostico.VisualizadorDeGolpes.RegistrarCirculo(
                Centro, raio, Diagnostico.VisualizadorDeGolpes.CorDeGolpe);

            int total = Physics2D.OverlapCircle(Centro, raio, _filtro, _buffer);

            for (int i = 0; i < total; i++)
            {
                var hurtbox = _buffer[i].GetComponent<Hurtbox>();
                if (hurtbox == null) hurtbox = _buffer[i].GetComponentInParent<Hurtbox>();

                if (hurtbox == null)
                {
                    // ESTE é o diagnóstico que faltava em 2026-08-27, e a falta dele custou
                    // uma noite inteira de trabalho parecer sadia enquanto a Tumba estava
                    // intocável. A consulta ACHOU um colisor na camada alvo e não achou
                    // hurtbox nenhuma nele nem acima dele -- quase sempre porque a máscara
                    // inclui a camada do colisor de MOVIMENTO (Enemy) mas não a da hurtbox
                    // (EnemyHurtbox), que é um objeto FILHO.
                    //
                    // Sem isto, o golpe passa branco em silêncio, que é indistinguível de
                    // "errei a mira".
                    if (!_jaReclamouDeAlvoSurdo)
                    {
                        _jaReclamouDeAlvoSurdo = true;
                        Debug.LogError(
                            $"[Hitbox] '{name}' acertou o colisor '{_buffer[i].name}' " +
                            $"(camada '{LayerMask.LayerToName(_buffer[i].gameObject.layer)}') " +
                            "e não achou Hurtbox nele nem acima dele — o golpe passou branco. " +
                            "Quase sempre é máscara de camada sem a camada da hurtbox: ela " +
                            "vive num objeto FILHO, e a busca sobe, não desce.", this);
                    }

                    continue;
                }

                // Não se fere a si mesmo.
                //
                // A versão anterior comparava com transform.root, e carregava a MESMA armadilha
                // que quebrou o portão de profundidade em 2026-09-04: num contêiner de cena
                // compartilhado (Inimigos_Playtest, TumbaDeAbdul_Conteudo), transform.root é o
                // contêiner, e "todo inimigo é filho do contêiner" leria como "todo inimigo sou
                // eu" -- o golpe pularia o elenco inteiro. Hoje não acontece porque o Damião é
                // raiz de cena, mas isso é sorte de arrumação, não invariante.
                //
                // Comparar o CORPO é exato: cada ator tem o seu, e contêiner de organização é
                // GameObject vazio, sem Rigidbody2D.
                var corpoDoAlvo = _buffer[i].attachedRigidbody;
                bool souEu = _corpoDoDono != null && corpoDoAlvo != null
                    ? corpoDoAlvo == _corpoDoDono
                    : hurtbox.transform.IsChildOf(transform.parent != null
                                                  ? transform.parent : transform);
                if (souEu) continue;

                // ── PORTÃO DE PROFUNDIDADE (decisão do Vini, 2026-09-04: uma célula) ──
                //
                // Sem ele o golpe alcançava TRÊS CÉLULAS de profundidade. A conta: alcance 1,2
                // mais raio 0,6, saindo da altura do torso (~1,0), cobre o chão de 0,8 a 3,2
                // células ao norte de quem bate. A causa é a projeção: uma unidade de mundo em
                // Y vale DUAS células de chão (célula 1,0 x 0,5), então um círculo verdadeiro
                // em mundo é uma elipse deitada no chão -- funda o dobro do que é larga.
                //
                // Achatar a hitbox só levaria de 3,0 para 2,4 células, porque o problema não é
                // a forma do círculo e sim o eixo em que ele é medido. O portão mede em
                // PROFUNDIDADE DE CHÃO, comparando raízes, e por isso corta de verdade.
                //
                // O ponto de referência é AlturaDeChaoDoGolpe -- onde o golpe cai --, e não a
                // raiz de quem bate. Fosse a raiz, golpear para o norte erraria: o alvo à
                // frente estaria a 1,2 de profundidade e o portão o rejeitaria, quando ele é
                // exatamente quem se quis acertar.
                if (profundidadeMaxima > 0f)
                {
                    float profundidade = Mathf.Abs(
                        AlturaDeChaoDe(_buffer[i].attachedRigidbody, hurtbox.transform)
                        - AlturaDeChaoDoGolpe);

                    if (profundidade > profundidadeMaxima) continue;
                }

                // Aliados (Yug-Neth e companheiros futuros) nunca são atingidos pelo golpe do
                // jogador -- nem por acidente no meio de uma luta. A taxonomia de layers do
                // projeto é um conjunto fechado, então quem protege é este MARCADOR, não uma
                // camada própria (ver YugNethAI). A regra é do golpe e não da hitbox: o Byakhee
                // pode e deve derrubar o companheiro.
                if (pouparAliados && hurtbox.GetComponentInParent<Aliado>() != null) continue;

                if (!_jaAtingidos.Add(hurtbox)) continue;

                hurtbox.Receber(_resultado);

                // Corpo, do lado de quem APANHA. Este é o caminho pelo qual o inimigo acerta
                // o jogador (hoje só as garras do Byakhee); o caminho oposto -- o golpe do
                // Damião -- é resolvido em MaoFisicaBridge.ResolverGolpe. São os dois únicos
                // pontos onde um golpe aterrissa, e por isso os dois únicos que precisam
                // conhecer a repulsão: nenhum dos 9 arquivos de IA foi tocado.
                //
                // A direção sai do CENTRO da hitbox para o alvo, e não da direção do golpe:
                // quem está na borda do círculo tem de ser jogado para fora, não para o lado.
                var empurrao = RepulsaoDeImpacto.GarantirPara(hurtbox);
                if (empurrao != null)
                    empurrao.Empurrar((Vector2)hurtbox.transform.position - Centro,
                                      _resultado.ForcaRepulsao);

                HitStop.Bater(_resultado.Dano);
            }
        }

        /// <summary>
        /// Desenha a área no Scene view: vermelha enquanto ativa, cinza em repouso. Uma hitbox
        /// invisível é impossível de calibrar — e calibrar é justamente o trabalho de dar feel.
        /// </summary>
        private void OnDrawGizmosSelected()
        {
            Gizmos.color = Application.isPlaying && _ativa
                ? new Color(1f, 0.2f, 0.2f, 0.9f)
                : new Color(0.6f, 0.6f, 0.6f, 0.4f);

            Gizmos.DrawWireSphere(Centro, raio);
        }
    }
}
