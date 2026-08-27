using System.Collections.Generic;

namespace FavelaAmarela.Core.Abilities
{
    /// <summary>
    /// Uma arma montada a partir de <b>efeitos</b>, não de uma classe própria.
    ///
    /// <para>É a peça central de <c>habilidades_de_item.md</c>: o Alfanje de Alhazred, hoje
    /// 60 linhas de classe C#, vira <b>uma lista com <c>EfeitoDeDano</c>,
    /// <c>EfeitoDeAtordoamento</c> e <c>EfeitoDeRepulsao</c></b>. Arma nova deixa de custar um
    /// arquivo, um valor de enum e uma linha na fábrica.</para>
    ///
    /// <para>Continua sendo POCO puro, testável com <c>new HabilidadeComposta(...)</c> sem
    /// cena nem Unity rodando — a Regra de Ouro 6 do <c>CLAUDE.md</c> vale igual. E o ganho de
    /// teste é maior: um efeito testado uma vez vale para toda arma futura que o use, em vez
    /// de a mesma asserção ser recontada em cada classe de arma.</para>
    /// </summary>
    public sealed class HabilidadeComposta : IArmaComHabilidade
    {
        private readonly IReadOnlyList<IEfeitoDeHabilidade> _efeitosDoBasico;
        private readonly IReadOnlyList<IEfeitoDeHabilidade> _efeitosDaHabilidade;

        private readonly float _duracaoBasico;
        private readonly float _cooldownBasico;
        private readonly float _duracaoHabilidade;
        private readonly float _cooldownHabilidade;

        /// <inheritdoc/>
        public string NomeDaArma { get; }

        /// <inheritdoc/>
        public string NomeHabilidade { get; }

        /// <summary>
        /// Monta a arma. As listas são guardadas como estão — quem constrói é o
        /// <c>HabilidadeDef</c>, e ele não as reusa depois.
        /// </summary>
        /// <param name="nomeDaArma">Nome diegético da arma.</param>
        /// <param name="nomeHabilidade">Nome diegético da habilidade.</param>
        /// <param name="efeitosDoBasico">Efeitos do ataque básico.</param>
        /// <param name="efeitosDaHabilidade">Efeitos da habilidade.</param>
        /// <param name="duracaoBasico">Quanto tempo o básico prende o ator.</param>
        /// <param name="cooldownBasico">Cadência do básico. É consultada de verdade desde
        /// 2026-08-27 — antes disso <c>CanActivate</c> não era chamado por ninguém.</param>
        /// <param name="duracaoHabilidade">Quanto tempo a habilidade prende o ator.</param>
        /// <param name="cooldownHabilidade">Recarga da habilidade.</param>
        public HabilidadeComposta(
            string nomeDaArma, string nomeHabilidade,
            IReadOnlyList<IEfeitoDeHabilidade> efeitosDoBasico,
            IReadOnlyList<IEfeitoDeHabilidade> efeitosDaHabilidade,
            float duracaoBasico, float cooldownBasico,
            float duracaoHabilidade, float cooldownHabilidade)
        {
            NomeDaArma = string.IsNullOrWhiteSpace(nomeDaArma) ? "Arma sem nome" : nomeDaArma;
            NomeHabilidade = string.IsNullOrWhiteSpace(nomeHabilidade)
                ? "Habilidade sem nome"
                : nomeHabilidade;

            _efeitosDoBasico = efeitosDoBasico ?? new IEfeitoDeHabilidade[0];
            _efeitosDaHabilidade = efeitosDaHabilidade ?? new IEfeitoDeHabilidade[0];

            _duracaoBasico = duracaoBasico;
            _cooldownBasico = cooldownBasico;
            _duracaoHabilidade = duracaoHabilidade;
            _cooldownHabilidade = cooldownHabilidade;
        }

        /// <inheritdoc/>
        public bool CanActivate(float timeSinceLastUse) => timeSinceLastUse >= _cooldownBasico;

        /// <inheritdoc/>
        public ArmaResult Execute() =>
            Compor(_efeitosDoBasico, _duracaoBasico, _cooldownBasico);

        /// <inheritdoc/>
        public bool CanActivateHabilidade(float timeSinceLastAbilityUse) =>
            timeSinceLastAbilityUse >= _cooldownHabilidade;

        /// <inheritdoc/>
        public ArmaResult ExecuteHabilidade() =>
            Compor(_efeitosDaHabilidade, _duracaoHabilidade, _cooldownHabilidade);

        /// <summary>
        /// Aplica os efeitos <b>em ordem</b> sobre um golpe em branco. A ordem importa: efeitos
        /// que mantêm "o maior valor" (atordoamento, repulsão) dependem de quem já escreveu.
        /// </summary>
        private static ArmaResult Compor(IReadOnlyList<IEfeitoDeHabilidade> efeitos,
                                         float duracao, float cooldown)
        {
            var golpe = new ConstrutorDeGolpe(duracao, cooldown);

            for (int i = 0; i < efeitos.Count; i++)
                efeitos[i]?.Aplicar(golpe);

            return golpe.Construir();
        }
    }
}
