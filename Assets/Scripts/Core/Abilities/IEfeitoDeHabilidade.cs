namespace FavelaAmarela.Core.Abilities
{
    /// <summary>
    /// Um efeito atômico de golpe — a peça que torna arma nova <b>conteúdo</b> em vez de
    /// código.
    ///
    /// <para><b>O problema que isto resolve</b> (diagnosticado em
    /// <c>habilidades_de_item.md</c>, 2026-08-10): cada arma com habilidade própria era uma
    /// classe C# escrita à mão, mais um valor de enum, mais uma linha na <c>WeaponFactory</c>.
    /// <i>"Uma dungeon inteira de armas novas é uma dungeon inteira de classes C# novas."</i></para>
    ///
    /// <para><b>A régua de quando ainda escrever classe</b>, do mesmo documento: se o efeito é
    /// "dano ou status com número configurável", é <b>dado</b>; se tem lógica condicional
    /// própria — estado, contador, gatilho por fase de luta —, é <b>código</b>. O Escudo Mágico
    /// do Abdul continua merecendo classe; um alfanje que atordoa e repele, não.</para>
    ///
    /// <para><b>Divergência declarada do design escrito.</b> O documento propunha
    /// <c>Aplicar(AlvoDeEfeito alvo)</c>, com o efeito agindo direto sobre o alvo. Aqui o efeito
    /// <b>compõe um <see cref="ConstrutorDeGolpe"/></b>, que vira <c>ArmaResult</c>. O motivo é
    /// concreto: <c>ArmaResult</c> já é o valor que carrega efeito por todo o pipeline
    /// (<c>Hurtbox</c>, <c>EnemyStatusEffects</c>, <c>RepulsaoDeImpacto</c>, <c>HitStop</c>).
    /// Agir direto no alvo exigiria reescrever esse pipeline inteiro para ganhar a mesma coisa.</para>
    /// </summary>
    public interface IEfeitoDeHabilidade
    {
        /// <summary>Nome para diagnóstico e para o tooltip do item.</summary>
        string Nome { get; }

        /// <summary>Escreve a contribuição deste efeito no golpe em construção.</summary>
        void Aplicar(ConstrutorDeGolpe golpe);
    }
}
