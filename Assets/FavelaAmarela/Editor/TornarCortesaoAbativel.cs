using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using FavelaAmarela.Runtime.Enemies;

namespace FavelaAmarela.EditorTools
{
    /// <summary>
    /// Dá <see cref="EnemyBase"/> e ficha de atributos aos Cortesãos Pálidos, tornando-os
    /// abatíveis.
    ///
    /// <para><b>O que isto conserta.</b> O Vini jogou o Castelo e relatou: <i>"o inimigo tem
    /// colisor, mas não leva dano, nem causa"</i>. Estava certo e o código já admitia — o
    /// comentário do próprio <c>CortesaoPalido</c> dizia que ele <i>"não implementa
    /// IDanificavel: não existe caminho no jogo que tire vida dele"</i>. Medido: no
    /// <c>Castelo_Carcosa</c> inteiro, a única hurtbox era a do <c>Player_Damiao</c>, e o golpe
    /// do Damião consulta <b>só</b> a camada <c>EnemyHurtbox</c>.</para>
    ///
    /// <para><b>Por que basta acrescentar o componente.</b> <c>EnemyBase.Awake</c> chama
    /// <c>Hurtbox.GarantirPara</c> — a área atingível nasce do sprite, sem lista escrita à mão.
    /// Ele também traz <c>Vitalidade</c>, <c>IDanificavel</c>, <c>OnAbatido</c> e o espólio. E
    /// desde 2026-09-04 a <c>EnemyStateMachine</c> assina <c>OnGolpeRecebido</c>; o Cortesão não
    /// usa aquela FSM, então quem faz o papel de "reagir ao golpe" nele continua sendo a visão
    /// própria dele.</para>
    ///
    /// <para><b>Idempotente:</b> um Cortesão que já tenha <c>EnemyBase</c> é religado, não
    /// duplicado.</para>
    /// </summary>
    public static class TornarCortesaoAbativel
    {
        private const string Ficha = "Assets/FavelaAmarela/Config/Ficha_CortesaoPalido.asset";

        /// <summary>
        /// O Castelo é a fase final e o jogador chega nele por volta do nível 3 (ver
        /// <c>CLAUDE.md</c> §1.1). Um mob de nível 1 ali seria papel.
        /// </summary>
        private const int NivelDaUnidade = 3;

        [MenuItem("Tools/FavelaAmarela/Combate: tornar o Cortesão abatível")]
        public static void Executar()
        {
            if (!Application.isBatchMode &&
                !EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
            {
                Debug.Log("[CortesaoAbativel] Cancelado — havia cena modificada por salvar.");
                return;
            }

            // TIPO CONCRETO, e nao Object. Carregar como Object compila e roda, mas
            // SerializedProperty.objectReferenceValue REJEITA EM SILENCIO um objeto que nao
            // casa com o tipo do campo -- a primeira execucao desta ferramenta gravou
            // nivelDaUnidade e deixou a ficha nula, e o log disse "adicionado" nas duas vezes.
            // Com o tipo certo, um asset que nao importe vira null e cai na guarda abaixo.
            var ficha = AssetDatabase.LoadAssetAtPath<FavelaAmarela.Core.Combat.FichaAtributosConfig>(Ficha);
            if (ficha == null)
            {
                Debug.LogError($"[CortesaoAbativel] Ficha ausente em {Ficha}. Sem ela o " +
                               "EnemyBase cairia na ficha padrão (100/10/5) e o Cortesão do " +
                               "Castelo teria os números de um boneco de treino. Nada foi feito.");
                return;
            }

            var log = new StringBuilder("[CortesaoAbativel]\n");
            int novos = 0, religados = 0;

            foreach (var entrada in EditorBuildSettings.scenes)
            {
                if (!entrada.enabled || !File.Exists(entrada.path)) continue;

                Scene cena = EditorSceneManager.OpenScene(entrada.path, OpenSceneMode.Single);

                var cortesaos = cena.GetRootGameObjects()
                    .SelectMany(r => r.GetComponentsInChildren<CortesaoPalido>(true))
                    .ToList();

                if (cortesaos.Count == 0) continue;

                foreach (var c in cortesaos)
                {
                    // Sem ?? : GetComponent devolve o fake-null da Unity, que não é null por
                    // referência. Já custou uma execução desta pasta de ferramentas.
                    var corpo = c.GetComponent<EnemyBase>();
                    bool novo = corpo == null;
                    if (novo) corpo = c.gameObject.AddComponent<EnemyBase>();

                    // ATRIBUICAO DIRETA, e nao SerializedObject.
                    //
                    // O SerializedObject FUNCIONOU para o int (nivelDaUnidade persistiu) e
                    // NAO funcionou para a referencia de asset: o read-back voltava nulo com
                    // os tipos medidos como identicos e IsInstanceOfType == True. Gastei tres
                    // execucoes numa teoria de incompatibilidade de tipo que o diagnostico
                    // derrubou. Nao sei por que o Apply nao pegou; sei que este caminho pega,
                    // e a verificacao final e o DISCO, nao o read-back.
                    var campo = typeof(EnemyBase).GetField("ficha",
                        System.Reflection.BindingFlags.NonPublic |
                        System.Reflection.BindingFlags.Instance);
                    var campoNivel = typeof(EnemyBase).GetField("nivelDaUnidade",
                        System.Reflection.BindingFlags.NonPublic |
                        System.Reflection.BindingFlags.Instance);

                    if (campo == null || campoNivel == null)
                    {
                        Debug.LogError("[CortesaoAbativel] campo sumiu de EnemyBase.", corpo);
                        continue;
                    }

                    campo.SetValue(corpo, ficha);
                    campoNivel.SetValue(corpo, NivelDaUnidade);
                    EditorUtility.SetDirty(corpo);

                    // LE DE VOLTA, do proprio objeto. A ferramenta nao pode reportar sucesso
                    // por ter executado a atribuicao -- tem de reportar por ela ter PEGADO.
                    if (campo.GetValue(corpo) == null)
                    {
                        Debug.LogError($"[CortesaoAbativel] A ficha nao pegou em '{c.name}'.",
                                       corpo);
                        continue;
                    }

                    if (novo) novos++; else religados++;

                    log.AppendLine($"   {Path.GetFileNameWithoutExtension(entrada.path)} / " +
                                   $"{c.name} -> EnemyBase {(novo ? "adicionado" : "religado")}, " +
                                   $"nível {NivelDaUnidade}");
                }

                EditorSceneManager.MarkSceneDirty(cena);
                EditorSceneManager.SaveScene(cena);
            }

            log.AppendLine($"   total: {novos} novo(s), {religados} religado(s)");
            log.AppendLine("   a hurtbox NÃO é criada aqui: nasce em EnemyBase.Awake, do sprite.");
            Debug.Log(log.ToString());
        }
    }
}
