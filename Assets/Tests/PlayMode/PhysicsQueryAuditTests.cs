using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using FavelaAmarela.Core.Stealth;
using FavelaAmarela.Runtime.Enemies;
using FavelaAmarela.Runtime.Interaction;
using FavelaAmarela.Runtime.Itens;
using FavelaAmarela.Player;

namespace FavelaAmarela.Tests.PlayMode
{
    /// <summary>
    /// Audita <b>cada tipo de consulta de proximidade</b> que o jogo usa, com o jogo rodando.
    ///
    /// <para><b>Por que esta suíte não repete o golpe nem os i-frames.</b> Os dois já têm
    /// guarda em <c>HitboxAuditTests</c> — limite de alcance, alcance + 0,1, as quatro direções
    /// isométricas, pelas costas, e os dois lados do i-frame. Reescrever as asserções aqui
    /// criaria <b>duas fontes da verdade para a mesma regra</b>, que o doc de
    /// <c>ColisoresDoElencoTests</c> chama de modo de falha mais repetido deste projeto: uma
    /// das cópias envelhece calada. No lugar disso, os itens 1 e 2 viram <b>guardas de
    /// cobertura</b>: se alguém apagar aqueles testes, estes falham dizendo qual foi
    /// perdido.</para>
    ///
    /// <para><b>O que foi medido e contraria o enunciado do pedido</b> (2026-09-04):</para>
    /// <list type="number">
    ///   <item><b>Coleta não é por trigger.</b> <c>ColetavelDeItem</c> implementa
    ///   <c>IInteragivel</c>: quem acha é o <c>DetectorDeInteracao</c>, com um
    ///   <c>Physics2D.OverlapCircle</c> de buffer <b>fixo em 8 slots</b>. A consulta existe e é
    ///   testável; o que não existe é o <c>OnTriggerEnter2D</c> de coleta.</item>
    ///   <item><b>Audição não é consulta de física.</b> <c>EnemyPerception</c> assina
    ///   <c>SoundBroadcastService.OnSomEmitido</c> e compara
    ///   <c>Vector2.Distance</c> contra <c>Mathf.Min(som.RaioEfetivo, raioAudicao)</c>. É
    ///   detecção de inimigo, e vale testar — só não passa pela <c>Physics2D</c>.</item>
    ///   <item><b>Não existe AoE de chefe.</b> Varri o projeto: as únicas
    ///   <c>OverlapCircleAll</c> são as duas habilidades de relíquia em
    ///   <c>ArtefatosBridge</c>. Nem Abdul, nem Byakhee, nem o Rei em Amarelo têm ataque em
    ///   área. O item 5 testa o efeito em área que <b>existe</b>.</item>
    /// </list>
    ///
    /// <para>Todo teste <b>loga o valor medido</b> antes de afirmar, e falha dizendo o número
    /// que veio e o que era esperado.</para>
    /// </summary>
    public sealed class PhysicsQueryAuditTests
    {
        private readonly List<GameObject> _lixo = new List<GameObject>();

        private GameObject Novo(string nome, params System.Type[] componentes)
        {
            var go = new GameObject(nome, componentes);
            _lixo.Add(go);
            return go;
        }

        [TearDown]
        public void TearDown()
        {
            foreach (var go in _lixo)
                if (go != null) Object.Destroy(go);

            _lixo.Clear();
        }

        /// <summary>
        /// Sprite mínimo de verdade. Existe porque <c>Hurtbox.GarantirPara</c> — chamado no
        /// <c>Awake</c> de todo <c>EnemyBase</c> — falha ALTO quando não há sprite, e com razão:
        /// sem corpo desenhado não dá para derivar a área atingível, e o inimigo fica
        /// impossível de acertar. Dar um sprite ao rig é mais honesto que silenciar o erro.
        /// </summary>
        private static Sprite SpriteDeTeste()
        {
            var tex = new Texture2D(32, 32);
            return Sprite.Create(tex, new Rect(0f, 0f, 32f, 32f), new Vector2(0.5f, 0f), 32f);
        }

        /// <summary>Lê um campo serializado privado — o teste mede o JOGO, não uma cópia.</summary>
        private static T Campo<T>(object alvo, string nome)
        {
            var f = alvo.GetType().GetField(nome,
                BindingFlags.NonPublic | BindingFlags.Instance);

            Assert.IsNotNull(f,
                $"O campo '{nome}' sumiu de {alvo.GetType().Name}. Este teste o lê por reflexão " +
                "de propósito: fixar o número numa constante do teste faria a suíte continuar " +
                "verde depois de alguém mudar o valor no jogo.");

            return (T)f.GetValue(alvo);
        }

