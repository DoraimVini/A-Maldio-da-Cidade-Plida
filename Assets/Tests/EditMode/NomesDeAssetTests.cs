using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using NUnit.Framework;

namespace FavelaAmarela.Tests.EditMode
{
    /// <summary>
    /// Guarda os nomes de arquivo que a <b>Unity transforma em nome de estado de Animator</b>.
    ///
    /// <para><b>O aviso que originou este arquivo (2026-08-28).</b> Toda importação despejava no
    /// console <c>'.' is not allowed in State name</c>, vindo de
    /// <c>ScriptedImporter:GenerateAssetData</c> — sem dizer <b>qual</b> asset. A causa era um
    /// único arquivo: <c>Damiao_Clean_Spritesheet.sliced.aseprite</c>. O importador de Aseprite
    /// gera um <c>AnimatorController</c> a partir do arquivo e deriva os nomes de estado do nome
    /// dele; ponto é caractere <b>ilegal</b> em nome de estado, então a Unity reclama a cada
    /// reimportação, para sempre.</para>
    ///
    /// <para>Aviso repetido é pior que aviso: ele treina a pessoa a não ler o console. Foi ruído
    /// de console que escondeu por semanas o <i>"There are no audio listeners in the scene"</i>
    /// — três cenas mudas, incluindo a Fase 1 do Vertical Slice.</para>
    ///
    /// <para><b>Só arquivos de animação.</b> Ponto em nome de textura ou de áudio é inofensivo;
    /// a regra existe onde o nome vira estado.</para>
    /// </summary>
    public sealed class NomesDeAssetTests
    {
        /// <summary>
        /// Extensões cujo importador gera <c>AnimatorController</c> a partir do arquivo, e
        /// portanto derivam nome de estado do nome do asset.
        /// </summary>
        private static readonly string[] GeramEstadoDeAnimator = { ".aseprite", ".ase" };

        [Test]
        public void NenhumArquivoDeAnimacao_TemPontoNoNome()
        {
            var comPonto = new List<string>();
            var vistos = 0;

            foreach (var caminho in Directory.GetFiles("Assets", "*", SearchOption.AllDirectories))
            {
                string extensao = Path.GetExtension(caminho).ToLowerInvariant();
                if (!GeramEstadoDeAnimator.Contains(extensao)) continue;

                vistos++;

                // Path.GetFileNameWithoutExtension tira só a ÚLTIMA extensão: em
                // "Damiao_Clean_Spritesheet.sliced.aseprite" sobra "…Spritesheet.sliced",
                // que é exatamente o nome que a Unity tentaria usar como estado.
                string nome = Path.GetFileNameWithoutExtension(caminho);
                if (nome.Contains('.'))
                    comPonto.Add($"{caminho.Replace('\\', '/')} → estado \"{nome}\"");
            }

            Assert.Greater(vistos, 0,
                "Nenhum arquivo de animação encontrado — este guarda parou de olhar para o " +
                "projeto (a arte mudou de formato?).");

            Assert.IsEmpty(comPonto,
                "Arquivo(s) de animação com ponto no nome:" + Environment.NewLine + "  " +
                string.Join(Environment.NewLine + "  ", comPonto) + Environment.NewLine +
                "A Unity deriva o nome do estado de Animator do nome do arquivo, e ponto é " +
                "ilegal em nome de estado: ela avisa a CADA reimportação, para sempre. " +
                "Conserto: renomear trocando o ponto por '_' (leve o .meta junto, para o GUID " +
                "sobreviver e nenhuma referência quebrar).");
        }
    }
}
