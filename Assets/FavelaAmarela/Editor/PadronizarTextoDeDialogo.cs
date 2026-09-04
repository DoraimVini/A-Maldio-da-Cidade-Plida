using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;
using FavelaAmarela.Runtime.UI;

namespace FavelaAmarela.EditorTools
{
    /// <summary>
    /// Põe todo texto de <b>diálogo</b> — fala e escolha — na mesma tipografia, que se ajusta
    /// ao tamanho do texto em vez de exigir que o texto caiba num tamanho fixo.
    ///
    /// <para><b>O defeito, medido no playtest de 2026-08-28.</b> O Vini reportou: <i>"a do
    /// Abdul está pequena demais e mal dá para ler o texto, já a da Cassilda as letras estão
    /// grandes demais e não cabem na caixa"</i>. É o <b>mesmo componente</b>
    /// (<c>PainelDeEscolha</c>) autorado à mão em cada cena:</para>
    ///
    /// <list type="table">
    ///   <item><term>Tumba_De_Alhazred (Abdul)</term><description>fonte <b>14</b> e <b>16</b></description></item>
    ///   <item><term>Santuario_Yhtill (Cassilda)</term><description>fonte <b>60</b></description></item>
    ///   <item><term>HUD_Gameplay / CaixaDeDialogo</term><description>fonte <b>60</b></description></item>
    /// </list>
    ///
    /// <para>Quatro vezes de diferença no mesmo widget. É a mesma família do zoom de câmera
    /// (sete ferramentas, sete valores): peça autorada em vários lugares, sem nada mantendo os
    /// lugares em acordo.</para>
    ///
    /// <para><b>Por que Best Fit, e não um número melhor.</b> Um tamanho fixo é uma aposta sobre
    /// o comprimento do texto, e o texto deste jogo varia <b>4×</b>: a fala mais curta do Abdul
    /// tem 72 caracteres, a reação mais longa da Cassilda tem <b>278</b>. Nenhum número serve
    /// aos dois. <c>Best Fit</c> escolhe o tamanho por caixa e por fala, que é exatamente a
    /// pergunta que estava sendo respondida à mão — e errada nas duas pontas.</para>
    ///
    /// <para><b>E o overflow vertical vira Overflow, não Truncate.</b> Com Truncate, texto que
    /// não cabe é <b>cortado sem aviso</b> — o jogador perde o fim da frase e nada denuncia.
    /// Transbordar é feio e é <i>visível</i>; cortar é limpo e é mentira. Entre um defeito que
    /// se vê e um que não se vê, este projeto já pagou caro demais pelo segundo.</para>
    ///
    /// <para><b>⚠ ESTA FERRAMENTA JÁ SE CORROMPEU SOZINHA — e por isso ela agora se recusa a
    /// escrever no escuro.</b> Em batch mode, a <c>AssetDatabase</c> serve o artefato em cache do
    /// <c>Library/</c>, e tanto <c>LoadPrefabContents</c> quanto <c>OpenScene</c> carregam a
    /// partir <b>dele</b>, não do arquivo. Quando o <c>.unity</c> ou o <c>.prefab</c> foi editado
    /// fora do Editor, ela lia o valor <b>velho</b>, "corrigia" o que já estava certo, e gravava
    /// o velho por cima do novo. Cinco rodadas seguidas relatando a mesma mudança, com o disco
    /// voltando atrás a cada uma. Nem <c>ImportAsset(ForceUpdate)</c> nem trocar atribuição
    /// direta por <c>SerializedObject</c> resolveram.</para>
    ///
    /// <para>Por isso, antes de escrever, ela <b>compara o que a Unity leu com o que está no
    /// arquivo</b> e aborta a gravação se divergirem. Prefere não fazer nada a desfazer. E ao
    /// terminar ela relê o disco (<see cref="Conferir"/>): o relatório dela não é evidência —
    /// já foi mentira cinco vezes.</para>
    /// </summary>
    public static class PadronizarTextoDeDialogo
    {
        /// <summary>
        /// Os números vêm de <see cref="PadraoDeTextoDeDialogo"/> — a mesma fonte que o guarda
        /// <c>TipografiaDeDialogoTests</c> lê. Duplicá-los aqui seria repetir o defeito que esta
        /// ferramenta existe para consertar.
        /// </summary>
        private const int TamanhoMinimo = PadraoDeTextoDeDialogo.TamanhoMinimo;

