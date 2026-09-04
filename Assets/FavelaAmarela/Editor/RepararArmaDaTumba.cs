using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace FavelaAmarela.EditorTools
{
    /// <summary>
    /// Utilitário de Editor: religa o fluxo da arma da Tumba na <b>cena aberta</b>.
    ///
    /// <para><b>Por que existe (2026-08-14):</b> um playtest mostrou três sintomas que pareciam
    /// bugs distintos e eram wiring de cena:</para>
    /// <list type="number">
    ///   <item>Uma arma aparecia na mão de Damião ao entrar na Tumba — <c>armaInicialParaTeste</c>
    ///   estava sobrescrito para <c>EstileteDeIrem</c> em <c>Tumba_De_Alhazred</c>, sobra do
    ///   balanceamento do Byakhee. O prefab está correto em <c>Nenhuma</c>.</item>
    ///   <item>O baú não entregava arma — <c>tabela</c> em <c>fileID: 0</c>, sem
    ///   <c>Drop_BauDaTumba</c> ligada.</item>
    ///   <item>A arma sumia ao sair da Tumba — consequência de (1): a arma de teste é equipada
    ///   direto no <c>Awake</c> e <b>nunca entra no inventário</b>, então não há o que restaurar.
    ///   (O bug de código que agravava isso — <c>Start</c> assinando <c>OnSlotChanged</c> sem
    ///   aplicar o slot corrente — foi corrigido em <c>MaoFisicaBridge</c>.)</item>
    /// </list>
    ///
    /// <para>Mesmo padrão de <c>WireConfigAssets</c>: atribui via <see cref="SerializedObject"/>.
    /// Não toca em baú que já tenha tabela nem em baú com <c>forcarArma</c> — esse é override
    /// deliberado. Rode com a cena desejada aberta e <b>salve depois</b>; o guarda
    /// <c>BauDaTumbaNoMundoTests</c> confere o resultado.</para>
    /// </summary>
    public static class RepararArmaDaTumba
    {
        private const string CaminhoDaTabela =
            "Assets/FavelaAmarela/Config/Drops/Drop_BauDaTumba.asset";

        [MenuItem("Tools/FavelaAmarela/Reparar Arma da Tumba (cena aberta)")]
        public static void Reparar()
        {
            int mudancas = 0;
            mudancas += LigarTabelaDosBaus();
            mudancas += DesarmarOverrideDeTeste();

            if (mudancas == 0)
            {
                Debug.Log("[RepararArmaDaTumba] Nada a fazer nesta cena.");
                return;
            }

            EditorSceneManager.MarkSceneDirty(
                UnityEngine.SceneManagement.SceneManager.GetActiveScene());
            Debug.Log($"[RepararArmaDaTumba] {mudancas} correção(ões). " +
                      "SALVE A CENA (Ctrl+S) para gravar.");
        }

        /// <summary>Liga <c>Drop_BauDaTumba</c> em todo baú sem fonte de arma.</summary>
        private static int LigarTabelaDosBaus()
        {
            var tabela = AssetDatabase.LoadAssetAtPath<Object>(CaminhoDaTabela);
            if (tabela == null)
            {
                Debug.LogError($"[RepararArmaDaTumba] Asset não encontrado: '{CaminhoDaTabela}'.");
                return 0;
            }

            int ligados = 0;

            foreach (var bau in Object.FindObjectsByType<Runtime.GameLoop.BauDaTumba>(
                         FindObjectsInactive.Include))
            {
                var so = new SerializedObject(bau);
                var pTabela = so.FindProperty("tabela");
                var pForcar = so.FindProperty("forcarArma");

                if (pTabela == null)
                {
                    Debug.LogError("[RepararArmaDaTumba] Campo 'tabela' não existe em BauDaTumba.");
                    continue;
                }

                if (pTabela.objectReferenceValue != null) continue;
                if (pForcar != null && pForcar.boolValue) continue; // override de teste deliberado

                Undo.RecordObject(bau, "Ligar tabela do baú");
                pTabela.objectReferenceValue = tabela;
                so.ApplyModifiedProperties();
                EditorUtility.SetDirty(bau);
                ligados++;

                Debug.Log($"[RepararArmaDaTumba] Baú '{bau.name}': tabela ligada.", bau);
            }

            return ligados;
        }

        /// <summary>
        /// Devolve <c>armaInicialParaTeste</c> a <c>Nenhuma</c>. No jogo real Damião começa
        /// desarmado — a arma vem do baú.
        /// </summary>
        /// <summary>
        /// <b>Inerte desde 2026-08-27.</b> O campo <c>armaInicialParaTeste</c> e o enum
        /// <c>ArmaDeTeste</c> saíram da <c>MaoFisicaBridge</c> na Fase 4 da itemização, junto
        /// com as classes C# de arma: eram a <b>terceira</b> lista de armas do projeto — depois
        /// do enum <c>TipoArmaFisica</c> e do dicionário da fábrica —, todas mantidas à mão e
        /// obrigadas a concordar entre si.
        ///
        /// <para>O defeito que este método consertava <b>deixou de ser possível</b>: não há
        /// mais campo para alguém sobrescrever numa cena e esquecer. Quem quer testar combate
        /// isolado usa o Carcosa Debugger, que concede a arma pelo inventário de verdade — e
        /// por isso ela sobrevive à troca de cena, que era justamente o sintoma
        /// ("a arma sumiu") que a arma de teste produzia.</para>
        ///
        /// <para>Fica como no-op documentado em vez de sumir para que o relatório da ferramenta
        /// continue com a mesma forma, e para que quem procurar pelo defeito antigo encontre a
        /// explicação de por que ele não existe mais.</para>
        /// </summary>
        private static int DesarmarOverrideDeTeste() => 0;
    }
}
