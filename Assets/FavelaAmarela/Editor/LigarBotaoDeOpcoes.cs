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
            var resumo = new List<string> { NoHud(), NoMenuPrincipal("botaoDeOpcoes", NomeDoBotao, "Opções") };

            AssetDatabase.SaveAssets();

            string quebra = System.Environment.NewLine + "  ";
            Debug.Log($"{Marcador} Concluído:" + quebra + string.Join(quebra, resumo));
        }

        /// <summary>
        /// O botão de <b>Créditos</b>, só no menu principal — créditos não se abrem da pausa.
        /// Mesma receita: clona um irmão, desce um degrau, liga no campo. Ver <c>PainelDeCreditos</c>
        /// para o porquê da tela (condição de licença do Sucart e do Warren Clark).
        /// </summary>
        [MenuItem("Tools/FavelaAmarela/UI: ligar o botão de Créditos")]
        public static void ExecutarCreditos()
        {
            string resumo = NoMenuPrincipal("botaoDeCreditos", "Botao_Creditos", "Créditos");
            AssetDatabase.SaveAssets();
            Debug.Log($"[BotaoDeCreditos] Concluído: {resumo}");
        }

        // ── Tela de pausa, dentro do HUD persistente ──────────────────────────

        private static string NoHud()
        {
            using (var escopo = new PrefabUtility.EditPrefabContentsScope(Hud))
            {
                var menu = escopo.prefabContentsRoot
                    .GetComponentInChildren<MenuDePause>(includeInactive: true);

                if (menu == null) return "HUD: nenhum MenuDePause encontrado";

                return Acrescentar(menu, menu.gameObject, "HUD (tela de pausa)", "botaoDeOpcoes", NomeDoBotao, "Opções");
            }
        }

        // ── Menu principal, na cena ───────────────────────────────────────────

        private static string NoMenuPrincipal(string campo, string nomeDoBotao, string rotulo)
        {
            var cena = EditorSceneManager.OpenScene(CenaDoMenu, OpenSceneMode.Single);

            var menu = Object.FindObjectsByType<MenuPrincipal>(
                    FindObjectsInactive.Include)
                .FirstOrDefault();

            if (menu == null) return "Cena_Menu: nenhum MenuPrincipal encontrado";

            string resultado = Acrescentar(menu, menu.gameObject, "Cena_Menu", campo, nomeDoBotao, rotulo);

            EditorSceneManager.MarkSceneDirty(cena);
            EditorSceneManager.SaveScene(cena);

            return resultado;
        }

        // ── A peça comum ──────────────────────────────────────────────────────

        /// <summary>
        /// Acha o campo <c>botaoDeOpcoes</c> do menu, e o preenche — clonando um botão irmão
        /// quando ainda não existe um.
        /// </summary>
        private static string Acrescentar(MonoBehaviour menu, GameObject raiz, string onde,
                                          string campo, string nomeDoBotao, string rotulo)
        {
            var so = new SerializedObject(menu);
            var prop = so.FindProperty(campo);

            if (prop == null)
                return $"{onde}: campo '{campo}' não existe em {menu.GetType().Name}";

            if (prop.objectReferenceValue != null)
            {
                // ANTES esta guarda fazia `return "já estava ligado"` e pronto -- e é por isso
                // que o menu de pause ficou com "Opções" em cima de "Sair do jogo" durante
                // semanas. O clone foi criado por uma versão deste script que ainda não tinha o
                // DescerUmDegrau; quando o degrau chegou, a guarda de idempotência já impedia
                // qualquer execução de alcançá-lo. A ferramenta sabia do defeito, tinha a cura,
                // e não conseguia aplicá-la no estrago que ela mesma havia feito.
                //
                // Ligar a referência e POSICIONAR o botão são dois trabalhos: o primeiro é
                // idempotente, o segundo é reparo. Separados.
                var jaLigado = prop.objectReferenceValue as Button;

                if (jaLigado == null)
                    return $"{onde}: '{campo}' aponta para algo que não é Button";

                return $"{onde}: já estava ligado{Desempilhar(jaLigado, raiz)}";
            }

            // Um botão que já exista com o nome (execução anterior interrompida).
            var existente = raiz.GetComponentsInChildren<Button>(includeInactive: true)
                .FirstOrDefault(b => b.name == nomeDoBotao);

            if (existente == null)
            {
                var modelo = EscolherModelo(raiz, nomeDoBotao);
                if (modelo == null) return $"{onde}: nenhum botão para clonar como modelo";

                var clone = Object.Instantiate(modelo.gameObject, modelo.transform.parent);
                clone.name = nomeDoBotao;
                clone.transform.SetSiblingIndex(modelo.transform.GetSiblingIndex() + 1);

                // Clonar copia as ÂNCORAS junto. Onde não há layout automático, isso põe o
                // clone exatamente em cima do modelo -- e o irmão posterior desenha por cima,
                // então o botão clonado ESCONDE o original. Foi o que aconteceu no menu
                // principal: o "Sair" continuou lá, ativo e clicável, invisível debaixo do
                // "Opções", e o jogo pareceu não ter como fechar.
                if (modelo.GetComponentInParent<LayoutGroup>() == null)
                    DescerUmDegrau(clone.GetComponent<RectTransform>(),
                                   modelo.GetComponent<RectTransform>(), raiz);

                // Herdou os listeners do modelo junto: um botão "Opções" que também fecha o
                // jogo seria pior que nenhum botão.
                var botao = clone.GetComponent<Button>();
                botao.onClick = new Button.ButtonClickedEvent();

                Renomear(clone, rotulo);

                existente = botao;
            }

            prop.objectReferenceValue = existente;
            so.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(menu);

            return $"{onde}: '{nomeDoBotao}' criado e ligado";
        }

        /// <summary>
        /// Se o botão estiver <b>em cima de um irmão</b>, desce um degrau. Roda mesmo quando a
        /// referência já está ligada — é o reparo que a guarda de idempotência bloqueava.
        ///
        /// <para>Compara as <b>âncoras</b>, não os retângulos: neste projeto todo posicionamento
        /// de UI é por âncora normalizada (não há um único <c>LayoutGroup</c> no HUD), então
        /// âncoras iguais são a definição de "um em cima do outro".</para>
        /// </summary>
        /// <returns>Texto para o resumo — vazio quando não havia nada a consertar.</returns>
        private static string Desempilhar(Button botao, GameObject raiz)
        {
            var meu = botao.GetComponent<RectTransform>();
            if (meu == null || meu.parent == null) return "";

            var colidido = raiz.GetComponentsInChildren<Button>(includeInactive: true)
                .Where(b => b != botao)
                .Select(b => b.GetComponent<RectTransform>())
                .FirstOrDefault(r => r != null && r.parent == meu.parent &&
                                     r.anchorMin == meu.anchorMin &&
                                     r.anchorMax == meu.anchorMax);

            if (colidido == null) return "";

            var antes = meu.anchorMin;
            DescerUmDegrau(meu, colidido, raiz);

            if (meu.anchorMin == antes)
                return $" — EMPILHADO em '{colidido.name}' e não consegui medir o degrau";

            EditorUtility.SetDirty(botao);

            return $" — estava EMPILHADO em '{colidido.name}' (âncoras idênticas); " +
                   $"desceu de y {antes.y:0.###} para {meu.anchorMin.y:0.###}";
        }

        /// <summary>
        /// Desloca o clone um "degrau" para baixo, medindo o degrau nos <b>irmãos que já
        /// existem</b> em vez de chutar um número.
        ///
        /// <para>O espaçamento de um menu é decisão de layout de quem o desenhou; copiá-lo dos
        /// próprios botões faz o clone entrar no ritmo da tela em vez de impor o meu.</para>
        /// </summary>
        private static void DescerUmDegrau(RectTransform clone, RectTransform modelo,
                                           GameObject raiz)
        {
            var irmaos = raiz.GetComponentsInChildren<Button>(includeInactive: true)
                .Select(b => b.GetComponent<RectTransform>())
                .Where(r => r != null && r != clone && r.parent == modelo.parent)
                .Select(r => r.anchorMin.y)
                .Distinct()
                .OrderByDescending(y => y)
                .ToArray();

            // Menos de dois irmãos: não há degrau a medir. Meio décimo da própria altura é um
            // fallback conservador -- separa sem inventar um ritmo.
            float degrau = irmaos.Length >= 2
                ? irmaos[0] - irmaos[1]
                : (modelo.anchorMax.y - modelo.anchorMin.y) * 1.3f;

            if (degrau <= 0f) return;

            // Um degrau abaixo do irmão MAIS BAIXO, não do modelo (2026-09-10): o segundo botão
            // clonado do "Sair" (Créditos) cairia exatamente onde o primeiro (Opções) já estava.
            float maisBaixo = irmaos.Length > 0 ? irmaos[irmaos.Length - 1] : modelo.anchorMin.y;
            float altura = modelo.anchorMax.y - modelo.anchorMin.y;

            clone.anchorMin = new Vector2(modelo.anchorMin.x, maisBaixo - degrau);
            clone.anchorMax = new Vector2(modelo.anchorMax.x, maisBaixo - degrau + altura);
        }

        /// <summary>
        /// O botão a clonar. Prefere "Continuar"/"Sair" — os que certamente têm o estilo final
        /// da tela — e cai em qualquer um.
        /// </summary>
        private static Button EscolherModelo(GameObject raiz, string nomeDoBotao)
        {
            var botoes = raiz.GetComponentsInChildren<Button>(includeInactive: true)
                .Where(b => b.name != nomeDoBotao)
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
