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

            string limpeza = ApagarOQueNaoSeEntrega(pasta);

            Debug.Log($"{Marcador} OK — {resumo.result}, " +
                      $"{resumo.totalSize / (1024 * 1024)} MB, " +
                      $"{resumo.totalTime.TotalSeconds:0} s, " +
                      $"{resumo.totalWarnings} aviso(s).\n" +
                      $"{Marcador} Console de runtime (F1): " +
                      $"{(desenvolvimento ? "PRESENTE" : "AUSENTE — não foi compilado")}\n" +
                      $"{Marcador} {limpeza}\n" +
                      $"{Marcador} Saída: {Path.GetFullPath(opcoes.locationPathName)}");
        }

        /// <summary>
        /// Apaga da pasta de saída o que a Unity gera ao lado da build e <b>manda não enviar</b>:
        /// as pastas <c>*_BurstDebugInformation_DoNotShip</c> (e
        /// <c>*_BackUpThisFolder_ButDontShipItWithYourGame</c>, do IL2CPP).
        ///
        /// <para><b>O que havia nela (2026-09-10, o Vini perguntou).</b> Um único
        /// <c>lib_burst_generated.txt</c> de 240 KB: o log do compilador Burst — opções, a lista
        /// de funções compiladas dos pacotes da Unity, e <b>19 caminhos absolutos da máquina</b>
        /// (<c>C:/Users/Vini/...</c>). Não roda nada e o jogo não a lê; serve para casar um crash
        /// do Burst com o símbolo. Fora do zip de entrega, sempre.</para>
        /// </summary>
        private static string ApagarOQueNaoSeEntrega(string pasta)
        {
            if (!Directory.Exists(pasta)) return "nada a limpar";

            var apagadas = new System.Collections.Generic.List<string>();
            foreach (var dir in Directory.GetDirectories(pasta))
            {
                string nome = Path.GetFileName(dir);
                if (!nome.EndsWith("_DoNotShip") && !nome.Contains("ButDontShipItWithYourGame")) continue;

                Directory.Delete(dir, recursive: true);
                apagadas.Add(nome);
            }

            return apagadas.Count == 0
                ? "nada marcado como DoNotShip ao lado da build"
                : $"apagado(s) da saída: {string.Join(", ", apagadas)}";
        }
    }
}
