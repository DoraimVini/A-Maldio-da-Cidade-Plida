using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
using FavelaAmarela.Inventario;

namespace FavelaAmarela.EditorTools
{
    /// <summary>
    /// Autora a <b>escada de armas</b>: três famílias em três degraus cada.
    ///
    /// <para><b>O defeito que isto conserta (2026-09-01).</b> Medido: o jogo inteiro tem
    /// <b>três armas</b> — as três do baú da Tumba, entregues no começo. Depois dele, não existe
    /// no jogo uma arma que o jogador já não tenha. Todo drop é uma cópia de uma das três.</para>
    ///
    /// <para>Somado à curva de grau, que no nível 1 dá <b>80,6% de Inerte</b> — e Inerte
    /// significa "sem modificadores" —, o resultado é que <b>oito em cada dez drops são uma arma
    /// repetida sem afixo nenhum</b>. Foi exatamente o relato do Vini: <i>"todos os drops
    /// fracos"</i> e <i>"não temos evolução de itens"</i>. As duas queixas são o mesmo defeito
    /// visto de dois ângulos, e nenhuma delas é matemática: é <b>catálogo</b>.</para>
    ///
    /// <para><b>O tier muda SÓ a faixa de dano.</b> Crítico, precisão, cadência, alcance e
    /// habilidade continuam os da família. É a decisão de design que mantém as três identidades
    /// vivas em todos os degraus: o Alfanje continua sendo o que erra e explode, e o Estilete o
    /// que quase nunca erra e quase nunca dói — <b>em qualquer tier</b>. Escalar tudo junto
    /// borraria as três numa só, com números maiores.</para>
    ///
    /// <para><b>Passo de ×1,45 por tier</b>, contra ×1,25 por nível de item. Achar um tier
    /// precisa valer mais que subir um nível, senão o degrau não é um evento.</para>
    /// </summary>
    public static class MontarEscadaDeArmas
    {
        private const string Marcador = "[EscadaDeArmas]";
        private const string PastaDeBases = "Assets/FavelaAmarela/Config/Armas";
        private const string PastaDeItens = "Assets/FavelaAmarela/Config/Resources/Itens";

        /// <summary>Quanto cada degrau multiplica a faixa de dano da família.</summary>
        private const float PassoDeTier = 1.45f;

        private readonly struct Degrau
        {
            public readonly string BaseExistente, NomeDaBase, NomeDoItem, NomeVisivel, Onde;
            public readonly int Tier;

            public Degrau(string baseExistente, int tier, string nomeDaBase, string nomeDoItem,
                          string nomeVisivel, string onde)
            {
                BaseExistente = baseExistente; Tier = tier;
                NomeDaBase = nomeDaBase; NomeDoItem = nomeDoItem;
                NomeVisivel = nomeVisivel; Onde = onde;
            }
        }

        /// <summary>
        /// Os seis degraus novos. Os nomes seguem o padrão das três armas originais —
        /// <i>forma + origem</i> — e a origem <b>se aproxima de Carcosa</b> a cada tier, que é a
        /// forma diegética de dizer "mais fundo": Alhazred era um homem; o Rei não é.
        ///
        /// <para>Todos os topônimos são léxico já estabelecido no projeto (Ruínas Pálidas,
        /// Aldebaran, Sinal Amarelo, Yhtill, Máscara Pálida) — nenhum foi inventado aqui.</para>
        /// </summary>
        private static readonly Degrau[] Degraus =
        {
            // ── Alfanje: pesado, erra mais, crítico devastador ────────────────
            new Degrau("BaseArma_Alfanje", 2, "BaseArma_Alfanje_T2",
                       "Item_Arma_AlfanjeDasRuinasPalidas", "Alfanje das Ruínas Pálidas",
                       "Fases 2–4"),

            new Degrau("BaseArma_Alfanje", 3, "BaseArma_Alfanje_T3",
                       "Item_Arma_AlfanjeDoRei", "Alfanje do Rei",
                       "Castelo de Carcosa"),

            // ── Cravo: equilibrado ────────────────────────────────────────────
            new Degrau("BaseArma_Cravo", 2, "BaseArma_Cravo_T2",
                       "Item_Arma_CravoDeAldebaran", "Cravo de Aldebaran",
                       "Fases 2–4"),

            new Degrau("BaseArma_Cravo", 3, "BaseArma_Cravo_T3",
                       "Item_Arma_CravoDoSinalAmarelo", "Cravo do Sinal Amarelo",
                       "Castelo de Carcosa"),

            // ── Lâmina fina: rápida, precisa, pouco dano por golpe ────────────
            new Degrau("BaseArma_LaminaFina", 2, "BaseArma_LaminaFina_T2",
                       "Item_Arma_EstileteDeYhtill", "Estilete de Yhtill",
                       "Fases 2–4"),

            new Degrau("BaseArma_LaminaFina", 3, "BaseArma_LaminaFina_T3",
                       "Item_Arma_EstileteDaMascaraPalida", "Estilete da Máscara Pálida",
                       "Castelo de Carcosa"),
        };

