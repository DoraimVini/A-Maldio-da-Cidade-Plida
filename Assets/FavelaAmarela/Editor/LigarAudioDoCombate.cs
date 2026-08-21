using UnityEditor;
using UnityEngine;
using FavelaAmarela.Runtime.Audio;
using FavelaAmarela.Runtime.Enemies;

namespace FavelaAmarela.EditorTools
{
    /// <summary>
    /// Fecha os dois buracos que deixavam o combate mudo.
    ///
    /// <para><b>O diagnóstico (2026-08-20).</b> O Vini relatou que a luta contra o Byakhee
    /// <i>"não tem feel bom"</i>. Contando os disparos de <c>SomDoJogo</c> no código,
    /// <b>quatro dos nove sons nunca eram tocados por ninguém</b> — só existiam como forma de
    /// onda em <c>SinteseDeSom</c>. Dois eram o golpe e a habilidade de arma. Somado a isso, o
    /// <c>AudioDeCombate</c> estava <b>só no <c>Cultista.prefab</c></b>. O resultado em jogo:
    /// atacar o chefe não fazia som, e acertá-lo também não.</para>
    ///
    /// <para>Não é bug de mixagem nem de volume — é wiring ausente, o modo de falha recorrente
    /// deste projeto: o sistema existia inteiro e não estava ligado em ponta nenhuma.</para>
    ///
    /// <para><b>Por que no prefab e não na cena:</b> os dois componentes pertencem à entidade,
    /// não ao mapa. Pondo no prefab, toda cena presente e futura herda — e nenhuma sexta lista
    /// de cenas precisa ser mantida à mão (ver <c>CenasNaoFicamParaTrasTests</c>).</para>
    /// </summary>
    public static class LigarAudioDoCombate
    {
        private const string PrefabDamiao =
            "Assets/FavelaAmarela/Art/Characters/Damiao/Player_Damiao.prefab";

        private const string PrefabByakhee = "Assets/FavelaAmarela/Art/Enemies/Byakhee.prefab";

        [MenuItem("Tools/FavelaAmarela/Áudio: ligar o som do combate")]
        public static void Executar()
        {
            bool ok = true;

            ok &= Garantir<AudioDoJogador>(PrefabDamiao, "golpe e habilidade de Damião");
            ok &= Garantir<AudioDeCombate>(PrefabByakhee, "dano e abate do Byakhee");

            if (ok)
                Debug.Log("[LigarAudioDoCombate] Concluído. O golpe de Damião e os acertos no " +
                          "Byakhee passam a soar.");
        }

        /// <summary>
        /// Acrescenta o componente ao prefab se ele não estiver lá, e confere no <b>disco</b>
        /// que ficou. O retorno das APIs de prefab já mentiu neste projeto mais de uma vez.
        /// </summary>
        private static bool Garantir<T>(string caminho, string paraQue) where T : Component
        {
            var raiz = PrefabUtility.LoadPrefabContents(caminho);
            if (raiz == null)
            {
                Debug.LogError($"[LigarAudioDoCombate] Prefab não carregou: {caminho}");
                return false;
            }

            bool jaTinha;

            try
            {
                jaTinha = raiz.GetComponent<T>() != null;
                if (!jaTinha) raiz.AddComponent<T>();

                PrefabUtility.SaveAsPrefabAsset(raiz, caminho, out bool salvou);

                if (!salvou)
                {
                    Debug.LogError($"[LigarAudioDoCombate] SaveAsPrefabAsset recusou {caminho}.");
                    return false;
                }
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(raiz);
            }

            AssetDatabase.Refresh();

            // Relê do disco pelo GUID do script, em vez de confiar no que acabei de escrever.
            string metaDoScript = AssetDatabase.FindAssets($"{typeof(T).Name} t:MonoScript")
                                               .Length > 0
                ? AssetDatabase.GUIDToAssetPath(
                      AssetDatabase.FindAssets($"{typeof(T).Name} t:MonoScript")[0]) + ".meta"
                : null;

            if (metaDoScript == null || !System.IO.File.Exists(metaDoScript))
            {
                Debug.LogWarning($"[LigarAudioDoCombate] Não achei o .meta de {typeof(T).Name} " +
                                 "para conferir no disco.");
                return true;
            }

            var guid = System.Text.RegularExpressions.Regex.Match(
                System.IO.File.ReadAllText(metaDoScript), @"guid: ([0-9a-f]{32})");

            bool noDisco = guid.Success
                        && System.IO.File.ReadAllText(caminho).Contains(guid.Groups[1].Value);

            if (!noDisco)
            {
                Debug.LogError($"[LigarAudioDoCombate] {typeof(T).Name} não apareceu no YAML de " +
                               $"{System.IO.Path.GetFileName(caminho)} depois de salvar.");
                return false;
            }

            Debug.Log($"[LigarAudioDoCombate] {typeof(T).Name} em " +
                      $"{System.IO.Path.GetFileName(caminho)} ({paraQue}) — " +
                      $"{(jaTinha ? "já estava" : "acrescentado")}, conferido no disco.");
            return true;
        }
    }
}
