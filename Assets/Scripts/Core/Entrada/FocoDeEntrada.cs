using System;
using System.Collections.Generic;

namespace FavelaAmarela.Core.Entrada
{
    /// <summary>Quem está no comando do teclado num dado momento.</summary>
    public enum CamadaDeEntrada
    {
        /// <summary>O jogo. Andar, atacar, esquivar, interagir, usar item.</summary>
        Jogo = 0,

        /// <summary>Um painel modal do jogo: inventário, escolha de diálogo, pausa.</summary>
        PainelModal = 1,

        /// <summary>O console de diagnóstico, que tem campos de texto.</summary>
        Console = 2,
    }

    /// <summary>
    /// POCO puro. O <b>árbitro de foco de entrada</b>: uma pilha que responde a uma pergunta
    /// só — <i>quem está no comando agora?</i>
    ///
    /// <para><b>Por que precisou existir (2026-09-02).</b> A auditoria mediu <b>sete</b>
    /// disputas de tecla, e a causa não era nenhuma delas: era não haver <b>camada de input
    /// nenhuma</b>. Três fontes liam teclado ao mesmo tempo — o asset de ações, três scripts
    /// lendo <c>Keyboard.current</c> cru, e o <c>EventSystem</c> com um asset próprio — e
    /// <b>nenhuma conhecia as outras</b>. Ninguém chamava
    /// <c>SwitchCurrentActionMap</c>/<c>Enable</c>/<c>Disable</c>: o mapa "Player" ficava sempre
    /// ligado, com painel aberto e com o jogo pausado.</para>
    ///
    /// <para><b><c>Time.timeScale = 0</c> não engole tecla nenhuma</b> — <c>Update()</c> continua
    /// rodando. Foi por isso que, com o inventário aberto, F1–F4 continuavam disparando
    /// Artefatos, 1–8 continuavam consumindo itens, o clique continuava golpeando; e por isso
    /// <b>digitar "3" no console consumia o item do slot 3</b>.</para>
    ///
    /// <para><b>Pilha, e não um único valor:</b> o console pode abrir por cima do inventário, e
    /// fechá-lo tem de devolver o comando ao inventário, não ao jogo. Devolver uma camada que
    /// não está no topo remove <b>a ocorrência mais alta dela</b>, para uma liberação fora de
    /// ordem não corromper o resto.</para>
    ///
    /// <para>Sem Unity: testável com <c>new FocoDeEntrada()</c>.</para>
    /// </summary>
    public sealed class FocoDeEntrada
    {
        private readonly List<CamadaDeEntrada> _pilha = new List<CamadaDeEntrada>();

        /// <summary>Quem está no comando. Com a pilha vazia, é o jogo.</summary>
        public CamadaDeEntrada Atual =>
            _pilha.Count == 0 ? CamadaDeEntrada.Jogo : _pilha[_pilha.Count - 1];

        /// <summary>
        /// Se o <b>jogo</b> pode ler entrada agora. É a pergunta que todo leitor de tecla de
        /// gameplay faz antes de agir.
        /// </summary>
        public bool JogoNoComando => Atual == CamadaDeEntrada.Jogo;

        /// <summary>Quantas camadas estão empilhadas sobre o jogo.</summary>
        public int Profundidade => _pilha.Count;

        /// <summary>Disparado quando o comando muda de dono. Recebe o novo dono.</summary>
        public event Action<CamadaDeEntrada> OnMudou;

        /// <summary>
        /// Toma o comando para a camada dada.
        ///
        /// <para><see cref="CamadaDeEntrada.Jogo"/> é o piso, não uma camada que se toma:
        /// pedi-lo é ignorado. Sem isso, um chamador distraído "tomaria o jogo" por cima de um
        /// painel aberto e devolveria o controle a quem não deveria tê-lo.</para>
        /// </summary>
        public void Tomar(CamadaDeEntrada camada)
        {
            if (camada == CamadaDeEntrada.Jogo) return;

            var antes = Atual;
            _pilha.Add(camada);

            if (Atual != antes) OnMudou?.Invoke(Atual);
        }

        /// <summary>
        /// Devolve o comando: remove a <b>ocorrência mais alta</b> da camada dada.
        ///
        /// <para>Devolver o que não se tem é silencioso de propósito — um painel que fecha duas
        /// vezes (por Esc e pelo botão, digamos) não pode derrubar a camada de outro.</para>
        /// </summary>
        /// <returns>Se havia o que devolver.</returns>
        public bool Devolver(CamadaDeEntrada camada)
        {
            for (int i = _pilha.Count - 1; i >= 0; i--)
            {
                if (_pilha[i] != camada) continue;

                var antes = Atual;
                _pilha.RemoveAt(i);

                if (Atual != antes) OnMudou?.Invoke(Atual);
                return true;
            }

            return false;
        }

        /// <summary>Se a camada dada está em algum ponto da pilha.</summary>
        public bool Tem(CamadaDeEntrada camada) => _pilha.Contains(camada);

        /// <summary>
        /// Devolve tudo ao jogo. Para troca de cena: um painel destruído no meio do caminho
        /// deixaria a pilha suja e o jogador sem controle, sem nada explicando.
        /// </summary>
        public void Limpar()
        {
            if (_pilha.Count == 0) return;

            _pilha.Clear();
            OnMudou?.Invoke(CamadaDeEntrada.Jogo);
        }
    }
}
