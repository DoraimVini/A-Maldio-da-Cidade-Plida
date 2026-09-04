using System.Collections;
using System.Reflection;
using System.Text.RegularExpressions;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace FavelaAmarela.Tests.PlayMode
{
    /// <summary>
    /// Mede o combate <b>rodando</b>: quem acerta quem, a que distância, em que direção, e o
    /// que os i-frames realmente cobrem.
    ///
    /// <para><b>Por que PlayMode e não EditMode.</b> A hitbox deste projeto não é um colisor —
    /// é <c>Physics2D.OverlapCircle</c> rodado a cada <c>FixedUpdate</c> enquanto a janela do
    /// golpe está aberta (ver <c>Runtime.Combat.Hitbox</c>). Nada disso existe fora do Play: sem
    /// física rodando não há consulta, e um teste de EditMode só conseguiria reler os números do
    /// YAML — que é exatamente o que a auditoria escrita já faz. Este aqui mede o
    /// <b>comportamento</b>.</para>
    ///
    /// <para><b>O rig é montado em código, não numa cena de asset</b> — convenção do projeto,
    /// mesma de <c>OAltarResponde</c>. Uma cena de teste é mais um arquivo para envelhecer sem
    /// ninguém notar; o rig em código quebra na compilação quando um componente muda de
    /// assinatura.</para>
    ///
    /// <para><b>Quando algo não colide, a falha diz o motivo exato</b> — sem hurtbox, sem
    /// sprite, camada ausente, FSM não injetada. Um <c>Assert.IsTrue(acertou)</c> seco mandaria
    /// procurar em cinco lugares.</para>
    /// </summary>
    public sealed class HitboxAuditTests
    {
        // ── o rig ────────────────────────────────────────────────────────────
        private GameObject _chao;
        private GameObject _jogador;
        private GameObject _contêinerDoJogador;
        private GameObject _contêinerDosInimigos;

        /// <summary>
        /// Onde o rig monta o elenco. <b>Não é a origem</b> de propósito: na Tumba os atores
        /// ficam por volta de y = -14, e código que confunde posição de ator com posição de
        /// contêiner só erra fora do zero.
        /// </summary>
        private static readonly Vector3 OrigemDoRig = new Vector3(12f, -14f, 0f);
        private GameObject _inimigo;

        private FavelaAmarela.Player.MaoFisicaBridge _mao;
        private FavelaAmarela.Player.EsquivaBridge _esquiva;
        private FavelaAmarela.Runtime.Combat.VitalidadeBridge _vidaDoJogador;
        private AlvoDeTeste _alvo;

        /// <summary>
        /// Quantas vezes o alvo foi <b>atingido</b> — não quanto dano levou.
        ///
        /// <para>A mão vazia deste jogo causa <b>dano zero</b>: o log do próprio
        /// <c>MaoFisicaBridge</c> diz <c>arma=DESARMADO (mão vazia) ... dano=0</c>. Medir dano
        /// faria todo golpe correto parecer errado. E acerto é a medida certa para uma
        /// auditoria de <b>geometria</b> — quanto dói é balanceamento, e muda.</para>
        /// </summary>
        private int _acertosNoInimigo => _alvo != null ? _alvo.GolpesRecebidos : 0;

        private void ZerarAcertos()
        {
            if (_alvo != null) { _alvo.DanoAcumulado = 0f; _alvo.GolpesRecebidos = 0; }
        }

        private float _danoNoJogador;

        /// <summary>Geometria do golpe de mão vazia, lida do próprio código (não copiada).</summary>
        private float _alcance, _raio, _janela, _preparo;

        /// <summary>Meia-largura da hurtbox do inimigo, medida do colisor que o jogo criou.</summary>
        private float _meiaLarguraDoInimigo;

        private const string CamadaHurtboxInimigo = "EnemyHurtbox";
        private const string CamadaHurtboxJogador = "PlayerHurtbox";

        [TearDown]
        public void TearDown()
        {
            foreach (var go in new[] { _inimigo, _jogador, _chao,
                                       _contêinerDoJogador, _contêinerDosInimigos })
                if (go != null) Object.DestroyImmediate(go);

            _danoNoJogador = 0f;
        }

        // ── montagem ─────────────────────────────────────────────────────────

        /// <summary>
        /// Sprite de 1×2 unidades a PPU 32, criada em runtime.
        ///
        /// <para><c>Hurtbox.GarantirPara</c> deriva a área de <c>sprite.bounds</c> e <b>recusa
        /// sem sprite</b> — com um <c>LogError</c> dizendo que o dono ficaria intocável. Então o
        /// rig precisa de uma sprite de verdade, e não de um <c>SpriteRenderer</c> vazio.</para>
        /// </summary>
        private static Sprite SpriteDeCorpo()
        {
            var tex = new Texture2D(32, 64);
            var pixels = new Color32[32 * 64];
            for (int i = 0; i < pixels.Length; i++) pixels[i] = new Color32(255, 255, 255, 255);
            tex.SetPixels32(pixels);
            tex.Apply();

            // Pivô no pé (0.5, 0), como todo o elenco: o jogo ordena profundidade por -y.
            // Sprite.Create(Texture2D, Rect, Vector2 pivot, float ppu) -- nesta ordem.
            return Sprite.Create(tex, new Rect(0, 0, 32, 64), new Vector2(0.5f, 0f), 32f);
        }

        private IEnumerator Montar()
        {
            // O rig roda SEM FichaAtributosConfig de propósito: este teste mede geometria, não
            // balanceamento, e a bridge tem fallback documentado ("Usando base"). O LogError
            // dela é legítimo e esperado — sem isto o Test Framework reprova todo teste da
            // classe por causa de um aviso que o rig provoca de propósito.
            LogAssert.Expect(LogType.Error, new Regex("Nenhuma ficha encontrada"));

            ExigirCamada(CamadaHurtboxInimigo);
            ExigirCamada(CamadaHurtboxJogador);

            // ── o chão ───────────────────────────────────────────────────────
            // Não participa do dano. Está aqui como CONTROLE NEGATIVO: se um golpe algum dia
            // acertar o chão, a máscara está errada. Camada Default, sólido, como o cenário.
            _chao = new GameObject("Chao");
            _chao.transform.position = OrigemDoRig + new Vector3(0f, -0.5f, 0f);
            var piso = _chao.AddComponent<BoxCollider2D>();
            piso.size = new Vector2(40f, 1f);

            // ── o jogador ────────────────────────────────────────────────────
            // O JOGADOR VIVE DENTRO DE UM CONTÊINER E LONGE DA ORIGEM, como na cena de
            // verdade. As duas coisas são deliberadas, e a falta delas escondeu um bug real:
            //
            //  - Na Tumba, os atores são filhos de GameObjects de organização
            //    (Inimigos_Playtest, TumbaDeAbdul_Conteudo) que ficam em y = 0, enquanto os
            //    atores estão por volta de y = -14. Um rig com todo mundo solto na raiz faz
            //    transform.root devolver o próprio ator -- e código que usa transform.root
            //    passa no teste e quebra em jogo. Foi exatamente o que aconteceu com o portão
            //    de profundidade em 2026-09-04: TODO golpe do Damião passou a ser rejeitado.
            //
            //  - Montar tudo em y = 0 esconde o mesmo defeito por outro caminho: com ator e
            //    contêiner no mesmo Y, a diferença dá zero e a conta errada "acerta".
            _contêinerDoJogador = new GameObject("Conteudo_DaCena");
            _contêinerDosInimigos = new GameObject("Inimigos_DaCena");

            _jogador = new GameObject("Damiao", typeof(Rigidbody2D));
            _jogador.tag = "Player";
            _jogador.transform.SetParent(_contêinerDoJogador.transform, false);
            _jogador.transform.position = OrigemDoRig;

            var srJogador = _jogador.AddComponent<SpriteRenderer>();
            srJogador.sprite = SpriteDeCorpo();

            _vidaDoJogador = _jogador.AddComponent<FavelaAmarela.Runtime.Combat.VitalidadeBridge>();
            _mao = _jogador.AddComponent<FavelaAmarela.Player.MaoFisicaBridge>();
            _esquiva = _jogador.AddComponent<FavelaAmarela.Player.EsquivaBridge>();

            // O PlayerMovement vem POR ULTIMO, e a ordem importa: o Awake dele resolve as
            // bridges por GetComponent e injeta nelas a PlayerStateMachine que ele mesmo TICA
            // (PlayerMovement.cs:246-255 e :354). Adicionando-o antes, ele não acha ninguém,
            // ninguém recebe FSM, e uma FSM injetada à mão por fora nunca avança -- o primeiro
            // golpe entra em Atacando e o ator fica preso ali para sempre.
            _jogador.AddComponent<FavelaAmarela.Player.PlayerMovement>();

            FavelaAmarela.Runtime.Combat.Hurtbox.GarantirPara(_jogador, CamadaHurtboxJogador);

            // ── o inimigo ────────────────────────────────────────────────────
            _inimigo = new GameObject("Alvo", typeof(Rigidbody2D));
            _inimigo.transform.SetParent(_contêinerDosInimigos.transform, false);
            var srInimigo = _inimigo.AddComponent<SpriteRenderer>();
            srInimigo.sprite = SpriteDeCorpo();
            _alvo = _inimigo.AddComponent<AlvoDeTeste>();

            var hurtboxDoInimigo =
                FavelaAmarela.Runtime.Combat.Hurtbox.GarantirPara(_inimigo, CamadaHurtboxInimigo);

            Assert.IsNotNull(hurtboxDoInimigo,
                "Hurtbox.GarantirPara devolveu null para o inimigo. Ele deriva a área de " +
                "sprite.bounds e recusa sem sprite — sem hurtbox o alvo fica INTOCÁVEL, porque " +
                "o golpe do jogador só consulta a camada de hurtbox.");

            yield return null;   // deixa os Awake rodarem

            _vidaDoJogador.OnDanoSofrido += d => _danoNoJogador += d;

            LerGeometriaDoGolpe();
            MedirHurtboxDoInimigo(hurtboxDoInimigo);

            yield return new WaitForFixedUpdate();
        }

        private static void ExigirCamada(string nome)
        {
            Assert.GreaterOrEqual(LayerMask.NameToLayer(nome), 0,
                $"A camada '{nome}' não existe no TagManager. Sem ela a máscara do golpe fica " +
                "vazia e NADA é atingível — foi conferido em 2026-09-03 que as camadas 11 e 12 " +
                "(PlayerHitbox/EnemyHitbox) estão vazias, então não confie no índice, confie no nome.");
        }

        /// <summary>
        /// Lê alcance, raio e janela <b>do próprio componente</b>, por reflexão.
        ///
        /// <para>Copiar 1,2 / 0,6 / 0,1 para cá faria o teste passar depois de alguém mudar a
        /// geometria da arma — ele mediria a cópia, não o jogo. É o modo de falha que este
        /// projeto mais repete.</para>
        /// </summary>
        private void LerGeometriaDoGolpe()
        {
            _alcance = LerPropriedadePrivada("AlcanceAtual");
            _raio = LerPropriedadePrivada("RaioAtual");
            _janela = LerPropriedadePrivada("JanelaAtual");
            _preparo = LerPropriedadePrivada("PreparoAtual");

            Assert.Greater(_alcance, 0f, "AlcanceAtual veio zero — o golpe não sairia do corpo.");
            Assert.Greater(_raio, 0f, "RaioAtual veio zero.");
            Assert.Greater(_janela, 0f, "JanelaAtual veio zero — a hitbox nunca ficaria ativa.");
        }

        private float LerPropriedadePrivada(string nome)
        {
            var p = typeof(FavelaAmarela.Player.MaoFisicaBridge).GetProperty(
                nome, BindingFlags.NonPublic | BindingFlags.Instance);

            Assert.IsNotNull(p,
                $"A propriedade '{nome}' não existe mais em MaoFisicaBridge. Este teste lê a " +
                "geometria do componente de propósito, para não medir uma cópia — se ela foi " +
                "renomeada, atualize aqui.");

            return (float)p.GetValue(_mao);
        }

        private void MedirHurtboxDoInimigo(FavelaAmarela.Runtime.Combat.Hurtbox hurtbox)
        {
            var col = hurtbox.GetComponent<Collider2D>();
            Assert.IsNotNull(col, "A Hurtbox do inimigo nasceu sem Collider2D.");
            _meiaLarguraDoInimigo = col.bounds.extents.x;

            Assert.Greater(_meiaLarguraDoInimigo, 0f,
                "A hurtbox do inimigo tem largura zero — Collider2D.bounds fica vazio quando o " +
                "colisor está desligado ou o objeto inativo.");
        }

        // ── o alcance máximo real ────────────────────────────────────────────

        /// <summary>
        /// Distância de centro a centro em que o golpe <b>ainda</b> acerta.
        ///
        /// <para>Não é <c>alcance + raio</c>: a hitbox é um círculo cuja borda externa fica em
        /// <c>alcance + raio</c>, e a hurtbox do alvo é uma caixa que se estende <b>para trás</b>
        /// pela metade da própria largura. O contato acontece quando a borda do círculo encosta
        /// na borda da caixa — então a distância de centro a centro é a soma das três.</para>
        /// </summary>
        private float AlcanceMaximoDeCentroACentro => _alcance + _raio + _meiaLarguraDoInimigo;

        private IEnumerator Golpear(Vector2 direcao, float distancia)
        {
            ZerarAcertos();

            _inimigo.transform.position = _jogador.transform.position
                                          + (Vector3)(direcao.normalized * distancia);

            // Transform mexido por código não chega à física até o próximo passo — sem isto a
            // consulta enxerga o colisor na posição ANTERIOR e o teste mede o quadro errado.
            Physics2D.SyncTransforms();

            _mao.TryAtacar(direcao.normalized);

            // Preparo + janela + folga. Esperar só a janela mediria o golpe ANTES de ele
            // existir: desde 2026-09-03 a hitbox só abre depois da fase de preparo.
            float ate = Time.time + _preparo + _janela + 0.05f;
            while (Time.time < ate) yield return new WaitForFixedUpdate();
        }

        // ── 1. alcance do golpe do jogador ───────────────────────────────────

        [UnityTest]
        public IEnumerator OGolpe_AcertaNoLimiteDoAlcance()
        {
            yield return Montar();

            float limite = AlcanceMaximoDeCentroACentro;
            yield return Golpear(Vector2.right, limite - 0.05f);

            Assert.Greater(_acertosNoInimigo, 0,
                $"O golpe NÃO acertou a {limite - 0.05f:0.###} unidades, logo dentro do limite " +
                $"teórico de {limite:0.###} (alcance {_alcance:0.##} + raio {_raio:0.##} + " +
                $"meia-largura da hurtbox {_meiaLarguraDoInimigo:0.##}). Se este falhar e o " +
                "irmão OGolpe_ErraAlemDoAlcance passar, o alcance real é MENOR que o autorado.");
        }

        [UnityTest]
        public IEnumerator OGolpe_ErraAlemDoAlcance()
        {
            yield return Montar();

            float alem = AlcanceMaximoDeCentroACentro + 0.1f;
            yield return Golpear(Vector2.right, alem);

            Assert.AreEqual(0, _acertosNoInimigo,
                $"O golpe acertou a {alem:0.###} unidades, ALÉM do limite teórico de " +
                $"{AlcanceMaximoDeCentroACentro:0.###}. O alcance real é maior que o autorado — " +
                "o jogador atinge de mais longe do que a arma promete.");
        }

        // ── 2. direção ───────────────────────────────────────────────────────

        /// <summary>
        /// As quatro direções do mundo. A base isométrica remapeia o <b>input</b>
        /// (<c>BaseIsometrica.ParaMundo</c>), mas o que chega em <c>TryAtacar</c> já é vetor de
        /// mundo — então é isto que a hitbox gira.
        /// </summary>
        [UnityTest]
        public IEnumerator OGolpe_AcertaNasQuatroDirecoes()
        {
            yield return Montar();

            var direcoes = new (string Nome, Vector2 Dir)[]
            {
                ("direita", Vector2.right), ("esquerda", Vector2.left),
                ("cima", Vector2.up), ("baixo", Vector2.down),
            };

            float perto = _alcance;      // bem dentro, para isolar direção de alcance
            var falhas = new System.Text.StringBuilder();

            foreach (var (nome, dir) in direcoes)
            {
                // O cooldown da arma bloquearia o segundo golpe no mesmo frame.
                yield return EsperarCooldown();
                yield return Golpear(dir, perto);

                if (_acertosNoInimigo <= 0)
                    falhas.AppendLine($"  {nome}: não acertou a {perto:0.##} unidades");
            }

            Assert.IsEmpty(falhas.ToString(),
                "O golpe não sai em todas as direções — a hitbox gira o deslocamento pela " +
                "direção recebida, então uma direção morta significa que o vetor não chegou:" +
                System.Environment.NewLine + falhas);
        }

        /// <summary>
        /// O golpe NÃO pode acertar quem está do lado oposto.
        ///
        /// <para>É o defeito que o doc da <c>Hitbox</c> nomeia: <i>"estar atrás do Byakhee, a
        /// 1,4 unidade, levava garrada igual — lê como injustiça, porque contradiz o que se
        /// vê"</i>. Vale para o jogador também.</para>
        /// </summary>
        [UnityTest]
        public IEnumerator OGolpe_NaoAcertaPelasCostas()
        {
            yield return Montar();

            ZerarAcertos();
            _inimigo.transform.position =
                _jogador.transform.position + new Vector3(-_alcance, 0f, 0f);   // atrás
            Physics2D.SyncTransforms();

            _mao.TryAtacar(Vector2.right);                                   // golpe à frente

            float ate = Time.time + _preparo + _janela + 0.05f;
            while (Time.time < ate) yield return new WaitForFixedUpdate();

            Assert.AreEqual(0, _acertosNoInimigo,
                $"O golpe para a DIREITA acertou um alvo a {_alcance:0.##} unidades à ESQUERDA. " +
                "A hitbox está resolvendo por distância radial em vez de por direção.");
        }

        // ── 2b. portão de profundidade (uma célula, decisão de 2026-09-04) ───

        /// <summary>A hitbox do Damião, criada em runtime por <c>MaoFisicaBridge</c>.</summary>
        private FavelaAmarela.Runtime.Combat.Hitbox HitboxDoJogador()
        {
            var h = _jogador.GetComponentInChildren<FavelaAmarela.Runtime.Combat.Hitbox>(true);

            Assert.IsNotNull(h,
                "O Damião está sem Hitbox. Ela é criada em Hitbox.GarantirPara pelo " +
                "MaoFisicaBridge; sem ela o golpe não pode acertar nada.");

            return h;
        }

        private static void DefinirProfundidade(
            FavelaAmarela.Runtime.Combat.Hitbox hitbox, float valor)
        {
            var campo = typeof(FavelaAmarela.Runtime.Combat.Hitbox).GetField(
                "profundidadeMaxima",
                System.Reflection.BindingFlags.NonPublic |
                System.Reflection.BindingFlags.Instance);

            Assert.IsNotNull(campo,
                "O campo 'profundidadeMaxima' sumiu da Hitbox. É ele que limita a " +
                "profundidade de chão do golpe.");

            campo.SetValue(hitbox, valor);
        }

        /// <summary>Golpeia numa direção com o alvo numa posição arbitrária.</summary>
        private IEnumerator GolpearComAlvoEm(Vector2 direcao, Vector2 posicaoDoAlvo)
        {
            ZerarAcertos();

            _inimigo.transform.position = _jogador.transform.position + (Vector3)posicaoDoAlvo;
            Physics2D.SyncTransforms();

            _mao.TryAtacar(direcao.normalized);

            float ate = Time.time + _preparo + _janela + 0.05f;
            while (Time.time < ate) yield return new WaitForFixedUpdate();
        }

        /// <summary>
        /// A guarda que faltava em 2026-09-04, e cuja falta quebrou o combate inteiro.
        ///
        /// <para>O portão media profundidade com <c>transform.root</c>. Numa cena real os
        /// atores são filhos de contêineres de organização que ficam em <b>y = 0</b>, enquanto
        /// os atores estão por volta de <b>y = -14</b> — então <c>transform.root</c> devolvia o
        /// contêiner, a diferença dava 14, e <b>todo golpe do Damião era rejeitado</b>. O rig
        /// antigo montava tudo solto na raiz e em y = 0, onde <c>transform.root</c> é o próprio
        /// ator: o defeito não tinha como aparecer.</para>
        ///
        /// <para>Este teste afirma a propriedade diretamente, sem depender de acerto ou erro.</para>
        /// </summary>
        [UnityTest]
        public IEnumerator AProfundidadeDoGolpe_SegueOAtor_ENaoOConteinerDaCena()
        {
            yield return Montar();

            var hitbox = HitboxDoJogador();

            // Golpe para a direita: o deslocamento fica horizontal, então a profundidade do
            // golpe tem de ser exatamente a do Damião.
            yield return Golpear(Vector2.right, _alcance);

            float doAtor = _jogador.transform.position.y;
            float doConteiner = _contêinerDoJogador.transform.position.y;
            float medida = hitbox.AlturaDeChaoDoGolpe;

            Debug.Log($"[HitboxAudit] profundidade: ator y={doAtor:0.##}, " +
                      $"contêiner y={doConteiner:0.##}, AlturaDeChaoDoGolpe={medida:0.##}");

            Assert.AreNotEqual(doAtor, doConteiner,
                "O rig pôs ator e contêiner no mesmo Y, então este teste não distingue os dois " +
                "e passaria mesmo com o defeito. Ver OrigemDoRig.");

            Assert.AreEqual(doAtor, medida, 0.05f,
                $"A profundidade do golpe veio {medida:0.##}, e o Damião está em " +
                $"{doAtor:0.##}. O contêiner da cena está em {doConteiner:0.##} — se a medida " +
                "bate com ELE, o código está usando transform.root em vez do ator, e em jogo " +
                "isso rejeita todos os golpes.");
        }

        /// <summary>
        /// Um alvo a menos de <b>uma célula</b> de profundidade continua sendo acertado.
        /// </summary>
        [UnityTest]
        public IEnumerator OGolpe_AcertaDentroDeUmaCelulaDeProfundidade()
        {
            yield return Montar();

            const float profundidade = 0.4f;   // menos que a célula de 0,5
            yield return GolpearComAlvoEm(Vector2.right, new Vector2(_alcance, profundidade));

            Debug.Log($"[HitboxAudit] portão: alvo a {profundidade:0.##} de profundidade " +
                      $"(célula = {FavelaAmarela.Runtime.Combat.Hitbox.ProfundidadeDeUmaCelula:0.##}); " +
                      $"acertos = {_acertosNoInimigo}");

            Assert.Greater(_acertosNoInimigo, 0,
                $"O alvo está a {profundidade:0.##} unidades de profundidade — dentro da célula " +
                $"de {FavelaAmarela.Runtime.Combat.Hitbox.ProfundidadeDeUmaCelula:0.##} — e o " +
                "golpe não acertou. O portão está cortando perto demais e transformou um acerto " +
                "legítimo em erro.");
        }

        /// <summary>
        /// O teste que <b>prova</b> o portão: mesma posição, medida duas vezes. Com o portão
        /// desligado o alvo é acertado; com uma célula, não.
        ///
        /// <para>Medir só o erro não provaria nada — o alvo poderia estar simplesmente longe
        /// demais para o círculo. Provar o acerto primeiro, na MESMA posição, isola o portão
        /// como a única causa da diferença.</para>
        /// </summary>
        [UnityTest]
        public IEnumerator AlemDeUmaCelula_OPortaoEhOQueRejeita_ENaoADistancia()
        {
            yield return Montar();

            var posicao = new Vector2(_alcance, 1.0f);   // 2 células ao norte
            var hitbox = HitboxDoJogador();

            // ── 1. portão desligado: tem de acertar ──
            DefinirProfundidade(hitbox, 0f);
            yield return GolpearComAlvoEm(Vector2.right, posicao);
            int semPortao = _acertosNoInimigo;

            Debug.Log($"[HitboxAudit] portão: alvo em {posicao} — sem portão, acertos = {semPortao}");

            Assert.Greater(semPortao, 0,
                $"Com o portão DESLIGADO o alvo em {posicao} não foi acertado, então ele está " +
                "fora do alcance do círculo e esta posição não serve para medir o portão. " +
                "Aproxime o alvo, ou o teste vira um falso verde.");

            // ── 2. portão de uma célula: tem de errar ──
            yield return EsperarCooldown();
            DefinirProfundidade(hitbox,
                FavelaAmarela.Runtime.Combat.Hitbox.ProfundidadeDeUmaCelula);

            yield return GolpearComAlvoEm(Vector2.right, posicao);
            int comPortao = _acertosNoInimigo;

            Debug.Log($"[HitboxAudit] portão: mesma posição, com uma célula, " +
                      $"acertos = {comPortao}");

            Assert.AreEqual(0, comPortao,
                $"O alvo em {posicao} está a 1,0 unidade de profundidade — DUAS células — e o " +
                $"golpe o acertou mesmo com o portão em " +
                $"{FavelaAmarela.Runtime.Combat.Hitbox.ProfundidadeDeUmaCelula:0.##}. " +
                $"Com o portão desligado ele levou {semPortao} acerto(s) na mesma posição, " +
                "então a distância não é o que muda: o portão não está sendo aplicado.");
        }

        // ── 3. controle negativo: a máscara ──────────────────────────────────

        [UnityTest]
        public IEnumerator OGolpe_NaoAcertaOChao()
        {
            yield return Montar();

            // O chão é sólido, na camada Default, e fica ao alcance. Se aparecer no resultado,
            // a máscara do golpe está larga demais.
            _chao.transform.position = new Vector3(_alcance, 0f, 0f);
            Physics2D.SyncTransforms();

            yield return Golpear(Vector2.right, AlcanceMaximoDeCentroACentro + 5f);

            Assert.AreEqual(0, _acertosNoInimigo,
                "Com o inimigo longe e só o chão ao alcance, algo levou dano. A máscara do " +
                "golpe está enxergando a camada errada.");
        }

        // ── 4. golpe do inimigo no jogador ───────────────────────────────────

        /// <summary>
        /// Monta a hitbox do lado do inimigo pelo mesmo caminho que o Byakhee usa
        /// (<c>Hitbox.GarantirPara</c> + <c>Armar</c>) — que é o único caminho de inimigo com
        /// janela ativa neste projeto. Cultista, Esqueleto e Sseth ainda batem por proximidade
        /// instantânea; ver <c>systems/auditoria_hitbox_hurtbox.md</c>.
        /// </summary>
        private FavelaAmarela.Runtime.Combat.Hitbox ArmarHitboxDoInimigo(float raio, float alcance)
        {
            var mascara = LayerMask.GetMask(CamadaHurtboxJogador);

            var hitbox = FavelaAmarela.Runtime.Combat.Hitbox.GarantirPara(
                _inimigo, "Hitbox_Teste", mascara, raio, alcance, pouparAliados: false);

            Assert.IsNotNull(hitbox,
                "Hitbox.GarantirPara devolveu null para o inimigo — sem ela não há golpe de " +
                "inimigo para medir.");

            return hitbox;
        }

        private IEnumerator GolpeDoInimigo(float distancia, float raio, float alcance)
        {
            _danoNoJogador = 0f;

            _inimigo.transform.position =
                _jogador.transform.position + new Vector3(distancia, 0f, 0f);
            Physics2D.SyncTransforms();

            var hitbox = ArmarHitboxDoInimigo(raio, alcance);
            var golpe = new FavelaAmarela.Core.Abilities.ArmaResult(
                true, 0f, 0f, false, 0f, 10f);

            // Aponta de volta para o jogador, que está na origem.
            hitbox.Armar(golpe, 0.2f, Vector2.left);

            float ate = Time.time + 0.25f;
            while (Time.time < ate) yield return new WaitForFixedUpdate();
        }

        [UnityTest]
        public IEnumerator OGolpeDoInimigo_AcertaDentroDoAlcance()
        {
            yield return Montar();

            const float raio = 0.6f, alcance = 1.2f;
            yield return GolpeDoInimigo(alcance, raio, alcance);

            Assert.Greater(_danoNoJogador, 0f,
                $"O golpe do inimigo a {alcance:0.##} unidades não acertou o jogador. " +
                "Confira se a hurtbox do Damião está na camada PlayerHurtbox e se o colisor " +
                "dela está ligado.");
        }

        [UnityTest]
        public IEnumerator OGolpeDoInimigo_ErraAlemDoAlcance()
        {
            yield return Montar();

            const float raio = 0.6f, alcance = 1.2f;
            float meiaLarguraDoJogador = MeiaLarguraDaHurtboxDoJogador();
            float alem = alcance + raio + meiaLarguraDoJogador + 0.1f;

            yield return GolpeDoInimigo(alem, raio, alcance);

            Assert.AreEqual(0f, _danoNoJogador,
                $"O golpe do inimigo acertou a {alem:0.###} unidades, além do limite de " +
                $"{alcance + raio + meiaLarguraDoJogador:0.###}.");
        }

        private float MeiaLarguraDaHurtboxDoJogador()
        {
            var hurtbox = _jogador.GetComponentInChildren<FavelaAmarela.Runtime.Combat.Hurtbox>(true);
            Assert.IsNotNull(hurtbox, "O Damião do rig está sem Hurtbox.");

            var col = hurtbox.GetComponent<Collider2D>();
            Assert.IsNotNull(col, "A Hurtbox do Damião está sem Collider2D.");
            return col.bounds.extents.x;
        }

        // ── 5. i-frames da Esquiva ───────────────────────────────────────────

        /// <summary>
        /// Os i-frames deste projeto <b>desligam o Collider2D da hurtbox</b> — não trocam de
        /// camada. É a única forma que funciona aqui: o dano é resolvido por <b>consulta</b>
        /// (<c>OverlapCircle</c>), e consulta não olha a matriz de colisão, só a máscara.
        /// </summary>
        [UnityTest]
        public IEnumerator DuranteOsIFrames_OJogadorNaoLevaDano()
        {
            yield return Montar();

            var hurtbox = _jogador.GetComponentInChildren<FavelaAmarela.Runtime.Combat.Hurtbox>(true);
            var col = hurtbox.GetComponent<Collider2D>();

            // Desliga o colisor pelo mesmo mecanismo da EsquivaBridge. Chamar
            // TryActivateEsquiva exigiria EsquivaConfig autorada e Vigor — dependências que
            // não são o que este teste mede.
            col.enabled = false;
            Physics2D.SyncTransforms();

            yield return GolpeDoInimigo(1.2f, 0.6f, 1.2f);

            Assert.AreEqual(0f, _danoNoJogador,
                "O jogador levou dano com a hurtbox DESLIGADA. Os i-frames da Esquiva não " +
                "protegem contra nada — e como o dano é resolvido por consulta, desligar o " +
                "colisor era a única defesa que funcionava.");
        }

        [UnityTest]
        public IEnumerator DepoisDosIFrames_OJogadorVoltaALevarDano()
        {
            yield return Montar();

            var hurtbox = _jogador.GetComponentInChildren<FavelaAmarela.Runtime.Combat.Hurtbox>(true);
            var col = hurtbox.GetComponent<Collider2D>();

            col.enabled = false;
            yield return new WaitForFixedUpdate();
            col.enabled = true;            // fim dos i-frames
            Physics2D.SyncTransforms();

            yield return GolpeDoInimigo(1.2f, 0.6f, 1.2f);

            Assert.Greater(_danoNoJogador, 0f,
                "Depois dos i-frames o jogador continuou intocável. O colisor foi religado e " +
                "mesmo assim a consulta não o achou — é o espelho do bug que o try/finally da " +
                "EsquivaBridge existe para impedir (invulnerabilidade permanente).");
        }

        // ── 6. as três fases ─────────────────────────────────────────────────

        /// <summary>
        /// Durante o PREPARO o golpe ainda não acerta — e depois dele, acerta.
        ///
        /// <para>É a fase que passou a existir em 2026-09-03. Até aqui a hitbox era armada no
        /// mesmo quadro do comando: o golpe saía do nada, sem telegrafo, e não havia instante
        /// em que ele já estava decidido e ainda não tinha acertado.</para>
        ///
        /// <para>Os dois lados são um teste só de propósito. Só o primeiro passaria com a
        /// hitbox <b>quebrada</b> (nunca arma); só o segundo passaria com o preparo
        /// <b>ignorado</b>. Juntos, prendem a fase entre duas paredes.</para>
        /// </summary>
        [UnityTest]
        public IEnumerator OPreparo_AdiaOAcerto_ENaoOImpede()
        {
            yield return Montar();

            Assert.Greater(_preparo, 0f,
                "O preparo veio zero — a fase não existe e este teste não mede nada.");

            ZerarAcertos();
            _inimigo.transform.position =
                _jogador.transform.position + new Vector3(_alcance, 0f, 0f);
            Physics2D.SyncTransforms();

            _mao.TryAtacar(Vector2.right);

            // Metade do preparo: o golpe foi comandado e a área ainda não abriu.
            float meio = Time.time + _preparo * 0.5f;
            while (Time.time < meio) yield return new WaitForFixedUpdate();

            Assert.AreEqual(0, _acertosNoInimigo,
                $"O golpe acertou {_preparo * 0.5f:0.###} s depois do comando, dentro do " +
                $"preparo de {_preparo:0.###} s. A hitbox está abrindo cedo demais — sem " +
                "preparo o golpe não tem telegrafo nenhum.");

            // Agora atravessa a janela inteira.
            float fim = Time.time + _preparo + _janela + 0.05f;
            while (Time.time < fim) yield return new WaitForFixedUpdate();

            Assert.Greater(_acertosNoInimigo, 0,
                "Passado o preparo e a janela inteira, o golpe nunca acertou. O preparo deixou " +
                "de adiar e passou a IMPEDIR — provavelmente a corrotina foi cortada, ou a FSM " +
                "saiu de Atacando antes de a janela abrir.");
        }

        /// <summary>
        /// A hitbox sai do <b>corpo</b>, não do pé.
        ///
        /// <para>O pivô de todo o elenco é BottomCenter, então uma hitbox em
        /// <c>localPosition</c> zero fica no chão. Medido em 2026-09-03: com raio 0,6 o círculo
        /// ia de y −0,60 a +0,60, <b>metade debaixo do chão</b>, e cruzava só 27% da hurtbox do
        /// alvo (0,46 de 1,72) — a canela. É de onde vem "meu golpe passa por baixo".</para>
        /// </summary>
        [UnityTest]
        public IEnumerator AHitbox_SaiDoCorpoENaoDoPe()
        {
            yield return Montar();

            var hitbox = _jogador.GetComponentInChildren<FavelaAmarela.Runtime.Combat.Hitbox>(true);
            Assert.IsNotNull(hitbox,
                "O Damião do rig está sem Hitbox — MaoFisicaBridge.GarantirHitbox não rodou.");

            float altura = hitbox.transform.localPosition.y;

            Assert.Greater(altura, 0f,
                $"A hitbox está em y {altura:0.###} — no PÉ. Com o pivô em BottomCenter isso " +
                "põe metade do círculo debaixo do chão, e as hurtboxes ficam no corpo.");

            // E ela tem de cruzar a maior parte do corpo do alvo, não a canela.
            var hurtbox = _inimigo
                .GetComponentInChildren<FavelaAmarela.Runtime.Combat.Hurtbox>(true)
                .GetComponent<Collider2D>();

            float alvoBaixo = hurtbox.bounds.center.y - hurtbox.bounds.extents.y;
            float alvoAlto = hurtbox.bounds.center.y + hurtbox.bounds.extents.y;
            float golpeBaixo = altura - _raio;
            float golpeAlto = altura + _raio;

            float sobreposicao = Mathf.Max(0f, Mathf.Min(golpeAlto, alvoAlto)
                                             - Mathf.Max(golpeBaixo, alvoBaixo));
            float fracao = sobreposicao / (alvoAlto - alvoBaixo);

            Assert.Greater(fracao, 0.5f,
                $"O golpe cruza só {fracao * 100:0}% do corpo do alvo (de y " +
                $"{golpeBaixo:0.##} a {golpeAlto:0.##}, contra {alvoBaixo:0.##} a " +
                $"{alvoAlto:0.##}). Antes do conserto de 2026-09-03 eram 27% — a canela.");
        }

        // ── 7. o registro para revisão manual ────────────────────────────────

        /// <summary>
        /// Não afirma nada sobre balanceamento — <b>relata</b>. A geometria de arma é decisão de
        /// design e vai mudar; fixar valores aqui só criaria um teste que quebra a cada ajuste.
        /// O que ele garante é que os números <b>existem e são coerentes</b>, e imprime o resto
        /// para leitura humana ao lado da célula isométrica de 1 × 0,5.
        /// </summary>
        [UnityTest]
        public IEnumerator AGeometriaMedida_EhRegistradaParaRevisao()
        {
            yield return Montar();

            float celula = FavelaAmarela.Core.Player.BaseIsometrica.AlturaDeCelulaPadrao;
            float limite = AlcanceMaximoDeCentroACentro;

            var hurtboxInimigo = _inimigo
                .GetComponentInChildren<FavelaAmarela.Runtime.Combat.Hurtbox>(true)
                .GetComponent<Collider2D>();

            var hurtboxJogador = _jogador
                .GetComponentInChildren<FavelaAmarela.Runtime.Combat.Hurtbox>(true)
                .GetComponent<Collider2D>();

            Debug.Log(
                "[HitboxAudit] medido em jogo, célula isométrica 1 × " + celula.ToString("0.##") +
                System.Environment.NewLine +
                $"  golpe do jogador : alcance {_alcance:0.###}  raio {_raio:0.###}" +
                System.Environment.NewLine +
                $"  fases            : preparo {_preparo:0.###} s  ativo {_janela:0.###} s " +
                $"({_janela / Time.fixedDeltaTime:0.#} ticks) " +
                $"total {_preparo + _janela + LerPropriedadePrivada("RecuperacaoAtual"):0.###} s" +
                System.Environment.NewLine +
                $"  cobre de {_alcance - _raio:0.###} a {_alcance + _raio:0.###} à frente " +
                $"= {(_alcance + _raio) / 1f:0.##} larguras de célula" +
                System.Environment.NewLine +
                $"  hurtbox do alvo  : {hurtboxInimigo.bounds.size.x:0.###} × " +
                $"{hurtboxInimigo.bounds.size.y:0.###} em {hurtboxInimigo.bounds.center}" +
                System.Environment.NewLine +
                $"  hurtbox do Damião: {hurtboxJogador.bounds.size.x:0.###} × " +
                $"{hurtboxJogador.bounds.size.y:0.###} em {hurtboxJogador.bounds.center}" +
                System.Environment.NewLine +
                $"  acerto máximo de centro a centro: {limite:0.###}" +
                System.Environment.NewLine +
                $"  origem do golpe  : y {_jogador.GetComponentInChildren<FavelaAmarela.Runtime.Combat.Hitbox>(true).transform.localPosition.y:0.###} " +
                "(0 seria o pé, com metade do círculo debaixo do chão)");

            Assert.Greater(_janela, Time.fixedDeltaTime * 2f,
                $"A janela do golpe é {_janela:0.###} s e o Fixed Timestep é " +
                $"{Time.fixedDeltaTime:0.###} s — isso dá " +
                $"{_janela / Time.fixedDeltaTime:0.#} consultas. Com menos de duas, uma queda " +
                "de framerate muda a chance de acertar um alvo em movimento.");

            Assert.Greater(limite, celula,
                $"O alcance máximo do golpe ({limite:0.###}) não chega a uma altura de célula " +
                $"({celula}). O jogador não conseguiria acertar quem está na célula vizinha.");
        }

        private IEnumerator EsperarCooldown()
        {
            // A arma recusa o golpe seguinte antes da cadência (arma.CanActivate). Meio segundo
            // cobre a mão vazia com folga.
            float ate = Time.time + 0.5f;
            while (Time.time < ate) yield return null;
        }
    }
}
