using System;

namespace FavelaAmarela.Core.Enemies
{
    /// <summary>Configuração dos pulsos da Fase 4 do Rito do Olhar.</summary>
    public sealed class ParametrosDoVerbo
    {
        /// <summary>Segundos entre pulsos.</summary>
        public float Intervalo { get; set; } = 8f;

        /// <summary>Resiliência perdida por quem está exposto no instante do pulso.</summary>
        public float Custo { get; set; } = 8f;

        /// <summary>
        /// Quantas coberturas o Verbo <b>não</b> desfaz. Com zero, o jogador lento ficava sem
        /// sombra nenhuma no meio da fase e colapsava sem saída (medido na simulação): a fase tem
        /// de apertar, não trancar. Com uma, o último Nobre resiste — longe, mas existe.
        /// </summary>
        public int CoberturasQueResistem { get; set; } = 1;
    }

    /// <summary>
    /// O Verbo, na Fase 4: o Rei fala, e a cada pulso <b>uma cobertura se desfaz</b>, na ordem
    /// que o adaptador definiu (do Nobre mais próximo do Altar para o mais distante). Quem está
    /// exposto no instante do pulso também perde Resiliência de uma vez.
    ///
    /// <para>A pergunta da fase: <i>chego ao fim antes de ficar sem onde me esconder?</i></para>
    /// </summary>
    public sealed class VerboDoRei
    {
        private readonly ParametrosDoVerbo _p;
        private readonly int _coberturas;
        private float _relogio;

        /// <summary>Quantas coberturas já se desfizeram.</summary>
        public int Desfeitas { get; private set; }

        /// <summary>Um pulso aconteceu; o argumento é a cobertura desfeita, ou −1 se não restava nenhuma.</summary>
        public event Action<int> OnPulso;

        /// <summary>Cria o Verbo para <paramref name="coberturas"/> coberturas.</summary>
        public VerboDoRei(ParametrosDoVerbo parametros, int coberturas)
        {
            _p = parametros ?? new ParametrosDoVerbo();
            _coberturas = Math.Max(0, coberturas);
        }

        /// <summary>Avança; devolve a Resiliência perdida de uma vez neste quadro (≤ 0).</summary>
        public float Avancar(float dt, bool exposto)
        {
            if (dt <= 0f || _p.Intervalo <= 0f) return 0f;

            _relogio += dt;
            float perda = 0f;

            while (_relogio >= _p.Intervalo)
            {
                _relogio -= _p.Intervalo;

                int desfeita = Desfeitas < _coberturas - Math.Max(0, _p.CoberturasQueResistem) ? Desfeitas : -1;
                if (desfeita >= 0) Desfeitas++;

                if (exposto) perda -= _p.Custo;
                OnPulso?.Invoke(desfeita);
            }

            return perda;
        }
    }
}
