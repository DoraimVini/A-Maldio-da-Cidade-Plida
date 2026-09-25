namespace FavelaAmarela.Runtime.Audio
{
    /// <summary>
    /// Os sons que o jogo dispara. Enum fechado e pequeno de propósito: cada entrada nova é
    /// decisão deliberada, e o <see cref="BancoDeSons"/> mapeia cada uma para um clipe
    /// autorado — som novo é asset, não código.
    /// </summary>
    public enum SomDoJogo
    {
        /// <summary>Damião emitiu ruído ao se mover. O som central do jogo.</summary>
        PassoDeDamiao = 0,

        /// <summary>Golpe desferido pela Mão Física.</summary>
        GolpeDesferido = 1,

        /// <summary>Habilidade da arma disparada.</summary>
        HabilidadeDeArma = 2,

        /// <summary>Uma entidade recebeu dano.</summary>
        EntidadeFerida = 3,

        /// <summary>Uma entidade foi abatida.</summary>
        EntidadeAbatida = 4,

        /// <summary>Damião entrou em Pânico (Resiliência abaixo do limiar).</summary>
        EntrouEmPanico = 5,

        /// <summary>Damião entrou em Colapso.</summary>
        Colapso = 6,

        /// <summary>Item recolhido do chão.</summary>
        ItemRecolhido = 7,

        /// <summary>Habilidade de Artefato invocada.</summary>
        ArtefatoInvocado = 8
    }
}
