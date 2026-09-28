using System;

namespace FavelaAmarela.Core.Enemies
{
    /// <summary>As fases do Rito do Olhar, na ordem em que acontecem.</summary>
    public enum FaseDoRito
    {
        /// <summary>O Damião ainda não entrou no Trono. Nada corre.</summary>
        Aguardando,
        /// <summary>Fase 1 — só o Rei, o Altar e os Nobres. Ensina: ser visto custa.</summary>
        Chegada,
        /// <summary>Fase 2 — o olhar vira farol; a Máscara abre de tempos em tempos.</summary>
        Mascara,
        /// <summary>Fase 3 — fragmentos da Peça ardem nos altares de relíquia.</summary>
        Peca,
        /// <summary>Fase 4 — o Rei fala; cada pulso desfaz um Nobre.</summary>
        Verbo,
        /// <summary>Fase 5 — o Rei se retira; o selo fecha sozinho; os Ecos punem quem para.</summary>
        Queda,
        /// <summary>Vitória: o selo fechou.</summary>
        Selado,
        /// <summary>Derrota: a mente do Damião se desfez.</summary>
        Colapso,
    }

    /// <summary>
    /// O que o adaptador mede do mundo a cada quadro e entrega ao rito.
    ///
    /// <para>É um <c>readonly struct</c> porque viaja todo quadro (Regra de Ouro 1).</para>
    /// </summary>
    public readonly struct LeituraDoRito
    {
        /// <summary>Uma linha do Rei até o Damião não encontra cobertura.</summary>
        public readonly bool LinhaLivre;

        /// <summary>
        /// Ângulo, em graus, entre a frente do Rei e a direção ao Damião (com sinal). O rito
        /// decide com ele se o Damião está dentro do farol da Fase 2.
        /// </summary>
        public readonly float AnguloDoJogador;

        /// <summary>A base do Damião está dentro do Altar de Selamento.</summary>
        public readonly bool NoAltar;

        /// <summary>Índice do altar de relíquia sob os pés do Damião, ou −1.</summary>
        public readonly int AltarSobOPe;

        /// <summary>Monta uma leitura.</summary>
        public LeituraDoRito(bool linhaLivre, float anguloDoJogador, bool noAltar, int altarSobOPe = -1)
        {
            LinhaLivre = linhaLivre;
            AnguloDoJogador = anguloDoJogador;
            NoAltar = noAltar;
            AltarSobOPe = altarSobOPe;
        }
    }

    /// <summary>
    /// Os efeitos das relíquias equipadas sobre o rito. As relíquias deixaram de ser chave e
    /// viraram ferramentas: cada uma facilita um lado do laço.
    /// </summary>
    public readonly struct ModificadoresDoRito
    {
        /// <summary>Multiplicador do avanço do selo (Necronomicon: as palavras do selo).</summary>
        public readonly float Selo;

        /// <summary>Multiplicador da Ancoragem na sombra (Patuá: ancora no escuro).</summary>
        public readonly float Ancoragem;

        /// <summary>Multiplicador do dreno do olhar (Anel do Sinal Amarelo: filtra o olhar).</summary>
        public readonly float Dreno;

        /// <summary>Monta os modificadores.</summary>
        public ModificadoresDoRito(float selo, float ancoragem, float dreno)
        {
            Selo = selo;
            Ancoragem = ancoragem;
            Dreno = dreno;
        }

        /// <summary>Sem relíquia nenhuma: tudo × 1.</summary>
        public static ModificadoresDoRito Nenhum => new ModificadoresDoRito(1f, 1f, 1f);

        /// <summary>Os modificadores a partir de quais relíquias estão equipadas.</summary>
        public static ModificadoresDoRito DasReliquias(bool necronomicon, bool patua, bool anel,
                                                       ParametrosDoRito p)
            => new ModificadoresDoRito(
                necronomicon ? p.BonusDoNecronomicon : 1f,
                patua ? p.BonusDoPatua : 1f,
                anel ? p.BonusDoAnel : 1f);
    }

    /// <summary>
    /// Os números do rito. Todos calibráveis sem recompilar: o adaptador os preenche a partir
    /// de campos serializados do Rei.
    ///
    /// <para><b>Calibrados por simulação em 2026-09-28</b> (<c>ORitoCabeNoTempoTests</c>), não
    /// copiados do plano: com os números do plano o jogador disciplinado <b>colapsava na Fase 4</b>
    /// — o Verbo desfazia todas as coberturas antes de o selo fechar. Mudou: marcos 20/40/65/90,
    /// dreno do Verbo 8, pulso a cada 8 s custando 8, e o último Nobre resiste. Resultado: ~3 min
    /// com as três relíquias; ~4 min 20 s sem o Patuá; ~7 min 20 s sem nenhuma (vencível).</para>
    /// </summary>
    public sealed class ParametrosDoRito
    {
        /// <summary>Selo por segundo, exposto no Altar, sem relíquias (em pontos de 0 a 100).</summary>
        public float SeloPorSegundo { get; set; } = 1.0f;

        /// <summary>Resiliência recuperada por segundo na sombra, sem relíquias.</summary>
        public float AncoragemPorSegundo { get; set; } = 3f;

