using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace FavelaAmarela.EditorTools
{
    /// <summary>
    /// Gera a build do jogo, em <b>Windows 64</b>, e <b>prova</b> o resultado.
    ///
    /// <para><b>Por que existe (2026-08-29).</b> O projeto não tinha script de build nenhum —
    /// toda build saía do diálogo do Editor, à mão, sem registro do que entrou nela. Para um
    /// projeto de edital com prazo, "eu acho que marquei Development Build" não é um estado
    /// aceitável: é a diferença entre entregar o jogo e entregar o jogo com um console de
    /// trapaça dentro.</para>
    ///
    /// <para><b>Os dois modos são deliberadamente separados em menus diferentes</b>, e o nome de
    /// cada um diz o que ele faz. Um único menu com uma caixinha seria a mesma armadilha do
    /// diálogo do Editor.</para>
    ///
    /// <list type="bullet">
    ///   <item><b>Desenvolvimento</b> — define <c>DEVELOPMENT_BUILD</c>, então o
    ///   <c>ConsoleDeCarcosa</c> (F1) existe. É a build para <i>jogar aferindo</i>: conceder
    ///   item, pular para um chefe, subir de nível.</item>
    ///   <item><b>Entrega</b> — sem símbolo nenhum. O console <b>não é compilado</b>: a classe
    ///   nem chega a existir no player.</item>
    /// </list>
    /// </summary>
    public static class GerarBuild
    {
        private const string Marcador = "[Build]";

        [MenuItem("Tools/FavelaAmarela/Build: DESENVOLVIMENTO (com console F1)")]
        public static void Desenvolvimento() => Construir(desenvolvimento: true);

        [MenuItem("Tools/FavelaAmarela/Build: ENTREGA (sem console)")]
        public static void Entrega() => Construir(desenvolvimento: false);

        /// <summary>
        /// Ponto de entrada para linha de comando. Lê <c>-favelaModo entrega</c> dos argumentos;
        /// qualquer outra coisa (ou nada) constrói em modo de desenvolvimento.
        /// </summary>
        public static void PelaLinhaDeComando()
        {
            var args = Environment.GetCommandLineArgs();
            int i = Array.IndexOf(args, "-favelaModo");

            bool entrega = i >= 0 && i + 1 < args.Length &&
                           string.Equals(args[i + 1], "entrega", StringComparison.OrdinalIgnoreCase);

            Construir(desenvolvimento: !entrega);
        }

        private static void Construir(bool desenvolvimento)
        {
            var cenas = EditorBuildSettings.scenes
                .Where(c => c.enabled)
                .Select(c => c.path)
                .ToArray();

            if (cenas.Length == 0)
            {
                Debug.LogError($"{Marcador} Nenhuma cena habilitada em Build Settings — a build " +
                               "sairia vazia.");
                return;
            }

            string pasta = Path.Combine("Builds", desenvolvimento ? "Desenvolvimento" : "Entrega");
            Directory.CreateDirectory(pasta);

            var opcoes = new BuildPlayerOptions
            {
                scenes = cenas,
                locationPathName = Path.Combine(pasta, "CaminhoParaCarcosa.exe"),
                target = BuildTarget.StandaloneWindows64,
                targetGroup = BuildTargetGroup.Standalone,

                // É ESTE bit que define DEVELOPMENT_BUILD, e portanto o que decide se o
                // ConsoleDeCarcosa existe no player. Nada mais precisa ser tocado.
                options = desenvolvimento
                    ? BuildOptions.Development | BuildOptions.AllowDebugging
                    : BuildOptions.None,
            };

            Debug.Log($"{Marcador} Iniciando build de " +
                      $"{(desenvolvimento ? "DESENVOLVIMENTO" : "ENTREGA")} com " +
                      $"{cenas.Length} cena(s) → {opcoes.locationPathName}");

            var relatorio = BuildPipeline.BuildPlayer(opcoes);
            var resumo = relatorio.summary;

            if (resumo.result != BuildResult.Succeeded)
            {
                // O relatório traz os erros; imprimi-los aqui evita ter de caçá-los no log.
                foreach (var passo in relatorio.steps)
                {
                    foreach (var msg in passo.messages)
                    {
                        if (msg.type == LogType.Error || msg.type == LogType.Exception)
                            Debug.LogError($"{Marcador}   {passo.name}: {msg.content}");
                    }
                }

                Debug.LogError($"{Marcador} FALHOU: {resumo.result}, {resumo.totalErrors} erro(s).");
                return;
            }

            Debug.Log($"{Marcador} OK — {resumo.result}, " +
                      $"{resumo.totalSize / (1024 * 1024)} MB, " +
                      $"{resumo.totalTime.TotalSeconds:0} s, " +
                      $"{resumo.totalWarnings} aviso(s).\n" +
                      $"{Marcador} Console de runtime (F1): " +
                      $"{(desenvolvimento ? "PRESENTE" : "AUSENTE — não foi compilado")}\n" +
                      $"{Marcador} Saída: {Path.GetFullPath(opcoes.locationPathName)}");
        }
    }
}