        /// <inheritdoc cref="TamanhoMinimo"/>
        private const int TamanhoMaximo = PadraoDeTextoDeDialogo.TamanhoMaximo;

        [MenuItem("Tools/FavelaAmarela/UI: padronizar o texto de diálogo")]
        public static void Executar()
        {
            // Reimporta antes de ler. Em batch mode a AssetDatabase serve o artefato em cache
            // do Library, e LoadPrefabContents carrega a partir DELE -- não do arquivo. Se o
            // .prefab foi editado fora do Editor, a ferramenta lê o valor velho, "corrige" o que
            // já estava certo, e grava o velho por cima. Foi exatamente o que aconteceu aqui.
            AssetDatabase.ImportAsset(CaminhoDaHud, ImportAssetOptions.ForceUpdate);
            AssetDatabase.Refresh(ImportAssetOptions.ForceUpdate);

            var divergentes = ArquivosComCacheVelho();
            if (divergentes.Count > 0)
            {
                Debug.LogError("[TextoDeDialogo] ABORTADO sem escrever nada. A AssetDatabase está " +
                               "servindo cache velho destes arquivos:" + Quebra +
                               string.Join(Quebra, divergentes) + Quebra +
                               "Escrever agora GRAVARIA O VALOR VELHO por cima do que está no " +
                               "disco. Conserto: apagar Library/ e reabrir o projeto, ou fazer a " +
                               "mudança pelo Editor aberto." + Quebra + Conferir());
                return;
            }

            var resumo = new List<string>();

            resumo.AddRange(NoPrefabDaHud());

            foreach (var caminho in Cenas())
            {
                var cena = EditorSceneManager.OpenScene(caminho, OpenSceneMode.Single);
                var mudancas = new List<string>();

                foreach (var alvo in AlvosNaCenaAberta())
                    mudancas.AddRange(Aplicar(alvo, Path.GetFileNameWithoutExtension(caminho)));

                if (mudancas.Count == 0) continue;

                resumo.AddRange(mudancas);
                EditorSceneManager.MarkSceneDirty(cena);
                if (!EditorSceneManager.SaveScene(cena))
                    resumo.Add($"{Path.GetFileName(caminho)}: SaveScene RECUSOU");
            }

            // SEM AssetDatabase.SaveAssets() aqui, e isso é o conserto de um defeito real:
            // SaveAsPrefabAsset já grava o arquivo, e um SaveAssets() depois reescreve o ASSET
            // que continua carregado em memória — e ele nunca foi modificado, porque quem foi
            // modificado é a CÓPIA devolvida por LoadPrefabContents. O efeito era a ferramenta
            // gravar 44 e depois gravar 60 por cima, relatando "60 → 44" a cada rodada, para
            // sempre. Foram quatro rodadas idênticas até o disco denunciar.
            Debug.Log("[TextoDeDialogo] " +
                      (resumo.Count == 0
                          ? "Nada a mudar."
                          : "Mudanças:\n  " + string.Join("\n  ", resumo)) +
                      "\n  " + Conferir());
        }

        /// <summary>
        /// Arquivos cujo conteúdo em disco <b>já está no padrão</b> mas que a Unity ainda lê
        /// fora dele — a assinatura de artefato velho no <c>Library/</c>. Escrever nesse estado
        /// desfaz o que está correto.
        /// </summary>
        private static List<string> ArquivosComCacheVelho()
        {
            var fora = new List<string>();

            foreach (var caminho in Cenas().Concat(new[] { CaminhoDaHud }))
            {
                if (!File.Exists(caminho)) continue;
                if (!NoPadraoEmDisco(caminho)) continue;   // disco fora: a ferramenta TEM o que fazer

                // Disco no padrão. Se a Unity ainda enxerga outra coisa, é cache velho.
                var memoria = ValoresQueAUnityEnxerga(caminho);
                if (memoria != null && memoria != (TamanhoMinimo, TamanhoMaximo))
                    fora.Add($"{Path.GetFileName(caminho)}: disco diz " +
                             $"{TamanhoMinimo}/{TamanhoMaximo}, a Unity lê " +
                             $"{memoria.Value.Item1}/{memoria.Value.Item2}");
            }

            return fora;
        }

