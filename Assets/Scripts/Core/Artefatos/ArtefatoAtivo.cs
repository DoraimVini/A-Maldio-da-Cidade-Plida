using System.Collections.Generic;

namespace FavelaAmarela.Core.Artefatos
{
    /// <summary>
    /// A habilidade ativa de um Artefato: um nome diegético, um custo de Resiliência Mental,
    /// um cooldown próprio e a lista ordenada de efeitos que dispara.
    ///
    /// <para>Cada Artefato entrega <b>uma</b> habilidade — é o que mantém a barra de artefatos
    /// legível com 4 slots e impede que o sistema vire uma sopa de botões.</para>
    ///
    /// <para>O POCO <b>decide</b> se pode disparar e o que acontece; quem cobra o RM e arma o
    /// relógio do cooldown é o adaptador Runtime, como no par <c>IArmaComHabilidade</c> /
    /// <c>MaoFisicaBridge</c>.</para>
    /// </summary>
    public sealed class ArtefatoAtivo
    {
        private readonly IReadOnlyList<IEfeitoDeArtefato> _efeitos;

        /// <summary>Nome diegético mostrado na barra de artefatos.</summary>
        public string Nome { get; }

        /// <summary>Resiliência Mental cobrada por uso.</summary>
        public float CustoRM { get; }

        /// <summary>Segundos até poder usar de novo.</summary>
        public float Cooldown { get; }

        /// <summary>Segundos que o efeito permanece no mundo.</summary>
        public float Duracao { get; }

        /// <summary>Monta a habilidade. Valores negativos são saneados para zero.</summary>
        public ArtefatoAtivo(string nome, float custoRM, float cooldown, float duracao,
            IReadOnlyList<IEfeitoDeArtefato> efeitos)
        {
            Nome = string.IsNullOrWhiteSpace(nome) ? "Artefato" : nome;
            CustoRM = custoRM < 0f ? 0f : custoRM;
            Cooldown = cooldown < 0f ? 0f : cooldown;
            Duracao = duracao < 0f ? 0f : duracao;
            _efeitos = efeitos ?? new List<IEfeitoDeArtefato>();
        }

        /// <summary>
        /// Se a habilidade pode disparar agora. Exige RM <b>estritamente maior</b> que o custo
        /// quando há custo: gastar a última lasca de Resiliência e colapsar por causa da
        /// própria habilidade seria punição sem aviso.
        /// </summary>
        /// <param name="rmAtual">Resiliência Mental atual de Damião.</param>
        /// <param name="tempoDesdeUltimoUso">Segundos desde o último disparo bem-sucedido.</param>
        public bool PodeAtivar(float rmAtual, float tempoDesdeUltimoUso)
        {
            if (tempoDesdeUltimoUso < Cooldown) return false;
            if (CustoRM > 0f && rmAtual <= CustoRM) return false;
            return true;
        }

        /// <summary>
        /// Aplica os efeitos em ordem e devolve o que o adaptador precisa cobrar. Não checa
        /// pré-condição — quem chama já passou por <see cref="PodeAtivar"/>.
        /// </summary>
        public ResultadoDeArtefato Ativar(IContextoDeArtefato ctx)
        {
            for (int i = 0; i < _efeitos.Count; i++)
                _efeitos[i]?.Aplicar(ctx);

            return new ResultadoDeArtefato(true, CustoRM, Duracao, Cooldown);
        }
    }
}
