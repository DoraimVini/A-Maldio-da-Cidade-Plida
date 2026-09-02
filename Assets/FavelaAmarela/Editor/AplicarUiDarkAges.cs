using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

namespace FavelaAmarela.EditorTools
{
    /// <summary>
    /// Veste a interface com o pacote <b>Dark Ages UI</b> (Hypnobius), que já estava no projeto
    /// e era usado por <b>uma sprite só</b>.
    ///
    /// <para><b>O pedido do Vini (2026-09-01):</b> <i>"vamos usar essa UI nas coisas que estão
    /// faltando."</i></para>
    ///
    /// <para><b>O que a medição encontrou.</b> Das 25 artes do pacote, apenas 3 estavam
    /// fatiadas e <b>1</b> referenciada — o <c>painel_ornado</c>, em 4 painéis. Enquanto isso o
    /// <c>HUD_Gameplay.prefab</c> tinha <b>37 Images no sprite padrão da Unity</b> (aquele
    /// retângulo branco arredondado): inventário, barra de itens, tela de pause e tela de
    /// Colapso inteiros desenhados com caixa genérica. O Castelo tinha 15, e os 6 botões do
    /// menu principal <b>nem sprite tinham</b>.</para>
    ///
    /// <para><b>O que esta ferramenta NÃO toca, de propósito:</b></para>
    /// <list type="bullet">
    ///   <item>Os <c>Icone</c> dentro dos slots. Eles <b>devem</b> ficar vazios — quem os
    ///   preenche é o runtime, com o ícone do item que estiver ali. Pôr arte neles desenharia
    ///   um item fantasma em todo slot vazio.</item>
    ///   <item>As <c>Barra_*</c> (Vitalidade, Resiliência, Vigor, Companheiro). Já têm arte
    ///   autorada (<c>bar_fill</c>, <c>bar_background</c>) e um bug de <c>fillAmount</c> caro
    ///   de consertar atrás delas.</item>
    /// </list>
    ///
    /// <para><b>Licença:</b> uso comercial e modificação liberados; proíbe redistribuir o
    /// pacote. O repositório é privado, então a cláusula não pega. Crédito é opcional —
    /// e devido.</para>
    /// </summary>
    public static class AplicarUiDarkAges
    {
        private const string Marcador = "[UiDarkAges]";
        private const string Folha =
            "Assets/ThirdParty/DarkAgesUI/DarkAgesUi_v1.0/32x32-Tilesheet.png";

        private const string HudPrefab = "Assets/FavelaAmarela/Resources/HUD_Gameplay.prefab";

        /// <summary>
        /// Uma regra: todo <c>Image</c> cujo caminho na hierarquia casar com
        /// <see cref="Contem"/> recebe <see cref="Sprite"/>.
        ///
        /// <para>Casamento por <b>caminho</b>, não por nome solto: existem três "Slot_1" no
        /// HUD, em painéis diferentes e com papéis diferentes.</para>
        /// </summary>
        private readonly struct Regra
        {
            public readonly string Contem, Sprite, Razao;

            public Regra(string contem, string sprite, string razao)
            {
                Contem = contem; Sprite = sprite; Razao = razao;
            }
        }

        private static readonly Regra[] RegrasDoHud =
        {
            new Regra("/PainelDeInventario/Janela/PainelDeFicha", "painel_ornado",
                "a ficha não tinha sprite NENHUM. Quase virou pergaminho — a ficha é leitura, e " +
                "material diferente separaria as duas metades da janela sem legenda —, mas o " +
                "texto dela é dourado-pálido (luminância 0,89) e o pergaminho é creme: seria " +
                "bonito e ilegível. Trocar a cor do texto é decisão de design, não conserto"),

            new Regra("/CaixaDeDialogo", "painel_ornado",
                "a caixa onde TODA conversa do jogo acontece estava no retângulo branco padrão " +
                "da Unity"),

            new Regra("/PainelDeInventario/Janela/Mochila/Slot_", "slot_vazio",
                "os 12 slots da mochila"),

            new Regra("/PainelDeInventario/Janela/Corpo/Corpo_", "moldura_slot",
                "os 7 slots de equipamento ganham a moldura ORNADA, e a mochila a simples: " +
                "o que está vestido tem de se distinguir do que está guardado num relance"),

            new Regra("/PainelDeInventario/Janela", "painel_ornado",
                "a janela do inventário, no mesmo painel do menu principal"),

            new Regra("/BarraDeItens/Slot_", "slot_vazio",
                "os 8 slots da barra de itens, iguais aos da mochila — é o mesmo gesto"),

            new Regra("/Tela_Pause/Botao_", "botao", "os botões do pause"),
            new Regra("/Tela_Colapso/Painel/Opcoes/Botao_", "botao", "os botões do Colapso"),
            new Regra("/Tela_Colapso/Painel", "painel_ornado", "o painel do Colapso"),
            new Regra("/Tela_Pause", "painel_ornado", "o fundo do pause"),
            new Regra("/BarraDeItens", "painel_ornado", "o trilho da barra de itens"),
        };