        /// <summary>Se todo texto de diálogo do arquivo já está no padrão, lido do disco.</summary>
        private static bool NoPadraoEmDisco(string caminho)
        {
            foreach (Match m in Regex.Matches(File.ReadAllText(caminho), PadraoNoYaml))
            {
                if (m.Groups[1].Value != "1") continue;

                if (m.Groups[2].Value != TamanhoMinimo.ToString() ||
                    m.Groups[3].Value != TamanhoMaximo.ToString())
                    return false;
            }

            return true;
        }

        private static (int, int)? ValoresQueAUnityEnxerga(string caminho)
        {
            if (caminho == CaminhoDaHud)
            {
                var asset = AssetDatabase.LoadAssetAtPath<GameObject>(caminho);
                var dica = asset != null ? asset.GetComponentInChildren<TutorialHintUI>(true) : null;
                var t = dica != null ? dica.TextoDeSaida : null;
                return t == null ? null : (t.resizeTextMinSize, t.resizeTextMaxSize);
            }

            var cena = EditorSceneManager.OpenScene(caminho, OpenSceneMode.Single);
            if (!cena.IsValid()) return null;

            foreach (var alvo in AlvosNaCenaAberta())
                return (alvo.resizeTextMinSize, alvo.resizeTextMaxSize);

            return null;
        }

        /// <summary>
        /// Relê os arquivos <b>do disco</b> e conta quantos textos de diálogo ficaram fora do
        /// padrão. É o Corolário 4 do COMMANDMENT aplicado à própria ferramenta: o log dela não
        /// é evidência — esta classe mentiu quatro rodadas seguidas, dizendo que tinha mudado um
        /// valor que continuava igual no arquivo.
        /// </summary>
        private static string Conferir()
        {
            int fora = 0;

            foreach (var caminho in Cenas().Concat(new[] { CaminhoDaHud }))
            {
                foreach (Match m in Regex.Matches(File.ReadAllText(caminho), PadraoNoYaml))
                {
                    // Só os que já estão em Best Fit; os outros Text da HUD são rótulos fixos.
                    if (m.Groups[1].Value != "1") continue;

                    if (m.Groups[2].Value != TamanhoMinimo.ToString() ||
                        m.Groups[3].Value != TamanhoMaximo.ToString())
                        fora++;
                }
            }

            return fora == 0
                ? "CONFERIDO NO DISCO: nenhum texto de diálogo fora do padrão."
                : $"ATENÇÃO — CONFERIDO NO DISCO: {fora} texto(s) ainda fora do padrão. " +
                  "A gravação NÃO pegou; não confie no relatório acima.";
        }

        /// <summary>Quebra de linha indentada dos relatórios.</summary>
        private const string Quebra = "\n  ";

        /// <summary>
        /// O trio Best Fit / mínimo / máximo como a Unity serializa. Numa constante porque três
        /// lugares desta classe precisam dele — e repetir o mesmo regex três vezes seria a
        /// mesma doença que esta classe existe para curar.
        /// </summary>
        private const string PadraoNoYaml =
            @"m_BestFit:\s*(\d+)\s*\r?\n\s*m_MinSize:\s*(\d+)\s*\r?\n\s*m_MaxSize:\s*(\d+)";

        private const string CaminhoDaHud = "Assets/FavelaAmarela/Resources/HUD_Gameplay.prefab";

        private static IEnumerable<string> Cenas() =>
            Directory.GetFiles("Assets/Scenes", "*.unity", SearchOption.AllDirectories)
                     .Select(c => c.Replace(Path.DirectorySeparatorChar, '/'))
                     .OrderBy(c => c);

        /// <summary>
        /// Os <c>Text</c> de diálogo da cena aberta. <b>Derivado, não por nome:</b> é o campo
        /// <c>texto</c> de quem escreve diálogo (<see cref="TutorialHintUI"/> e
        /// <see cref="PainelDeEscolha"/>). Uma lista de nomes de GameObject envelheceria — e
        /// aqui os objetos se chamam todos "Texto", o que a tornaria ambígua além de frágil.
        /// </summary>
        private static IEnumerable<Text> AlvosNaCenaAberta()
        {
            foreach (var dica in Object.FindObjectsByType<TutorialHintUI>(FindObjectsInactive.Include))
            {
                if (dica.TextoDeSaida != null) yield return dica.TextoDeSaida;
            }

            foreach (var escolha in Object.FindObjectsByType<PainelDeEscolha>(FindObjectsInactive.Include))
            {
                if (escolha.TextoDeSaida != null) yield return escolha.TextoDeSaida;
            }
        }

