using System.Collections;
using System.Reflection;
using System.Text.RegularExpressions;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace FavelaAmarela.Tests.PlayMode
{
    /// <summary>
    /// Guarda que o golpe do <b>Esqueleto Invocado</b> passa pela <c>Hurtbox</c> do Damião, e
    /// não direto na <c>VitalidadeBridge</c>.
    ///
    /// <para><b>O defeito que isto fecha (2026-09-04).</b> O Esqueleto era o último inimigo do
    /// projeto com golpe instantâneo: <c>TentarGolpear</c> chamava
    /// <c>_vitalidadeDoAlvo.ReceberDanoFisico(ataque)</c> assim que a cadência estourava, tendo
    /// como única condição um <c>Vector2.Distance</c> medido no <c>FixedUpdate</c>. Sem janela e
    /// sem geometria.</para>
    ///
    /// <para><b>E a consequência era maior que "não dava para esquivar no tempo".</b> Os quadros
    /// de invencibilidade da Esquiva neste projeto são implementados <b>desligando o colisor da
    /// hurtbox</b> (ver <c>EsquivaBridge.EsquivaIFramesCoroutine</c>) — o dano chega por
    /// consulta, então sumir da consulta é ficar imune. Batendo direto na bridge, o Esqueleto
    /// <b>não consultava nada</b>: rolar por cima dele custava vida do mesmo jeito. E os
    /// Esqueletos existem justamente para pressionar o jogador enquanto ele procura as Pedras de
    /// Poder na luta do Abdul, que é quando mais se rola.</para>
    ///
    /// <para><b>Por que o teste desliga o colisor em vez de chamar a Esquiva de verdade.</b>
    /// Desligar o colisor <i>é</i> o mecanismo — a Esquiva não tem outro. Acioná-la pela
    /// <c>EsquivaBridge</c> arrastaria FSM, Vigor e <c>EsquivaConfig</c> para dentro de um teste
    /// que mede outra coisa, e faria a falha apontar para cinco lugares em vez de um.</para>
    /// </summary>
    public sealed class GolpeDoEsqueletoTests
    {
        private const string CamadaHurtboxJogador = "PlayerHurtbox";

        /// <summary>
        /// Onde o rig monta o elenco. <b>Não é a origem</b>, pela mesma razão que em
        /// <c>HitboxAuditTests</c>: na Tumba os atores ficam por volta de y = -14, e o portão de
        /// profundidade da <c>Hitbox</c> compara alturas de chão. Um rig achatado no zero faz
        /// toda conta errada dar certo.
        /// </summary>
        private static readonly Vector3 OrigemDoRig = new Vector3(12f, -14f, 0f);

        private GameObject _contêinerDoJogador;
        private GameObject _contêinerDosInimigos;
        private GameObject _jogador;
        private GameObject _esqueleto;

        private FavelaAmarela.Runtime.Combat.VitalidadeBridge _vidaDoJogador;
        private Collider2D _colisorDaHurtbox;
        private float _danoNoJogador;

        /// <summary>Cadência curta: o padrão do prefab é 1,5 s e o teste não mede espera.</summary>
        private const float CadenciaDeTeste = 0.05f;

        /// <summary>
        /// Sprite de 1×2 unidades a PPU 32, pivô no pé — como todo o elenco.
        /// <c>Hurtbox.GarantirPara</c> deriva a área de <c>sprite.bounds</c> e recusa sem sprite.
        /// </summary>
        private static Sprite SpriteDeCorpo()
        {
            var tex = new Texture2D(32, 64);
            var pixels = new Color32[32 * 64];
            for (int i = 0; i < pixels.Length; i++) pixels[i] = new Color32(255, 255, 255, 255);
            tex.SetPixels32(pixels);
            tex.Apply();

            return Sprite.Create(tex, new Rect(0, 0, 32, 64), new Vector2(0.5f, 0f), 32f);
        }

        private IEnumerator Montar()
        {
            // O rig roda sem FichaAtributosConfig de propósito: mede geometria, não
            // balanceamento, e a bridge tem fallback documentado. Sem isto o Test Framework
            // reprova a classe inteira por um erro que o próprio rig provoca.
            LogAssert.Expect(LogType.Error, new Regex("Nenhuma ficha encontrada"));

            Assert.GreaterOrEqual(LayerMask.NameToLayer(CamadaHurtboxJogador), 0,
                $"A camada '{CamadaHurtboxJogador}' não existe no TagManager — sem ela a " +
                "máscara do golpe fica vazia e o jogador é inatingível por qualquer inimigo.");

            _contêinerDoJogador = new GameObject("Conteudo_DaCena");
            _contêinerDosInimigos = new GameObject("Inimigos_DaCena");

            // ── o jogador ────────────────────────────────────────────────────
            _jogador = new GameObject("Damiao", typeof(Rigidbody2D));
            _jogador.tag = "Player";
            _jogador.transform.SetParent(_contêinerDoJogador.transform, false);
            _jogador.transform.position = OrigemDoRig;
            _jogador.AddComponent<SpriteRenderer>().sprite = SpriteDeCorpo();
            _vidaDoJogador = _jogador.AddComponent<FavelaAmarela.Runtime.Combat.VitalidadeBridge>();

            var hurtbox = FavelaAmarela.Runtime.Combat.Hurtbox.GarantirPara(
                _jogador, CamadaHurtboxJogador);
            Assert.IsNotNull(hurtbox, "Hurtbox.GarantirPara devolveu null para o Damião.");
            _colisorDaHurtbox = hurtbox.GetComponent<Collider2D>();
            Assert.IsNotNull(_colisorDaHurtbox,
                "A hurtbox do Damião não tem Collider2D — os i-frames da Esquiva desligam " +
                "exatamente este colisor, então sem ele não há o que medir.");

            // ── o esqueleto, à esquerda e dentro do alcance ──────────────────
            _esqueleto = new GameObject("EsqueletoInvocado", typeof(Rigidbody2D));
            _esqueleto.transform.SetParent(_contêinerDosInimigos.transform, false);
            _esqueleto.transform.position = OrigemDoRig + new Vector3(-0.5f, 0f, 0f);

            // Sprite e colisor ANTES do componente: o Awake dele chama Hurtbox.GarantirPara,
            // que lê sprite.bounds, e o [RequireComponent] pede um Collider2D concreto —
            // Collider2D é abstrato e a Unity não tem como criá-lo sozinha.
            _esqueleto.AddComponent<SpriteRenderer>().sprite = SpriteDeCorpo();
            _esqueleto.AddComponent<CircleCollider2D>().isTrigger = true;

            var invocado = _esqueleto.AddComponent<FavelaAmarela.Runtime.Enemies.EsqueletoInvocado>();
            DefinirCampo(invocado, "cadenciaDeAtaque", CadenciaDeTeste);

            yield return null;   // deixa os Awake rodarem

            invocado.Bind(_jogador.transform);
            _vidaDoJogador.OnDanoSofrido += d => _danoNoJogador += d;

            _danoNoJogador = 0f;
            Physics2D.SyncTransforms();
            yield return new WaitForFixedUpdate();
        }

        private static void DefinirCampo(object alvo, string nome, float valor)
        {
            var campo = alvo.GetType().GetField(
                nome, BindingFlags.Instance | BindingFlags.NonPublic);

            Assert.IsNotNull(campo,
                $"O campo '{nome}' sumiu de {alvo.GetType().Name}. O teste lê o valor real do " +
                "componente em vez de repetir um número — se o campo foi renomeado, o teste " +
                "tem de acompanhar em vez de medir outra coisa em silêncio.");

            campo.SetValue(alvo, valor);
        }

        /// <summary>Deixa o Esqueleto bater por tempo suficiente para a cadência estourar.</summary>
        private IEnumerator DeixarGolpear()
        {
            float ate = Time.time + CadenciaDeTeste + 0.3f;
            while (Time.time < ate) yield return new WaitForFixedUpdate();
        }

        [TearDown]
        public void Derrubar()
        {
            foreach (var go in new[] { _jogador, _esqueleto, _contêinerDoJogador, _contêinerDosInimigos })
                if (go != null) Object.DestroyImmediate(go);
        }

        [UnityTest]
        public IEnumerator OEsqueleto_ContinuaFerindoOJogador()
        {
            yield return Montar();
            yield return DeixarGolpear();

            Assert.Greater(_danoNoJogador, 0f,
                "O Esqueleto encostado no Damião não causou dano nenhum. A migração do golpe " +
                "instantâneo para a Hitbox com janela deixou o inimigo inofensivo — que é pior " +
                "que o defeito que ela veio consertar.");
        }

        [UnityTest]
        public IEnumerator OGolpe_PassaPelaHurtbox_EntaoOsIFramesProtegem()
        {
            yield return Montar();

            // É isto que a Esquiva faz: tira a hurtbox do ar. Ver EsquivaIFramesCoroutine.
            _colisorDaHurtbox.enabled = false;
            Physics2D.SyncTransforms();

            yield return DeixarGolpear();

            Assert.AreEqual(0f, _danoNoJogador,
                $"Com a hurtbox do Damião desligada — que é exatamente o que os i-frames da " +
                $"Esquiva fazem — o Esqueleto ainda tirou {_danoNoJogador:0.##} de vida. Isso " +
                "significa que o golpe voltou a bater direto na VitalidadeBridge, sem consultar " +
                "a hurtbox, e portanto que rolar por cima de um Esqueleto custa vida do mesmo " +
                "jeito.");
        }
    }
}