        private const string OpcoesPrefab = "Assets/FavelaAmarela/Resources/Painel_Opcoes.prefab";

        /// <summary>Sprite "-" limpa a Image em vez de vestir. Ver o Viewport, abaixo.</summary>
        private const string Limpar = "-";

        private static readonly Regra[] RegrasDasOpcoes =
        {
            new Regra("/Template/Viewport/Content/Item/ItemFundo", "slot_vazio",
                "cada linha do seletor de quadros — o mesmo material dos slots, porque é a " +
                "mesma ação: escolher um entre vários"),

            // ANTES da regra do Template, porque a primeira que casa vence e o caminho do
            // Viewport contém o do Template. Ele usa RectMask2D, que recorta por retângulo e
            // NÃO precisa de gráfico -- vestir aqui desenharia uma moldura ornada dentro da
            // outra.
            new Regra("/Seletor_Quadros/Template/Viewport", Limpar,
                "o Viewport recorta por RectMask2D e não precisa de arte nenhuma"),

            new Regra("/Seletor_Quadros/Template", "painel_ornado",
                "a lista que se abre no seletor de quadros não tinha fundo: as opções " +
                "apareciam soltas por cima do que estivesse atrás"),
        };

        private static readonly (string Cena, Regra[] Regras)[] RegrasDasCenas =
        {
            ("Assets/Scenes/Cena_Menu.unity", new[]
            {
                new Regra("Botao_", "botao",
                    "os 6 botões do menu não tinham sprite nenhum — eram texto sobre o nada"),
            }),

            ("Assets/Scenes/Castelo_Carcosa.unity", new[]
            {
                new Regra("Painel_Desfecho", "painel_ornado",
                    "a tela final do jogo não tinha fundo NENHUM — a última coisa que o jogador " +
                    "vê era texto sobre o vazio. Escuro pelo mesmo motivo da ficha: o texto do " +
                    "desfecho é dourado-pálido"),
            }),
        };

        [MenuItem("Tools/FavelaAmarela/UI: vestir a interface com o Dark Ages UI")]
        public static void Executar()
        {
            var sprites = AssetDatabase.LoadAllAssetsAtPath(Folha)
                .OfType<Sprite>()
                .ToDictionary(s => s.name, s => s);

            var resumo = new List<string>
            {
                $"folha: {sprites.Count} sprite(s) fatiado(s) — " + string.Join(", ", sprites.Keys.OrderBy(k => k)),
            };

            // Toda regra tem de achar seu sprite ANTES de qualquer escrita: metade aplicada é
            // pior que nada aplicado, porque some no meio de um diff grande.
            var exigidos = RegrasDoHud.Select(r => r.Sprite)
                .Concat(RegrasDasOpcoes.Select(r => r.Sprite))
                .Concat(RegrasDasCenas.SelectMany(c => c.Regras).Select(r => r.Sprite))
                .Where(s => s != Limpar)
                .Distinct()
                .ToArray();

            var faltando = exigidos.Where(s => !sprites.ContainsKey(s)).ToArray();

            if (faltando.Length > 0)
            {
                Debug.LogError($"{Marcador} Sprite(s) não fatiado(s) na folha: " +
                               string.Join(", ", faltando) + ". NADA foi tocado.");
                return;
            }

            resumo.Add(AplicarNoPrefab(HudPrefab, RegrasDoHud, sprites));
            resumo.Add(AplicarNoPrefab(OpcoesPrefab, RegrasDasOpcoes, sprites));

            foreach (var (cena, regras) in RegrasDasCenas)
                resumo.Add(AplicarNaCena(cena, regras, sprites));

            AssetDatabase.SaveAssets();

            string quebra = System.Environment.NewLine + "  ";
            Debug.Log($"{Marcador} Concluído:" + quebra + string.Join(quebra, resumo));
        }

