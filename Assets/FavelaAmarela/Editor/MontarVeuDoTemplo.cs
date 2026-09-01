using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using FavelaAmarela.Inventario;
using FavelaAmarela.Runtime.Environment;
using FavelaAmarela.Runtime.Itens;

namespace FavelaAmarela.EditorTools
{
    /// <summary>
    /// Monta o <b>véu da tempestade sobre o Templo do Povo Serpente</b> e a <b>Carta das
    /// Areias</b> que o atravessa.
    ///
    /// <para><b>A ideia é do Vini (2026-09-01):</b> <i>"vamos esconder o templo debaixo da
    /// tempestade — se não tiver o mapa, a tempestade joga o Damião para outro canto."</i></para>
    ///
    /// <para><b>Onde a carta fica, e por quê.</b> No <b>Santuário de Yhtill</b>, a noroeste. É
    /// caminho crítico — a quest da Cassilda leva o jogador até lá —, fica no lado <b>oposto</b>
    /// ao Templo, e o Santuário é, na ficção, o lugar onde alguém <i>estudou</i> o deserto. O
    /// jogador que tenta o leste cedo demais aprende que falta algo; quem segue a história ganha
    /// a chave sem saber que ganhou.</para>
    ///
    /// <para><b>Isto é proposta de colocação</b>, não decreto: mover o coletável de lugar é
    /// arrastar um objeto na cena, e a mecânica não depende de onde ele está.</para>
    /// </summary>
    public static class MontarVeuDoTemplo
    {
        private const string Marcador = "[VeuDoTemplo]";
        private const string Cena = "Assets/Scenes/Deserto_Hali.unity";
        private const string PastaDeItens = "Assets/FavelaAmarela/Config/Resources/Itens";

        private const string IdDaCarta = "Item_Chave_CartaDasAreias";

        [MenuItem("Tools/FavelaAmarela/Deserto: esconder o Templo sob a tempestade")]
        public static void Executar()
        {
            var resumo = new List<string> { CriarACarta() };

            var cena = EditorSceneManager.OpenScene(Cena, OpenSceneMode.Single);

            resumo.Add(MontarOVeu(cena));
            resumo.Add(SemearACarta(cena));

            EditorSceneManager.MarkSceneDirty(cena);
            EditorSceneManager.SaveScene(cena);
            AssetDatabase.SaveAssets();

            string quebra = System.Environment.NewLine + "  ";
            Debug.Log($"{Marcador} Concluído:" + quebra + string.Join(quebra, resumo));
        }

        // ── A carta ───────────────────────────────────────────────────────────

        private static string CriarACarta()
        {
            string caminho = $"{PastaDeItens}/{IdDaCarta}.asset";

            var def = AssetDatabase.LoadAssetAtPath<ItemDef>(caminho);
            bool nova = def == null;

            if (nova)
            {
                def = ScriptableObject.CreateInstance<ItemDef>();
                AssetDatabase.CreateAsset(def, caminho);
            }

            def.Id = IdDaCarta;
            def.Nome = "Carta das Areias";

            // Chave, e não Consumível: não se gasta, e não deve ocupar slot da barra de itens.
            def.Tipo = ItemType.Chave;
            def.SlotEquipamento = EquipmentSlot.Nenhum;

            EditorUtility.SetDirty(def);
            AssetDatabase.SaveAssetIfDirty(def);

            return $"{IdDaCarta} [{(nova ? "CRIADA" : "atualizada")}]: 'Carta das Areias', " +
                   "item de Chave — não se gasta e não ocupa a barra";
        }

        // ── O véu ─────────────────────────────────────────────────────────────

