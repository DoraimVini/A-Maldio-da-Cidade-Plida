using System;

namespace FavelaAmarela.Core.Enemies
{
    /// <summary>Configuração dos fragmentos da Fase 3 do Rito do Olhar.</summary>
    public sealed class ParametrosDosFragmentos
    {
        /// <summary>Segundos exposto em cima do fragmento para lê-lo.</summary>
        public float TempoDeLeitura { get; set; } = 1.5f;

        /// <summary>Selo ganho de uma vez ao ler um fragmento.</summary>
        public float Selo { get; set; } = 8f;

        /// <summary>Resiliência perdida de uma vez ao ler um fragmento.</summary>
        public float Custo { get; set; } = 15f;

        /// <summary>Segundos até o próximo fragmento aparecer.</summary>
        public float Reaparecimento { get; set; } = 10f;
    }

    /// <summary>
    /// Os fragmentos da Peça, na Fase 3: um de cada vez, num dos altares de relíquia — sempre em
    /// campo aberto. Ler um é um atalho no selo que custa mente de uma vez.
    ///
    /// <para>O próximo fragmento aparece no altar seguinte, nunca no mesmo seguido: o jogador
    /// precisa atravessar a sala exposto para buscá-lo, e essa travessia é a decisão.</para>
    /// </summary>
    public sealed class FragmentosDaPeca
    {
        private readonly ParametrosDosFragmentos _p;
        private readonly int _altares;
        private float _leitura;
        private float _espera;
        private int _ultimo = -1;

        /// <summary>Altar com fragmento agora, ou −1.</summary>
        public int Ativo { get; private set; } = -1;

        /// <summary>Progresso da leitura do fragmento ativo, de 0 a 1.</summary>
        public float ProgressoDaLeitura => _p.TempoDeLeitura > 0f ? _leitura / _p.TempoDeLeitura : 0f;

        /// <summary>Um fragmento apareceu neste altar.</summary>
        public event Action<int> OnApareceu;

        /// <summary>O fragmento deste altar foi lido.</summary>
        public event Action<int> OnLido;

        /// <summary>Cria os fragmentos para <paramref name="altares"/> altares.</summary>
        public FragmentosDaPeca(ParametrosDosFragmentos parametros, int altares)
        {
            _p = parametros ?? new ParametrosDosFragmentos();
            _altares = Math.Max(0, altares);
        }

        /// <summary>Faz o primeiro fragmento aparecer (no altar 0). Sem altares, não faz nada.</summary>
        public void Comecar()
        {
            if (_altares == 0) return;
            Aparecer(0);
        }

        /// <summary>
        /// Avança. Devolve o selo e a Resiliência ganhos ou perdidos de uma vez neste quadro —
        /// zero, a não ser no quadro em que um fragmento é lido.
        /// </summary>
        /// <param name="dt">Segundos.</param>
        /// <param name="altarSobOPe">Altar sob os pés do Damião, ou −1.</param>
        /// <param name="exposto">Se ele está sendo visto — ler exige estar exposto.</param>
        public (float selo, float resiliencia) Avancar(float dt, int altarSobOPe, bool exposto)
        {
            if (dt <= 0f || _altares == 0) return (0f, 0f);

            if (Ativo < 0)
            {
                _espera -= dt;
                if (_espera <= 0f) Aparecer((_ultimo + 1) % _altares);
                return (0f, 0f);
            }

            if (altarSobOPe != Ativo || !exposto)
            {
                _leitura = 0f;
                return (0f, 0f);
            }

            _leitura += dt;
            if (_leitura < _p.TempoDeLeitura) return (0f, 0f);

            int lido = Ativo;
            _ultimo = lido;
            Ativo = -1;
            _leitura = 0f;
            _espera = _p.Reaparecimento;
            OnLido?.Invoke(lido);
            return (_p.Selo, -_p.Custo);
        }

        /// <summary>Some com o fragmento ativo (fim da fase).</summary>
        public void Encerrar()
        {
            Ativo = -1;
            _leitura = 0f;
            _espera = float.MaxValue;
        }

        private void Aparecer(int altar)
        {
            Ativo = altar;
            _leitura = 0f;
            OnApareceu?.Invoke(altar);
        }
    }
}