        // ── Aplicação ─────────────────────────────────────────────────────────

        private static string CaminhoDe(Transform t)
        {
            var partes = new List<string>();
            for (var atual = t; atual != null; atual = atual.parent) partes.Add(atual.name);
            partes.Reverse();
            return "/" + string.Join("/", partes);
        }

        /// <summary>
        /// Aplica a PRIMEIRA regra que casar. A ordem da lista é a precedência: as regras mais
        /// específicas (caminhos mais fundos) vêm antes das genéricas.
        /// </summary>
        private static int Vestir(IEnumerable<Image> imagens, Regra[] regras,
                                  Dictionary<string, Sprite> sprites, List<string> detalhes)
        {
            int mudados = 0;

            foreach (var img in imagens)
            {
                string caminho = CaminhoDe(img.transform);

                // Os Icone são preenchidos em runtime com o ícone do item. Arte fixa aqui
                // desenharia um item fantasma em todo slot vazio.
                if (img.name == "Icone") continue;

                // As barras têm arte autorada própria.
                if (caminho.Contains("/Barra_")) continue;

                var regra = regras.FirstOrDefault(r => caminho.Contains(r.Contem));
                if (regra.Sprite == null) continue;

                var alvo = regra.Sprite == Limpar ? null : sprites[regra.Sprite];

                if (alvo == null)
                {
                    if (img.sprite == null) continue;
                    Undo.RecordObject(img, "Limpar UI");
                    img.sprite = null;
                    EditorUtility.SetDirty(img);
                    mudados++;
                    detalhes.Add($"{caminho} → (limpa)");
                    continue;
                }

                if (img.sprite == alvo && img.type == Image.Type.Sliced) continue;

                Undo.RecordObject(img, "Vestir UI");

                img.sprite = alvo;
                // Sliced com multiplicador 1: é exatamente como o painel_ornado já está
                // configurado nos 4 lugares onde o pacote era usado.
                img.type = Image.Type.Sliced;
                img.pixelsPerUnitMultiplier = 1f;
                img.fillCenter = true;

                EditorUtility.SetDirty(img);
                mudados++;
                detalhes.Add($"{caminho} → {regra.Sprite}");
            }

            return mudados;
        }

        private static string AplicarNoPrefab(string caminho, Regra[] regras,
                                              Dictionary<string, Sprite> sprites)
        {
            string nome = System.IO.Path.GetFileName(caminho);
            var raiz = PrefabUtility.LoadPrefabContents(caminho);

            try
            {
                var detalhes = new List<string>();
                int n = Vestir(raiz.GetComponentsInChildren<Image>(true), regras,
                               sprites, detalhes);

                if (n == 0) return $"{nome}: nada a mudar (já vestido)";

                PrefabUtility.SaveAsPrefabAsset(raiz, caminho, out bool gravou);
                if (!gravou) return $"{nome}: SaveAsPrefabAsset RECUSOU";

                return $"{nome}: {n} Image(s) vestida(s) — " + Agrupar(detalhes);
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(raiz);
            }
        }

        private static string AplicarNaCena(string caminhoDaCena, Regra[] regras,
                                            Dictionary<string, Sprite> sprites)
        {
            string nome = System.IO.Path.GetFileName(caminhoDaCena);
            var cena = EditorSceneManager.OpenScene(caminhoDaCena, OpenSceneMode.Single);

            var imagens = cena.GetRootGameObjects()
                .SelectMany(r => r.GetComponentsInChildren<Image>(true));

            var detalhes = new List<string>();
            int n = Vestir(imagens, regras, sprites, detalhes);

            if (n == 0) return $"{nome}: nada a mudar (já vestido)";

            EditorSceneManager.MarkSceneDirty(cena);
            EditorSceneManager.SaveScene(cena);

            return $"{nome}: {n} Image(s) vestida(s) — " + Agrupar(detalhes);
        }

        /// <summary>Resume "12 slots viraram slot_vazio" em vez de 12 linhas iguais.</summary>
        private static string Agrupar(List<string> detalhes) =>
            string.Join("; ", detalhes
                .GroupBy(d => d.Substring(d.IndexOf('→')))
                .Select(g => $"{g.Count()}× {g.Key.Trim()}"));
    }
}
