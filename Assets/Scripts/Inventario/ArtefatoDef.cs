// Assets/Scripts/Inventario/ArtefatoDef.cs
using System.Collections.Generic;
using UnityEngine;
using FavelaAmarela.Core.Artefatos;

namespace FavelaAmarela.Inventario
{
    /// <summary>Qual efeito a habilidade de um Artefato dispara. Cresce devagar e de propósito.</summary>
    public enum TipoDeEfeitoDeArtefato
    {
        /// <summary>Revela entidades através da parede (Necronomicon).</summary>
        Revelacao = 0,

        /// <summary>Devolve Resiliência Mental de uma vez (Patuá).</summary>
        Ancoragem = 1,

        /// <summary>Cala os passos de Damião (Anel do Sinal Amarelo).</summary>
        Silencio = 2,

        /// <summary>Faz os serpentinos hesitarem (Coroa de Ossos).</summary>
        Aplacamento = 3
    }

    /// <summary>
    /// Definição autorada de um Artefato: a passiva que ele concede enquanto equipado e a
    /// habilidade ativa que ele coloca na barra.
    ///
    /// <para>Artefato <b>não ocupa slot de corpo</b> — não compete com arma e armadura. Ele
    /// vive no inventário de Artefatos, de quatro slots, e só vale enquanto encaixado lá.</para>
    ///
    /// <para>Espelha o <c>EcoDef</c> (dado que concede passivas) e a relação
    /// <c>FichaAtributosConfig.CriarFicha()</c> → POCO: o <c>ScriptableObject</c> nasce por cima
    /// do Core, nunca o contrário.</para>
    /// </summary>
    [CreateAssetMenu(menuName = "Favela Amarela/Artefato", fileName = "Artefato_")]
    public class ArtefatoDef : ScriptableObject
    {
        [Header("Identidade")]
        [Tooltip("Id estável, usado no save e no inventário de artefatos. Nunca mudar depois de criado.")]
        public string Id;

        public string Nome;
        public Sprite Icone;

        [TextArea(2, 4)]
        public string Descricao;

        [Header("Vínculo com o item coletável")]
        [Tooltip("O ItemDef que, ao ser coletado, concede este Artefato. [ASSET]")]
        public ItemDef Item;

        [Header("Passiva (enquanto equipado)")]
        [Tooltip("Modificadores somados pelo GerenciadorEfeitosPassivos enquanto o Artefato estiver num slot.")]
        public List<ModificadorFixo> Passivas = new List<ModificadorFixo>();

        [Header("Habilidade ativa (um por Artefato)")]
        [Tooltip("Nome diegético mostrado na barra de artefatos.")]
        public string NomeDaHabilidade = "";

        [Tooltip("Qual efeito a habilidade dispara.")]
        public TipoDeEfeitoDeArtefato TipoDeEfeito = TipoDeEfeitoDeArtefato.Revelacao;

        [Min(0f)]
        [Tooltip("Resiliência Mental cobrada por uso.")]
        public float CustoRM = 0f;

        [Min(0f)]
        [Tooltip("Segundos até poder usar de novo.")]
        public float Cooldown = 20f;

        [Min(0f)]
        [Tooltip("Segundos que o efeito dura no mundo. Ignorado pela Ancoragem, que é instantânea.")]
        public float Duracao = 5f;

        [Min(0f)]
        [Tooltip("Alcance do efeito. Ignorado pelos efeitos que não têm área.")]
        public float Raio = 8f;

        [Min(0f)]
        [Tooltip("Intensidade do efeito. Hoje só a Ancoragem usa (quanto de RM devolve).")]
        public float Valor = 0f;

        /// <summary>
        /// Monta a habilidade do Core a partir deste dado. Uma habilidade por Artefato, por
        /// decisão de design — a barra tem quatro slots e precisa continuar legível.
        /// </summary>
        public ArtefatoAtivo CriarAtivo()
        {
            var efeitos = new List<IEfeitoDeArtefato>(1) { CriarEfeito() };
            return new ArtefatoAtivo(NomeDaHabilidade, CustoRM, Cooldown, Duracao, efeitos);
        }

        private IEfeitoDeArtefato CriarEfeito()
        {
            switch (TipoDeEfeito)
            {
                case TipoDeEfeitoDeArtefato.Ancoragem:
                    return new EfeitoDeAncoragem(Valor);
                case TipoDeEfeitoDeArtefato.Silencio:
                    return new EfeitoDeSilencio(Duracao);
                case TipoDeEfeitoDeArtefato.Aplacamento:
                    return new EfeitoDeAplacamento(Raio, Duracao);
                default:
                    return new EfeitoDeRevelacao(Raio, Duracao);
            }
        }

#if UNITY_EDITOR
        private void OnValidate()
        {
            if (string.IsNullOrEmpty(Id))
            {
                Id = System.Guid.NewGuid().ToString();
                UnityEditor.EditorUtility.SetDirty(this);
            }
        }
#endif
    }
}
