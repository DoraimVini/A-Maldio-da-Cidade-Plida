using UnityEditor;
using UnityEngine;

namespace FavelaAmarela.EditorTools
{
    /// <summary>
    /// Acerta a identidade que a build carrega para fora: nome da janela, nome do executável e
    /// pasta de dados do jogador.
    ///
    /// <para><b>Por que existe (2026-08-20):</b> o <c>productName</c> do projeto era
    /// <b>"A Maldição da Cidade Pálida"</b> — o nome do <b>repositório</b>, mantido por razões
    /// históricas. O título oficial visível ao jogador é <b>"Caminho para Carcosa"</b> (decisão
    /// do Vini de 2026-08-11, registrada no topo do <c>CLAUDE.md</c>, que manda todo texto novo
    /// mostrado ao jogador usar esse nome). Sem esta correção, a janela do jogo e o
    /// <c>.exe</c> entregues ao edital sairiam com o título errado.</para>
    ///
    /// <para>E <c>companyName</c> estava em <c>DefaultCompany</c>, que vai parar no caminho de
    /// save do jogador (<c>%APPDATA%/DefaultCompany/...</c>) e nas propriedades do executável.</para>
    ///
    /// <para><b>O que esta ferramenta NÃO faz:</b> ícone. Definir ícone exige um asset de
    /// textura que ainda não existe — está no plano da build como pendência de arte, e arte foi
    /// adiada por decisão do Vini.</para>
    /// </summary>
    public static class PrepararIdentidadeDaBuild
    {
        /// <summary>Título oficial visível ao jogador. Ver <c>CLAUDE.md</c>, primeira seção.</summary>
        private const string TituloOficial = "Caminho para Carcosa";

        private const string Estudio = "Favela Amarela";

        [MenuItem("Tools/FavelaAmarela/Build: preparar identidade")]
        public static void Executar()
        {
            string produtoAntes = PlayerSettings.productName;
            string estudioAntes = PlayerSettings.companyName;

            PlayerSettings.productName = TituloOficial;
            PlayerSettings.companyName = Estudio;

            AssetDatabase.SaveAssets();

            // Relê do próprio PlayerSettings em vez de assumir que a atribuição pegou.
            bool ok = PlayerSettings.productName == TituloOficial
                   && PlayerSettings.companyName == Estudio;

            if (!ok)
            {
                Debug.LogError("[PrepararIdentidadeDaBuild] Os valores não ficaram gravados: " +
                               $"produto='{PlayerSettings.productName}', " +
                               $"estúdio='{PlayerSettings.companyName}'.");
                return;
            }

            Debug.Log($"[PrepararIdentidadeDaBuild] produto '{produtoAntes}' → '{TituloOficial}'; " +
                      $"estúdio '{estudioAntes}' → '{Estudio}'. " +
                      "Ícone segue pendente (precisa de arte).");
        }
    }
}
