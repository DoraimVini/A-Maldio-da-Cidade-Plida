namespace FavelaAmarela.Core.Artefatos
{
    /// <summary>
    /// Resultado imutável de uma tentativa de usar a habilidade de um Artefato. Espelha o
    /// papel de <c>ArmaResult</c> no combate: o POCO decide, o adaptador cobra o custo e
    /// arma o cooldown.
    /// </summary>
    public readonly struct ResultadoDeArtefato
    {
        /// <summary>Se a habilidade chegou a disparar.</summary>
        public readonly bool Sucesso;

        /// <summary>Resiliência Mental a cobrar de Damião.</summary>
        public readonly float CustoRM;

        /// <summary>Quanto tempo o efeito dura no mundo.</summary>
        public readonly float Duracao;

        /// <summary>Quanto tempo até a habilidade poder ser usada de novo.</summary>
        public readonly float Cooldown;

        /// <summary>Monta um resultado.</summary>
        public ResultadoDeArtefato(bool sucesso, float custoRM, float duracao, float cooldown)
        {
            Sucesso = sucesso;
            CustoRM = custoRM;
            Duracao = duracao;
            Cooldown = cooldown;
        }

        /// <summary>Tentativa que não disparou (sem RM, em recarga ou sem artefato).</summary>
        public static ResultadoDeArtefato Falhou => new ResultadoDeArtefato(false, 0f, 0f, 0f);
    }
}
