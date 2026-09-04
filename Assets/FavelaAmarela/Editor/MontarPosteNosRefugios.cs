using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using FavelaAmarela.Runtime.GameLoop;
using FavelaAmarela.Runtime.Enemies;

namespace FavelaAmarela.EditorTools
{
    /// <summary>
    /// Dá corpo visível aos Refúgios de Luz: monta um <b>Poste de Osso</b> em cada
    /// <see cref="RefugioDeLuz"/> das cenas do Build Settings e liga os campos.
    ///
    /// <para><b>O que isto conserta.</b> Medido em 2026-09-04: os três Refúgios do Deserto —
    /// e os das outras cenas — eram <c>Transform</c> + <c>CircleCollider2D</c> + script, <b>sem
    /// <c>SpriteRenderer</c></b>. Ou seja: o único ponto de save do jogo era um círculo
    /// invisível no chão. O jogador atravessava, o jogo curava, salvava e gravava
    /// <c>Jogador.Refugio.Ponto</c> — e nada aparecia na tela.</para>
    ///
    /// <para><b>Idempotente:</b> um Refúgio que já tenha o poste é reaproveitado e religado, não
    /// duplicado. Rodar duas vezes não cria dois postes.</para>
    ///
    /// <para><b>Por que ferramenta e não edição de YAML:</b> boa parte dos Refúgios é objeto de
    /// cena com filhos, e criar GameObject filho + ligar referência serializada por texto é
    /// como se colam componentes no objeto errado. A Unity resolve os fileIDs.</para>
    /// </summary>
    public static class MontarPosteNosRefugios
    {
        private const string PastaDaArte = "Assets/FavelaAmarela/Art/Props/PosteDeOsso";
        private const string NomeDoPoste = "Poste";

        /// <summary>
        /// Uma volta da chama por segundo e pouco: fogo que pisca rápido lê como lâmpada com mau
        /// contato. Mesma cadência da aura da Pedra de Poder.
        /// </summary>
        private const float QuadrosPorSegundo = 8f;

        [MenuItem("Tools/FavelaAmarela/Cena: montar o Poste nos Refúgios de Luz")]
        public static void Executar()
        {
            if (!Application.isBatchMode &&
                !EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
            {
                Debug.Log("[PosteDosRefugios] Cancelado — havia cena modificada por salvar.");
                return;
            }

            var apagado = AssetDatabase.LoadAssetAtPath<Sprite>(
                Path.Combine(PastaDaArte, "Poste_Apagado.png"));

            var acesos = Enumerable.Range(0, 4)
                .Select(i => AssetDatabase.LoadAssetAtPath<Sprite>(
                    Path.Combine(PastaDaArte, $"Poste_Aceso_{i}.png")))
                .ToArray();

            if (apagado == null || acesos.Any(s => s == null))
            {
                Debug.LogError($"[PosteDosRefugios] Falta arte em {PastaDaArte}. Esperado " +
                               "Poste_Apagado.png e Poste_Aceso_0..3.png. Nada foi montado.");
                return;
            }

            var log = new StringBuilder("[PosteDosRefugios]\n");
            int montados = 0, religados = 0;

            foreach (var entrada in EditorBuildSettings.scenes)
            {
                if (!entrada.enabled || !File.Exists(entrada.path)) continue;

                Scene cena = EditorSceneManager.OpenScene(entrada.path, OpenSceneMode.Single);
                string nomeDaCena = Path.GetFileNameWithoutExtension(entrada.path);

                var refugios = cena.GetRootGameObjects()
                    .SelectMany(r => r.GetComponentsInChildren<RefugioDeLuz>(true))
                    .ToList();

                if (refugios.Count == 0) continue;

                bool mexeu = false;

                foreach (var refugio in refugios)
                {
                    var existente = refugio.transform.Find(NomeDoPoste);
                    bool novo = existente == null;

                    var poste = novo ? new GameObject(NomeDoPoste).transform : existente;

                    if (novo)
                    {
                        poste.SetParent(refugio.transform, false);
                        poste.localPosition = Vector3.zero;
                        montados++;
                    }
                    else
                    {
                        religados++;
                    }

                    // NAO usar ?? aqui. GetComponent devolve o "fake null" da Unity quando o
                    // componente falta, e ?? compara por REFERENCIA -- ele nao consulta o
                    // operador == sobrecarregado de UnityEngine.Object. Resultado: o ?? nao cai
                    // no AddComponent, a variavel fica com o fake-null e o acesso seguinte
                    // estoura MissingComponentException. Foi exatamente o que aconteceu na
                    // primeira execucao desta ferramenta.
                    var sr = poste.GetComponent<SpriteRenderer>();
                    if (sr == null) sr = poste.gameObject.AddComponent<SpriteRenderer>();
                    sr.sprite = apagado;

                    // O poste é alto: sem Y-sort ele seria desenhado por baixo de quem passa
                    // atrás. -y*10 é o fator do projeto inteiro (ver DynamicYSort e o
                    // LevelBlockoutGenerator).
                    sr.sortingOrder = Mathf.RoundToInt(-poste.position.y * 10f);

                    var chama = poste.GetComponent<AnimadorEmLaco>();
                    if (chama == null) chama = poste.gameObject.AddComponent<AnimadorEmLaco>();

                    Escrever(chama, "quadros", acesos);
                    Escrever(chama, "quadrosPorSegundo", QuadrosPorSegundo);
                    chama.enabled = false;   // nasce apagado; o descanso liga

                    Escrever(refugio, "visualDoPoste", sr);
                    Escrever(refugio, "posteAceso", acesos[0]);
                    Escrever(refugio, "chamaDoPoste", chama);

                    log.AppendLine($"   {nomeDaCena} / {refugio.name} " +
                                   $"-> {(novo ? "montado" : "religado")}, " +
                                   $"sortingOrder {sr.sortingOrder}");
                    mexeu = true;
                }

                if (!mexeu) continue;

                EditorSceneManager.MarkSceneDirty(cena);
                EditorSceneManager.SaveScene(cena);
            }

            log.AppendLine($"   total: {montados} montado(s), {religados} religado(s)");
            Debug.Log(log.ToString());
        }

        /// <summary>
        /// Escreve num campo serializado privado. <c>SerializedObject</c> e não reflexão pura:
        /// só ele marca a cena como suja de um jeito que sobrevive ao salvamento — atribuir por
        /// <c>FieldInfo</c> muda o objeto em memória e a Unity grava o valor antigo.
        /// </summary>
        private static void Escrever(Object alvo, string campo, object valor)
        {
            var so = new SerializedObject(alvo);
            var prop = so.FindProperty(campo);

            if (prop == null)
            {
                Debug.LogError($"[PosteDosRefugios] Campo '{campo}' não existe em " +
                               $"{alvo.GetType().Name}. Ele foi renomeado, e a ligação ficaria " +
                               "muda.", alvo);
                return;
            }

            switch (valor)
            {
                case Sprite[] quadros:
                    prop.arraySize = quadros.Length;
                    for (int i = 0; i < quadros.Length; i++)
                        prop.GetArrayElementAtIndex(i).objectReferenceValue = quadros[i];
                    break;
                case float f: prop.floatValue = f; break;
                case Object o: prop.objectReferenceValue = o; break;
                default:
                    Debug.LogError($"[PosteDosRefugios] Tipo não tratado para '{campo}'.");
                    return;
            }

            so.ApplyModifiedPropertiesWithoutUndo();
        }
    }
}
