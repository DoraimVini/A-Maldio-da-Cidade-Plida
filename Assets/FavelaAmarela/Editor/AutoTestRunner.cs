using UnityEditor;
using UnityEditor.TestTools.TestRunner.Api;
using UnityEngine;
using System.IO;

namespace FavelaAmarela.Editor
{
    [InitializeOnLoad]
    public static class AutoTestRunner
    {
        private const string PrefsKey = "FavelaAmarela_AutoRunTests_v6";

        static AutoTestRunner()
        {
            if (!EditorPrefs.GetBool(PrefsKey, false))
            {
                EditorPrefs.SetBool(PrefsKey, true);
                EditorApplication.delayCall += RunTests;
            }
        }

        private static void RunTests()
        {
            Debug.Log("[AutoTestRunner] Iniciando testes automatizados (EditMode)...");
            var api = ScriptableObject.CreateInstance<TestRunnerApi>();
            api.RegisterCallbacks(new TestCallbacks());
            api.Execute(new ExecutionSettings(new Filter { testMode = TestMode.EditMode }));
        }

        private class TestCallbacks : ICallbacks
        {
            public void RunStarted(ITestAdaptor testsToRun) { }

            public void RunFinished(ITestResultAdaptor result) 
            { 
                string relatorio = $"Resultado: {result.TestStatus}\n" +
                                   $"Passaram: {result.PassCount}\n" +
                                   $"Falharam: {result.FailCount}\n\n";

                if (result.FailCount > 0)
                {
                    relatorio += "=== FALHAS ===\n" + GetFailures(result);
                }

                File.WriteAllText("TestResults_Auto.txt", relatorio);
                Debug.Log($"[AutoTestRunner] Testes concluídos! Falhas: {result.FailCount}");
            }

            public void TestStarted(ITestAdaptor test) { }

            public void TestFinished(ITestResultAdaptor result) { }

            private string GetFailures(ITestResultAdaptor result)
            {
                string failures = "";
                if (result.TestStatus == TestStatus.Failed && !result.HasChildren)
                {
                    failures += $"- {result.Name}: {result.Message}\n{result.StackTrace}\n\n";
                }
                foreach (var child in result.Children)
                {
                    failures += GetFailures(child);
                }
                return failures;
            }
        }
    }
}
