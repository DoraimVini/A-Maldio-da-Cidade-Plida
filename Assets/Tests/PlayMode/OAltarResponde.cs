using System.Collections;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UI;

namespace FavelaAmarela.Tests.PlayMode
{
    /// <summary>
    /// Guarda que o <b>ponto focal de relíquia responde ao jogador</b> — nos três desfechos.
    ///
    /// <para><b>O que motivou (2026-09-02).</b> O Vini: "acho que o que não está funcionando são
    /// os altares das relíquias". Medido, os altares funcionam <b>mecanicamente</b>: colisor
    /// presente, camada Default (a mesma de todo interagível que funciona), <c>artefatoId</c> e
    /// <c>rei</c> ligados nos três. O que não existia era <b>resposta</b>:</para>
    ///
    /// <list type="bullet">
    ///   <item>sem a relíquia: um <c>Debug.Log</c> — que num build não existe;</item>
    ///   <item>com a relíquia: troca de sprite, e <c>spriteInativo</c>/<c>spriteAtivo</c> estão
    ///         <b>vazios</b> nos três altares da cena.</item>
    /// </list>
    ///
    /// <para>Ou seja: o jogador apertava E e a tela não mudava em desfecho nenhum. Isso é
    /// indistinguível de um altar quebrado — e foi exatamente o que ele concluiu.</para>
    ///
    /// <para><b>A armadilha fina, que este teste fixa em pedra:</b> o ponto focal exige
    /// <c>Contem</c> (porte), não <c>Possui</c> (posse). São 4 slots de Artefato e 3 relíquias,
    /// então qualquer outro Artefato portado deixa uma delas dormente — e o altar recusava sem
    /// dizer por quê.</para>
    /// </summary>
    public sealed class OAltarResponde
    {
        private GameObject _hud;
        private GameObject _jogador;
        private GameObject _rei;
        private GameObject _altar;

        private FavelaAmarela.Runtime.UI.TutorialHintUI _caixa;
        private FavelaAmarela.Player.ArtefatosBridge _artefatos;
        private FavelaAmarela.Runtime.Itens.PontoFocalDeReliquia _ponto;

        private const string Id = "anel_sinal_amarelo";

        [TearDown]
        public void TearDown()
        {
            foreach (var go in new[] { _altar, _rei, _jogador, _hud })
                if (go != null) Object.DestroyImmediate(go);
        }

        private IEnumerator Montar()
        {
            FavelaAmarela.Runtime.UI.HUDController.GarantirInstancia();
            yield return null;

            var hud = Object.FindAnyObjectByType<FavelaAmarela.Runtime.UI.HUDController>(
                FindObjectsInactive.Include);
            Assert.IsNotNull(hud, "O HUD não subiu.");
            _hud = hud.gameObject;

            _caixa = _hud.GetComponentInChildren<FavelaAmarela.Runtime.UI.TutorialHintUI>(true);
            Assert.IsNotNull(_caixa, "Sem TutorialHintUI no HUD.");
            _caixa.TextoDeSaida.text = "";

            // Damiao de verdade o suficiente: DOIS componentes cobravam PlayerMovement dele
            // (a ponte de Artefatos, pelo Resguardo do Sinal; o Rei, pelo LookDirection que
            // decide se a Mascara Palida foi evitada). Montar o componente e mais honesto que
            // empilhar excecoes de log -- o rig passa a parecer com o jogo.
            _jogador = new GameObject("Damiao", typeof(Rigidbody2D));
            // O Rei procura a tag Player no Awake para saber quem observar. Dar a tag e mais
            // honesto do que esperar o erro: o rig passa a parecer com o jogo.
            _jogador.tag = "Player";
            _jogador.AddComponent<FavelaAmarela.Player.PlayerMovement>();
            _artefatos = _jogador.AddComponent<FavelaAmarela.Player.ArtefatosBridge>();

            // Esta fica como excecao: a ResilienciaBridge traria a ficha, o save e a barra de
            // UI atras dela, e o Colapso final nao e o que este teste mede.
            LogAssert.Expect(LogType.Error, new System.Text.RegularExpressions.Regex(
                "ReiEmAmarelo.*ResilienciaBridge"));

            _rei = new GameObject("Rei");
            _rei.SetActive(false);   // segura o Awake ate os campos estarem postos
            var rei = _rei.AddComponent<FavelaAmarela.Runtime.Enemies.ReiEmAmareloAI>();
            _rei.SetActive(true);

            _altar = new GameObject("Ponto_Focal", typeof(CircleCollider2D));
            _altar.SetActive(false);
            _ponto = _altar.AddComponent<FavelaAmarela.Runtime.Itens.PontoFocalDeReliquia>();
            Definir(_ponto, "artefatoId", Id);
            Definir(_ponto, "rei", rei);
            _altar.SetActive(true);

            yield return null;
        }

