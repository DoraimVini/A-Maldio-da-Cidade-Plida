using System;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace FavelaAmarela.EditorTools
{
    /// <summary>
    /// Utilitário de Editor: monta os pickups de gameplay (Patuá + Arma) num root
    /// <c>Gameplay_Items</c> na RAIZ da cena — fora do <c>Blockout_Root</c>, para que
    /// uma regeneração de blockout (que destrói os filhos do Blockout_Root) NUNCA mais
    /// os apague. Cada pickup ganha SpriteRenderer (visível), Collider2D trigger e o
    /// componente de pickup, com a dica (hintUI) fiada. Idempotente/re-executável.
    /// </summary>
    public static class SetupGameplayItems
    {
        [MenuItem("Tools/FavelaAmarela/Setup Gameplay Items (Patuá + Arma)")]
        public static void Setup()
        {
            var root = GameObject.Find("Gameplay_Items") ?? new GameObject("Gameplay_Items");
            root.transform.SetParent(null); // raiz da cena, nunca sob Blockout_Root

            var hintType = ResolverTipo("FavelaAmarela.Runtime.UI.TutorialHintUI");
            var hint = hintType != null ? UnityEngine.Object.FindAnyObjectByType(hintType) as UnityEngine.Object : null;

            SetupPickup(root, "Patua_Pickup", new Vector3(4f, -37f, 0f),
                "Assets/FavelaAmarela/Art/Items/Patua.png",
                "FavelaAmarela.Runtime.GameLoop.PatuaPickup", hint, obrigatorio: true);

            SetupPickup(root, "Arma_Pickup", new Vector3(6f, -36f, 0f),
                "Assets/FavelaAmarela/Art/Items/BarraEnferrujada.png",
                "FavelaAmarela.Runtime.GameLoop.ArmaPickup", hint, obrigatorio: false);

            EditorSceneManager.MarkSceneDirty(root.scene);
            EditorSceneManager.SaveScene(root.scene);
            Debug.Log("[Items] Gameplay_Items montado (fora do Blockout_Root) e cena salva.");
        }

        private static void SetupPickup(GameObject root, string nome, Vector3 pos, string spritePath,
            string typeName, UnityEngine.Object hint, bool obrigatorio)
        {
            var sprite = AssetDatabase.LoadAssetAtPath<Sprite>(spritePath);
            if (sprite == null)
            {
                if (obrigatorio)
                    Debug.LogError($"[Items] Sprite obrigatório não encontrado (ou não é Sprite): '{spritePath}'.");
                else
                    Debug.LogWarning($"[Items] '{nome}' pulado — sprite '{spritePath}' ainda não existe (arte da arma pendente de aprovação/import). Re-rode este menu depois.");
                return;
            }

            var tf = root.transform.Find(nome);
            var go = tf != null ? tf.gameObject : new GameObject(nome);
            go.transform.SetParent(root.transform, false);
            go.transform.position = pos;

            // Checks explícitos == null (o operador ?? não respeita o fake-null do Unity).
            var sr = go.GetComponent<SpriteRenderer>();
            if (sr == null) sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = sprite;
            sr.sortingOrder = Mathf.RoundToInt(-pos.y * 10f); // convenção -y*10 (fica acima do chão)

            var col = go.GetComponent<BoxCollider2D>();
            if (col == null) col = go.AddComponent<BoxCollider2D>();
            col.isTrigger = true;
            col.size = Vector2.one;

            var type = ResolverTipo(typeName);
            if (type == null) { Debug.LogError($"[Items] Tipo '{typeName}' não encontrado (recompile?)."); return; }
            var comp = go.GetComponent(type);
            if (comp == null) comp = go.AddComponent(type);

            if (hint != null)
            {
                var so = new SerializedObject(comp);
                var p = so.FindProperty("hintUI");
                if (p != null) { p.objectReferenceValue = hint; so.ApplyModifiedPropertiesWithoutUndo(); }
            }

            Debug.Log($"[Items] '{nome}' configurado em {pos} com '{spritePath}'.");
        }

        private static Type ResolverTipo(string fullName)
        {
            foreach (var asm in AppDomain.CurrentDomain.GetAssemblies())
            {
                var t = asm.GetType(fullName);
                if (t != null) return t;
            }
            return null;
        }
    }
}
