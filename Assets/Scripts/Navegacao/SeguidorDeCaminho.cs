using System.Collections.Generic;
using UnityEngine;
using FavelaAmarela.Core.Navegacao;

namespace FavelaAmarela.Runtime.Navegacao
{
    /// <summary>
    /// Traduz "quero chegar ali" em "ande nesta direção agora", contornando o que houver no
    /// caminho.
    ///
    /// <para><b>É a peça que faltava em nove unidades.</b> Todas elas escrevem velocidade na
    /// direção do alvo e vão em linha reta — o que funciona hoje porque o Deserto de Hali é um
    /// plano aberto com um lago, e deixa de funcionar no instante em que o mapa ganhar
    /// geometria.</para>
    ///
    /// <para><b>Não move nada.</b> Devolve uma direção; quem escreve no <c>Rigidbody2D</c>
    /// continua sendo o componente de movimento de cada unidade. Assim esta peça entra sem
    /// reescrever nenhuma IA, e cada uma mantém a própria aceleração, velocidade e
    /// animação.</para>
    ///
    /// <para><b>Degrada para linha reta, de propósito.</b> Sem <c>NavegacaoDoMundo</c> em cena,
    /// ou sem caminho possível, ele devolve a direção crua do alvo — o comportamento de hoje.
    /// Um seguidor que devolvesse zero nesses casos transformaria "sem malha" em "inimigo
    /// paralisado", trocando um defeito visível por um pior.</para>
    /// </summary>
    [AddComponentMenu("Favela Amarela/Navegação/Seguidor de Caminho")]
    public sealed class SeguidorDeCaminho : MonoBehaviour
    {
        [Header("Recálculo")]
        [Tooltip("Segundos entre recálculos. Onze Cultistas recalculando todo quadro é " +
                 "travamento, não perseguição.")]
        [Min(0.05f)]
        [SerializeField] private float intervaloDeRecalculo = 0.35f;

        [Tooltip("Se o alvo se mover mais que isto (em unidades) desde o último cálculo, " +
                 "recalcula antes da hora — perseguir um alvo que correu é o caso que importa.")]
        [Min(0.1f)]
        [SerializeField] private float toleranciaDoAlvo = 1.5f;

        [Header("Chegada")]
        [Tooltip("Distância para considerar um ponto do caminho alcançado. Pequeno demais faz " +
                 "o ator orbitar o waypoint sem nunca chegar.")]
        [Min(0.05f)]
        [SerializeField] private float raioDeChegada = 0.25f;

        [Header("Diagnóstico")]
        [SerializeField] private bool desenharCaminho;

        // Uma busca POR SEGUIDOR: a classe reaproveita listas internas, então compartilhar uma
        // instância entre unidades faria o caminho de um sobrescrever o do outro.
        private readonly BuscaDeCaminho _busca = new BuscaDeCaminho();
        private readonly List<Vector3> _pontos = new List<Vector3>(32);

        private int _proximo;
        private float _tempoAteRecalcular;
        private Vector3 _alvoDoUltimoCalculo;
        private bool _temCaminho;

        /// <summary>Se há um caminho calculado e ainda não percorrido.</summary>
        public bool TemCaminho => _temCaminho && _proximo < _pontos.Count;

        /// <summary>Quantos pontos o caminho corrente tem. Para o console de diagnóstico.</summary>
        public int PontosNoCaminho => _pontos.Count;