        /// <summary>
        /// A caixa de diálogo viva mora no <c>HUD_Gameplay.prefab</c>, que é asset — não aparece
        /// em varredura de cena.
        /// </summary>
        private static IEnumerable<string> NoPrefabDaHud()
        {
            const string caminho = CaminhoDaHud;
            var resumo = new List<string>();

            if (!File.Exists(caminho))
            {
                resumo.Add($"HUD_Gameplay: prefab ausente em '{caminho}'");
                return resumo;
            }

            var raiz = PrefabUtility.LoadPrefabContents(caminho);

            try
            {
                foreach (var dica in raiz.GetComponentsInChildren<TutorialHintUI>(true))
                    resumo.AddRange(Aplicar(dica.TextoDeSaida, "HUD_Gameplay"));

                foreach (var escolha in raiz.GetComponentsInChildren<PainelDeEscolha>(true))
                    resumo.AddRange(Aplicar(escolha.TextoDeSaida, "HUD_Gameplay"));

                if (resumo.Count > 0)
                {
                    // O retorno de SaveAsPrefabAsset é evidência; supor que ele gravou não é.
                    PrefabUtility.SaveAsPrefabAsset(raiz, caminho, out bool gravou);
                    if (!gravou)
                        resumo.Add("HUD_Gameplay: SaveAsPrefabAsset RECUSOU — o arquivo não mudou");
                }
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(raiz);
            }

            return resumo;
        }

        /// <summary>
        /// Escreve o padrão num <c>Text</c>, <b>via <c>SerializedObject</c></b>.
        ///
        /// <para>A versão anterior atribuía as propriedades em C# direto
        /// (<c>texto.resizeTextMaxSize = 44</c>). Dentro de
        /// <c>PrefabUtility.LoadPrefabContents</c> isso <b>não é gravado</b>: o
        /// <c>SaveAsPrefabAsset</c> devolvia sucesso, o log dizia "60 → 44", e o arquivo
        /// continuava 60 — cinco rodadas seguidas. <c>SerializedObject</c> +
        /// <c>ApplyModifiedPropertiesWithoutUndo</c> é o caminho que o resto das ferramentas
        /// deste projeto já usa e que comprovadamente persiste
        /// (<c>MarcarCorposImpregnados</c>, <c>PadraoDeCamera</c>).</para>
        /// </summary>
        private static IEnumerable<string> Aplicar(Text texto, string onde)
        {
            var notas = new List<string>();
            if (texto == null) return notas;

            var so = new SerializedObject(texto);
            var fonte = so.FindProperty("m_FontData");

            if (fonte == null)
            {
                notas.Add($"{onde} · '{texto.name}': sem m_FontData — o Text da UGUI mudou de forma");
                return notas;
            }

            Ajustar(fonte, "m_BestFit", 1, notas, onde, texto.name, "Best Fit");
            Ajustar(fonte, "m_MinSize", TamanhoMinimo, notas, onde, texto.name, "mínimo");
            Ajustar(fonte, "m_MaxSize", TamanhoMaximo, notas, onde, texto.name, "máximo");
            Ajustar(fonte, "m_HorizontalOverflow", 0, notas, onde, texto.name, "horizontal (Wrap)");

            // Truncate corta a fala sem avisar. Ver o resumo desta classe.
            Ajustar(fonte, "m_VerticalOverflow", 1, notas, onde, texto.name, "vertical (Overflow)");

            if (notas.Count > 0) so.ApplyModifiedPropertiesWithoutUndo();

            return notas;
        }

        private static void Ajustar(SerializedProperty fonte, string campo, int alvo,
                                    List<string> notas, string onde, string nome, string rotulo)
        {
            var prop = fonte.FindPropertyRelative(campo);
            if (prop == null)
            {
                notas.Add($"{onde} · '{nome}': campo '{campo}' não existe mais");
                return;
            }

            if (prop.intValue == alvo) return;

            notas.Add($"{onde} · '{nome}': {rotulo} {prop.intValue} → {alvo}");
            prop.intValue = alvo;
        }
    }
}
