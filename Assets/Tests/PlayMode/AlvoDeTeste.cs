using UnityEngine;
using FavelaAmarela.Core.Abilities;
using FavelaAmarela.Core.Combat;

namespace FavelaAmarela.Tests.PlayMode
{
    /// <summary>
    /// Alvo mínimo para os testes de combate: implementa <see cref="IDanificavel"/>, garante a
    /// própria hurtbox como todo inimigo de verdade, e só <b>anota</b> o que recebeu.
    ///
    /// <para><b>Por que não usar a <c>VitalidadeBridge</c> no lugar dele.</b> O <c>Awake</c>
    /// dela chama <c>Hurtbox.GarantirPara(gameObject, "PlayerHurtbox")</c> — sem condição. No
    /// jogo isso está certo, porque ela só existe no Damião e no Yug-Neth, os dois aliados
    /// (conferido nos seis prefabs em 2026-09-03). Mas num inimigo ela criaria a hurtbox na
    /// camada do <b>jogador</b>, e a chamada seguinte com <c>EnemyHurtbox</c> acharia a que já
    /// existe e devolveria ela mesma, na camada errada — o alvo ficaria intocável pelo golpe do
    /// jogador, e o teste acusaria o jogo por um defeito do rig.</para>
    ///
    /// <para><b>Por que num arquivo próprio, e não aninhado no teste.</b> Nasceu aninhado dentro
    /// de <c>HitboxAuditTests</c> e o
    /// <c>GolpeAlcancaAHurtboxTests.TodoDanificavel_GaranteASuaHurtbox</c> o reprovou. A
    /// detecção daquele guarda procura um <c>MonoScript</c> cujo <b>nome de arquivo</b> seja o
    /// nome do tipo — uma classe aninhada não tem arquivo, então ela reprovava por não ser
    /// encontrável, e nenhuma chamada escrita lá dentro a salvaria. Dava para isentar assembly
    /// de teste no guarda; extrair custou menos e não enfraquece nada.</para>
    ///
    /// <para><b>Ele mede ACERTO, não dano.</b> A mão vazia deste jogo causa dano zero — o log do
    /// próprio <c>MaoFisicaBridge</c> diz <c>arma=DESARMADO (mão vazia) ... dano=0</c>. Uma
    /// auditoria de geometria pergunta se o golpe <b>chegou</b>; quanto dói é balanceamento, e
    /// muda.</para>
    /// </summary>
    [AddComponentMenu("")]   // fora do menu: é peça de teste, não de cena
    public sealed class AlvoDeTeste : MonoBehaviour, IDanificavel
    {
        /// <summary>Quantas vezes uma hitbox entregou golpe aqui.</summary>
        public int GolpesRecebidos { get; set; }

        /// <summary>Soma do dano recebido. Zero com mão vazia, e isso é esperado.</summary>
        public float DanoAcumulado { get; set; }

        /// <summary>Cenário destrutível comum — leva crítico furtivo normalmente.</summary>
        public bool EhAparicaoPrimordial => false;

        /// <summary>
        /// Garante a própria hurtbox, exatamente como <c>EsqueletoInvocado</c>,
        /// <c>PedraDePoder</c> e <c>AbdulAlhazredAI</c> fazem.
        ///
        /// <para>Não é cerimônia para calar um teste: é o contrato que ele guarda. Quem
        /// implementa <see cref="IDanificavel"/> e não garante hurtbox fica <b>intocável</b>,
        /// porque o golpe do jogador só consulta a camada de hurtbox. Um dublê que não cumpre o
        /// contrato mediria um alvo que o jogo não tem.</para>
        /// </summary>
        private void Awake()
            => FavelaAmarela.Runtime.Combat.Hurtbox.GarantirPara(gameObject, "EnemyHurtbox");

        /// <inheritdoc />
        public void ReceberGolpe(ArmaResult resultado)
        {
            GolpesRecebidos++;
            DanoAcumulado += resultado.Dano;
        }
    }
}
