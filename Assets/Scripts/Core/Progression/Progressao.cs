using System;
using System.Collections.Generic;

namespace FavelaAmarela.Core.Progression
{
    /// <summary>
    /// POCO puro do <b>Labirinto de Carcosa</b>: nível de Exposição, pontos de Eco por gastar e
    /// quais nós da árvore já foram destrancados.
    ///
    /// <para><b>Guarda ids, não assets.</b> <c>EcoDef</c> é um <c>ScriptableObject</c>, e o Core
    /// não conhece <c>UnityEngine</c> (ver <c>Core/CLAUDE.md</c>). A resolução id→asset é
    /// responsabilidade do <c>ProgressionBridge</c>, que também traduz os pré-requisitos de um
    /// Eco para a lista de ids que <see cref="TryDesbloquearEco"/> espera.</para>
    ///
    /// <para><b>Vocabulário diegético:</b> a moeda de progressão é a <b>Exposição</b> (ganha por
    /// explorar e por eventos narrativos, nunca por grind); cada nível concede <b>1 Ponto de
    /// Eco</b>; e os nós da árvore são os <b>Ecos da Memória</b>.</para>
    ///
    /// <para>Extraído do <c>ProgressionManager</c> (MonoBehaviour) em 2026-08-18, Fase 3 da
    /// refatoração de managers.</para>
    /// </summary>
    public sealed class Progressao
    {
        private readonly int[] _curvaDeExposicao;
        private readonly HashSet<string> _ecosDesbloqueados = new HashSet<string>();

        private int _nivelAtual = 1;
        private int _exposicaoAtual;
        private int _pontosDeEco;

        /// <summary>Disparado sempre que Exposição é somada, mesmo sem subir de nível.</summary>
        public event Action OnExposicaoGanha;

        /// <summary>Disparado a cada nível ganho, com o nível novo.</summary>
        public event Action<int> OnLevelUp;

        /// <summary>Disparado ao destrancar um Eco, com o id dele.</summary>
        public event Action<string> OnEcoDesbloqueado;

        /// <summary>Nível de Exposição corrente. Começa em 1.</summary>
        public int NivelAtual => _nivelAtual;

        /// <summary>Exposição acumulada.</summary>
        public int ExposicaoAtual => _exposicaoAtual;

        /// <summary>Pontos de Eco ainda não gastos na árvore.</summary>
        public int PontosDeEcoDisponiveis => _pontosDeEco;

        /// <summary>Ids dos Ecos já destrancados.</summary>
        public IReadOnlyCollection<string> EcosDesbloqueados => _ecosDesbloqueados;

        /// <summary>Teto de nível, derivado do tamanho da curva.</summary>
        public int NivelMaximo => _curvaDeExposicao.Length;

        /// <summary>Se já chegou ao teto — a partir daqui a Exposição para de ser contada.</summary>
        public bool NoTeto => _nivelAtual >= _curvaDeExposicao.Length;

        /// <param name="curvaDeExposicao">
        /// Exposição acumulada necessária para cada nível, indexada por nível−1. O primeiro
        /// elemento é o nível 1 e vale 0. O tamanho do vetor <b>é</b> o teto de nível — num jogo
        /// de ~4h a curva é fechada de propósito, sem scaling infinito.
        /// </param>
        public Progressao(int[] curvaDeExposicao)
        {
            if (curvaDeExposicao == null || curvaDeExposicao.Length == 0)
                throw new ArgumentException("A curva de Exposição não pode ser vazia.",
                    nameof(curvaDeExposicao));

            _curvaDeExposicao = (int[])curvaDeExposicao.Clone();
        }

