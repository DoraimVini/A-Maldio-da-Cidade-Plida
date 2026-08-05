using UnityEngine;
using System.Collections.Generic;
using FavelaAmarela.Inventario;

namespace FavelaAmarela.Progression
{
    public enum TipoEco
    {
        Menor,
        Notavel,
        Keystone,
        Ponte
    }

    public enum CaminhoEco
    {
        Neutro,
        Sobrevivente,
        Ocultista,
        Protetor
    }

    /// <summary>
    /// Define um nó (Eco da Memória) na Árvore do Limiar de Carcosa.
    /// Um Eco fornece um conjunto de Modificadores Fixos ao GerenciadorEfeitosPassivos.
    /// </summary>
    [CreateAssetMenu(fileName = "Eco_", menuName = "Favela Amarela/Progression/Eco da Memória")]
    public class EcoDef : ScriptableObject
    {
        [Header("Identidade (Persistência)")]
        [Tooltip("GUID exclusivo deste Eco. NUNCA mude após criar, pois é salvo no perfil do jogador.")]
        public string Id;

        [Header("Visual e UI")]
        public string NomeDoEco;
        [TextArea] public string Descricao;
        public Sprite Icone;

        [Header("Propriedades do Nó")]
        public TipoEco Tipo = TipoEco.Menor;
        public CaminhoEco Caminho = CaminhoEco.Neutro;
        
        [Tooltip("Lista de Ecos que devem estar ativos para este ser liberado. Vazio se for um nó inicial.")]
        public List<EcoDef> PreRequisitos = new List<EcoDef>();

        [Header("Efeitos Passivos (Bônus e Custos)")]
        [Tooltip("Modificadores aplicados no GerenciadorEfeitosPassivos quando o nó for desbloqueado.")]
        public List<ModificadorFixo> Modificadores = new List<ModificadorFixo>();

        private void Reset()
        {
            if (string.IsNullOrEmpty(Id))
            {
                Id = System.Guid.NewGuid().ToString();
            }
        }
    }
}
