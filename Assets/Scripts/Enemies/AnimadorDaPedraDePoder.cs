using UnityEngine;

namespace FavelaAmarela.Runtime.Enemies
{
    /// <summary>
    /// Camada Runtime. Gira a aura da <see cref="PedraDePoder"/> — mesmo padrão dos
    /// <c>AnimadorDo*</c> do elenco: um <see cref="MonoBehaviour"/> que escreve
    /// <c>SpriteRenderer.sprite</c> direto, sem <c>AnimatorController</c>.
    ///
    /// <para><b>Por que isto não é enfeite (2026-09-03).</b> A Pedra sustenta o Escudo Mágico
    /// do Abdul na Fase 1, e quebrá-la é a <b>única</b> forma de causar dano naquela fase — é o
    /// que, nas palavras do doc da própria <see cref="PedraDePoder"/>, <i>"transforma a Fase 1
    /// numa luta de arena (procurar e quebrar) em vez de bater no escudo"</i>. Até aqui a Pedra
    /// era uma imagem parada de 27×44 px: nada na tela dizia <b>quais</b> pedras ainda seguravam
    /// o escudo, e "procurar e quebrar" virava palpite. A aura é esse aviso.</para>
    ///
    /// <para><b>Sem estado, de propósito.</b> <c>PedraDePoder.Estilhacar()</c> termina em
    /// <c>gameObject.SetActive(false)</c> — a Pedra some inteira ao quebrar, e a aura vai junto.
    /// Não há um segundo estado para refletir, então este componente só gira enquanto a Pedra
    /// existe. Um <c>OnDestroy</c>/<c>OnQuebrada</c> aqui seria ligação morta.</para>
    ///
    /// <para>Não há POCO por trás porque não há regra por trás: isto é cadência de quadro, não
    /// decisão de jogo.</para>
    /// </summary>
    [RequireComponent(typeof(SpriteRenderer))]
    [AddComponentMenu("Favela Amarela/Enemies/Animador da Pedra de Poder")]
    public sealed class AnimadorDaPedraDePoder : MonoBehaviour
    {
        [Header("Quadros da aura")]
        [Tooltip("Os 12 quadros de Art/Enemies/PedraDePoder, em ordem. [ASSET pixel art]")]
        [SerializeField] private Sprite[] quadros;

        [Header("Cadência")]
        [Min(1f)]
        [Tooltip("O anel dá uma volta completa no ciclo; 10 fecha a volta em pouco mais de " +
                 "um segundo, devagar o bastante para ler como pulso e não como piscada.")]
        [SerializeField] private float quadrosPorSegundo = 10f;

        private SpriteRenderer _sprite;
        private float _relogio;
        private int _quadro;

        private void Awake() => _sprite = GetComponent<SpriteRenderer>();

        private void Update()
        {
            if (quadros == null || quadros.Length == 0) return;

            _relogio += Time.deltaTime * quadrosPorSegundo;
            if (_relogio < 1f) return;

            _relogio -= 1f;
            _quadro = (_quadro + 1) % quadros.Length;
            _sprite.sprite = quadros[_quadro];
        }
    }
}
