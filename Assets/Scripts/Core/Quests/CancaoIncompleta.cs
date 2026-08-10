using System;

namespace FavelaAmarela.Core.Quests
{
    /// <summary>
    /// Em que ponto a quest "A Canção Incompleta" está.
    /// </summary>
    public enum EstadoDaQuest
    {
        /// <summary>Damião ainda não falou com Cassilda.</summary>
        NaoIniciada = 0,

        /// <summary>Cassilda pediu os fragmentos; faltam entregas.</summary>
        EmAndamento = 1,

        /// <summary>
        /// Todos os fragmentos chegaram, mas a canção ainda não foi completada: a rainha
        /// está cobrando as estrofes que não consegue mais evocar. <b>Ter tudo entregue não
        /// é ter a recompensa</b> — ver <see cref="RecitalDaCancao"/>.
        /// </summary>
        Recitando = 2,

        /// <summary>Todos os fragmentos foram entregues e o Patuá, recebido.</summary>
        Concluida = 3,
    }

    /// <summary>
    /// POCO puro. A quest de Cassilda: <b>"A Canção Incompleta"</b> — recolher os
    /// fragmentos dos diários dos nobres de Yhtill e devolvê-los à rainha, que só então
    /// pode cantar o nome de cada um. Recompensa: o Patuá das Luas Gêmeas.
    ///
    /// <para><b>Escopo reduzido para 3 fragmentos</b> (decisão do Vini, 2026-08-01). O
    /// design original pedia 5, mas os de nº 4 e 5 (Crônicas de Lord Aldaron) ficam no
    /// Templo da Serpente — dungeon que não existe. Com 5, a quest seria impossível de
    /// fechar no Vertical Slice; com 3, ela tem começo, meio e fim.</para>
    ///
    /// <para>Aceita <b>entrega parcial</b>: a rainha recebe cada fragmento na hora, com fala
    /// própria, e só entrega o Patuá quando tiver todos. É o que permite a quest ser feita
    /// em qualquer ordem de exploração — as quests do overworld são independentes entre si
    /// (decisão de 2026-07-28).</para>
    ///
    /// <para><b>Entregar tudo não fecha a quest.</b> Com os fragmentos na mão, a rainha
    /// cobra as estrofes que a canção não traz escritas (<see cref="RecitalDaCancao"/>) — só
    /// depois vem o Patuá. É a diferença entre <see cref="EstadoDaQuest.Recitando"/> e
    /// <see cref="EstadoDaQuest.Concluida"/>.</para>
    /// </summary>
    public sealed class CancaoIncompleta
    {
        /// <summary>Quantos fragmentos a quest exige.</summary>
        public const int TotalPadrao = 3;

        private readonly bool[] _entregues;
        private readonly RecitalDaCancao _recital;

        /// <summary>Estado corrente.</summary>
        public EstadoDaQuest Estado { get; private set; } = EstadoDaQuest.NaoIniciada;

        /// <summary>Quantos fragmentos a quest exige no total.</summary>
        public int Total => _entregues.Length;

        /// <summary>Quantos já foram entregues a Cassilda.</summary>
        public int Entregues { get; private set; }

        /// <summary>Quantos ainda faltam.</summary>
        public int Restantes => Total - Entregues;

        /// <summary>Se todos foram entregues (independente do Patuá já ter sido dado).</summary>
        public bool TodosEntregues => Entregues >= Total;

        /// <summary>Disparado a cada entrega, com o índice do fragmento entregue.</summary>
        public event Action<int> OnFragmentoEntregue;

        /// <summary>Disparado quando a quest fecha (Patuá entregue).</summary>
        public event Action OnConcluida;

        /// <summary>
        /// O recital das estrofes finais. Nunca nulo — sem estrofes autoradas ele nasce
        /// completo, e a quest se comporta como antes (entregar tudo → Patuá).
        /// </summary>
        public RecitalDaCancao Recital => _recital;

        /// <param name="total">Quantos fragmentos exigir. Clampado a no mínimo 1.</param>
        /// <param name="respostasDaCancao">
        /// Índice da opção correta de cada estrofe cobrada no fim. Omitir (ou passar vazio)
        /// dispensa o recital — útil para os testes das regras de entrega, que não têm nada
        /// a ver com a canção.
        /// </param>
        public CancaoIncompleta(int total = TotalPadrao, params int[] respostasDaCancao)
        {
            _entregues = new bool[total < 1 ? 1 : total];
            _recital = new RecitalDaCancao(respostasDaCancao ?? Array.Empty<int>());
        }

        /// <summary>
        /// Cassilda faz o pedido. Idempotente — falar com ela de novo não reinicia nada.
        /// </summary>
        public void Iniciar()
        {
            if (Estado == EstadoDaQuest.NaoIniciada) Estado = EstadoDaQuest.EmAndamento;
        }

        /// <summary>Se um fragmento específico já foi entregue.</summary>
        public bool FoiEntregue(int indice)
            => indice >= 0 && indice < _entregues.Length && _entregues[indice];

        /// <summary>
        /// Entrega um fragmento. <b>Entregar o mesmo duas vezes não conta</b> — sem isso, um
        /// bug de UI ou um duplo-clique inflaria o progresso e daria o Patuá cedo demais.
        ///
        /// <para>Iniciar a quest é implícito: se o jogador achou um fragmento antes de falar
        /// com a rainha, entregar já a coloca em andamento.</para>
        /// </summary>
        /// <returns>Se a entrega contou.</returns>
        public bool Entregar(int indice)
        {
            if (indice < 0 || indice >= _entregues.Length) return false;
            if (_entregues[indice]) return false;
            if (Estado == EstadoDaQuest.Concluida) return false;

            Iniciar();
            _entregues[indice] = true;
            Entregues++;

            // A última página não fecha a quest sozinha: se ainda falta canção, abre o
            // recital em vez de deixar Concluir() liberado direto. Sem estrofes autoradas
            // (_recital já nasce completo) o comportamento fica como sempre foi — Estado
            // continua EmAndamento e Concluir() é quem fecha, chamado à parte pelo runtime.
            if (TodosEntregues && Estado == EstadoDaQuest.EmAndamento && !_recital.Completo)
                Estado = EstadoDaQuest.Recitando;

            OnFragmentoEntregue?.Invoke(indice);
            return true;
        }

        /// <summary>
        /// Responde a estrofe que a rainha está cobrando agora. Só vale durante o
        /// <see cref="EstadoDaQuest.Recitando"/> — antes disso ela ainda espera páginas,
        /// depois já não há o que perguntar.
        /// </summary>
        /// <param name="opcaoEscolhida">Índice da opção escolhida pelo jogador.</param>
        /// <returns>Se acertou. Errar não custa nada além de tentar de novo.</returns>
        public bool Responder(int opcaoEscolhida)
            => Estado == EstadoDaQuest.Recitando && _recital.Responder(opcaoEscolhida);

        /// <summary>
        /// Fecha a quest e marca o Patuá como entregue. Exige <b>todos os fragmentos</b> —
        /// a rainha não adianta a recompensa — <b>e a canção completa</b>: enquanto faltar
        /// uma estrofe, os nomes dos nobres não descansam e não há o que comemorar.
        /// </summary>
        /// <returns>Se a quest foi concluída agora (false se já estava, se falta fragmento ou se falta estrofe).</returns>
        public bool Concluir()
        {
            if (!TodosEntregues || Estado == EstadoDaQuest.Concluida) return false;
            if (!_recital.Completo) return false;

            Estado = EstadoDaQuest.Concluida;
            OnConcluida?.Invoke();
            return true;
        }
    }
}
