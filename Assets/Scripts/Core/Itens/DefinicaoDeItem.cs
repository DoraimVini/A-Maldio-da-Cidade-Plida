using System;
using FavelaAmarela.Core.Abilities;

// ─────────────────────────────────────────────────────────────────────────────
// NOTA DE ARQUIVO (2026-08-01) — por que o domínio de itens está todo aqui
//
// Estes tipos deveriam morar em arquivos separados (Inventario.cs, PilhaDeItens.cs).
// Foram consolidados porque o AssetDatabase da Unity **parou de indexar arquivos .cs
// novos** nesta pasta: um erro de sintaxe proposital dentro de Inventario.cs não gerou
// nenhum erro de compilação, provando que a Unity ignorava o arquivo por completo.
// Descartados: sintaxe, namespace, cobertura do .asmdef, .meta da pasta, colisão de GUID
// e cache de DLL. Este arquivo é o único caminho compilado da pasta.
//
// Ao reiniciar o Editor (o que reconstrói o índice de assets), separar em um tipo por
// arquivo. Nada no código precisa mudar para isso — só mover os blocos.
//
// Precedente do projeto para tipos relacionados juntos: Sangramento.cs (3 tipos) e
// Vitalidade.cs (2 tipos).
// ─────────────────────────────────────────────────────────────────────────────

namespace FavelaAmarela.Core.Itens
{
    /// <summary>
    /// O que um consumível faz ao ser usado. Os nomes seguem o vocabulário diegético
    /// (skill <c>favela-lore-enforcer</c>): cura de sanidade é <b>Ancoragem</b>, cura de
    /// corpo é <b>Estabilização</b> — nunca "heal".
    /// </summary>
    public enum TipoDeEfeito
    {
        /// <summary>Nenhum efeito — item de lore, chave ou material.</summary>
        Nenhum = 0,

        /// <summary>Ancoragem: devolve Resiliência Mental (a sanidade).</summary>
        Ancorar = 1,

        /// <summary>Estabilização: devolve Vitalidade corpórea (a carne).</summary>
        Estabilizar = 2,

        /// <summary>Estanca a Ferida de Aklo em curso.</summary>
        EstancarFeridas = 3,
    }

    /// <summary>
    /// POCO imutável. <b>O que um item é</b> — não quantos você tem (isso é da
    /// <see cref="PilhaDeItens"/>). Um único destes existe por tipo de item no jogo,
    /// autorado como asset e convertido pelo <c>ItemConfig</c> (Runtime), mesmo padrão de
    /// <c>FichaAtributosConfig</c> → <c>FichaDeAtributos</c>.
    ///
    /// <para><b>O <see cref="Id"/> é a identidade real</b>, não o nome: o nome visível pode
    /// mudar por revisão de texto sem invalidar saves ou empilhamento.</para>
    /// </summary>
    public sealed class DefinicaoDeItem
    {
        /// <summary>Identificador estável e imutável (ex.: <c>"cinza_de_ancora"</c>).</summary>
        public string Id { get; }

        /// <summary>Nome visível ao jogador. Deve seguir o vocabulário diegético.</summary>
        public string Nome { get; }

        /// <summary>Descrição visível ao jogador.</summary>
        public string Descricao { get; }

        /// <summary>Quantos cabem numa pilha. 1 = não empilha.</summary>
        public int PilhaMaxima { get; }

        /// <summary>O que faz ao ser consumido.</summary>
        public TipoDeEfeito Efeito { get; }

        /// <summary>Magnitude do efeito (quanto restaura). Ignorado se <see cref="Efeito"/> não usa.</summary>
        public float Potencia { get; }

        /// <summary>
        /// Qual arma da Tumba este item empunha, ou <c>null</c> se não é arma.
        ///
        /// <para><b>Armas são itens</b> (decisão do Vini, 2026-08-01). Antes havia dois
        /// sistemas paralelos — o slot único da Mão Física e o inventário — que não se
        /// falavam. O design já pressupunha a unificação: <c>systems/abilities.md</c> diz que
        /// trocar o que está empunhado <b>só pode ser feito sob a luz de um Refúgio</b>, o
        /// que exige haver onde guardar a arma que não está em uso.</para>
        /// </summary>
        public ArmaDaTumba? ArmaEquipavel { get; }

        /// <summary>Se este item é uma arma que pode ser empunhada.</summary>
        public bool EhEquipavel => ArmaEquipavel.HasValue;

        /// <summary>
        /// Se some do inventário ao ser usado. <b>Arma não some ao ser empunhada</b> — ela
        /// muda de estado, não é gasta.
        /// </summary>
        public bool ConsomeAoUsar => Efeito != TipoDeEfeito.Nenhum && !EhEquipavel;

        /// <summary>Se pode empilhar com outro do mesmo tipo.</summary>
        public bool Empilhavel => PilhaMaxima > 1;