        [MenuItem("Tools/FavelaAmarela/Itens: montar a escada de armas")]
        public static void Executar()
        {
            var resumo = new List<string>();

            foreach (var d in Degraus) resumo.Add(Aplicar(d));

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            string quebra = System.Environment.NewLine + "  ";
            Debug.Log($"{Marcador} Concluído:" + quebra + string.Join(quebra, resumo));
        }

        private static string Aplicar(Degrau d)
        {
            var original = AssetDatabase.LoadAssetAtPath<BaseDeArma>(
                $"{PastaDeBases}/{d.BaseExistente}.asset");

            if (original == null) return $"{d.NomeDaBase}: base '{d.BaseExistente}' não existe";

            float fator = Mathf.Pow(PassoDeTier, d.Tier - 1);

            // ── A família, um degrau acima ────────────────────────────────────
            string caminhoBase = $"{PastaDeBases}/{d.NomeDaBase}.asset";
            var nova = AssetDatabase.LoadAssetAtPath<BaseDeArma>(caminhoBase);
            bool baseNova = nova == null;

            if (baseNova)
            {
                nova = ScriptableObject.CreateInstance<BaseDeArma>();
                AssetDatabase.CreateAsset(nova, caminhoBase);
            }

            nova.NomeDaFamilia = original.NomeDaFamilia;

            // Geometria, tempo e porte: IDÊNTICOS. O tier não muda como a arma se sente na mão.
            nova.Alcance = original.Alcance;
            nova.Raio = original.Raio;
            nova.JanelaAtiva = original.JanelaAtiva;
            nova.Empunhadura = original.Empunhadura;
            nova.Entrega = original.Entrega;

            // A HABILIDADE ganha asset próprio por degrau. Ela carrega o NOME DA ARMA, que é
            // texto visível ao jogador: reusar a do T1 faria o "Alfanje do Rei" anunciar a
            // habilidade do "Alfanje de Alhazred". O guarda
            // GeometriaDeArmaTests.AFamilia_EstaLigadaAHabilidadeDaPropriaArma pegou isso.
            //
            // Só o nome da arma muda. O NOME e o PERCENTUAL da habilidade continuam os da
            // família -- "Golpe do Deserto" é do Alfanje, em qualquer degrau, e o tier já sobe
            // o dano pela faixa branca.
            nova.Habilidade = HabilidadeDoDegrau(original.Habilidade, d);

            // Identidade de combate: IDÊNTICA. É o que mantém a escolha entre famílias viva em
            // todos os tiers, em vez de as três convergirem para "a mais forte".
            nova.ChanceCriticaBase = original.ChanceCriticaBase;
            nova.MultiplicadorCritico = original.MultiplicadorCritico;
            nova.PrecisaoBase = original.PrecisaoBase;

            // Só a faixa sobe.
            nova.DanoMinBase = Mathf.Round(original.DanoMinBase * fator);
            nova.DanoMaxBase = Mathf.Round(original.DanoMaxBase * fator);

            EditorUtility.SetDirty(nova);
            AssetDatabase.SaveAssetIfDirty(nova);

            // ── O item que aponta para ela ────────────────────────────────────
            string caminhoItem = $"{PastaDeItens}/{d.NomeDoItem}.asset";
            var item = AssetDatabase.LoadAssetAtPath<ItemDef>(caminhoItem);
            bool itemNovo = item == null;

            if (itemNovo)
            {
                item = ScriptableObject.CreateInstance<ItemDef>();
                AssetDatabase.CreateAsset(item, caminhoItem);
            }

            var modeloDoItem = ItemDaFamilia(d.BaseExistente);

            item.Id = d.NomeDoItem;
            item.Nome = d.NomeVisivel;
            item.Tipo = ItemType.Arma;
            item.Base = nova;

            if (modeloDoItem != null)
            {
                // Herda slot, ícone, empilhamento e o TipoArmaFisica do irmão de T1: são
                // propriedades da FAMÍLIA, não do degrau. Sem isto o item novo nasceria sem
                // slot de equipamento -- equipar e continuar desarmado.
                item.SlotEquipamento = modeloDoItem.SlotEquipamento;
                item.Icone = modeloDoItem.Icone;
                item.Empunhadura = modeloDoItem.Empunhadura;
                item.ArmaFisica = modeloDoItem.ArmaFisica;
            }

            EditorUtility.SetDirty(item);
            AssetDatabase.SaveAssetIfDirty(item);

            float esperado = Esperado(nova);

            return $"{d.NomeVisivel} [T{d.Tier}]: {nova.DanoMinBase:0}–{nova.DanoMaxBase:0} " +
                   $"(esperado {esperado:0.#})" +
                   (modeloDoItem == null ? "  <<< SEM modelo de T1: slot e ícone em branco" : "") +
                   $" — fonte prevista: {d.Onde}";
        }

