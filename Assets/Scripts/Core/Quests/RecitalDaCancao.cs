using System;

namespace FavelaAmarela.Core.Quests
{
    /// <summary>
    /// POCO puro. O <b>recital final</b> da quest de Cassilda: depois de receber os
    /// fragmentos, a rainha pede que Damião diga as estrofes que ela não consegue mais
    /// evocar. Ela cantou a Canção por eras até gastar as palavras — <b>não as chama, mas
    /// as reconhece</b>. É esse reconhecimento que dá à pergunta uma resposta certa.
    ///
    /// <para>Cada estrofe é uma escolha entre opções; só uma está certa. <b>Errar não tem
    /// custo mecânico</b> (decisão do Vini, 2026-08-02) — o jogador repete a mesma estrofe
    /// quantas vezes precisar, e a única consequência é a rainha ficar mais fria. Drenar
    /// Resiliência Mental aqui seria contradição: o Santuário é área de calmaria e tem um
    /// Refúgio a poucos passos, que anularia a penalidade.</para>
    ///
    /// <para>O recital <b>não é persistido pela metade</b>: quem sai do Santuário no meio
    /// refaz as duas perguntas. São vinte segundos de conversa, e a alternativa custaria
    /// uma chave de save por estrofe.</para>
    ///
    /// <para>Este POCO não conhece o <i>texto</i> de nada — só quantas estrofes existem e
    /// qual opção é a certa em cada uma. As falas e os versos são conteúdo autorado no
    /// Inspector da camada Runtime.</para>
    /// </summary>
    public sealed class RecitalDaCancao
    {
        private readonly int[] _respostasCertas;

        /// <summary>Quantas estrofes o recital cobra. Pode ser zero (recital vazio).</summary>
        public int Total => _respostasCertas.Length;

        /// <summary>
        /// Índice (0-based) da estrofe sendo cobrada agora. Quando chega a
        /// <see cref="Total"/>, o recital acabou — ver <see cref="Completo"/>.
        /// </summary>
        public int EstrofeAtual { get; private set; }

        /// <summary>
        /// Se todas as estrofes já foram acertadas. Um recital <b>vazio</b> nasce completo:
        /// é o que mantém o comportamento antigo da quest (entregar tudo → Patuá) para quem
        /// ainda não autorou as estrofes no Inspector.
        /// </summary>
        public bool Completo => EstrofeAtual >= Total;

        /// <summary>Quantas vezes o jogador errou, somando todas as estrofes.</summary>
        public int Erros { get; private set; }

        /// <summary>Disparado ao acertar, com o índice da estrofe que acabou de fechar.</summary>
        public event Action<int> OnAcerto;

        /// <summary>Disparado ao errar, com o índice da estrofe que continua em aberto.</summary>
        public event Action<int> OnErro;

        /// <param name="respostasCertas">
        /// Índice da opção correta de cada estrofe, na ordem em que são cobradas. Um array
        /// vazio cria um recital que já nasce <see cref="Completo"/>.
        /// </param>
        /// <exception cref="ArgumentNullException">Se <paramref name="respostasCertas"/> for nulo.</exception>
        public RecitalDaCancao(params int[] respostasCertas)
        {
            _respostasCertas = respostasCertas
                ?? throw new ArgumentNullException(nameof(respostasCertas));
        }

        /// <summary>
        /// Qual opção fecha a estrofe de índice <paramref name="estrofe"/>. Devolve -1 fora
        /// da faixa — a rainha não tem resposta para uma estrofe que a canção não tem.
        /// </summary>
        public int RespostaCertaDe(int estrofe)
            => estrofe >= 0 && estrofe < _respostasCertas.Length ? _respostasCertas[estrofe] : -1;

        /// <summary>
        /// Responde a estrofe corrente. Acertar avança para a próxima; errar deixa tudo como
        /// está, e o jogador tenta de novo a <b>mesma</b> estrofe — nunca volta ao começo,
        /// porque perder um acerto já conquistado seria punição, e o desenho é sem punição.
        /// </summary>
        /// <param name="opcaoEscolhida">Índice da opção que o jogador escolheu.</param>
        /// <returns><c>true</c> se acertou.</returns>
        public bool Responder(int opcaoEscolhida)
        {
            if (Completo) return false;

            if (opcaoEscolhida != _respostasCertas[EstrofeAtual])
            {
                Erros++;
                OnErro?.Invoke(EstrofeAtual);
                return false;
            }

            int acertada = EstrofeAtual;
            EstrofeAtual++;
            OnAcerto?.Invoke(acertada);
            return true;
        }
    }
}
