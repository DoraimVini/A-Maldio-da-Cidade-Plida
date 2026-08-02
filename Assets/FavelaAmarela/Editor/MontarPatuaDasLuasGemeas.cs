using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using FavelaAmarela.Core.Itens;
using FavelaAmarela.Runtime.Itens;
using FavelaAmarela.Runtime.Quests;
using FavelaAmarela.Runtime.Rendering;
using FavelaAmarela.Runtime.UI;

namespace FavelaAmarela.EditorTools
{
    /// <summary>
    /// Ferramenta de Editor. Cria o <b>Patuá das Luas Gêmeas</b> — a recompensa da quest de
    /// Cassilda — e o liga ao NPC. Sem isto, concluir a quest mostrava a fala final e
    /// <b>não entregava nada</b>.
    ///
    /// <para>Cria três coisas: o <c>ItemConfig</c> (o que o item é), o prefab do coletável
    /// (o que Cassilda larga no chão) e a ligação no campo <c>prefabPatua</c> dela.</para>
    ///
    /// <para><b>Não confundir com o patuá da Tumba</b> (<c>Patua_Pickup.prefab</c>): são
    /// itens diferentes. Aquele perdeu o efeito quando o Salto Dimensional saiu do jogo e
    /// segue sem propósito definido; este tem efeito no GDD (−40% de dreno de RM no escuro).</para>
    ///
    /// <para>Idempotente: reaproveita os assets se já existirem.</para>
    /// </summary>
    public static class MontarPatuaDasLuasGemeas
    {
        // Cassilda mora dentro do Santuário desde que ele virou cena própria (2026-08-02).
        private const string CenaDaCassilda = "Assets/Scenes/Santuario_Yhtill.unity";
        private const string PastaItens = "Assets/FavelaAmarela/Config/Itens";
        private const string PastaPrefabs = "Assets/FavelaAmarela/Art/Items";
        private const string CaminhoItem = PastaItens + "/Item_PatuaDasLuasGemeas.asset";
        private const string CaminhoPrefab = PastaPrefabs + "/Patua_DasLuasGemeas.prefab";
        private const string SpritePatua = PastaPrefabs + "/Patua.png";

        private const string Descricao =
            "Fios das vestes de Yhtill, tecidos sob duas luas que não piscam. " +
            "Desacelera o que o escuro faz com a sua mente.";

        [MenuItem("Tools/FavelaAmarela/Montar Patua das Luas Gemeas")]
        public static void Executar()
        {
            var item = GarantirItemConfig();
            var prefab = GarantirPrefab(item);
            LigarNaCassilda(prefab);
        }

        private static ItemConfig GarantirItemConfig()
        {
            var existente = AssetDatabase.LoadAssetAtPath<ItemConfig>(CaminhoItem);
            if (existente != null) return existente;

            if (!AssetDatabase.IsValidFolder(PastaItens))
                AssetDatabase.CreateFolder("Assets/FavelaAmarela/Config", "Itens");

            var item = ScriptableObject.CreateInstance<ItemConfig>();
            var so = new SerializedObject(item);
            so.FindProperty("id").stringValue = "patua_luas_gemeas";
            so.FindProperty("nome").stringValue = "Patuá das Luas Gêmeas";
            so.FindProperty("descricao").stringValue = Descricao;
            so.FindProperty("pilhaMaxima").intValue = 1;   // relíquia: só existe uma

            // Efeito Nenhum: é relíquia passiva, não consumível. Não some ao ser "usada".
            so.FindProperty("efeito").enumValueIndex = (int)TipoDeEfeito.Nenhum;

            var icone = AssetDatabase.LoadAssetAtPath<Sprite>(SpritePatua);
            if (icone != null) so.FindProperty("icone").objectReferenceValue = icone;
            so.ApplyModifiedPropertiesWithoutUndo();

            AssetDatabase.CreateAsset(item, CaminhoItem);
            AssetDatabase.SaveAssets();

            Debug.Log($"[Patuá] ItemConfig criado em {CaminhoItem}.", item);
            return item;
        }

        private static GameObject GarantirPrefab(ItemConfig item)
        {
            var existente = AssetDatabase.LoadAssetAtPath<GameObject>(CaminhoPrefab);
            if (existente != null) return existente;

            var go = new GameObject("Patua_DasLuasGemeas");

            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = AssetDatabase.LoadAssetAtPath<Sprite>(SpritePatua);
            if (sr.sprite == null)
                Debug.LogWarning($"[Patuá] Sprite não encontrado em {SpritePatua} — " +
                                 "o coletável fica invisível.");

            go.AddComponent<DynamicYSort>();

            var col = go.AddComponent<CircleCollider2D>();
            col.isTrigger = true;
            col.radius = 0.6f;

            var coletavel = go.AddComponent<ColetavelDeItem>();
            var so = new SerializedObject(coletavel);
            so.FindProperty("item").objectReferenceValue = item;
            so.FindProperty("chaveDeSave").stringValue = "Quest.Cassilda.PatuaRecolhido";
            so.FindProperty("mensagem").stringValue =
                "Você recolheu o Patuá das Luas Gêmeas. Ele pesa menos do que deveria.";
            so.ApplyModifiedPropertiesWithoutUndo();

            var prefab = PrefabUtility.SaveAsPrefabAsset(go, CaminhoPrefab);
            Object.DestroyImmediate(go);

            Debug.Log($"[Patuá] Prefab criado em {CaminhoPrefab}.", prefab);
            return prefab;
        }

        private static void LigarNaCassilda(GameObject prefab)
        {
            // Salva sem perguntar: um diálogo modal travaria a Unity vinda da ponte MCP.
            var cenaAtiva = EditorSceneManager.GetActiveScene();
            if (cenaAtiva.isDirty && !string.IsNullOrEmpty(cenaAtiva.path))
                EditorSceneManager.SaveScene(cenaAtiva);

            string cenaOriginal = cenaAtiva.path;
            var cena = EditorSceneManager.OpenScene(CenaDaCassilda, OpenSceneMode.Single);

            var cassilda = Object.FindAnyObjectByType<CassildaNPC>(FindObjectsInactive.Include);
            if (cassilda == null)
            {
                Debug.LogError("[Patuá] Cassilda não encontrada no Deserto — nada foi ligado.");
            }
            else
            {
                var so = new SerializedObject(cassilda);
                so.FindProperty("prefabPatua").objectReferenceValue = prefab;
                so.ApplyModifiedPropertiesWithoutUndo();
                EditorUtility.SetDirty(cassilda);

                EditorSceneManager.MarkSceneDirty(cena);
                EditorSceneManager.SaveScene(cena);
                Debug.Log("[Patuá] Ligado em Cassilda — a quest agora entrega recompensa.", cassilda);
            }

            if (!string.IsNullOrEmpty(cenaOriginal) && cenaOriginal != CenaDaCassilda)
                EditorSceneManager.OpenScene(cenaOriginal, OpenSceneMode.Single);
        }
    }
}
