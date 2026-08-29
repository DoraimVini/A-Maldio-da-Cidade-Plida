using System;
using System.Collections.Generic;
using System.Linq;

namespace FavelaAmarela.Inventario
{
    /// <summary>
    /// Os parâmetros de um item a ser autorado, e <b>tudo que pode estar errado neles</b>.
    ///
    /// <para><b>Por que isto é POCO e não vive no Editor.</b> O asmdef
    /// <c>FavelaAmarela.Tests.EditMode</c> referencia só <c>Core</c> e <c>Runtime</c>, com
    /// <c>overrideReferences: true</c> — nenhum teste consegue invocar código do assembly do
    /// Editor. Com a validação aqui, ela é testável; com ela dentro da janela, seria testável
    /// só à mão, que é o mesmo que não ser.</para>
    ///
    /// <para><b>O que esta classe está impedindo.</b> Uma ferramenta que cria itens vai
    /// produzir, mais cedo ou mais tarde: item sem ícone (derruba a suíte inteira, porque
    /// <c>IconesDosItensTests</c> varre <c>Assets</c> por completo), <c>Id</c> duplicado (o
    /// <c>ItemDatabase</c> loga erro e <b>mantém o primeiro</b> — o segundo item simplesmente
    /// não existe), e afixo em atributo decorativo (o jogador lê o número e não recebe nada).
    /// Nenhum desses três aparece como erro na hora de criar: os três aparecem depois, longe.</para>
    /// </summary>
    public sealed class ReceitaDeItem
    {
        /// <summary>Nome visível ao jogador. Segue a skill <c>favela-lore-enforcer</c>.</summary>
        public string Nome = "";

        /// <summary>
        /// Chave do catálogo e do save. Slug legível é a convenção da maioria do catálogo
        /// (<c>capuz_farrapos</c>, <c>set_elmo</c>); só as 3 armas usam GUID cru, por acidente
        /// histórico do <c>OnValidate</c>.
        /// </summary>
        public string Id = "";

        /// <summary>Categoria.</summary>
        public ItemType Tipo = ItemType.Armadura;

        /// <summary>Onde equipa. <c>Nenhum</c> para consumível e relíquia.</summary>
        public EquipmentSlot Slot = EquipmentSlot.Nenhum;

        /// <summary>1 = não empilhável (armas, armaduras). &gt;1 = consumível.</summary>
        public int EmpilhamentoMaximo = 1;

        /// <summary>Quantas mãos toma. Só faz sentido em arma.</summary>
        public Empunhadura Empunhadura = Empunhadura.UmaMao;

        /// <summary>Tem ícone atribuído? O asset em si é responsabilidade da camada de Editor.</summary>
        public bool TemIcone;

        /// <summary>Modificadores fixos (implícitos da base).</summary>
        public List<ModificadorFixo> Modificadores = new List<ModificadorFixo>();

        /// <summary>
        /// A <b>família</b> da arma — o que carrega o dano branco, a geometria do golpe e a
        /// habilidade.
        ///
        /// <para><b>Sem ela a arma sai inerte</b>, e essa era a maior lacuna da Forja: ela criava
        /// um <c>ItemDef</c> de arma com <c>Base</c> nulo, o que em jogo significa equipar e
        /// continuar desarmado. O <c>MaoFisicaBridge</c> grita nesse caso, mas gritar depois de
        /// o item existir é tarde — o autor já achou que tinha terminado.</para>
        /// </summary>
        public BaseDeArma Base;

        /// <summary>
        /// Nível do item. Escala a faixa de dano branco pela mesma lei que escala a ficha
        /// (<c>EscalaDeNivel</c>), e abre o pool de afixos: um item de nível 3 alcança
        /// afixos que um de nível 1 nunca rola.
        /// </summary>
        public int NivelDoItem = 1;

        /// <summary>
        /// Grau de impregnação a pré-visualizar. Não vai para o <c>ItemDef</c> — grau é da
        /// <c>ItemInstance</c>, por exemplar. Serve para a Forja mostrar quantos afixos o item
        /// receberia e quais.
        /// </summary>
        public FavelaAmarela.Core.Loot.GrauDeImpregnacao Grau =
            FavelaAmarela.Core.Loot.GrauDeImpregnacao.Inerte;

        /// <summary>
        /// Os atributos sem consumidor no jogo. Delega para <see cref="NomesDeAtributo.SemEfeito"/>
        /// — a lista morava aqui e em mais dois lugares, e três cópias divergiriam na primeira
        /// vez que alguém implementasse um dos quatro.
        /// </summary>
        public static IReadOnlyList<StatType> AtributosSemEfeito => NomesDeAtributo.SemEfeito;

