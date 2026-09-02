using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace FavelaAmarela.EditorTools
{
    /// <summary>
    /// Reaplica no prefab do HUD os tamanhos de fonte que as <b>ferramentas de montagem</b> já
    /// aprenderam — para o conserto valer no arquivo que existe, e não só no próximo que for
    /// gerado.
    ///
    /// <para><b>Por que uma ferramenta separada (2026-09-02).</b> Os montadores
    /// (<c>BuildPainelDeFicha</c>, <c>MontarBarraDeItens</c>) constroem a tela <b>do zero</b> e
    /// rodá-los agora apagaria a fiação que as últimas sessões ligaram — molduras, ícones,
    /// estados de botão. Consertar a origem <b>e</b> reparar o existente são dois trabalhos, e
    /// misturá-los foi o que fez o <c>LigarBotaoDeOpcoes</c> ficar incapaz de curar o próprio
    /// estrago.</para>
    ///
    /// <para>Tudo aqui foi <b>medido</b> pelo <c>LayoutDaUiTests</c>, o primeiro teste do
    /// projeto que carrega o HUD e mede o layout de verdade — não são números escolhidos a
    /// olho.</para>
    /// </summary>
    public static class CaberTextoDaUi
    {
        private const string Marcador = "[CaberTexto]";
        private const string Hud = "Assets/FavelaAmarela/Resources/HUD_Gameplay.prefab";

        /// <summary>
        /// Fonte por caminho na hierarquia. <b>Nenhum destes é palpite:</b> cada um saiu de uma
        /// falha do <c>LayoutDaUiTests</c> com a medida junto.
        /// </summary>
        private static readonly (string Contem, int Fonte, string Razao)[] Tamanhos =
        {
            ("/PainelDeFicha/Corpo", 26,
             "a coluna tem 236 unidades úteis; com fonte 39 cabiam ~12 caracteres e " +
             "'Resistência' era partida no meio. Com 26 cabem ~18"),
        };

        [MenuItem("Tools/FavelaAmarela/UI: reaplicar os tamanhos de fonte medidos")]
        public static void Executar()
        {
            var raiz = PrefabUtility.LoadPrefabContents(Hud);
            var resumo = new List<string>();

            try
            {
                int mudados = 0;

                foreach (var txt in raiz.GetComponentsInChildren<Text>(true))
                {
                    string caminho = Caminho(txt.transform);

                    foreach (var (contem, fonte, razao) in Tamanhos)
                    {
                        if (!caminho.Contains(contem)) continue;
                        if (txt.fontSize == fonte) break;

                        resumo.Add($"{caminho}: fonte {txt.fontSize} → {fonte} — {razao}");

                        Undo.RecordObject(txt, "Fonte medida");
                        txt.fontSize = fonte;
                        EditorUtility.SetDirty(txt);
                        mudados++;
                        break;
                    }
                }

                if (mudados == 0) resumo.Add("nada a mudar (as fontes já são as medidas)");
                else
                {
                    PrefabUtility.SaveAsPrefabAsset(raiz, Hud, out bool gravou);
                    if (!gravou) resumo.Add("PREFAB: SaveAsPrefabAsset RECUSOU");
                }
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(raiz);
            }

            AssetDatabase.SaveAssets();

            string quebra = System.Environment.NewLine + "  ";
            Debug.Log($"{Marcador} Concluído:" + quebra + string.Join(quebra, resumo));
        }

        private static string Caminho(Transform t)
        {
            var partes = new List<string>();
            for (var a = t; a != null; a = a.parent) partes.Add(a.name);
            partes.Reverse();
            return "/" + string.Join("/", partes);
        }
    }
}