        private static string MontarOVeu(UnityEngine.SceneManagement.Scene cena)
        {
            var entrada = cena.GetRootGameObjects()
                .SelectMany(r => r.GetComponentsInChildren<Transform>(true))
                .FirstOrDefault(t => t.name.Contains("TemploSerpente") &&
                                     t.name.StartsWith("Entrada"));

            if (entrada == null)
                return "véu: 'Entrada_TemploSerpente' não encontrada — nada montado";

            // A ALTURA vem dos limites do mapa, não de um número. A primeira versão usava
            // 14×26 fixo, e a medição mostrou o estrago: sobravam 29 unidades de corredor livre
            // por baixo e 7 por cima -- o jogador simplesmente contornava, e o véu não vedava
            // nada. Um véu que se pode contornar é pior que nenhum: promete uma regra e não a
            // cumpre.
            var limites = cena.GetRootGameObjects()
                .SelectMany(r => r.GetComponentsInChildren<Transform>(true))
                .Where(t => t.name.StartsWith("Limite_"))
                .ToArray();

            if (limites.Length == 0) return "véu: sem Limite_* para medir a faixa — nada montado";

            float alturaDoMapa = limites.Max(t => Mathf.Abs(t.position.y)) * 2f + 4f;

            // A faixa começa a OESTE da entrada, mas depois dos consumíveis que vivem no leste
            // (x 23,5 e 27,4): vedá-los junto tornaria dois dos nove consumíveis do Deserto
            // reféns da carta, e consumível é finito e não-farmável neste jogo.
            const float ComecoDaFaixa = 31f;

            float bordaLeste = limites.Max(t => Mathf.Abs(t.position.x)) + 2f;
            float larguraDaFaixa = bordaLeste - ComecoDaFaixa;
            float centroX = ComecoDaFaixa + larguraDaFaixa / 2f;

            var existente = cena.GetRootGameObjects()
                .SelectMany(r => r.GetComponentsInChildren<VeuDaTempestade>(true))
                .FirstOrDefault();

            var go = existente != null
                ? existente.gameObject
                : new GameObject("Veu_DaTempestade_Templo");

            if (existente == null)
            {
                go.transform.SetParent(entrada.parent, worldPositionStays: true);
                go.AddComponent<BoxCollider2D>();
                go.AddComponent<VeuDaTempestade>();
            }

            go.transform.position = new Vector3(centroX, 0f, 0f);

            var col = go.GetComponent<BoxCollider2D>();
            col.isTrigger = true;
            col.size = new Vector2(larguraDaFaixa, alturaDoMapa);

            return $"véu: '{go.name}' {(existente != null ? "REDIMENSIONADO" : "criado")} em " +
                   $"x {ComecoDaFaixa:0}..{bordaLeste:0}, altura {alturaDoMapa:0} — veda a faixa " +
                   $"leste INTEIRA, sem passagem por cima nem por baixo, e sem prender os dois " +
                   "consumíveis que vivem em x 23,5 e 27,4";
        }

        // ── A carta, no mundo ─────────────────────────────────────────────────

        private static string SemearACarta(UnityEngine.SceneManagement.Scene cena)
        {
            var def = AssetDatabase.LoadAssetAtPath<ItemDef>($"{PastaDeItens}/{IdDaCarta}.asset");
            if (def == null) return "carta no mundo: ItemDef ausente";

            var raizes = cena.GetRootGameObjects();

            var jaExiste = raizes
                .SelectMany(r => r.GetComponentsInChildren<ColetavelDeItem>(true))
                .FirstOrDefault(c => c.name.Contains("CartaDasAreias"));

            if (jaExiste != null) return "carta no mundo: já semeada";

            var santuario = raizes
                .SelectMany(r => r.GetComponentsInChildren<Transform>(true))
                .FirstOrDefault(t => t.name.Contains("Santuario") &&
                                     !t.name.StartsWith("Setor_"));

            if (santuario == null) return "carta no mundo: marco do Santuário não encontrado";

            var go = new GameObject("Coletavel_CartaDasAreias",
                                    typeof(SpriteRenderer), typeof(BoxCollider2D));

            go.transform.SetParent(santuario.parent, worldPositionStays: true);
            go.transform.position = santuario.position + new Vector3(2.5f, -1.5f, 0f);

            var col = go.GetComponent<BoxCollider2D>();
            col.isTrigger = true;
            col.size = Vector2.one;

            // Nasce inativo: o Awake do ColetavelDeItem exige um ItemDef, e AddComponent o
            // dispararia ANTES do Configurar. Mesmo cuidado do DropAoAbater.
            go.SetActive(false);

            var coletavel = go.AddComponent<ColetavelDeItem>();

            // Chave de save própria: recolher a carta tem de ser lembrado entre sessões, senão
            // ela renasce e o jogador recolhe a mesma coisa para sempre.
            coletavel.Configurar(def, 1, "Deserto.CartaDasAreias.Recolhida");

            go.SetActive(true);

            var sr = go.GetComponent<SpriteRenderer>();
            if (def.Icone != null) sr.sprite = def.Icone;
            sr.sortingOrder = Mathf.RoundToInt(-go.transform.position.y * 10f);

            return $"carta no mundo: semeada em {go.transform.position} (junto ao Santuário de " +
                   "Yhtill, lado OPOSTO ao Templo)";
        }
    }
}
