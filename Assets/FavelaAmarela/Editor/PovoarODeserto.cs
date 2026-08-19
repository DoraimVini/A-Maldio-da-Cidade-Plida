using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using FavelaAmarela.Runtime.GameLoop;
using FavelaAmarela.Runtime.Persistencia;

namespace FavelaAmarela.EditorTools
{
    /// <summary>
    /// Ferramenta de Editor. Povoa o <b>Deserto de Hali</b> com Cultistas e coloca a
    /// <b>Coisa do Cemitério</b> — itens 6 e 7 da lista do edital, que tinham código pronto e
    /// testado e <b>zero instâncias em cena</b> (auditoria de 2026-08-11).
    ///
    /// <para><b>A densidade acompanha a tempestade, de propósito.</b> A percepção do Cultista
    /// é 100% sonora e a tempestade abafa o ruído do Damião — ou seja, quanto mais forte a
    /// tempestade, <b>mais furtivo</b> o jogador fica. Então setor de tempestade alta aguenta
    /// mais inimigos sem ficar injusto, e setor calmo precisa ser esparso. É o que transforma
    /// a tempestade de efeito visual em decisão: atravessar no auge dela é arriscado de perto,
    /// mas é quando você passa despercebido.</para>
    ///
    /// <para><b>Determinística:</b> a mesma semente sempre produz o mesmo mapa, para playtest
    /// ser reprodutível. Idempotente: refaz o grupo do zero a cada execução.</para>
    /// </summary>
    public static class PovoarODeserto
    {
        private const string CaminhoCena = "Assets/Scenes/Deserto_Hali.unity";
        private const string PrefabCultista = "Assets/FavelaAmarela/Art/Enemies/Cultista.prefab";
        private const string PrefabCoisa = "Assets/FavelaAmarela/Art/Enemies/CoisaDoCemiterio.prefab";

        private const string NomeDoGrupo = "Inimigos_Deserto";

        /// <summary>Semente fixa: mesmo mapa a cada execução, para playtest reprodutível.</summary>
        private const int Semente = 20260811;

        /// <summary>Quantos Cultistas num setor de tempestade fraca.</summary>
        private const int MinimoPorSetor = 1;

        /// <summary>Quantos num setor de tempestade máxima (onde o jogador é mais furtivo).</summary>
        private const int MaximoPorSetor = 5;

        /// <summary>
        /// Setor de chegada: fica <b>vazio</b>. O jogador acorda no deserto sem arma e sem
        /// saber das regras — emboscá-lo no primeiro minuto ensina frustração, não tensão.
        /// </summary>
        private const string SetorDeChegada = "Setor_Entrada";

        /// <summary>Onde a Coisa do Cemitério mora. Longe da chegada, e com espaço para correr.</summary>
        private const string SetorDaCoisa = "Setor_DesertoCentral";

        [MenuItem("Tools/FavelaAmarela/Povoar o Deserto de Hali")]
        public static void Executar()
        {
            var cultista = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabCultista);
            var coisa = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabCoisa);

            if (cultista == null)
            {
                Debug.LogError($"[PovoarDeserto] Prefab do Cultista não encontrado em '{PrefabCultista}'.");
                return;
            }

            var cenaAtiva = EditorSceneManager.GetActiveScene();
            if (cenaAtiva.isDirty && !string.IsNullOrEmpty(cenaAtiva.path))
                EditorSceneManager.SaveScene(cenaAtiva);

            string cenaOriginal = cenaAtiva.path;
            var cena = EditorSceneManager.OpenScene(CaminhoCena, OpenSceneMode.Single);

            int total = Povoar(cultista, coisa);

            EditorSceneManager.MarkSceneDirty(cena);
            EditorSceneManager.SaveScene(cena);

            if (!string.IsNullOrEmpty(cenaOriginal) && cenaOriginal != CaminhoCena)
                EditorSceneManager.OpenScene(cenaOriginal, OpenSceneMode.Single);