        // ═══════════════════════════════════════════════════════════════════
        // 1 e 2 — guardas de cobertura, não duplicatas
        // ═══════════════════════════════════════════════════════════════════

        private static void ExigirTestes(string classe, params string[] metodos)
        {
            var t = System.AppDomain.CurrentDomain.GetAssemblies()
                .SelectMany(a => { try { return a.GetTypes(); } catch { return new System.Type[0]; } })
                .FirstOrDefault(x => x.Name == classe);

            Assert.IsNotNull(t,
                $"A classe de teste '{classe}' não existe mais. Ela é quem guarda estas " +
                "asserções; sem ela, esta área do combate fica descoberta e esta suíte é o " +
                "único aviso.");

            var existentes = t.GetMethods(BindingFlags.Public | BindingFlags.Instance)
                              .Select(m => m.Name).ToHashSet();

            var faltando = metodos.Where(m => !existentes.Contains(m)).ToList();

            Assert.IsEmpty(faltando,
                $"Sumiram de {classe}: {string.Join(", ", faltando)}.\n" +
                "Estes testes são a guarda real desta consulta. Se foram renomeados, atualize " +
                "esta lista; se foram apagados, a cobertura foi perdida.");

            Debug.Log($"[PhysicsQueryAudit] cobertura confirmada: {classe} " +
                      $"({metodos.Length} teste(s)).");
        }

        /// <summary>
        /// <b>Item 1 — golpe corpo a corpo.</b> As asserções de distância vivem em
        /// <c>HitboxAuditTests</c>, que já mede no limite, além do limite, nas quatro direções
        /// isométricas e pelas costas.
        /// </summary>
        [Test]
        public void Item1_OGolpeCorpoACorpo_ContinuaCoberto()
        {
            ExigirTestes("HitboxAuditTests",
                "OGolpe_AcertaNoLimiteDoAlcance",
                "OGolpe_ErraAlemDoAlcance",
                "OGolpe_AcertaNasQuatroDirecoes",
                "OGolpe_NaoAcertaPelasCostas");
        }

        /// <summary>
        /// <b>Item 2 — i-frames da esquiva.</b> Os dois lados da janela já são medidos em
        /// <c>HitboxAuditTests</c>.
        /// </summary>
        [Test]
        public void Item2_OsIFramesDaEsquiva_ContinuamCobertos()
        {
            ExigirTestes("HitboxAuditTests",
                "DuranteOsIFrames_OJogadorNaoLevaDano",
                "DepoisDosIFrames_OJogadorVoltaALevarDano");
        }

        // ═══════════════════════════════════════════════════════════════════
        // 3 — coleta: Physics2D.OverlapCircle do DetectorDeInteracao
        // ═══════════════════════════════════════════════════════════════════

        /// <summary>Monta o Damião mínimo que a consulta de interação precisa.</summary>
        private DetectorDeInteracao MontarDetector(out GameObject jogador)
        {
            // O detector exige PlayerInput para LER o botão. A consulta de proximidade
            // (AtualizarAlvo) roda mesmo sem ele — é o que este teste mede —, mas o Awake
            // reclama alto, de propósito. Esperamos o erro em vez de silenciá-lo.
            LogAssert.Expect(LogType.Error,
                new System.Text.RegularExpressions.Regex("DetectorDeInteracao.*PlayerInput"));

            jogador = Novo("Damiao_Teste");
            return jogador.AddComponent<DetectorDeInteracao>();
        }

