using System;

namespace FavelaAmarela.Core.Dialogo
{
    /// <summary>
    /// Uma opção numa escolha de diálogo ramificado (ex.: "Lutar" / "Concordar com ele").
    /// <see cref="Id"/> é o que o código de jogo usa para decidir o que acontece — o texto
    /// é só o que aparece na tela.
    /// </summary>
    public readonly struct OpcaoDeDialogo
    {
        /// <summary>Texto mostrado ao jogador. Passa pela skill favela-lore-enforcer.</summary>
        public readonly string Texto;

        /// <summary>Identidade opaca desta opção — o chamador decide o que ela significa.</summary>
        public readonly int Id;

        public OpcaoDeDialogo(string texto, int id)
        {
            Texto = texto;
            Id = id;
        }
    }

    /// <summary>
    /// Cursor de navegação numa lista pequena de opções (setas/stick para mover, botão
    /// para confirmar) — a mecânica pura por trás de uma escolha de diálogo ramificado
    /// como a conversa com o Abdul (lutar × concordar).
    ///
    /// <para>POCO puro: não sabe de UI nem de input. O adaptador Runtime lê o eixo
    /// vertical do movimento e chama <see cref="Avancar"/>/<see cref="Retroceder"/>; ao
    /// confirmar, lê <see cref="IndiceAtual"/> e resolve o <see cref="OpcaoDeDialogo.Id"/>
    /// correspondente.</para>
    /// </summary>
    public sealed class NavegadorDeOpcoes
    {
        /// <summary>Quantas opções existem nesta escolha. Sempre &gt;= 1.</summary>
        public int Quantidade { get; }

        /// <summary>Índice da opção destacada agora (0-based).</summary>
        public int IndiceAtual { get; private set; }

        /// <param name="quantidade">Número de opções. Deve ser &gt;= 1.</param>
        public NavegadorDeOpcoes(int quantidade)
        {
            if (quantidade <= 0)
                throw new ArgumentOutOfRangeException(nameof(quantidade),
                    "Uma escolha precisa de ao menos uma opção.");
            Quantidade = quantidade;
        }

        /// <summary>
        /// Move o destaque para a próxima opção, dando a volta da última para a
        /// primeira. Com uma única opção, não faz nada (fica nela).
        /// </summary>
        public void Avancar() => IndiceAtual = (IndiceAtual + 1) % Quantidade;

        /// <summary>
        /// Move o destaque para a opção anterior, dando a volta da primeira para a
        /// última. Com uma única opção, não faz nada (fica nela).
        /// </summary>
        public void Retroceder() => IndiceAtual = (IndiceAtual - 1 + Quantidade) % Quantidade;
    }
}
