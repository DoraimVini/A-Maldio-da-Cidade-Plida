using System.Linq;
using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace FavelaAmarela.EditorTools
{
    /// <summary>
    /// Remove os dois <c>Collider2D</c> de gatilho órfãos que a instância do Byakhee carrega na
    /// <c>Portoes_Das_Ruinas</c> — um <c>CircleCollider2D</c> e um <c>BoxCollider2D</c>, ambos
    /// <c>isTrigger</c>, <b>sem callback e sem componente que os explique</b>.
    ///
    /// <para><b>Por que eles não são inofensivos.</b> O Byakhee está na camada <c>Enemy</c>, e
    /// <c>Enemy</c> está em <c>DetectorDeInteracao.CamadasPadraoDeInteragiveis</c>. Aquele
    /// <c>Physics2D.OverlapCircle</c> tem <b>8 slots fixos</b> e descarta o excedente em ordem
    /// arbitrária — o próprio componente documenta o sintoma: <i>"o 'E' simplesmente não faz
    /// nada, sem uma linha no console"</i>. Cada órfão come um slot perto do chefe.</para>
    ///
    /// <para><b>Por que uma ferramenta e não edição de YAML.</b> A instância do Byakhee é um
    /// <c>PrefabInstance</c>, e componente adicionado a instância vive em dois lugares: o bloco
    /// do componente e a contabilidade do <c>PrefabInstance</c>. Editar o texto à mão deixa uma
    /// das duas para trás — e este projeto já pagou por regex em YAML de cena. A Unity resolve
    /// as duas de uma vez.</para>
    ///
    /// <para><b>O que ela NÃO toca:</b> a cápsula sólida (a pegada de movimento) e a hurtbox,
    /// que vive num objeto FILHO. A varredura é só nos colisores do próprio GameObject do
    /// Byakhee, e só nos que são <c>isTrigger</c>.</para>
    /// </summary>
    public static class RemoverGatilhosOrfaosDoByakhee
    {
        private const string Cena = "Assets/Scenes/Portoes_Das_Ruinas.unity";
        private const string Alvo = "Byakhee";

        [MenuItem("Tools/FavelaAmarela/Colisores: remover gatilhos órfãos do Byakhee")]
        public static void Executar()
        {
            if (!Application.isBatchMode &&
                !EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
            {
                Debug.Log("[GatilhosOrfaos] Cancelado — havia cena modificada por salvar.");
                return;
            }

            var cena = EditorSceneManager.OpenScene(Cena, OpenSceneMode.Single);

            var byakhee = cena.GetRootGameObjects()
                .SelectMany(r => r.GetComponentsInChildren<Transform>(true))
                .FirstOrDefault(t => t.name == Alvo);

            if (byakhee == null)
            {
                Debug.LogError($"[GatilhosOrfaos] Não achei um GameObject '{Alvo}' em {Cena}. " +
                               "Ou ele foi renomeado, ou está noutra cena — em qualquer caso, " +
                               "nada foi removido.");
                return;
            }

            // Só os colisores do PRÓPRIO objeto: a hurtbox mora num filho e não pode ser tocada.
            var noProprio = byakhee.GetComponents<Collider2D>();
            var log = new StringBuilder();
            log.AppendLine($"[GatilhosOrfaos] '{byakhee.name}' " +
                           $"(camada {LayerMask.LayerToName(byakhee.gameObject.layer)}) tem " +
                           $"{noProprio.Length} colisor(es) no próprio objeto:");

            var remover = noProprio.Where(c => c.isTrigger).ToList();

            foreach (var c in noProprio)
            {
                log.AppendLine($"   {c.GetType().Name} isTrigger={c.isTrigger} " +
                               $"offset={c.offset} -> {(c.isTrigger ? "REMOVER" : "mantido")}");
            }

            if (remover.Count == 0)
            {
                log.AppendLine("   nada a remover — os órfãos já não estão aqui.");
                Debug.Log(log.ToString());
                return;
            }

            foreach (var c in remover) Object.DestroyImmediate(c, allowDestroyingAssets: true);

            int sobraram = byakhee.GetComponents<Collider2D>().Length;
            int naHurtbox = byakhee.GetComponentsInChildren<Collider2D>(true).Length - sobraram;

            log.AppendLine($"   removidos: {remover.Count}");
            log.AppendLine($"   sobraram no próprio objeto: {sobraram} " +
                           $"(e {naHurtbox} nos filhos, intactos)");

            EditorSceneManager.MarkSceneDirty(cena);
            EditorSceneManager.SaveScene(cena);
            log.AppendLine($"   cena salva: {Cena}");

            Debug.Log(log.ToString());
        }
    }
}
