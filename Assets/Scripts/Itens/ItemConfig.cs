using UnityEngine;
using FavelaAmarela.Core.Abilities;
using FavelaAmarela.Core.Itens;

namespace FavelaAmarela.Runtime.Itens
{
    /// <summary>
    /// Autoria em asset de uma <see cref="DefinicaoDeItem"/> — um asset por tipo de item
    /// (Item_CinzaDeAncora, Item_EmplastroDeSal...). Adaptador Runtime puro: só serializa
    /// os campos e cospe o POCO via <see cref="CriarDefinicao"/>. Mesmo padrão de
    /// <c>FichaAtributosConfig</c> → <c>FichaDeAtributos</c>.
    ///
    /// <para><b>Texto visível ao jogador</b> (<see cref="nome"/>, <see cref="descricao"/>)
    /// deve seguir o vocabulário diegético da skill <c>favela-lore-enforcer</c>: nunca
    /// "cura X de HP" — é <b>Ancoragem</b> (sanidade) ou <b>Estabilização</b> (corpo).</para>
    /// </summary>
    [CreateAssetMenu(fileName = "Item_Novo", menuName = "Favela Amarela/Item")]
    public sealed class ItemConfig : ScriptableObject
    {
        [Header("Identidade")]
        [Tooltip("Identificador estável. NÃO mude depois que o item existir num save — " +
                 "é por ele que o inventário reconhece e empilha.")]
        [SerializeField] private string id = "item_novo";

        [Tooltip("Nome visível ao jogador. Segue o vocabulário diegético.")]
        [SerializeField] private string nome = "Item";

        [TextArea]
        [Tooltip("Descrição visível ao jogador.")]
        [SerializeField] private string descricao = "";

        [Header("Empilhamento")]
        [Tooltip("Quantos cabem numa pilha. 1 = não empilha.")]
        [Min(1)]
        [SerializeField] private int pilhaMaxima = 1;

        [Header("Efeito ao usar")]
        [Tooltip("O que acontece ao consumir. Nenhum = item de lore/chave (não é gasto).")]
        [SerializeField] private TipoDeEfeito efeito = TipoDeEfeito.Nenhum;

        [Tooltip("Quanto restaura. Escala 0–100, igual ao resto do combate.")]
        [Min(0f)]
        [SerializeField] private float potencia = 0f;

        [Header("Arma (opcional)")]
        [Tooltip("Marque se este item é uma das armas da Tumba. Armas são empunhadas em vez " +
                 "de consumidas, e nunca empilham.")]
        [SerializeField] private bool ehArma = false;

        [Tooltip("Qual arma da Tumba. Só vale se 'É arma' estiver marcado.")]
        [SerializeField] private ArmaDaTumba arma = ArmaDaTumba.CravoDeAklo;

        [Header("Visual")]
        [Tooltip("Ícone mostrado no inventário. [ASSET pixel art]")]
        [SerializeField] private Sprite icone;

        /// <summary>Ícone para a UI do inventário desenhar.</summary>
        public Sprite Icone => icone;

        /// <summary>Nome visível ao jogador — para prompts e mensagens, sem criar a POCO.</summary>
        public string NomeVisivel => string.IsNullOrWhiteSpace(nome) ? name : nome;

        /// <summary>
        /// Cria o POCO a partir do asset. Clampa defensivamente para um asset mal
        /// preenchido não derrubar a partida (Regra de Ouro 7 — fallback seguro).
        /// </summary>
        public DefinicaoDeItem CriarDefinicao() => new DefinicaoDeItem(
            id: string.IsNullOrWhiteSpace(id) ? name : id,   // cai no nome do asset se vazio
            nome: nome,
            descricao: descricao,
            pilhaMaxima: Mathf.Max(1, pilhaMaxima),
            efeito: efeito,
            potencia: Mathf.Max(0f, potencia),
            armaEquipavel: ehArma ? arma : (ArmaDaTumba?)null);
    }
}
