using System;

namespace FavelaAmarela.Core.Enemies
{
    /// <summary>
    /// POCO puro: o <b>Rito do Olhar</b> — o confronto final contra o Rei em Amarelo, redesenhado
    /// em 2026-09-28. Plano completo em <c>Docs/KnowledgeBundle/systems/dossie_luta_do_rei.md</c>.
    ///
    /// <para><b>O Rei não ataca, não tem vida e não morre: ele olha.</b> Ser visto drena a mente
    /// do Damião; estar exposto dentro do Altar de Selamento avança o selo; atrás de um Nobre
    /// Fossilizado ele fica fora da vista e a mente se ancora. O selo vai de 0 a 100, e cada
    /// marco abre uma fase que <b>muda a sala</b>, não o controle:</para>
    ///
    /// <list type="number">
    /// <item><b>Chegada</b> (0 → 15): só o olhar.</item>
    /// <item><b>Máscara</b> (15 → 35): o olhar vira farol (<see cref="OlharDoRei"/>).</item>
    /// <item><b>Peça</b> (35 → 60): fragmentos nos altares (<see cref="FragmentosDaPeca"/>).</item>
    /// <item><b>Verbo</b> (60 → 90): cada pulso desfaz uma cobertura (<see cref="VerboDoRei"/>).</item>
    /// <item><b>Queda</b> (90 → 100): o Rei se retira; o selo fecha sozinho. Os Ecos de Carcosa
    /// da cena punem quem para — isso é regra deles, não deste POCO.</item>
    /// </list>
    ///
    /// <para><b>Por que substituiu a <c>ReiEmAmareloFSM</c>.</b> A luta anterior — escudos que se
    /// revezavam e um Confronto em que o Rei sangrava — foi reprovada no playtest de 2026-09-10:
    /// <i>"Eu detestei a luta contra o Rei, está muito repetitiva."</i> Repetia a mesma pergunta
    /// de 5 a 8 vezes. Aqui cada fase pergunta outra coisa.</para>
    ///
    /// <para><b>Nada depende de para onde o Damião olha.</b> <c>PlayerMovement.LookDirection</c>
    /// só atualiza enquanto ele anda, e foi isso que matou a mecânica original de dar as costas.
    /// A exposição é linha de visão do Rei até ele — posição, não direção.</para>
    ///
    /// <para><b>A mente não mora aqui.</b> O rito devolve a variação da Resiliência a cada
    /// quadro (<see cref="ResultadoDoRito"/>) e o adaptador a aplica na
    /// <c>ResilienciaMental</c> de sempre. Quando ela zera, o adaptador chama
    /// <see cref="Colapsar"/>.</para>
    /// </summary>
    public sealed class RitoDoReiFSM
    {
        private readonly ParametrosDoRito _p;
        private readonly ModificadoresDoRito _m;
        private readonly OlharDoRei _olhar;
        private readonly FragmentosDaPeca _fragmentos;
        private readonly VerboDoRei _verbo;

        private float _tempoNaFase;

        /// <summary>A fase atual.</summary>
        public FaseDoRito Fase { get; private set; } = FaseDoRito.Aguardando;

        /// <summary>O selo, de 0 a 100.</summary>
        public float Selo { get; private set; }

        /// <summary>Segundos na fase atual.</summary>
        public float TempoNaFase => _tempoNaFase;

        /// <summary>Os modificadores das relíquias com que o rito foi montado.</summary>
        public ModificadoresDoRito Modificadores => _m;

        /// <summary>O farol (só varre na Fase 2; nas outras o olhar vê a sala toda).</summary>
        public OlharDoRei Olhar => _olhar;

        /// <summary>Os fragmentos da Fase 3.</summary>
        public FragmentosDaPeca Fragmentos => _fragmentos;

        /// <summary>Os pulsos da Fase 4.</summary>
        public VerboDoRei Verbo => _verbo;

        /// <summary>Se o olhar está em curso (fases 1 a 4).</summary>
        public bool OlharAtivo =>
            Fase == FaseDoRito.Chegada || Fase == FaseDoRito.Mascara
            || Fase == FaseDoRito.Peca || Fase == FaseDoRito.Verbo;

        /// <summary>A Máscara está aberta agora (só na Fase 2).</summary>
        public bool MascaraAberta => Fase == FaseDoRito.Mascara && _olhar.MascaraAberta;

        /// <summary>Terminou, de um jeito ou de outro.</summary>
        public bool Encerrado => Fase == FaseDoRito.Selado || Fase == FaseDoRito.Colapso;

        /// <summary>A fase mudou. (anterior, nova)</summary>
        public event Action<FaseDoRito, FaseDoRito> OnFaseMudou;