        private static void Definir(object alvo, string campo, object valor)
        {
            var f = alvo.GetType().GetField(campo,
                        BindingFlags.NonPublic | BindingFlags.Instance);
            Assert.IsNotNull(f, $"Campo '{campo}' não existe mais em {alvo.GetType().Name}.");
            f.SetValue(alvo, valor);
        }

        [UnityTest]
        public IEnumerator SemAReliquiaOAltarDizOQueFalta()
        {
            yield return Montar();

            _ponto.Interagir(_jogador);
            yield return null;

            // REGRA DURA: um altar que nao diz nada e indistinguivel de um altar quebrado.
            Assert.IsNotEmpty(_caixa.TextoDeSaida.text,
                "Apertei E sem a relíquia e o altar não disse NADA. Para quem joga, isso é " +
                "exatamente um altar quebrado — foi a conclusão do Vini em 2026-09-02.");
        }

        [UnityTest]
        public IEnumerator ComAReliquiaDormenteOAltarExplicaOPorque()
        {
            yield return Montar();

            // A relíquia POSSUÍDA e NÃO PORTADA, montada direto pelo Restaurar -- que é o
            // caminho do save e existe justamente para reconstruir posse e porte separados.
            //
            // Por que não montar "naturalmente": o jogo tem 4 Artefatos autorados e 4 slots,
            // então HOJE nada fica dormente. A primeira versão deste teste tentava encher os
            // slots e se AUTO-IGNORAVA por falta de um quinto Artefato -- e teste ignorado é
            // teste que não afirma nada. O estado passa a ser reachable no instante em que um
            // quinto Artefato for autorado, e a fala precisa estar pronta antes disso.
            _artefatos.Inventario.Restaurar(
                new[] { Id },
                new string[FavelaAmarela.Core.Artefatos.InventarioDeArtefatos.TotalDeSlots]);

            Assert.IsTrue(_artefatos.Inventario.Possui(Id), "A relíquia não ficou possuída.");
            Assert.IsFalse(_artefatos.Inventario.Contem(Id),
                "A relíquia ficou portada — o caso que este teste mede não foi montado.");

            _caixa.TextoDeSaida.text = "";
            _ponto.Interagir(_jogador);
            yield return null;

            StringAssert.Contains("dorme", _caixa.TextoDeSaida.text,
                "Com a relíquia POSSUÍDA mas dormente, o altar precisa dizer que ela não está " +
                "em mãos — senão o jogador acha que o altar está quebrado. Disse: " +
                $"'{_caixa.TextoDeSaida.text}'");
        }

        [UnityTest]
        public IEnumerator ComAReliquiaEmMaosOAltarDesperta()
        {
            yield return Montar();

            _artefatos.Adquirir(Id);
            Assert.IsTrue(_artefatos.Inventario.Contem(Id),
                $"'{Id}' não entrou portado — este teste não chega a medir o sucesso.");

            _caixa.TextoDeSaida.text = "";
            _ponto.Interagir(_jogador);
            yield return null;

            Assert.IsNotEmpty(_caixa.TextoDeSaida.text,
                "Ativei a relíquia e o altar não disse nada. A troca de sprite não serve de " +
                "aviso: spriteInativo e spriteAtivo estão VAZIOS nos três altares da cena.");
        }
    }
}