        /// <param name="id">Identificador estável. Obrigatório.</param>
        /// <param name="pilhaMaxima">Clampado a no mínimo 1. Armas nunca empilham.</param>
        /// <param name="potencia">Clampada a no mínimo 0.</param>
        /// <param name="armaEquipavel">Qual arma da Tumba este item é, se for uma.</param>
        public DefinicaoDeItem(string id, string nome = null, string descricao = null,
            int pilhaMaxima = 1, TipoDeEfeito efeito = TipoDeEfeito.Nenhum, float potencia = 0f,
            ArmaDaTumba? armaEquipavel = null)
        {
            if (string.IsNullOrWhiteSpace(id))
                throw new ArgumentException("Item precisa de um Id estável.", nameof(id));

            Id = id;
            Nome = string.IsNullOrWhiteSpace(nome) ? id : nome;
            Descricao = descricao ?? string.Empty;
            ArmaEquipavel = armaEquipavel;

            // Arma não empilha: duas do mesmo tipo numa pilha só seriam indistinguíveis, e
            // o slot da Mão Física é um só.
            PilhaMaxima = armaEquipavel.HasValue ? 1 : (pilhaMaxima < 1 ? 1 : pilhaMaxima);

            Efeito = efeito;
            Potencia = potencia < 0f ? 0f : potencia;
        }
    }

    /// <summary>
    /// Uma posição do inventário: o que está lá e quantos. <c>readonly struct</c> — o
    /// inventário devolve cópias, então ninguém altera uma pilha por fora sem passar pelos
    /// métodos que mantêm as invariantes.
    /// </summary>
    public readonly struct PilhaDeItens
    {
        /// <summary>O item, ou <c>null</c> se a posição está vazia.</summary>
        public readonly DefinicaoDeItem Item;

        /// <summary>Quantos. Sempre 0 quando <see cref="Item"/> é null.</summary>
        public readonly int Quantidade;

        /// <summary>Se não há nada aqui.</summary>
        public bool Vazia => Item == null || Quantidade <= 0;

        /// <summary>Se a pilha atingiu o teto do item.</summary>
        public bool Cheia => Item != null && Quantidade >= Item.PilhaMaxima;

        /// <summary>Quanto ainda cabe nesta pilha.</summary>
        public int EspacoLivre => Item == null ? 0 : Item.PilhaMaxima - Quantidade;

        /// <summary>
        /// Cria uma pilha. Item nulo ou quantidade não-positiva produzem uma pilha
        /// <b>vazia</b> em vez de um estado inconsistente; quantidade acima do teto do item
        /// é clampada.
        /// </summary>
        public PilhaDeItens(DefinicaoDeItem item, int quantidade)
        {
            if (item == null || quantidade <= 0)
            {
                Item = null;
                Quantidade = 0;
                return;
            }

            Item = item;
            Quantidade = quantidade > item.PilhaMaxima ? item.PilhaMaxima : quantidade;
        }
    }

    /// <summary>
    /// O que sai de um <see cref="Inventario.Consumir"/>. <c>readonly struct</c> para não
    /// alocar — usar item pode acontecer em sequência rápida no meio de uma luta.
    /// </summary>
    public readonly struct EfeitoDeUso
    {
        /// <summary>O efeito a aplicar.</summary>
        public readonly TipoDeEfeito Tipo;

        /// <summary>A magnitude.</summary>
        public readonly float Potencia;

        /// <summary>Se houve uso de fato (false quando a posição estava vazia ou o item é inerte).</summary>
        public bool Houve => Tipo != TipoDeEfeito.Nenhum;

        public EfeitoDeUso(TipoDeEfeito tipo, float potencia)
        {
            Tipo = tipo;
            Potencia = potencia;
        }
    }

    /// <summary>
    /// POCO puro. Inventário de Damião: um número fixo de posições, com empilhamento.
    ///
    /// <para><b>Deliberadamente enxuto</b> (restrição do <c>CLAUDE.md</c> §1): poucas
    /// posições, sem peso, sem categorias, sem ordenação automática. Existe para dar suporte
    /// aos consumíveis do Vertical Slice — não para virar um sistema de gerenciamento com
    /// grind. Poucas posições também é decisão de <b>tensão</b>: escolher o que deixar para
    /// trás é parte do horror de sobrevivência.</para>
    ///
    /// <para>Nenhuma dependência de Unity: quem aplica os efeitos no mundo é o adaptador
    /// Runtime, que recebe o <see cref="EfeitoDeUso"/> devolvido por <see cref="Consumir"/>.</para>
    /// </summary>
    public sealed class Inventario
    {
        /// <summary>Quantidade padrão de posições.</summary>
        public const int PosicoesPadrao = 8;

        private readonly PilhaDeItens[] _posicoes;

        /// <summary>Quantas posições existem.</summary>
        public int Posicoes => _posicoes.Length;