        /// <summary>O selo fechou — vitória.</summary>
        public event Action OnSelado;

        /// <summary>A mente se desfez — derrota.</summary>
        public event Action OnColapso;

        /// <summary>Cria o rito.</summary>
        /// <param name="parametros">Os números; <c>null</c> usa os padrões do plano.</param>
        /// <param name="modificadores">O efeito das relíquias equipadas.</param>
        /// <param name="coberturas">Quantas coberturas a sala tem (para o Verbo desfazer).</param>
        /// <param name="altaresDeFragmento">Quantos altares recebem fragmentos na Fase 3.</param>
        public RitoDoReiFSM(ParametrosDoRito parametros, ModificadoresDoRito modificadores,
                            int coberturas, int altaresDeFragmento)
        {
            _p = parametros ?? new ParametrosDoRito();
            _m = modificadores;
            _olhar = new OlharDoRei(_p.Olhar);
            _fragmentos = new FragmentosDaPeca(_p.Fragmentos, altaresDeFragmento);
            _verbo = new VerboDoRei(_p.Verbo, coberturas);
        }

        /// <summary>O Damião entrou no Trono: começa a Chegada. Idempotente.</summary>
        public void Iniciar()
        {
            if (Fase != FaseDoRito.Aguardando) return;
            Transicionar(FaseDoRito.Chegada);
        }

        /// <summary>A mente do Damião se desfez. Só tem efeito com o rito em curso.</summary>
        public void Colapsar()
        {
            if (Fase == FaseDoRito.Aguardando || Encerrado) return;
            Transicionar(FaseDoRito.Colapso);
            OnColapso?.Invoke();
        }

        /// <summary>Se o Rei vê o Damião, dada a leitura do mundo.</summary>
        public bool Ve(LeituraDoRito leitura)
        {
            if (!OlharAtivo || !leitura.LinhaLivre) return false;
            return Fase != FaseDoRito.Mascara || _olhar.Ve(leitura.AnguloDoJogador);
        }

        /// <summary>Avança o rito um quadro.</summary>
        public ResultadoDoRito Tick(float dt, LeituraDoRito leitura)
        {
            if (dt <= 0f || Fase == FaseDoRito.Aguardando || Encerrado)
                return new ResultadoDoRito(0f, false);

            _tempoNaFase += dt;
            if (Fase == FaseDoRito.Mascara) _olhar.Avancar(dt);

            bool exposto = Ve(leitura);
            var (deltaSelo, deltaRm) = ExposicaoAoRei.Calcular(
                Fase, exposto, leitura.NoAltar, MascaraAberta, _p, _m, dt);

            if (Fase == FaseDoRito.Peca)
            {
                var (seloFragmento, rmFragmento) = _fragmentos.Avancar(dt, leitura.AltarSobOPe, exposto);
                deltaSelo += seloFragmento;
                deltaRm += rmFragmento;
            }

            if (Fase == FaseDoRito.Verbo)
                deltaRm += _verbo.Avancar(dt, exposto);

            Selo = Math.Min(100f, Selo + deltaSelo);
            AvaliarMarcos();

            return new ResultadoDoRito(deltaRm, exposto);
        }

        private void AvaliarMarcos()
        {
            // Um laço, e não um "if": um fragmento pode pular um marco inteiro, e a fase tem de
            // acompanhar o selo, não ficar um quadro atrás de cada vez.
            for (int i = 0; i < 6; i++)
            {
                var proxima = ProximaFase();
                if (proxima == Fase) return;
                Transicionar(proxima);
                if (proxima == FaseDoRito.Selado)
                {
                    OnSelado?.Invoke();
                    return;
                }
            }
        }

        private FaseDoRito ProximaFase() => Fase switch
        {
            FaseDoRito.Chegada when Selo >= _p.MarcoMascara => FaseDoRito.Mascara,
            FaseDoRito.Mascara when Selo >= _p.MarcoPeca => FaseDoRito.Peca,
            FaseDoRito.Peca when Selo >= _p.MarcoVerbo => FaseDoRito.Verbo,
            FaseDoRito.Verbo when Selo >= _p.MarcoQueda => FaseDoRito.Queda,
            FaseDoRito.Queda when Selo >= 100f => FaseDoRito.Selado,
            _ => Fase,
        };

        private void Transicionar(FaseDoRito nova)
        {
            if (nova == Fase) return;

            var anterior = Fase;
            if (anterior == FaseDoRito.Peca) _fragmentos.Encerrar();

            Fase = nova;
            _tempoNaFase = 0f;

            if (nova == FaseDoRito.Peca) _fragmentos.Comecar();

            OnFaseMudou?.Invoke(anterior, nova);
        }
    }
}