            Debug.Log($"[PovoarDeserto] Pronto — {total} inimigo(s) no Deserto de Hali.");
        }

        private static int Povoar(GameObject cultista, GameObject coisa)
        {
            // Refaz do zero: sem isso, rodar duas vezes dobraria a população.
            var antigo = GameObject.Find(NomeDoGrupo);
            if (antigo != null) Object.DestroyImmediate(antigo);

            var grupo = new GameObject(NomeDoGrupo);
            grupo.transform.SetParent(null);

            var setores = Object.FindObjectsByType<TempestadeZonaTrigger>(
                FindObjectsInactive.Include);

            if (setores.Length == 0)
            {
                Debug.LogWarning("[PovoarDeserto] Nenhum setor de tempestade na cena — " +
                                 "rode antes 'Montar setores de tempestade do Deserto'.");
                return 0;
            }

            var rng = new System.Random(Semente);
            int total = 0;

            foreach (var setor in setores)
            {
                if (setor.name == SetorDeChegada)
                {
                    Debug.Log($"[PovoarDeserto] '{setor.name}' deixado vazio (setor de chegada).");
                    continue;
                }

                var area = ObterArea(setor);
                if (!area.HasValue) continue;

                int quantos = QuantidadePara(setor);
                for (int i = 0; i < quantos; i++)
                {
                    var pos = PontoDentro(area.Value, rng);
                    Instanciar(cultista, grupo.transform, pos, $"Cultista_{setor.name}_{i}");
                    total++;
                }

                Debug.Log($"[PovoarDeserto] '{setor.name}': {quantos} Cultista(s).");
            }

            total += ColocarACoisa(coisa, grupo.transform, setores, rng);
            return total;
        }

        private static int ColocarACoisa(GameObject coisa, Transform grupo,
            TempestadeZonaTrigger[] setores, System.Random rng)
        {
            if (coisa == null)
            {
                Debug.LogWarning($"[PovoarDeserto] Prefab da Coisa não encontrado em '{PrefabCoisa}'.");
                return 0;
            }

            // Uma só. Ela mata no toque e caça por faro — a tempestade, que protege contra os
            // Cultistas, não serve de nada aqui. Duas transformariam o Deserto num corredor
            // de morte em vez de num lugar com uma ameaça que se evita.
            var alvo = System.Array.Find(setores, s => s.name == SetorDaCoisa) ?? setores[0];

            var area = ObterArea(alvo);
            if (!area.HasValue) return 0;

            Instanciar(coisa, grupo, PontoDentro(area.Value, rng), "CoisaDoCemiterio");
            Debug.Log($"[PovoarDeserto] Coisa do Cemitério colocada em '{alvo.name}'.");
            return 1;
        }

        /// <summary>
        /// Densidade proporcional à tempestade do setor: a tempestade abafa o ruído do
        /// Damião, então onde ela é forte o jogador aguenta mais companhia.
        /// </summary>
        private static int QuantidadePara(TempestadeZonaTrigger setor)
        {
            var so = new SerializedObject(setor);
            float min = so.FindProperty("minimo")?.floatValue ?? 0.2f;
            float max = so.FindProperty("maximo")?.floatValue ?? 0.6f;

            float intensidadeMedia = Mathf.Clamp01((min + max) * 0.5f);
            return Mathf.RoundToInt(Mathf.Lerp(MinimoPorSetor, MaximoPorSetor, intensidadeMedia));
        }

        private static Bounds? ObterArea(TempestadeZonaTrigger setor)
        {
            var col = setor.GetComponent<Collider2D>();
            if (col == null)
            {
                Debug.LogWarning($"[PovoarDeserto] '{setor.name}' não tem Collider2D — pulado.");
                return null;
            }

            return col.bounds;
        }

        /// <summary>
        /// Ponto aleatório dentro da área, com margem para o inimigo não nascer colado na
        /// borda (e, na virada de setor, meio de fora).
        /// </summary>
        private static Vector3 PontoDentro(Bounds area, System.Random rng)
        {
            const float margem = 0.15f;

            float t(float a, float b) => Mathf.Lerp(a, b, (float)rng.NextDouble());

            float x = t(area.min.x + area.size.x * margem, area.max.x - area.size.x * margem);
            float y = t(area.min.y + area.size.y * margem, area.max.y - area.size.y * margem);

            return new Vector3(x, y, 0f);
        }

        private static void Instanciar(GameObject prefab, Transform pai, Vector3 posicao, string nome)
        {
            var go = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
            go.transform.SetParent(pai, false);
            go.transform.position = posicao;
            go.name = nome;

            // Chave por instância, para o abate persistir: sem ela, todo inimigo morto
            // ressuscita ao recarregar a cena. A chave nasce aqui porque em modo Prefab
            // todas as instâncias herdariam a mesma e uma sobrescreveria a outra no save.
            var persistente = go.GetComponent<ObjetoPersistente>();
            if (persistente == null) persistente = go.AddComponent<ObjetoPersistente>();
            persistente.GarantirChave();

            EditorUtility.SetDirty(go);
        }
    }
}
