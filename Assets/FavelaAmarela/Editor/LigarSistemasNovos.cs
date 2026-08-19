using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using FavelaAmarela.Inventario;
using FavelaAmarela.Player;
using FavelaAmarela.Runtime.Audio;
using FavelaAmarela.Runtime.Enemies;
using FavelaAmarela.Runtime.GameLoop;
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
        private const string TabelaBau = "Assets/FavelaAmarela/Config/Drops/Drop_BauDaTumba.asset";

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

                bool mudou = LigarJogador() | LigarAudioDaCena() | LigarBauDaTumba();

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

            // Os dois de áudio ficam no Damião: são sobre ele (o ruído que ele faz e o estado
            // da mente dele). O GameManager os encontra por tipo e injeta a fonte.
            mudou |= Garantir<AudioDeResiliencia>(go);
            mudou |= Garantir<AudioDeStealth>(go);

            if (mudou) EditorUtility.SetDirty(go);
            return mudou;
        }

        private static bool LigarAudioDaCena()
        {
            bool mudou = LimparAudioDeStealthAvulso();

            if (Object.FindAnyObjectByType<MixerDeAudio>(FindObjectsInactive.Include) == null)
            {
                var go = new GameObject("MixerDeAudio", typeof(MixerDeAudio));
                Undo.RegisterCreatedObjectUndo(go, "Mixer de Áudio");
                mudou = true;
                Debug.Log("[LigarSistemas] MixerDeAudio criado.");
            }

            return mudou;
        }

        /// <summary>
        /// Remove o <c>AudioDeStealth</c> que versões anteriores desta ferramenta criavam
        /// solto na cena. Ele agora mora no Damião; deixar os dois faria **dois** ouvintes do
        /// mesmo evento, e cada passo tocaria em dobro.
        /// </summary>
        private static bool LimparAudioDeStealthAvulso()
        {
            bool mudou = false;

            var todos = Object.FindObjectsByType<AudioDeStealth>(
                FindObjectsInactive.Include);

            foreach (var comp in todos)
            {
                // Só o que está fora do jogador. O do Damião é o que fica.
                if (comp.GetComponent<PlayerMovement>() != null) continue;

                Debug.Log($"[LigarSistemas] Removendo AudioDeStealth avulso de '{comp.name}' " +
                          "(mudou de casa: agora fica no Damião).");

                // O objeto avulso existia só para hospedar este componente.
                if (comp.gameObject.GetComponents<Component>().Length <= 2)
                    Object.DestroyImmediate(comp.gameObject);
                else
                    Object.DestroyImmediate(comp);

                mudou = true;
            }

            return mudou;
        }

        /// <summary>
        /// Aponta o <see cref="BauDaTumba"/> da cena para a <c>Drop_BauDaTumba</c>.
        ///
        /// <para><b>Bug que motivou (playtest de 2026-08-11: "o baú está quebrado"):</b> quando
        /// o baú migrou do <c>Random.Range</c> sobre <c>ItemDef[]</c> para a tabela de drop, o
        /// campo novo ficou <b>nulo na cena</b> e o array antigo foi removido do componente.
        /// Resultado: abrir o baú não entregava arma nenhuma. Eu registrei isto como "wiring
        /// manual pendente" e nunca automatizei — então nunca foi feito.</para>
        /// </summary>
        private static bool LigarBauDaTumba()
        {
            var bau = Object.FindAnyObjectByType<BauDaTumba>(FindObjectsInactive.Include);
            if (bau == null) return false; // cena sem baú é normal (só a Tumba tem)

            var tabela = AssetDatabase.LoadAssetAtPath<TabelaDeDrop>(TabelaBau);
            if (tabela == null)
            {
                Debug.LogError($"[LigarSistemas] Tabela do baú não encontrada em '{TabelaBau}'.");
                return false;
            }

            var so = new SerializedObject(bau);
            var prop = so.FindProperty("tabela");
            if (prop == null || prop.objectReferenceValue != null) return false;

            prop.objectReferenceValue = tabela;
            so.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(bau);

            Debug.Log("[LigarSistemas] BauDaTumba ligado à Drop_BauDaTumba — ele voltou a " +
                      "entregar arma.", bau);
            return true;
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