        /// <summary>
        /// Um <c>ItemDef</c> real do projeto. Um coletável sem item retorna cedo em
        /// <c>Interagir</c> — testar a coleta sem ele mediria o retorno cedo, não a coleta.
        /// </summary>
        private static FavelaAmarela.Inventario.ItemDef ItemDeTeste()
        {
            var todos = Resources.LoadAll<FavelaAmarela.Inventario.ItemDef>("Itens");

            Assert.IsNotEmpty(todos,
                "Nenhum ItemDef em Resources/Itens. O coletável precisa de um item autorado " +
                "para ter o que entregar; sem isso 'Interagir' retorna cedo e a coleta nunca " +
                "acontece — em jogo, é o baú que não dá nada ao ser aberto.");

            // O primeiro que NÃO é relíquia. Relíquia toma o caminho de Artefato dentro de
            // Interagir, que não passa pelo InventoryManager -- o teste mediria outra coisa,
            // e qual item vem primeiro depende da ordem de carga do Resources.
            var deReliquia = Resources.LoadAll<FavelaAmarela.Inventario.ArtefatoDef>("Artefatos")
                                      .Select(a => a.Item)
                                      .Where(i => i != null)
                                      .ToHashSet();

            var comum = todos.FirstOrDefault(i => !deReliquia.Contains(i));

            Assert.IsNotNull(comum,
                $"Todos os {todos.Length} ItemDef de Resources/Itens são relíquias. Este teste " +
                "precisa de um item comum para exercitar o caminho do InventoryManager.");

            return comum;
        }

        private GameObject MontarColetavel(Vector2 posicao)
        {
            // Na camada que o próprio detector considera interagível por padrão: fixar o nome
            // aqui faria o teste passar depois de alguém mudar a lista no jogo.
            string[] padrao = DetectorDeInteracao.CamadasPadraoDeInteragiveis;
            int camada = padrao.Select(LayerMask.NameToLayer).FirstOrDefault(c => c >= 0);

            Assert.GreaterOrEqual(camada, 0,
                "Nenhuma das camadas de interação padrão existe no TagManager: " +
                string.Join(", ", padrao) + ". Sem uma delas, a máscara do detector não casa " +
                "com nada e o 'E' nunca acha alvo em jogo.");

            // O Awake de ColetavelDeItem dispara no AddComponent, ANTES de Configurar poder
            // rodar -- e ele falha alto sem ItemDef, com razão: um coletável sem item é um baú
            // que não entrega nada. Num prefab o campo vem do Inspector; montado em codigo,
            // esse erro é inevitável. Declarar é mais honesto que silenciar a categoria toda.
            LogAssert.Expect(LogType.Error,
                new System.Text.RegularExpressions.Regex("ColetavelDeItem.*sem ItemDef"));

            var go = Novo("Coletavel_Teste", typeof(BoxCollider2D), typeof(ColetavelDeItem));
            go.layer = camada;
            go.transform.position = posicao;

            var col = go.GetComponent<BoxCollider2D>();
            col.isTrigger = true;
            col.size = new Vector2(0.5f, 0.5f);

            go.GetComponent<ColetavelDeItem>().Configurar(ItemDeTeste());

            return go;
        }

        [UnityTest]
        public IEnumerator Item3_OColetavel_EhAchadoDentroDoAlcance()
        {
            var detector = MontarDetector(out var jogador);
            jogador.transform.position = Vector3.zero;

            float alcance = Campo<float>(detector, "alcance");
            float dentro = alcance * 0.5f;

            var coletavel = MontarColetavel(new Vector2(dentro, 0f));

            yield return null;   // deixa o Update do detector rodar uma vez
            yield return null;

            Debug.Log($"[PhysicsQueryAudit] coleta: alcance do detector = {alcance:0.###}; " +
                      $"coletável a {dentro:0.###}; alvo = " +
                      $"{(detector.AlvoAtual == null ? "NENHUM" : detector.AlvoAtual.GetType().Name)}");

            Assert.IsNotNull(detector.AlvoAtual,
                $"O coletável está a {dentro:0.###} unidades, dentro do alcance de " +
                $"{alcance:0.###}, e a consulta não o achou. Physics2D.OverlapCircle não " +
                "encontrou nada — as causas prováveis são a camada do coletável fora de " +
                "'camadasInteragiveis' ou o colisor ausente.");

            Assert.AreSame(coletavel.GetComponent<ColetavelDeItem>(), detector.AlvoAtual,
                $"A consulta achou algo, mas não o coletável: veio " +
                $"{detector.AlvoAtual.GetType().Name}.");
        }

        [UnityTest]
        public IEnumerator Item3_OColetavel_NaoEhAchadoAlemDoAlcance()
        {
            var detector = MontarDetector(out var jogador);
            jogador.transform.position = Vector3.zero;

            float alcance = Campo<float>(detector, "alcance");
            float fora = alcance + 1f;

            MontarColetavel(new Vector2(fora, 0f));

            yield return null;
            yield return null;

            Debug.Log($"[PhysicsQueryAudit] coleta: coletável a {fora:0.###}, alcance " +
                      $"{alcance:0.###} — alvo = " +
                      $"{(detector.AlvoAtual == null ? "NENHUM (correto)" : "ACHOU")}");

            Assert.IsNull(detector.AlvoAtual,
                $"O coletável está a {fora:0.###} unidades, FORA do alcance de " +
                $"{alcance:0.###}, e a consulta o achou mesmo assim. O raio passado ao " +
                "OverlapCircle não é o campo 'alcance', ou o colisor é grande demais e sua " +
                "borda entra no círculo.");
        }

