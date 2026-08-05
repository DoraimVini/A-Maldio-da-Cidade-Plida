using System;
using System.Collections.Generic;

namespace FavelaAmarela.Progression
{
    [Serializable]
    public class ProgressionSaveData
    {
        public int nivelAtual;
        public int exposicaoAtual;
        public int pontosDeEcoDisponiveis;
        public List<string> ecosDesbloqueadosIds;

        public ProgressionSaveData() 
        {
            ecosDesbloqueadosIds = new List<string>();
        }

        public ProgressionSaveData(int nivel, int exposicao, int pontos, List<EcoDef> ecos)
        {
            nivelAtual = nivel;
            exposicaoAtual = exposicao;
            pontosDeEcoDisponiveis = pontos;
            
            ecosDesbloqueadosIds = new List<string>();
            if (ecos != null)
            {
                foreach (var eco in ecos)
                {
                    if (eco != null && !string.IsNullOrEmpty(eco.Id))
                    {
                        ecosDesbloqueadosIds.Add(eco.Id);
                    }
                }
            }
        }
    }
}
