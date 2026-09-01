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

            var existente = cena.GetRootGameObjects()
                .SelectMany(r => r.GetComponentsInChildren<VeuDaTempestade>(true))
                .FirstOrDefault();

            if (existente != null)
                return $"véu: já existe ('{existente.name}')";

            var go = new GameObject("Veu_DaTempestade_Templo");
            go.transform.SetParent(entrada.parent, worldPositionStays: true);

            // Centrado um pouco a OESTE da entrada: o véu tem de ser atravessado antes de
            // chegar, senão o jogador vê o Templo e só então é devolvido -- o que frustra em vez
            // de intrigar.
            go.transform.position = entrada.position + new Vector3(-6f, 0f, 0f);

            var col = go.AddComponent<BoxCollider2D>();
            col.isTrigger = true;
            col.size = new Vector2(14f, 26f);

            go.AddComponent<VeuDaTempestade>();

            return $"véu: 'Veu_DaTempestade_Templo' criado em {go.transform.position}, " +
                   "14×26, a oeste da entrada do Templo";
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