        private const string PastaDeHabilidades = "Assets/FavelaAmarela/Config/Habilidades";

        /// <summary>
        /// Clona a habilidade da família trocando <b>só o nome da arma</b>.
        ///
        /// <para>O <c>HabilidadeDef</c> é um asset por família, e carrega
        /// <c>NomeDaArma</c> — texto que o jogador lê. Sem um asset por degrau, todo tier
        /// anunciaria o nome do T1.</para>
        /// </summary>
        private static HabilidadeDef HabilidadeDoDegrau(HabilidadeDef original, Degrau d)
        {
            if (original == null) return null;

            Directory.CreateDirectory(PastaDeHabilidades);

            string caminho = $"{PastaDeHabilidades}/Habilidade_{d.NomeDoItem.Replace("Item_Arma_", "")}.asset";

            var nova = AssetDatabase.LoadAssetAtPath<HabilidadeDef>(caminho);
            bool novaHabilidade = nova == null;

            if (novaHabilidade)
            {
                nova = ScriptableObject.CreateInstance<HabilidadeDef>();
                AssetDatabase.CreateAsset(nova, caminho);
            }

            nova.NomeDaArma = d.NomeVisivel;

            // Tudo o mais é da FAMÍLIA e fica idêntico: o nome da habilidade, a cadência, a
            // duração e os efeitos. O degrau já sobe o dano pela faixa branca.
            nova.NomeDaHabilidade = original.NomeDaHabilidade;
            nova.DuracaoBasico = original.DuracaoBasico;
            nova.CooldownBasico = original.CooldownBasico;
            nova.DuracaoHabilidade = original.DuracaoHabilidade;
            nova.CooldownHabilidade = original.CooldownHabilidade;
            nova.EfeitosDoBasico = new List<EfeitoAutorado>(original.EfeitosDoBasico);
            nova.EfeitosDaHabilidade = new List<EfeitoAutorado>(original.EfeitosDaHabilidade);

            EditorUtility.SetDirty(nova);
            AssetDatabase.SaveAssetIfDirty(nova);

            return nova;
        }

        /// <summary>O item de T1 da mesma família, usado como molde das propriedades de família.</summary>
        private static ItemDef ItemDaFamilia(string nomeDaBase)
        {
            var baseT1 = AssetDatabase.LoadAssetAtPath<BaseDeArma>(
                $"{PastaDeBases}/{nomeDaBase}.asset");

            if (baseT1 == null) return null;

            return AssetDatabase.FindAssets("t:ItemDef", new[] { PastaDeItens })
                .Select(AssetDatabase.GUIDToAssetPath)
                .Select(AssetDatabase.LoadAssetAtPath<ItemDef>)
                .FirstOrDefault(i => i != null && i.Base == baseT1);
        }

        /// <summary>Valor esperado do golpe: média da faixa, corrigida por precisão e crítico.</summary>
        private static float Esperado(BaseDeArma b)
        {
            float media = (b.DanoMinBase + b.DanoMaxBase) * 0.5f;
            return media * b.PrecisaoBase *
                   (1f + b.ChanceCriticaBase * (b.MultiplicadorCritico - 1f));
        }
    }
}