        /// <summary>
        /// A consulta acha; agora o item é <b>recolhido</b>. Separado do teste de alcance
        /// porque são duas falhas diferentes: "não achou" e "achou e não recolheu".
        /// </summary>
        [UnityTest]
        public IEnumerator Item3_OColetavelAchado_EhRecolhido()
        {
            var detector = MontarDetector(out var jogador);
            jogador.transform.position = Vector3.zero;

            var coletavel = MontarColetavel(new Vector2(Campo<float>(detector, "alcance") * 0.5f, 0f));
            var script = coletavel.GetComponent<ColetavelDeItem>();

            yield return null;
            yield return null;

            Assert.IsTrue(script.PodeInteragir,
                "O coletável já nasceu marcado como coletado — o teste não teria o que medir.");

            var inv = FavelaAmarela.Inventario.InventoryManager.Instance;

            Assert.IsNotNull(inv,
                "InventoryManager.Instance está nulo em PlayMode. Ele se auto-instancia por " +
                "[RuntimeInitializeOnLoadMethod(BeforeSceneLoad)] — se sumiu, NENHUM item do " +
                "jogo pode ser recolhido, e o sintoma é o baú que não entrega nada.");

            script.Interagir(jogador);
            yield return null;

            Debug.Log($"[PhysicsQueryAudit] coleta: item entregue ao inventário; " +
                      $"PodeInteragir depois de Interagir = {script.PodeInteragir}");

            Assert.IsFalse(script.PodeInteragir,
                "O coletável continua disponível depois de Interagir, com item configurado e " +
                "InventoryManager presente. Ou a mochila estava cheia (o item fica no chão de " +
                "propósito, para não perder progresso em silêncio), ou a coleta não marcou o " +
                "objeto como consumido. Em jogo, os dois são 'apertei E e nada aconteceu'.");
        }

        // ═══════════════════════════════════════════════════════════════════
        // 4 — audição do inimigo
        // ═══════════════════════════════════════════════════════════════════

        private EnemyPerception MontarOuvinte(out SoundBroadcastService som)
        {
            som = new SoundBroadcastService();

            var go = Novo("Cultista_Teste");
            var p = go.AddComponent<EnemyPerception>();
            p.Bind(som);           // antes do Start, que loga erro se não houver serviço

            return p;
        }

        [UnityTest]
        public IEnumerator Item4_OInimigoOuve_DentroDoRaio()
        {
            var ouvinte = MontarOuvinte(out var som);
            ouvinte.transform.position = Vector3.zero;

            float raioAudicao = Campo<float>(ouvinte, "raioAudicao");
            float distancia = raioAudicao * 0.5f;

            yield return null;     // deixa o Start rodar com o serviço já injetado

            Assert.IsTrue(ouvinte.TemFonteDeSom,
                "O EnemyPerception não recebeu o serviço de som. Sem ele o inimigo é SURDO: " +
                "nunca entra em Alerta nem em Caça, e só reage se for golpeado.");

            // Raio efetivo maior que a acuidade: o alcance vira o mínimo dos dois.
            som.Emitir(new SomEmitido(new Vector2(distancia, 0f), raioAudicao * 2f));

            Debug.Log($"[PhysicsQueryAudit] audição: raioAudicao = {raioAudicao:0.###}; " +
                      $"som a {distancia:0.###}; EstaOuvindo = {ouvinte.EstaOuvindo}");

            Assert.IsTrue(ouvinte.EstaOuvindo,
                $"O som saiu a {distancia:0.###} unidades, dentro da acuidade de " +
                $"{raioAudicao:0.###}, e o inimigo não ouviu. O alcance efetivo é " +
                "Mathf.Min(som.RaioEfetivo, raioAudicao) — se este teste falha, um dos dois " +
                "lados do mínimo está sendo descartado, que é exatamente o bug que a versão " +
                "anterior de HandleSomEmitido tinha.");

            Assert.IsNotNull(ouvinte.UltimaOrigemConhecida,
                "Ouviu, mas não guardou a origem — a IA não teria para onde ir.");
        }

