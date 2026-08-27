namespace FavelaAmarela.Core.Abilities
{
    /// <summary>
    /// Acumulador mutável que os <see cref="IEfeitoDeHabilidade"/> preenchem antes de virar um
    /// <see cref="ArmaResult"/> imutável.
    ///
    /// <para><b>Por que existe.</b> <c>ArmaResult</c> é um <c>readonly struct</c> com
    /// <b>doze</b> parâmetros de construtor. Compor efeitos criando um struct novo a cada passo
    /// exigiria repetir os doze argumentos posicionais em cada efeito — e o XML doc de
    /// <c>ArmaResult.ComBonus</c> registra que reconstruir esse struct à mão em dois lugares já
    /// tinha sido fonte de bug antes.</para>
    ///
    /// <para>Cada efeito escreve só o campo que lhe diz respeito; o resto continua no padrão.</para>
    /// </summary>
    public sealed class ConstrutorDeGolpe
    {
        /// <summary>Quanto tempo o ator fica preso na animação do golpe.</summary>
        public float Duracao;

        /// <summary>Recarga até o próximo uso.</summary>
        public float Cooldown;

        /// <summary>Dano físico direto.</summary>
        public float Dano;

        /// <summary>Dano de anomalia (estática cósmica).</summary>
        public float TraumaAnomalia;

        /// <summary>Se o golpe atordoa, e por quanto tempo.</summary>
        public bool Atordoou;

        /// <inheritdoc cref="Atordoou"/>
        public float DuracaoAtordoamento;

        /// <summary>Se o golpe corta a conjuração de quem estiver conjurando.</summary>
        public bool InterrompeConjuracao;

        /// <summary>Sangramento aberto pelo golpe.</summary>
        public int AcumulosDeSangramento;

        /// <inheritdoc cref="AcumulosDeSangramento"/>
        public float SangramentoPorSegundo;

        /// <inheritdoc cref="AcumulosDeSangramento"/>
        public float DuracaoSangramento;

        /// <summary>Empurrão aplicado ao corpo atingido, modulado por <c>CorpoImpregnado</c>.</summary>
        public float ForcaRepulsao;

        /// <summary>Cria o acumulador já com o tempo do golpe, que não vem de efeito nenhum.</summary>
        public ConstrutorDeGolpe(float duracao, float cooldown)
        {
            Duracao = duracao;
            Cooldown = cooldown;
        }

        /// <summary>Fecha o golpe no struct imutável que o resto do jogo consome.</summary>
        public ArmaResult Construir() => new ArmaResult(
            success: true,
            durationSeconds: Duracao,
            cooldownSeconds: Cooldown,
            atordoou: Atordoou,
            duracaoAtordoamento: DuracaoAtordoamento,
            dano: Dano,
            interrompeConjuracao: InterrompeConjuracao,
            sangramentoPorSegundo: SangramentoPorSegundo,
            duracaoSangramento: DuracaoSangramento,
            forcaRepulsao: ForcaRepulsao,
            acumulosDeSangramento: AcumulosDeSangramento,
            traumaAnomalia: TraumaAnomalia);
    }
}
