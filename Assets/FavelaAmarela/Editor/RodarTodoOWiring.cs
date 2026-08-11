using System;
using UnityEditor;
using UnityEngine;

namespace FavelaAmarela.EditorTools
{
    /// <summary>
    /// Ferramenta de Editor. Roda <b>todas as ferramentas de wiring</b> na ordem certa, numa
    /// só passada.
    ///
    /// <para><b>Para que serve:</b> o projeto acumulou sete ferramentas que montam cena,
    /// prefab e build. Rodá-las à mão, na ordem correta, é trabalho chato e fácil de errar —
    /// e depois de um clone limpo (cenário real: o desastre de 2026-08-10) é o que separa um
    /// checkout de um jogo jogável.</para>
    ///
    /// <para><b>Roda headless:</b></para>
    /// <code>
    /// Unity.exe -batchmode -nographics -quit -projectPath . \
    ///   -executeMethod FavelaAmarela.EditorTools.RodarTodoOWiring.Executar
    /// </code>
    ///
    /// <para>Cada etapa é isolada num <c>try</c>: uma falha não impede as seguintes, e o
    /// relatório final diz exatamente o que passou e o que não. Todas as ferramentas
    /// chamadas aqui são idempotentes, então repetir é seguro.</para>
    /// </summary>
    public static class RodarTodoOWiring
    {
        [MenuItem("Tools/FavelaAmarela/Rodar TODO o wiring (na ordem)")]
        public static void Executar()
        {
            int ok = 0;
            int falhas = 0;

            // A ordem importa em dois pontos:
            //   • Prefabs antes das cenas — instâncias herdam o que o prefab ganhou.
            //   • Telas de fluxo antes dos Refúgios, porque o renascimento aponta para os
            //     pontos de chegada que a ferramenta dos Refúgios cria.
            Etapa("Cena de menu + build settings", MontarCenaDeMenu.Executar, ref ok, ref falhas);
            Etapa("Aliados (layer + barra de vida)", MontarAliados.Executar, ref ok, ref falhas);
            Etapa("Sistemas novos (artefatos, áudio, save, drop)", LigarSistemasNovos.Executar, ref ok, ref falhas);
            Etapa("Telas de fluxo (pause, colapso)", MontarTelasDeFluxo.Executar, ref ok, ref falhas);
            Etapa("Refúgios de Luz (+ pontos de renascimento)", MontarRefugiosDeLuz.Executar, ref ok, ref falhas);
            Etapa("Painel de inventário (Tab)", MontarPainelDeInventario.Executar, ref ok, ref falhas);
            Etapa("Povoar o Deserto de Hali", PovoarODeserto.Executar, ref ok, ref falhas);

            AssetDatabase.SaveAssets();

            Debug.Log($"===== WIRING COMPLETO: {ok} etapa(s) OK, {falhas} com falha =====");

            if (falhas > 0)
                Debug.LogError($"[Wiring] {falhas} etapa(s) falharam — veja os erros acima.");
        }

        /// <summary>
        /// Roda uma etapa isolada. Uma ferramenta que estoure não pode levar as outras com
        /// ela: em batch mode o resultado seria um projeto religado pela metade, sem sinal de
        /// onde parou.
        /// </summary>
        private static void Etapa(string nome, Action acao, ref int ok, ref int falhas)
        {
            Debug.Log($"----- [Wiring] {nome} -----");

            try
            {
                acao();
                ok++;
            }
            catch (Exception e)
            {
                falhas++;
                Debug.LogError($"[Wiring] Etapa '{nome}' falhou: {e.Message}\n{e.StackTrace}");
            }
        }
    }
}