        [UnityTest]
        public IEnumerator Item4_OInimigoNaoOuve_AlemDoRaio()
        {
            var ouvinte = MontarOuvinte(out var som);
            ouvinte.transform.position = Vector3.zero;

            float raioAudicao = Campo<float>(ouvinte, "raioAudicao");
            float distancia = raioAudicao + 5f;

            yield return null;

            som.Emitir(new SomEmitido(new Vector2(distancia, 0f), raioAudicao * 2f));

            Debug.Log($"[PhysicsQueryAudit] audição: som a {distancia:0.###}, acuidade " +
                      $"{raioAudicao:0.###} — EstaOuvindo = {ouvinte.EstaOuvindo}");

            Assert.IsFalse(ouvinte.EstaOuvindo,
                $"O som saiu a {distancia:0.###} unidades, FORA da acuidade de " +
                $"{raioAudicao:0.###}, e o inimigo ouviu. Um inimigo que escuta o mapa inteiro " +
                "torna a furtividade inútil e faz a arena inteira convergir no jogador.");
        }

        /// <summary>
        /// O som fraco é o que a furtividade usa: agachado, o raio efetivo cai. Se o mínimo dos
        /// dois for descartado, agachar deixa de ter efeito — foi o bug real registrado no doc
        /// de <c>HandleSomEmitido</c>.
        /// </summary>
        [UnityTest]
        public IEnumerator Item4_OSomFraco_NaoAlcanca_MesmoComOInimigoAtento()
        {
            var ouvinte = MontarOuvinte(out var som);
            ouvinte.transform.position = Vector3.zero;

            float raioAudicao = Campo<float>(ouvinte, "raioAudicao");
            const float raioAgachado = 2f;
            float distancia = raioAgachado + 1f;   // longe do som, perto do ouvido

            yield return null;

            Assert.Less(distancia, raioAudicao,
                $"Rig inválido: a distância {distancia:0.###} precisa caber dentro da acuidade " +
                $"de {raioAudicao:0.###} para o teste isolar o raio do SOM.");

            som.Emitir(new SomEmitido(new Vector2(distancia, 0f), raioAgachado));

            Debug.Log($"[PhysicsQueryAudit] audição: som fraco raio {raioAgachado:0.###} a " +
                      $"{distancia:0.###}, acuidade {raioAudicao:0.###} — " +
                      $"EstaOuvindo = {ouvinte.EstaOuvindo}");

            Assert.IsFalse(ouvinte.EstaOuvindo,
                $"O som carregava só {raioAgachado:0.###} de raio e o inimigo, a " +
                $"{distancia:0.###}, ouviu assim mesmo. Isso significa que som.RaioEfetivo foi " +
                "descartado e só raioAudicao valeu — e então agachar não muda nada.");
        }

        /// <summary>
        /// <b>Levar um golpe tem de contar.</b> Guarda o defeito que o Vini pegou jogando a
        /// Tumba em 2026-09-04: <i>"o primeiro cultista não me notou, nem quando eu batia"</i>.
        ///
        /// <para>Causa: <c>EnemyBase</c> disparava <c>OnGolpeRecebido</c> e <b>ninguém
        /// escutava</b>. A IA reagia a passo e ignorava facada — o estímulo mais inequívoco do
        /// jogo era o único que não chegava à percepção. E o <c>Chase</c> só se move quando há
        /// <c>UltimaOrigemConhecida</c>, que só o som preenchia: mesmo que o golpe levasse a
        /// Hurt, a caça seguinte seria um no-op silencioso.</para>
        /// </summary>
        [UnityTest]
        public IEnumerator Item4_LevarUmGolpe_PoeOInimigoEmCaca()
        {
            var ouvinte = MontarOuvinte(out _);
            ouvinte.transform.position = new Vector3(7f, -11f, 0f);

            yield return null;

            Assert.IsFalse(ouvinte.UltimaOrigemConhecida.HasValue,
                "O rig começou com origem conhecida — o teste não distinguiria o efeito do golpe.");

            bool entrouEmCaca = false;
            ouvinte.OnEntrouCaca += () => entrouEmCaca = true;

            ouvinte.NotarAgressao();
            yield return null;

            Debug.Log($"[PhysicsQueryAudit] agressão: suspeita = {ouvinte.Suspeita:0.##}; " +
                      $"origem = {ouvinte.UltimaOrigemConhecida}; caça = {entrouEmCaca}");

            Assert.IsTrue(entrouEmCaca,
                "Levar um golpe não disparou OnEntrouCaca. É esse evento que a " +
                "EnemyStateMachine escuta para entrar em Chase — sem ele, o inimigo apanha e " +
                "continua patrulhando, que foi exatamente o relato de playteste.");

            Assert.IsTrue(ouvinte.UltimaOrigemConhecida.HasValue,
                "Entrou em caça sem origem conhecida. O Chase só se move quando " +
                "UltimaOrigemConhecida tem valor — sem ela, a caça é um no-op silencioso e o " +
                "inimigo fica parado em estado de perseguição.");

            Assert.AreEqual(1f, ouvinte.Suspeita, 0.001f,
                $"A suspeita ficou em {ouvinte.Suspeita:0.##} depois de um golpe. Ser atingido " +
                "não é uma pista: é certeza, e tem de saturar o medidor.");
        }