        /// <summary>Dreno por segundo, exposto, na Fase 1.</summary>
        public float DrenoChegada { get; set; } = 6f;

        /// <summary>Dreno por segundo, exposto, na Fase 2 (com a Máscara fechada).</summary>
        public float DrenoMascara { get; set; } = 7f;

        /// <summary>Dreno por segundo, exposto, na Fase 3.</summary>
        public float DrenoPeca { get; set; } = 8f;

        /// <summary>Dreno por segundo, exposto, na Fase 4.</summary>
        public float DrenoVerbo { get; set; } = 8f;

        /// <summary>Selo em que a Fase 2 começa.</summary>
        public float MarcoMascara { get; set; } = 20f;

        /// <summary>Selo em que a Fase 3 começa.</summary>
        public float MarcoPeca { get; set; } = 40f;

        /// <summary>Selo em que a Fase 4 começa.</summary>
        public float MarcoVerbo { get; set; } = 65f;

        /// <summary>Selo em que a Fase 5 começa.</summary>
        public float MarcoQueda { get; set; } = 90f;

        /// <summary>Segundos que a Fase 5 leva para fechar o selo sozinha.</summary>
        public float DuracaoDaQueda { get; set; } = 30f;

        /// <summary>Multiplicador de selo do Necronomicon.</summary>
        public float BonusDoNecronomicon { get; set; } = 1.3f;

        /// <summary>Multiplicador de Ancoragem do Patuá.</summary>
        public float BonusDoPatua { get; set; } = 2f;

        /// <summary>Multiplicador de dreno do Anel.</summary>
        public float BonusDoAnel { get; set; } = 0.8f;

        /// <summary>Configuração do farol da Fase 2.</summary>
        public ParametrosDoOlhar Olhar { get; set; } = new ParametrosDoOlhar();

        /// <summary>Configuração dos fragmentos da Fase 3.</summary>
        public ParametrosDosFragmentos Fragmentos { get; set; } = new ParametrosDosFragmentos();

        /// <summary>Configuração dos pulsos da Fase 4.</summary>
        public ParametrosDoVerbo Verbo { get; set; } = new ParametrosDoVerbo();

        /// <summary>O dreno base da fase, ou zero fora das fases de olhar.</summary>
        public float DrenoDa(FaseDoRito fase) => fase switch
        {
            FaseDoRito.Chegada => DrenoChegada,
            FaseDoRito.Mascara => DrenoMascara,
            FaseDoRito.Peca => DrenoPeca,
            FaseDoRito.Verbo => DrenoVerbo,
            _ => 0f,
        };
    }

    /// <summary>O que um quadro do rito devolve para o adaptador aplicar no mundo.</summary>
    public readonly struct ResultadoDoRito
    {
        /// <summary>Variação da Resiliência Mental neste quadro (negativa drena, positiva ancora).</summary>
        public readonly float DeltaResiliencia;

        /// <summary>Se o Damião foi visto neste quadro — para o fio do olhar e a vinheta.</summary>
        public readonly bool Exposto;

        /// <summary>Monta o resultado.</summary>
        public ResultadoDoRito(float deltaResiliencia, bool exposto)
        {
            DeltaResiliencia = deltaResiliencia;
            Exposto = exposto;
        }
    }

    /// <summary>
    /// A regra do laço em si, sem estado: dado o que o mundo mostra, quanto o selo avança e
    /// quanto a mente muda.
    ///
    /// <table>
    /// <tr><td>Exposto e no Altar</td><td>selo avança</td><td>mente drena</td></tr>
    /// <tr><td>Exposto fora do Altar</td><td>—</td><td>mente drena</td></tr>
    /// <tr><td>Coberto</td><td>—</td><td>mente se ancora</td></tr>
    /// </table>
    ///
    /// <para>Não existe estado seguro que avance o selo: para vencer é preciso ser visto.</para>
    /// </summary>
    public static class ExposicaoAoRei
    {
        /// <summary>Multiplicador do dreno com a Máscara aberta.</summary>
        public const float DrenoComAMascaraAberta = 3f;

        /// <summary>Quanto o selo avança e a mente muda em <paramref name="dt"/> segundos.</summary>
        public static (float deltaSelo, float deltaResiliencia) Calcular(
            FaseDoRito fase, bool exposto, bool noAltar, bool mascaraAberta,
            ParametrosDoRito p, ModificadoresDoRito m, float dt)
        {
            if (dt <= 0f) return (0f, 0f);

            if (fase == FaseDoRito.Queda)
                return ((p.MarcoQueda < 100f ? (100f - p.MarcoQueda) : 0f) / Math.Max(0.01f, p.DuracaoDaQueda) * dt, 0f);

            float dreno = p.DrenoDa(fase);
            if (dreno <= 0f) return (0f, 0f);

            if (!exposto)
                return (0f, p.AncoragemPorSegundo * m.Ancoragem * dt);

            float multMascara = mascaraAberta ? DrenoComAMascaraAberta : 1f;
            float deltaRm = -dreno * multMascara * m.Dreno * dt;
            float deltaSelo = noAltar ? p.SeloPorSegundo * m.Selo * dt : 0f;
            return (deltaSelo, deltaRm);
        }
    }
}