        /// <summary>Disparado sempre que o conteúdo muda (para a UI redesenhar sem polling).</summary>
        public event Action OnMudou;

        /// <param name="posicoes">Número de posições. Clampado a no mínimo 1.</param>
        public Inventario(int posicoes = PosicoesPadrao)
        {
            _posicoes = new PilhaDeItens[posicoes < 1 ? 1 : posicoes];
        }

        /// <summary>Lê uma posição. Índice fora da faixa devolve pilha vazia em vez de estourar.</summary>
        public PilhaDeItens Ver(int indice)
            => indice < 0 || indice >= _posicoes.Length ? default : _posicoes[indice];

        /// <summary>Quantos exemplares de um item existem somando todas as posições.</summary>
        public int Contar(string idDoItem)
        {
            if (string.IsNullOrWhiteSpace(idDoItem)) return 0;

            int total = 0;
            for (int i = 0; i < _posicoes.Length; i++)
                if (!_posicoes[i].Vazia && _posicoes[i].Item.Id == idDoItem)
                    total += _posicoes[i].Quantidade;

            return total;
        }

        /// <summary>Se há espaço para ao menos um exemplar deste item. Não altera nada.</summary>
        public bool TemEspacoPara(DefinicaoDeItem item)
            => item != null && Guardar(item, 1, simular: true) == 0;

        /// <summary>
        /// Guarda itens. <b>Completa as pilhas existentes antes de ocupar posição nova</b> —
        /// senão o inventário enche por fragmentação com o jogador achando que tem espaço.
        /// </summary>
        /// <returns>Quantos <b>não couberam</b> (0 = guardou tudo).</returns>
        public int Adicionar(DefinicaoDeItem item, int quantidade = 1)
            => Guardar(item, quantidade, simular: false);

        private int Guardar(DefinicaoDeItem item, int quantidade, bool simular)
        {
            if (item == null || quantidade <= 0) return quantidade > 0 ? quantidade : 0;

            var destino = simular ? (PilhaDeItens[])_posicoes.Clone() : _posicoes;
            int restante = quantidade;

            // 1ª passada: completa pilhas do mesmo item.
            for (int i = 0; i < destino.Length && restante > 0; i++)
            {
                var p = destino[i];
                if (p.Vazia || p.Item.Id != item.Id || p.Cheia) continue;

                int cabe = p.EspacoLivre < restante ? p.EspacoLivre : restante;
                destino[i] = new PilhaDeItens(p.Item, p.Quantidade + cabe);
                restante -= cabe;
            }

            // 2ª passada: ocupa posições vazias.
            for (int i = 0; i < destino.Length && restante > 0; i++)
            {
                if (!destino[i].Vazia) continue;

                int cabe = item.PilhaMaxima < restante ? item.PilhaMaxima : restante;
                destino[i] = new PilhaDeItens(item, cabe);
                restante -= cabe;
            }

            if (!simular && restante != quantidade) OnMudou?.Invoke();
            return restante;
        }

        /// <summary>
        /// Retira exemplares de um item, varrendo as posições. Se não houver o suficiente,
        /// <b>não retira nada</b> — evita consumir metade de um custo que não podia ser pago.
        /// </summary>
        /// <returns>Se conseguiu retirar tudo.</returns>
        public bool Remover(string idDoItem, int quantidade = 1)
        {
            if (quantidade <= 0 || Contar(idDoItem) < quantidade) return false;

            int restante = quantidade;
            for (int i = 0; i < _posicoes.Length && restante > 0; i++)
            {
                var p = _posicoes[i];
                if (p.Vazia || p.Item.Id != idDoItem) continue;

                int tira = p.Quantidade < restante ? p.Quantidade : restante;
                _posicoes[i] = new PilhaDeItens(p.Item, p.Quantidade - tira);
                restante -= tira;
            }

            OnMudou?.Invoke();
            return true;
        }

        /// <summary>Esvazia uma posição inteira.</summary>
        public void Esvaziar(int indice)
        {
            if (indice < 0 || indice >= _posicoes.Length || _posicoes[indice].Vazia) return;

            _posicoes[indice] = default;
            OnMudou?.Invoke();
        }

        /// <summary>
        /// Usa o que estiver na posição. O inventário <b>não aplica</b> o efeito — ele não
        /// sabe o que é Vitalidade nem Resiliência. Devolve o que aconteceu para o adaptador
        /// Runtime aplicar no mundo.
        /// </summary>
        public EfeitoDeUso Consumir(int indice)
        {
            var p = Ver(indice);
            if (p.Vazia) return default;

            var item = p.Item;
            if (item.Efeito == TipoDeEfeito.Nenhum) return default; // item de lore: não gasta

            if (item.ConsomeAoUsar)
            {
                _posicoes[indice] = new PilhaDeItens(item, p.Quantidade - 1);
                OnMudou?.Invoke();
            }

            return new EfeitoDeUso(item.Efeito, item.Potencia);
        }
    }
}
