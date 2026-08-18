using System;
using System.Collections.Generic;

namespace FavelaAmarela.Progression
{
    /// <summary>
    /// Estado serializável da progressão. Guarda <b>ids</b> de Eco, não referências de asset —
    /// é o que permite o JSON sobreviver a mudanças no catálogo, e é a mesma forma que o POCO
    /// <c>Core.Progression.Progressao</c> usa internamente.
    /// </summary>
    [Serializable]
    public class ProgressionSaveData
    {
        public int nivelAtual;
        public int exposicaoAtual;
        public int pontosDeEcoDisponiveis;
        public List<string> ecosDesbloqueadosIds;

        public ProgressionSaveData()
        {
            nivelAtual = 1;
            ecosDesbloqueadosIds = new List<string>();
        }

        /// <summary>
        /// Construtor usado pelo <c>ProgressionBridge</c> — recebe os ids já extraídos do POCO.
        /// </summary>
        public ProgressionSaveData(int nivel, int exposicao, int pontos, List<string> idsDeEcos)
        {
            nivelAtual = nivel;
            exposicaoAtual = exposicao;
            pontosDeEcoDisponiveis = pontos;
            ecosDesbloqueadosIds = idsDeEcos ?? new List<string>();
        }
    }
}