        /// <summary>
        /// Tudo que impede este item de ser criado. Lista vazia = pode criar.
        /// </summary>
        /// <param name="idsExistentes">Ids já usados no catálogo, para recusar duplicata.</param>
        public IReadOnlyList<string> Problemas(IEnumerable<string> idsExistentes)
        {
            var erros = new List<string>();

            if (string.IsNullOrWhiteSpace(Nome))
                erros.Add("Sem nome: o inventário e a barra de ações desenhariam um rótulo vazio.");

            if (string.IsNullOrWhiteSpace(Id))
                erros.Add("Sem Id: é a chave do catálogo E do save. Sem ela o item não existe.");
            else if (Id.Any(char.IsWhiteSpace))
                erros.Add($"Id '{Id}' tem espaço. Use slug: minúsculas e underscore.");
            else if (idsExistentes != null &&
                     idsExistentes.Any(x => string.Equals(x, Id, StringComparison.Ordinal)))
                erros.Add($"Id '{Id}' já existe. O ItemDatabase loga erro e MANTÉM O PRIMEIRO — " +
                          "o item novo simplesmente não existiria, sem nada acusando em jogo.");

            if (!TemIcone)
                erros.Add("Sem ícone. Além de o jogador ver uma casa vazia na mochila, isto " +
                          "DERRUBA A SUÍTE INTEIRA: IconesDosItensTests varre Assets por " +
                          "completo e exige ícone em todo item autorado.");

            if (EmpilhamentoMaximo < 1)
                erros.Add($"Empilhamento {EmpilhamentoMaximo}: abaixo de 1 o item não cabe em " +
                          "slot nenhum.");

            if (Tipo == ItemType.Consumivel && Slot != EquipmentSlot.Nenhum)
                erros.Add("Consumível com slot de equipamento: ele seria vestível e não " +
                          "consumível.");

            if (EhEquipavel() && Slot == EquipmentSlot.Nenhum)
                erros.Add($"{Tipo} sem slot: o jogador pega o item e não consegue equipar.");

            if (Tipo == ItemType.Arma && EmpilhamentoMaximo > 1)
                erros.Add("Arma empilhável: equipar uma pilha não tem significado definido.");

            foreach (var mod in Modificadores ?? new List<ModificadorFixo>())
                if (AtributosSemEfeito.Contains(mod.Stat))
                    erros.Add($"'{NomesDeAtributo.De(mod.Stat)}' não tem consumidor no jogo: o " +
                              "jogador lê o número, ocupa o slot e não recebe nada. Um item que " +
                              "mente é pior que um item fraco.");

            return erros;
        }

        /// <summary>
        /// Avisos que <b>não</b> impedem a criação, mas que quem autora deveria ver.
        /// </summary>
        public IReadOnlyList<string> Avisos()
        {
            var avisos = new List<string>();

            if (Tipo == ItemType.Arma)
                avisos.Add("Arma precisa de uma BaseDeArma ligada depois de criada, senão é " +
                           "equipável e inerte — o Damião fica desarmado com ela na mão. " +
                           "Rode 'Armas: montar as bases (famílias)' ou ligue à mão.");

            if (Modificadores != null && Modificadores.Count == 0 && EhEquipavel())
                avisos.Add("Sem modificadores implícitos: o item só terá o que os afixos " +
                           "rolarem. É válido — uma base limpa —, só não é o mais comum.");

            if (Tipo == ItemType.Consumivel && EmpilhamentoMaximo == 1)
                avisos.Add("Consumível que não empilha ocupa uma casa da mochila por unidade. " +
                           "São 12 casas.");

            return avisos;
        }

        private bool EhEquipavel() =>
            Tipo == ItemType.Arma || Tipo == ItemType.Armadura || Tipo == ItemType.Amuleto;

        /// <summary>
        /// Sugere um <c>Id</c> a partir do nome, na convenção do catálogo: minúsculas, sem
        /// acento, underscore no lugar do espaço.
        /// </summary>
        public static string SugerirId(string nome)
        {
            if (string.IsNullOrWhiteSpace(nome)) return "";

            var limpo = new System.Text.StringBuilder();

            foreach (char c in nome.Trim().ToLowerInvariant())
            {
                if (char.IsLetterOrDigit(c)) limpo.Append(SemAcento(c));
                else if (char.IsWhiteSpace(c) || c == '-') limpo.Append('_');
            }

            return limpo.ToString().Trim('_');
        }

        private static char SemAcento(char c) => c switch
        {
            'á' or 'à' or 'â' or 'ã' or 'ä' => 'a',
            'é' or 'ê' or 'ë' => 'e',
            'í' or 'î' or 'ï' => 'i',
            'ó' or 'ô' or 'õ' or 'ö' => 'o',
            'ú' or 'û' or 'ü' => 'u',
            'ç' => 'c',
            _ => c,
        };
    }
}
