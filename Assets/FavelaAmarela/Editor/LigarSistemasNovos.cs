using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using FavelaAmarela.Inventario;
using FavelaAmarela.Player;
using FavelaAmarela.Runtime.Audio;
using FavelaAmarela.Runtime.Enemies;
using FavelaAmarela.Runtime.Itens;
using FavelaAmarela.Runtime.Persistencia;

namespace FavelaAmarela.EditorTools
{
    /// <summary>
    /// Ferramenta de Editor. Liga em cena e nos prefabs tudo que foi construído nas rodadas de
    /// 2026-08-10/11 e ficou <b>inerte por falta de wiring</b>: Artefatos, persistência de
    /// inventário/progressão, áudio e espólio ao abater.
    ///
    /// <para>Existe porque o acúmulo é grande e anexar componente por componente à mão erra
    /// fácil — esquecer <b>uma</b> ponte de persistência devolve a perder progresso em
    /// silêncio, que é exatamente o bug que acabou de ser corrigido.</para>
    ///
    /// <para>Idempotente: só adiciona o que falta, nunca duplica. Pode rodar quantas vezes
    /// quiser.</para>
    /// </summary>
    public static class LigarSistemasNovos
    {
        private static readonly string[] Cenas =
        {
            "Assets/Scenes/Deserto_Hali.unity",
            "Assets/Scenes/Playtest_RuinasPalidas.unity",
            "Assets/Scenes/Santuario_Yhtill.unity",
        };

        private const string PrefabCultista = "Assets/FavelaAmarela/Art/Enemies/Cultista.prefab";
        private const string PrefabCoisa = "Assets/FavelaAmarela/Art/Enemies/CoisaDoCemiterio.prefab";
        private const string TabelaCultista = "Assets/FavelaAmarela/Config/Drops/Drop_Cultista.asset";

        [MenuItem("Tools/FavelaAmarela/Ligar sistemas novos (artefatos, áudio, save, drop)")]
        public static void Executar()
        {
            LigarPrefabsDeInimigo();
            LigarCenas();

            AssetDatabase.SaveAssets();
            Debug.Log("[LigarSistemas] Pronto. Confira o console acima para o detalhe por cena.");
        }

        // ── Prefabs de inimigo ────────────────────────────────────────────────

        private static void LigarPrefabsDeInimigo()
        {
            var tabela = AssetDatabase.LoadAssetAtPath<TabelaDeDrop>(TabelaCultista);
            if (tabela == null)
                Debug.LogWarning($"[LigarSistemas] Tabela de drop não encontrada em '{TabelaCultista}'.");

            LigarInimigo(PrefabCultista, tabela);
            LigarInimigo(PrefabCoisa, null); // sem tabela própria ainda — só áudio
        }

        private static void LigarInimigo(string caminho, TabelaDeDrop tabela)
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(caminho);
            if (prefab == null)
            {
                Debug.LogWarning($"[LigarSistemas] Prefab não encontrado: '{caminho}'.");
                return;
            }

            var raiz = PrefabUtility.LoadPrefabContents(caminho);
            bool mudou = false;

            if (raiz.GetComponent<EnemyBase>() == null)
            {
                Debug.LogWarning($"[LigarSistemas] '{raiz.name}' não tem EnemyBase — pulado.");
                PrefabUtility.UnloadPrefabContents(raiz);
                return;
            }

            if (raiz.GetComponent<AudioDeCombate>() == null)
            {
                raiz.AddComponent<AudioDeCombate>();
                mudou = true;
            }

            if (tabela != null)
            {
                var drop = raiz.GetComponent<DropAoAbater>();
                if (drop == null)
                {
                    drop = raiz.AddComponent<DropAoAbater>();
                    mudou = true;
                }

                var so = new SerializedObject(drop);
                var prop = so.FindProperty("tabela");
                if (prop != null && prop.objectReferenceValue == null)
                {
                    prop.objectReferenceValue = tabela;
                    so.ApplyModifiedPropertiesWithoutUndo();
                    mudou = true;
                }
            }

