using System;

namespace FavelaAmarela.Core.Persistence
{
    /// <summary>
    /// DTO serializável do estado de progresso de Damião — o "esqueleto" do save
    /// (Fase 1, Slice 4). POCO puro: usa apenas <see cref="SerializableAttribute"/>
    /// do .NET, sem dependência de UnityEngine. A serialização JSON em si é feita
    /// pelo adaptador Runtime <c>SaveSystem</c> (via <c>JsonUtility</c>).
    ///
    /// Campos públicos em camelCase porque o <c>JsonUtility</c> só serializa campos
    /// públicos (ou <c>[SerializeField]</c>) — é a convenção de "campos serializados"
    /// da seção 4 do CLAUDE.md, não propriedades.
    /// </summary>
    [Serializable]
    public sealed class SaveData
    {
        /// <summary>
        /// Versão do formato do save. Permite migrar/rejeitar saves antigos quando
        /// os campos mudarem no futuro. Começa em 1.
        /// </summary>
        public int versao = 1;

        /// <summary>Resiliência Mental corrente no momento do save.</summary>
        public float resilienciaAtual;

        /// <summary>Se o Salto Dimensional já foi destravado (patuá da Zona 5).</summary>
        public bool saltoDesbloqueado;

        /// <summary>Se a Barra Enferrujada (Mão Física) já foi adquirida.</summary>
        public bool armaDesbloqueada;

        /// <summary>Posição X de Damião no mundo no momento do save.</summary>
        public float posX;

        /// <summary>Posição Y de Damião no mundo no momento do save.</summary>
        public float posY;
    }
}
