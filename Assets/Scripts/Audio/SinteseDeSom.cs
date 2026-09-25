using UnityEngine;

namespace FavelaAmarela.Runtime.Audio
{
    /// <summary>
    /// Gera clipes curtos por síntese, para o jogo ter <b>retorno sonoro antes de existir arte
    /// de áudio</b>. Não é substituto de som autorado — é andaime: assim que um clipe real
    /// entrar no <see cref="BancoDeSons"/>, ele ganha a preferência automaticamente.
    ///
    /// <para>Cada clipe é criado <b>uma vez</b> e reaproveitado (cache estático), então nada
    /// disto aloca em hot path.</para>
    /// </summary>
    public static class SinteseDeSom
    {
        private const int TaxaDeAmostragem = 44100;

        private static readonly System.Collections.Generic.Dictionary<SomDoJogo, AudioClip> Cache
            = new System.Collections.Generic.Dictionary<SomDoJogo, AudioClip>();

        /// <summary>Clipe sintetizado para este som, criado sob demanda e cacheado.</summary>
        public static AudioClip Obter(SomDoJogo som)
        {
            if (Cache.TryGetValue(som, out var pronto) && pronto != null) return pronto;

            var clipe = Sintetizar(som);
            Cache[som] = clipe;
            return clipe;
        }

        private static AudioClip Sintetizar(SomDoJogo som)
        {
            switch (som)
            {
                // Passo: ruído curto e abafado — some rápido, não compete com o ambiente.
                case SomDoJogo.PassoDeDamiao:
                    return Ruido("syn_passo", 0.09f, 0.25f, corteGrave: 0.35f);

                // Golpe: ruído mais seco e curto.
                case SomDoJogo.GolpeDesferido:
                    return Ruido("syn_golpe", 0.12f, 0.5f, corteGrave: 0.5f);

                case SomDoJogo.HabilidadeDeArma:
                    return Tom("syn_habilidade", 0.22f, 320f, 180f, 0.4f);

                case SomDoJogo.EntidadeFerida:
                    return Ruido("syn_ferida", 0.14f, 0.45f, corteGrave: 0.7f);

                case SomDoJogo.EntidadeAbatida:
                    return Tom("syn_abate", 0.45f, 220f, 70f, 0.45f);

                // Pânico: tom que sobe — a mente apertando.
                case SomDoJogo.EntrouEmPanico:
                    return Tom("syn_panico", 0.6f, 180f, 420f, 0.35f);

                // Colapso: tom que desaba.
                case SomDoJogo.Colapso:
                    return Tom("syn_colapso", 1.2f, 300f, 40f, 0.5f);

                case SomDoJogo.ItemRecolhido:
                    return Tom("syn_item", 0.18f, 520f, 780f, 0.3f);

                case SomDoJogo.ArtefatoInvocado:
                    return Tom("syn_artefato", 0.7f, 140f, 260f, 0.4f);

                default:
                    return Ruido("syn_generico", 0.1f, 0.3f, corteGrave: 0.5f);
            }
        }

        /// <summary>Ruído branco com decaimento exponencial e um filtro grave simples.</summary>
        private static AudioClip Ruido(string nome, float duracao, float amplitude, float corteGrave)
        {
            int amostras = Mathf.Max(1, Mathf.RoundToInt(duracao * TaxaDeAmostragem));
            var dados = new float[amostras];

            var rng = new System.Random(nome.GetHashCode());
            float anterior = 0f;

            for (int i = 0; i < amostras; i++)
            {
                float bruto = (float)(rng.NextDouble() * 2.0 - 1.0);

                // Passa-baixa de 1 polo: abafa o chiado e deixa o ruído mais "corpóreo".
                anterior += (bruto - anterior) * corteGrave;

                float envelope = Mathf.Exp(-6f * (i / (float)amostras));
                dados[i] = anterior * envelope * amplitude;
            }

            return Montar(nome, dados);
        }

        /// <summary>Seno com varredura de frequência e decaimento.</summary>
        private static AudioClip Tom(string nome, float duracao, float hzInicial, float hzFinal, float amplitude)
        {
            int amostras = Mathf.Max(1, Mathf.RoundToInt(duracao * TaxaDeAmostragem));
            var dados = new float[amostras];

            float fase = 0f;

            for (int i = 0; i < amostras; i++)
            {
                float t = i / (float)amostras;
                float hz = Mathf.Lerp(hzInicial, hzFinal, t);

                fase += 2f * Mathf.PI * hz / TaxaDeAmostragem;

                float envelope = Mathf.Exp(-4f * t);
                dados[i] = Mathf.Sin(fase) * envelope * amplitude;
            }

            return Montar(nome, dados);
        }

        private static AudioClip Montar(string nome, float[] dados)
        {
            var clipe = AudioClip.Create(nome, dados.Length, 1, TaxaDeAmostragem, false);
            clipe.SetData(dados, 0);
            return clipe;
        }
    }
}