        // ═══════════════════════════════════════════════════════════════════
        // 5 — efeito em área (Physics2D.OverlapCircleAll das relíquias)
        // ═══════════════════════════════════════════════════════════════════

        /// <summary>
        /// Constrói o contexto de artefato real, com o <c>OverlapCircleAll</c> e a máscara
        /// <c>camadasDeEntidade</c> que o jogo usa. A classe é privada aninhada em
        /// <c>ArtefatosBridge</c>; alcançá-la por reflexão é o que permite medir a
        /// <b>consulta de verdade</b> em vez de uma reconstrução dela.
        /// </summary>
        private static object ContextoReal(ArtefatosBridge bridge)
        {
            var tipo = typeof(ArtefatosBridge).GetNestedType("ContextoDeArtefatoUnity",
                BindingFlags.NonPublic);

            Assert.IsNotNull(tipo,
                "A classe aninhada 'ContextoDeArtefatoUnity' sumiu de ArtefatosBridge. É ela " +
                "que hospeda o Physics2D.OverlapCircleAll das habilidades de relíquia — o " +
                "único efeito em área do jogo.");

            return System.Activator.CreateInstance(
                tipo, BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public,
                null, new object[] { bridge }, null);
        }

        /// <summary>
        /// O <c>Awake</c> do <c>ArtefatosBridge</c> exige <c>PlayerMovement</c> — sem ele o
        /// Resguardo do Sinal não silencia passo nenhum, e o componente diz isso alto. Montar
        /// um <c>PlayerMovement</c> de verdade arrastaria FSM, bridges e input para dentro de
        /// um teste que só quer medir um <c>OverlapCircleAll</c>; declarar o erro mede a
        /// consulta sem fingir que o resto está ligado.
        /// </summary>
        private static ArtefatosBridge AdicionarBridge(GameObject jogador)
        {
            LogAssert.Expect(LogType.Error,
                new System.Text.RegularExpressions.Regex("ArtefatosBridge.*PlayerMovement"));

            return jogador.AddComponent<ArtefatosBridge>();
        }

        private GameObject MontarInimigoRevelavel(Vector2 posicao)
        {
            // EnemyBase exige Rigidbody2D e SpriteRenderer.
            var go = Novo("Inimigo_Teste", typeof(Rigidbody2D), typeof(SpriteRenderer),
                          typeof(CircleCollider2D));
            go.transform.position = posicao;

            // NA CAMADA DE INIMIGO, como no jogo. Resolvida por nome porque a ordem do
            // TagManager não é contrato.
            //
            // Até 2026-09-04 o rig deixava o inimigo na camada 0 e o teste passava assim mesmo
            // -- porque a máscara das relíquias era `~0`, TODAS as camadas. Ao apertar a
            // máscara para Enemy, este teste caiu, e caiu com razão: ele estava verde por
            // acidente, medindo uma consulta que não filtrava nada.
            int camadaDeInimigo = LayerMask.NameToLayer("Enemy");
            Assert.GreaterOrEqual(camadaDeInimigo, 0,
                "A camada 'Enemy' não existe no TagManager. As habilidades de relíquia " +
                "consultam por ela; sem ela, elas não acham inimigo nenhum em jogo.");
            go.layer = camadaDeInimigo;

            var rb = go.GetComponent<Rigidbody2D>();
            rb.bodyType = RigidbodyType2D.Dynamic;
            rb.gravityScale = 0f;

            // O sprite vem ANTES do EnemyBase: o Awake dele chama Hurtbox.GarantirPara, que
            // falha alto sem sprite. A ordem aqui não é estilo, é o que evita o erro.
            go.GetComponent<SpriteRenderer>().sprite = SpriteDeTeste();
            go.GetComponent<CircleCollider2D>().radius = 0.3f;

            // Ficha de atributos é campo do Inspector e este rig não tem uma. O EnemyBase
            // reclama e segue com uma ficha padrão — comportamento correto, e o teste o
            // reconhece em vez de silenciá-lo.
            LogAssert.Expect(LogType.Error,
                new System.Text.RegularExpressions.Regex("EnemyBase.*Ficha"));

            go.AddComponent<EnemyBase>();

            return go;
        }

