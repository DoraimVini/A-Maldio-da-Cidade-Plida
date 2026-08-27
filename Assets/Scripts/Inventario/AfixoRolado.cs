using System;

namespace FavelaAmarela.Inventario
{
    /// <summary>
    /// Um afixo já <b>rolado</b>, com o valor concreto que este exemplar do item recebeu.
    ///
    /// <para><b>Por que o valor é gravado e não a semente.</b> Semente é elegante até a primeira
    /// vez que alguém edita um <c>AfixoDef</c> — aí toda arma já dropada muda sozinha, e o
    /// jogador vê o item na mochila dele ficar diferente sem ter feito nada. D2 e PoE gravam os
    /// mods pelo mesmo motivo. Com o volume deste jogo, o custo de tamanho no save é
    /// irrelevante perto disso.</para>
    ///
    /// <para><c>[Serializable]</c> e classe (não struct) porque <c>JsonUtility</c> serializa
    /// listas de classes, e é por ele que o save passa.</para>
    /// </summary>
    [Serializable]
    public class AfixoRolado
    {
        /// <summary>Qual <c>AfixoDef</c> gerou este rolamento. Guardado para diagnóstico.</summary>
        public string AfixoId;

        /// <summary>O atributo afetado.</summary>
        public StatType Stat;

        /// <summary>O valor sorteado, dentro da faixa que o <c>AfixoDef</c> autorou.</summary>
        public float Valor;

        /// <summary>Construtor sem argumentos exigido por <c>JsonUtility</c>.</summary>
        public AfixoRolado() { }

        /// <summary>Cria um afixo rolado.</summary>
        public AfixoRolado(string afixoId, StatType stat, float valor)
        {
            AfixoId = afixoId;
            Stat = stat;
            Valor = valor;
        }

        /// <summary>Vira um modificador comum, para o pipeline de bônus não precisar saber a origem.</summary>
        public ModificadorFixo ComoModificador() => new ModificadorFixo(Stat, Valor);
    }
}