        /// <summary>
        /// A direção em que andar <b>agora</b> para chegar a <paramref name="alvo"/>, já
        /// contornando obstáculos. Vetor unitário, ou zero se já chegou.
        /// </summary>
        public Vector2 DirecaoPara(Vector3 alvo)
        {
            _tempoAteRecalcular -= Time.deltaTime;

            bool alvoFugiu = (alvo - _alvoDoUltimoCalculo).sqrMagnitude >
                             toleranciaDoAlvo * toleranciaDoAlvo;

            if (_tempoAteRecalcular <= 0f || alvoFugiu || !TemCaminho)
                Recalcular(alvo);

            // Sem caminho: a direção crua. É o comportamento de hoje, e é melhor que parar.
            if (!TemCaminho) return DirecaoCrua(alvo);

            Vector3 destinoDoPasso = _pontos[_proximo];

            // Consome os pontos já alcançados. Um laço, e não um `if`, porque um ator rápido
            // pode passar de dois waypoints no mesmo quadro -- e aí ele voltaria atrás para
            // pegar o que pulou.
            while (_proximo < _pontos.Count &&
                   ((Vector2)(_pontos[_proximo] - transform.position)).sqrMagnitude <
                       raioDeChegada * raioDeChegada)
            {
                _proximo++;
            }

            if (_proximo >= _pontos.Count) return DirecaoCrua(alvo);

            destinoDoPasso = _pontos[_proximo];

            Vector2 delta = destinoDoPasso - transform.position;
            return delta.sqrMagnitude > 0.0001f ? delta.normalized : Vector2.zero;
        }

        /// <summary>
        /// O <b>próximo ponto do mundo</b> em direção a <paramref name="alvo"/>, contornando o
        /// que houver. Sem caminho, devolve o próprio alvo.
        ///
        /// <para>Existe porque nem toda unidade quer uma direção: <c>YugNethAI</c> e
        /// <c>EsqueletoInvocado</c> passam um <b>destino</b> ao seguidor de alvo deles, que tem
        /// aceleração e suavização próprias. Devolver um ponto em vez de um vetor deixa esses
        /// dois ganharem contorno <b>sem perder o movimento que já têm</b> — o que é a diferença
        /// entre acrescentar navegação e reescrever a IA de cada um.</para>
        /// </summary>
        public Vector3 ProximoPontoPara(Vector3 alvo)
        {
            // Reaproveita todo o cálculo e a política de recálculo de DirecaoPara; só o formato
            // da resposta muda.
            DirecaoPara(alvo);

            return TemCaminho ? _pontos[_proximo] : alvo;
        }

        /// <summary>Esquece o caminho — usado quando a unidade troca de objetivo.</summary>
        public void Limpar()
        {
            _pontos.Clear();
            _proximo = 0;
            _temCaminho = false;
            _tempoAteRecalcular = 0f;
        }

        private Vector2 DirecaoCrua(Vector3 alvo)
        {
            Vector2 delta = alvo - transform.position;
            return delta.sqrMagnitude > 0.0001f ? delta.normalized : Vector2.zero;
        }

        private void Recalcular(Vector3 alvo)
        {
            _tempoAteRecalcular = intervaloDeRecalculo;
            _alvoDoUltimoCalculo = alvo;

            _pontos.Clear();
            _proximo = 0;
            _temCaminho = false;

            var mundo = NavegacaoDoMundo.Instancia;
            if (mundo == null || !mundo.Pronta) return;   // sem malha: linha reta

            var caminho = _busca.Encontrar(mundo,
                                           mundo.ParaCelula(transform.position),
                                           mundo.ParaCelula(alvo));

            if (caminho.Count == 0) return;   // sem caminho: linha reta, e quem chama que decida

            for (int i = 0; i < caminho.Count; i++)
                _pontos.Add(mundo.ParaMundo(caminho[i]));

            // O último ponto vira o alvo REAL, e não o centro da célula dele: parar no centro
            // da célula deixaria o perseguidor a até meia célula do jogador, o que em jogo se
            // lê como "ele parou sem motivo".
            if (_pontos.Count > 0) _pontos[^1] = alvo;

            _temCaminho = true;
        }

        private void OnDrawGizmosSelected()
        {
            if (!desenharCaminho || _pontos.Count == 0) return;

            Gizmos.color = new Color(0.83f, 0.70f, 0.24f, 0.9f);

            for (int i = _proximo; i < _pontos.Count; i++)
            {
                Gizmos.DrawSphere(_pontos[i], 0.08f);
                if (i + 1 < _pontos.Count) Gizmos.DrawLine(_pontos[i], _pontos[i + 1]);
            }
        }
    }
}