        private static bool FoiRevelado(GameObject alvo)
            => alvo.GetComponentInChildren<MarcadorDeRevelacao>() != null;

        [UnityTest]
        public IEnumerator Item5_OEfeitoEmArea_AtingeDentro_ENaoAtingeFora()
        {
            var jogador = Novo("Damiao_Teste");
            jogador.transform.position = Vector3.zero;
            var bridge = AdicionarBridge(jogador);

            yield return null;   // Awake do ArtefatosBridge

            const float raio = 4f;
            var perto = MontarInimigoRevelavel(new Vector2(raio * 0.5f, 0f));
            var longe = MontarInimigoRevelavel(new Vector2(raio + 3f, 0f));

            yield return new WaitForFixedUpdate();   // colisores registrados na física

            var ctx = ContextoReal(bridge);
            var metodo = ctx.GetType().GetMethod("RevelarEntidades");

            Assert.IsNotNull(metodo,
                "RevelarEntidades sumiu do contexto de artefato — é uma das duas consultas em " +
                "área do jogo (a outra, AplacarSerpentes, usa exatamente a mesma query).");

            metodo.Invoke(ctx, new object[] { raio, 5f });
            yield return null;

            bool dentro = FoiRevelado(perto);
            bool fora = FoiRevelado(longe);

            Debug.Log($"[PhysicsQueryAudit] área: raio {raio:0.###}; " +
                      $"inimigo a {raio * 0.5f:0.###} revelado = {dentro}; " +
                      $"inimigo a {raio + 3f:0.###} revelado = {fora}");

            Assert.IsTrue(dentro,
                $"O inimigo a {raio * 0.5f:0.###} unidades está dentro do raio de " +
                $"{raio:0.###} e NÃO foi atingido pelo efeito em área. O " +
                "Physics2D.OverlapCircleAll não o encontrou, ou o GetComponentInParent" +
                "<EnemyBase> falhou.");

            Assert.IsFalse(fora,
                $"O inimigo a {raio + 3f:0.###} unidades está FORA do raio de {raio:0.###} e " +
                "foi atingido assim mesmo. Um efeito em área que ignora o próprio raio atinge " +
                "a sala inteira.");
        }

        /// <summary>
        /// A máscara das duas habilidades é <c>camadasDeEntidade</c>, que nasce em
        /// <c>~0</c> — <b>todas as camadas</b>. Não é bug de correção (o filtro real é o
        /// <c>GetComponentInParent&lt;EnemyBase&gt;</c>), mas faz a consulta varrer parede,
        /// chão e gatilho para descartar depois. Este teste registra o valor em vez de o
        /// afirmar, para que a escolha fique visível quando alguém for ajustá-la.
        /// </summary>
        [Test]
        public void Item5_AMascaraDoEfeitoEmArea_EhRegistradaParaRevisao()
        {
            var jogador = Novo("Damiao_Teste");
            var bridge = AdicionarBridge(jogador);

            LayerMask mascara = Campo<LayerMask>(bridge, "camadasDeEntidade");

            Debug.Log($"[PhysicsQueryAudit] área: camadasDeEntidade = {mascara.value} " +
                      $"({(mascara.value == ~0 ? "TODAS as camadas" : "restrita")}). " +
                      "As duas habilidades de relíquia consultam com esta máscara.");

            Assert.AreNotEqual(0, mascara.value,
                "A máscara do efeito em área está VAZIA (0). Com máscara zero o " +
                "OverlapCircleAll não devolve nada e as duas habilidades de relíquia não " +
                "fazem absolutamente nada — sem erro no console.");
        }
    }
}