        /// <summary>
        /// Soma Exposição e sobe quantos níveis o total alcançar.
        ///
        /// <para>No teto a chamada é ignorada por completo — nem a Exposição é somada. É o
        /// comportamento que o <c>ProgressionManager</c> já tinha; preservado para não mexer em
        /// balanceamento junto com a refatoração.</para>
        /// </summary>
        public void AdicionarExposicao(int valor)
        {
            if (NoTeto) return;

            _exposicaoAtual += valor;
            OnExposicaoGanha?.Invoke();

            while (_nivelAtual < _curvaDeExposicao.Length
                   && _exposicaoAtual >= _curvaDeExposicao[_nivelAtual])
            {
                _nivelAtual++;
                _pontosDeEco++;
                OnLevelUp?.Invoke(_nivelAtual);
            }
        }

        /// <summary>Se um Eco já foi destrancado.</summary>
        public bool Possui(string idDoEco) =>
            !string.IsNullOrEmpty(idDoEco) && _ecosDesbloqueados.Contains(idDoEco);

        /// <summary>
        /// Tenta destrancar um Eco, gastando 1 ponto. O jogador só faz isso dentro de um
        /// <b>Santuário de Carcosa</b> — essa regra vive no Runtime, não aqui.
        ///
        /// <para><b>Pré-requisito é OU, não E:</b> basta ter <b>qualquer um</b> dos ids da lista.
        /// A árvore tem nós-Ponte ligando braços diferentes, e exigir todos os pré-requisitos
        /// tornaria esses nós inalcançáveis. Comportamento preservado do
        /// <c>ProgressionManager</c>.</para>
        /// </summary>
        /// <param name="idDoEco">Id do nó a destrancar.</param>
        /// <param name="idsDosPreRequisitos">
        /// Ids dos nós que liberam este. Nulo ou vazio = nó de entrada, sem pré-requisito.
        /// </param>
        /// <param name="motivo">Quando falha, por quê — para o chamador registrar ou mostrar.</param>
        /// <returns><c>true</c> se destrancou.</returns>
        public bool TryDesbloquearEco(string idDoEco, IReadOnlyList<string> idsDosPreRequisitos,
                                      out string motivo)
        {
            if (string.IsNullOrEmpty(idDoEco))
            {
                motivo = "Eco sem id — asset mal autorado.";
                return false;
            }

            if (_pontosDeEco <= 0)
            {
                motivo = "Sem Pontos de Eco disponíveis.";
                return false;
            }

            if (_ecosDesbloqueados.Contains(idDoEco))
            {
                motivo = "Eco já desbloqueado.";
                return false;
            }

            if (idsDosPreRequisitos != null && idsDosPreRequisitos.Count > 0)
            {
                bool alcancavel = false;
                for (int i = 0; i < idsDosPreRequisitos.Count; i++)
                {
                    if (_ecosDesbloqueados.Contains(idsDosPreRequisitos[i]))
                    {
                        alcancavel = true;
                        break;
                    }
                }

                if (!alcancavel)
                {
                    motivo = "Pré-requisitos não atendidos.";
                    return false;
                }
            }

            _pontosDeEco--;
            _ecosDesbloqueados.Add(idDoEco);
            OnEcoDesbloqueado?.Invoke(idDoEco);

            motivo = null;
            return true;
        }

        /// <summary>
        /// Restaura o estado vindo do save.
        ///
        /// <para><b>Não dispara eventos</b> de propósito: restaurar não é progredir. Disparar
        /// <c>OnLevelUp</c> ao carregar um save faria a UI de subida de nível piscar a cada
        /// troca de cena.</para>
        ///
        /// <para>Filtrar ids que não existem mais no catálogo é responsabilidade do chamador —
        /// aqui eles entram como estão, porque o Core não conhece o catálogo de assets.</para>
        /// </summary>
        public void Restaurar(int nivel, int exposicao, int pontos, IEnumerable<string> idsDeEcos)
        {
            _nivelAtual = Math.Max(1, nivel);
            _exposicaoAtual = Math.Max(0, exposicao);
            _pontosDeEco = Math.Max(0, pontos);

            _ecosDesbloqueados.Clear();
            if (idsDeEcos == null) return;

            foreach (var id in idsDeEcos)
            {
                if (!string.IsNullOrEmpty(id)) _ecosDesbloqueados.Add(id);
            }
        }
    }
}
