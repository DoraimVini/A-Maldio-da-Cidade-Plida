using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;
using FavelaAmarela.Runtime.UI;

namespace FavelaAmarela.EditorTools
{
    /// <summary>
    /// Cria e liga o botão <b>Opções</b> nos dois lugares onde ele precisa existir: a tela de
    /// pausa (dentro do <c>HUD_Gameplay</c>) e o menu principal (na <c>Cena_Menu</c>).
    ///
    /// <para><b>Por que os dois.</b> O jogador procura opções <i>antes</i> de começar — no menu
    /// — e <i>durante</i> a partida, quando o som incomoda. Ligar só um dos dois entrega metade
    /// da funcionalidade e a metade errada, dependendo de quem está jogando.</para>
    ///
    /// <para><b>O botão é clonado do que já existe</b> em cada tela, e não montado do zero: assim
    /// ele herda tipografia, sprite, tamanho e âncoras do menu em que vai viver, e não fica com
    /// cara de peça estranha colada. É também menos código para envelhecer.</para>
    ///
    /// <para><b>Idempotente:</b> rodar de novo não cria um segundo botão.</para>
    /// </summary>
    public static class LigarBotaoDeOpcoes
    {
        private const string Marcador = "[BotaoDeOpcoes]";
        private const string Hud = "Assets/FavelaAmarela/Resources/HUD_Gameplay.prefab";
        private const string CenaDoMenu = "Assets/Scenes/Cena_Menu.unity";
        private const string NomeDoBotao = "Botao_Opcoes";

        [MenuItem("Tools/FavelaAmarela/UI: ligar o botão de Opções")]
        public static void Executar()
        {
            var resumo = new List<string> { NoHud(), NoMenuPrincipal() };

            AssetDatabase.SaveAssets();

            string quebra = System.Environment.NewLine + "  ";
            Debug.Log($"{Marcador} Concluído:" + quebra + string.Join(quebra, resumo));
        }

        // ── Tela de pausa, dentro do HUD persistente ──────────────────────────

        private static string NoHud()
        {
            using (var escopo = new PrefabUtility.EditPrefabContentsScope(Hud))
            {
                var menu = escopo.prefabContentsRoot
                    .GetComponentInChildren<MenuDePause>(includeInactive: true);

                if (menu == null) return "HUD: nenhum MenuDePause encontrado";

                return Acrescentar(menu, menu.gameObject, "HUD (tela de pausa)");
            }
        }

        // ── Menu principal, na cena ───────────────────────────────────────────

        private static string NoMenuPrincipal()
        {
            var cena = EditorSceneManager.OpenScene(CenaDoMenu, OpenSceneMode.Single);

            var menu = Object.FindObjectsByType<MenuPrincipal>(
                    FindObjectsInactive.Include, FindObjectsSortMode.None)
                .FirstOrDefault();

            if (menu == null) return "Cena_Menu: nenhum MenuPrincipal encontrado";

            string resultado = Acrescentar(menu, menu.gameObject, "Cena_Menu");

            EditorSceneManager.MarkSceneDirty(cena);
            EditorSceneManager.SaveScene(cena);

            return resultado;
        }

        // ── A peça comum ──────────────────────────────────────────────────────

        /// <summary>
        /// Acha o campo <c>botaoDeOpcoes</c> do menu, e o preenche — clonando um botão irmão
        /// quando ainda não existe um.
        /// </summary>
        private static string Acrescentar(MonoBehaviour menu, GameObject raiz, string onde)
        {
            var so = new SerializedObject(menu);
            var prop = so.FindProperty("botaoDeOpcoes");

            if (prop == null)
                return $"{onde}: campo 'botaoDeOpcoes' não existe em {menu.GetType().Name}";

            if (prop.objectReferenceValue != null)
                return $"{onde}: já estava ligado";

            // Um botão que já exista com o nome (execução anterior interrompida).
            var existente = raiz.GetComponentsInChildren<Button>(includeInactive: true)
                .FirstOrDefault(b => b.name == NomeDoBotao);

            if (existente == null)
            {
                var modelo = EscolherModelo(raiz);
                if (modelo == null) return $"{onde}: nenhum botão para clonar como modelo";

                var clone = Object.Instantiate(modelo.gameObject, modelo.transform.parent);
                clone.name = NomeDoBotao;
                clone.transform.SetSiblingIndex(modelo.transform.GetSiblingIndex() + 1);

                // Herdou os listeners do modelo junto: um botão "Opções" que também fecha o
                // jogo seria pior que nenhum botão.
                var botao = clone.GetComponent<Button>();
                botao.onClick = new Button.ButtonClickedEvent();

                Renomear(clone, "Opções");

                existente = botao;
            }

            prop.objectReferenceValue = existente;
            so.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(menu);

            return $"{onde}: '{NomeDoBotao}' criado e ligado";
        }

        /// <summary>
        /// O botão a clonar. Prefere "Continuar"/"Sair" — os que certamente têm o estilo final
        /// da tela — e cai em qualquer um.
        /// </summary>
        private static Button EscolherModelo(GameObject raiz)
        {
            var botoes = raiz.GetComponentsInChildren<Button>(includeInactive: true)
                .Where(b => b.name != NomeDoBotao)
                .ToArray();

            if (botoes.Length == 0) return null;

            foreach (var preferido in new[] { "Sair", "Continuar" })
            {
                var achado = botoes.FirstOrDefault(
                    b => b.name.IndexOf(preferido, System.StringComparison.OrdinalIgnoreCase) >= 0);

                if (achado != null) return achado;
            }

            return botoes[0];
        }

        private static void Renomear(GameObject botao, string texto)
        {
            // Só Text legado: o projeto inteiro usa uGUI clássico (é o que TipografiaDeDialogo
            // afere), e TextMeshPro não está referenciado por esta assembly.
            foreach (var t in botao.GetComponentsInChildren<Text>(includeInactive: true))
                t.text = texto;
        }
    }
}
