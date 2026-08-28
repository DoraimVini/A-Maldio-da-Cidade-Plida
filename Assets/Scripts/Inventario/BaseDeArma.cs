using UnityEngine;
using FavelaAmarela.Core.Combat;
using FavelaAmarela.Core.Progression;
using FavelaAmarela.Core.Abilities;

namespace FavelaAmarela.Inventario
{
    /// <summary>
    /// A <b>família</b> de uma arma — o que o gênero chama de <i>base type</i> e o que
    /// D2, PoE, Grim Dawn e Elden Ring usam para que arma nova seja conteúdo, não código.
    ///
    /// <para><b>O buraco que isto fecha.</b> Até 2026-08-27, o <c>ItemDef</c> de uma arma não
    /// continha <b>um único número de combate</b> — só um valor de enum. Alcance e forma do
    /// golpe eram um campo do <c>MaoFisicaBridge</c>: <c>alcance = 1.2f</c>, <b>um número só
    /// para todas as armas</b>. O Estilete de Irem (lâmina fina) e o Alfanje de Alhazred (peso
    /// e espaço) tinham exatamente a mesma pegada, a mesma geometria e a mesma sensação. Só os
    /// números de dano diferiam.</para>
    ///
    /// <para>Num ARPG, <b>trocar de arma tem de ser sentido antes de ser lido</b>. É por isso
    /// que espada reta, montante e katana são coisas diferentes no Elden Ring mesmo com dano
    /// igual: o moveset é a identidade. Esta é a camada que faltava.</para>
    ///
    /// <para><b>Ainda não é a arma inteira.</b> Habilidade e números de dano seguem nos POCOs de
    /// <c>Core.Abilities</c> até a Fase 3/4 do plano (<c>habilidades_de_item.md</c>). Esta base
    /// carrega, por enquanto, o que governa <b>como o golpe ocupa espaço e tempo</b>.</para>
    /// </summary>
    [CreateAssetMenu(menuName = "Favela Amarela/Base de Arma", fileName = "BaseArma_")]
    public sealed class BaseDeArma : ScriptableObject
    {
        [Header("Identidade")]
        [Tooltip("Nome da família, para leitura humana. Ex.: \"Lâmina fina\", \"Alfanje\".")]
        public string NomeDaFamilia;

        [Header("Como o golpe ocupa espaço")]
        [Tooltip("Distância do corpo até o CENTRO da área do golpe. Adaga é curta; alfanje " +
                 "alcança. É o número que mais muda a sensação de uma arma.")]
        [Min(0.1f)]
        public float Alcance = 1.2f;

        [Tooltip("Raio da área atingida. Lâmina fina fura um ponto; alfanje varre um arco.")]
        [Min(0.05f)]
        public float Raio = 0.6f;

        [Header("Como o golpe ocupa tempo")]
        [Tooltip("Quanto tempo a área fica ativa. Janela curta exige mira; janela longa " +
                 "perdoa. Zero faria o golpe existir por um quadro só e parecer que falhou.")]
        [Min(0.02f)]
        public float JanelaAtiva = 0.1f;

        [Header("Regras de porte")]
        [Tooltip("Quantas mãos a arma toma. DuasMaos bloqueia a Mão Secundária.")]
        public Empunhadura Empunhadura = Empunhadura.UmaMao;

        [Header("Dano branco")]
        [Tooltip("Piso do dano da arma no nível 1. Dano de ARPG é FAIXA, não número fixo: é o " +
                 "intervalo que dá textura a golpes repetidos e o que os afixos de aumento " +
                 "percentual multiplicam.")]
        [Min(0f)]
        public float DanoMinBase = 8f;

        [Tooltip("Teto do dano da arma no nível 1.")]
        [Min(0f)]
        public float DanoMaxBase = 14f;

        [Header("Crítico e precisão")]
        [Tooltip("Chance de o golpe sair crítico, em fração (0,05 = 5%).")]
        [Range(0f, 1f)]
        public float ChanceCriticaBase = 0.05f;

        [Tooltip("Quanto o crítico multiplica o dano (1,5 = +50%).")]
        [Min(1f)]
        public float MultiplicadorCritico = 1.5f;

        [Tooltip("Chance de acertar, em fração. Falhar aqui é ERRAR de verdade — dano zero, " +
                 "não golpe de raspão (decisão do Vini, 2026-08-28). Arma pesada erra mais.")]
        [Range(0f, 1f)]
        public float PrecisaoBase = 0.90f;

        [Header("Comportamento")]
        [Tooltip("A habilidade desta arma, montada por EFEITOS no Inspector. [ASSET] " +
                 "Sem ela a arma é equipável e inerte — a bridge reclama alto ao equipar.")]
        public HabilidadeDef Habilidade;

        // O campo `Arquetipo` (TipoArmaFisica) saiu em 2026-08-27, junto com as três classes
        // de arma e a WeaponFactory. Ele era o caminho legado -- "se não houver HabilidadeDef,
        // construa a classe C#" --, e manter um caminho paralelo depois de a migração estar
        // provada equivalente seria manter viva a duplicação que a migração existiu para
        // remover.

        /// <summary>
        /// Constrói o POCO de combate desta arma. <b>É o único lugar que monta uma arma</b> —
        /// ter essa lógica espalhada seria criar mais uma divergência para manter à mão.
        /// </summary>
        /// <returns>
        /// A arma, ou <c>null</c> quando não há habilidade autorada — que é o mesmo que estar
        /// desarmado, e é o comportamento que a bridge já sabe tratar (e denunciar).
        /// </returns>
        public IArmaComHabilidade ConstruirArma(int nivelDoItem = 1) => Habilidade != null
            ? Habilidade.Construir(PerfilNoNivel(nivelDoItem))
            : null;

        /// <summary>
        /// O bloco de combate desta arma no nível pedido.
        ///
        /// <para>Hoje o nível só multiplica a faixa de dano; crítico e precisão são identidade
        /// da <b>família</b> e não crescem sozinhos — quem os aumenta é afixo. É o que mantém
        /// um alfanje sempre mais lento e mais errático que um estilete, por mais alto que seja
        /// o tier.</para>
        /// </summary>
        public PerfilDeArma PerfilNoNivel(int nivelDoItem)
        {
            float fator = EscalaDeNivel.FatorDeDano(nivelDoItem);

            return new PerfilDeArma(
                DanoMinBase * fator, DanoMaxBase * fator,
                ChanceCriticaBase, MultiplicadorCritico, PrecisaoBase);
        }

        /// <summary>
        /// Geometria padrão para quem não tem base ligada — os mesmos números que viviam
        /// codificados no <c>MaoFisicaBridge</c>, para o caminho antigo não mudar de
        /// comportamento enquanto a migração não termina.
        /// </summary>
        public const float AlcancePadrao = 1.2f;

        /// <inheritdoc cref="AlcancePadrao"/>
        public const float RaioPadrao = 0.6f;

        /// <inheritdoc cref="AlcancePadrao"/>
        public const float JanelaPadrao = 0.1f;
    }
}