            if (mudou)
            {
                PrefabUtility.SaveAsPrefabAsset(raiz, caminho);
                Debug.Log($"[LigarSistemas] Prefab '{raiz.name}' ligado (áudio de combate + espólio).");
            }

            PrefabUtility.UnloadPrefabContents(raiz);
        }

        // ── Cenas ─────────────────────────────────────────────────────────────

        private static void LigarCenas()
        {
            var cenaAtiva = EditorSceneManager.GetActiveScene();
            if (cenaAtiva.isDirty && !string.IsNullOrEmpty(cenaAtiva.path))
                EditorSceneManager.SaveScene(cenaAtiva);

            string cenaOriginal = cenaAtiva.path;

            foreach (var caminho in Cenas)
            {
                if (!System.IO.File.Exists(caminho)) continue;

                var cena = EditorSceneManager.OpenScene(caminho, OpenSceneMode.Single);

                bool mudou = LigarJogador() | LigarAudioDaCena();

                if (mudou)
                {
                    EditorSceneManager.MarkSceneDirty(cena);
                    EditorSceneManager.SaveScene(cena);
                    Debug.Log($"[LigarSistemas] Cena '{cena.name}' ligada.");
                }
                else
                {
                    Debug.Log($"[LigarSistemas] Cena '{cena.name}' já estava completa.");
                }
            }

            if (!string.IsNullOrEmpty(cenaOriginal))
                EditorSceneManager.OpenScene(cenaOriginal, OpenSceneMode.Single);
        }

        private static bool LigarJogador()
        {
            var jogador = Object.FindAnyObjectByType<PlayerMovement>(FindObjectsInactive.Include);
            if (jogador == null)
            {
                Debug.LogWarning("[LigarSistemas] Sem PlayerMovement nesta cena — jogador não ligado.");
                return false;
            }

            var go = jogador.gameObject;
            bool mudou = false;

            // Artefatos primeiro: a ponte de persistência deles exige a bridge no mesmo objeto.
            mudou |= Garantir<ArtefatosBridge>(go);
            mudou |= Garantir<EstadoPersistenteDosArtefatos>(go);

            // Sem estas duas, mochila/equipamento e nível se perdem ao recarregar — em
            // silêncio, que é o pior modo de perder progresso.
            mudou |= Garantir<EstadoPersistenteDoInventario>(go);
            mudou |= Garantir<EstadoPersistenteDaProgressao>(go);

            // A Resiliência é injetada pelo GameManager; o componente precisa existir na cena.
            mudou |= Garantir<AudioDeResiliencia>(go);

            if (mudou) EditorUtility.SetDirty(go);
            return mudou;
        }

        private static bool LigarAudioDaCena()
        {
            bool mudou = false;

            if (Object.FindAnyObjectByType<MixerDeAudio>(FindObjectsInactive.Include) == null)
            {
                var go = new GameObject("MixerDeAudio", typeof(MixerDeAudio));
                Undo.RegisterCreatedObjectUndo(go, "Mixer de Áudio");
                mudou = true;
                Debug.Log("[LigarSistemas] MixerDeAudio criado.");
            }

            // O AudioDeStealth é o que torna audível o ruído de Damião — o pilar do jogo.
            // Fica num objeto próprio porque o GameManager o encontra por tipo, não por filho.
            if (Object.FindAnyObjectByType<AudioDeStealth>(FindObjectsInactive.Include) == null)
            {
                var go = new GameObject("AudioDeStealth", typeof(AudioDeStealth));
                Undo.RegisterCreatedObjectUndo(go, "Áudio de Stealth");
                mudou = true;
                Debug.Log("[LigarSistemas] AudioDeStealth criado.");
            }

            return mudou;
        }

        /// <summary>Adiciona o componente se ainda não houver. Devolve se mudou algo.</summary>
        private static bool Garantir<T>(GameObject alvo) where T : Component
        {
            if (alvo.GetComponent<T>() != null) return false;

            alvo.AddComponent<T>();
            Debug.Log($"[LigarSistemas] '{typeof(T).Name}' adicionado em '{alvo.name}'.");
            return true;
        }
    }
}
