namespace FavelaAmarela.Core.Interaction
{
    /// <summary>
    /// Um candidato a alvo de interação, na forma que o Core entende: sem
    /// <c>GameObject</c>, sem <c>Transform</c>. O adaptador Runtime preenche isto a
    /// partir do que encontrou por proximidade e usa o <see cref="Id"/> para voltar ao
    /// componente concreto depois da escolha.
    /// </summary>
    public readonly struct CandidatoDeInteracao
    {
        /// <summary>Identidade opaca do candidato (o Runtime mapeia de volta ao componente).</summary>
        public readonly int Id;

        /// <summary>Distância até o jogador, em unidades de mundo.</summary>
        public readonly float Distancia;

        /// <summary>Se o alvo aceita interação agora (baú já aberto, porta travada... = false).</summary>
        public readonly bool Disponivel;

        /// <summary>
        /// Desempate explícito quando dois alvos estão praticamente à mesma distância.
        /// Maior vence. Serve para um item de história ganhar de um cenário decorativo.
        /// </summary>
        public readonly int Prioridade;

        public CandidatoDeInteracao(int id, float distancia, bool disponivel, int prioridade = 0)
        {
            Id = id;
            Distancia = distancia;
            Disponivel = disponivel;
            Prioridade = prioridade;
        }
    }

    /// <summary>
    /// Escolhe <b>qual</b> objeto o Damião interage quando aperta o botão — a regra pura,
    /// sem Unity, por trás do prompt "Pressione E".
    ///
    /// <para>Existe separado do detector físico porque a pergunta "quem está por perto?"
    /// é da Unity (colisores), mas "qual deles vale?" é regra de jogo: respeita alcance,
    /// ignora quem não pode ser usado agora, e desempata por prioridade e depois por
    /// proximidade. Determinístico — dois candidatos idênticos resolvem pelo menor
    /// <see cref="CandidatoDeInteracao.Id"/>, nunca "o que o Physics devolveu primeiro".</para>
    /// </summary>
    public sealed class SeletorDeInteracao
    {
        /// <summary>Distância máxima para um alvo ser considerado alcançável.</summary>
        public float Alcance { get; }

        /// <param name="alcance">Alcance de interação em unidades de mundo. Deve ser &gt; 0.</param>
        public SeletorDeInteracao(float alcance = 1.5f)
        {
            Alcance = alcance > 0f ? alcance : 1.5f;
        }

        /// <summary>
        /// Devolve o <see cref="CandidatoDeInteracao.Id"/> do melhor alvo, ou <c>null</c>
        /// se nenhum serve.
        ///
        /// <para>Recebe array + contagem (em vez de uma coleção) porque o detector chama
        /// isto a cada frame com um buffer pré-alocado — Regra de Ouro 1, zero lixo.</para>
        /// </summary>
        /// <param name="candidatos">Buffer de candidatos.</param>
        /// <param name="quantidade">Quantas posições do buffer são válidas.</param>
        public int? Selecionar(CandidatoDeInteracao[] candidatos, int quantidade)
        {
            if (candidatos == null || quantidade <= 0) return null;
            if (quantidade > candidatos.Length) quantidade = candidatos.Length;

            bool achou = false;
            int melhorId = 0;
            float melhorDistancia = 0f;
            int melhorPrioridade = 0;

            for (int i = 0; i < quantidade; i++)
            {
                var c = candidatos[i];

                if (!c.Disponivel) continue;
                if (c.Distancia > Alcance) continue;
                if (c.Distancia < 0f) continue;

                if (!achou || EhMelhor(c, melhorPrioridade, melhorDistancia, melhorId))
                {
                    achou = true;
                    melhorId = c.Id;
                    melhorDistancia = c.Distancia;
                    melhorPrioridade = c.Prioridade;
                }
            }

            return achou ? melhorId : (int?)null;
        }

        /// <summary>
        /// Ordem de decisão: prioridade maior vence; empatou, o mais perto vence;
        /// empatou de novo, o menor Id vence (garante resultado estável entre frames).
        /// </summary>
        private static bool EhMelhor(CandidatoDeInteracao c, int melhorPrioridade, float melhorDistancia, int melhorId)
        {
            if (c.Prioridade != melhorPrioridade) return c.Prioridade > melhorPrioridade;

            const float epsilon = 1e-4f;
            float diferenca = melhorDistancia - c.Distancia;
            if (diferenca > epsilon) return true;
            if (diferenca < -epsilon) return false;

            return c.Id < melhorId;
        }
    }
}
