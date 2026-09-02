using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using FavelaAmarela.Inventario;
using FavelaAmarela.Runtime.UI;

namespace FavelaAmarela.EditorTools
{
    /// <summary>
    /// Completa a <b>barra de ações</b> e a <b>barra de artefatos</b>, que estavam pela metade.
    ///
    /// <para><b>O que a auditoria de 2026-09-02 encontrou.</b> As duas barras não eram só
    /// "sem estilo" — estavam <b>funcionalmente incompletas</b>, e o código das duas já fazia
    /// tudo que faltava aparecer:</para>
    ///
    /// <list type="bullet">
    ///   <item><b><c>BarraDeAcoes.slots</c> estava VAZIO.</b> O <c>Update()</c> lê
    ///   <c>slots[0]</c> para animar a recarga; com o array vazio, <b>a recarga da habilidade
    ///   nunca era desenhada</b>. O sumário da própria classe diz o preço: <i>"sem isto, a
    ///   habilidade da arma dispara às cegas — o jogador não sabe o que tem na mão nem quando
    ///   pode usar"</i>. Os objetos <c>NomeDaHabilidade</c> e <c>Recarga</c> existiam na
    ///   hierarquia, soltos, sem ninguém os lendo.</item>
    ///
    ///   <item><b>Os 4 slots da <c>BarraDeArtefatos</c></b> tinham <c>grupo</c> e
    ///   <c>nomeDaHabilidade</c> ligados, e <c>icone</c>, <c>preenchimentoRecarga</c> e
    ///   <c>rotuloTecla</c> <b>nulos</b>. O <c>Redesenhar</c> já pinta o ícone do Artefato, já
    ///   escreve "F1".."F4" e o <c>Update</c> já preenche a recarga — tudo atrás de um
    ///   <c>if (!= null)</c> que nunca passava. Era fiação, não código.</item>
    /// </list>
    ///
    /// <para><b>E o ícone da habilidade</b> não existia como campo no <c>HabilidadeDef</c>.
    /// Ganhou um, e o valor é <b>derivado</b>: procura o <c>ItemDef</c> que usa aquela
    /// habilidade e toma o ícone dele. Casar por nome ("Alfanje" → Icone_Alfanje) funcionaria
    /// hoje e mentiria no dia em que alguém renomeasse um asset.</para>
    /// </summary>
    public static class CompletarBarrasDeAcao
    {
        private const string Marcador = "[BarrasDeAcao]";
        private const string Hud = "Assets/FavelaAmarela/Resources/HUD_Gameplay.prefab";
        private const string Folha =
            "Assets/ThirdParty/DarkAgesUI/DarkAgesUi_v1.0/32x32-Tilesheet.png";

        [MenuItem("Tools/FavelaAmarela/UI: completar as barras de ação e de artefatos")]
        public static void Executar()
        {
            var resumo = new List<string> { AutorarIconesDeHabilidade() };

            var raiz = PrefabUtility.LoadPrefabContents(Hud);

            try
            {
                resumo.Add(CompletarArtefatos(raiz));
                resumo.Add(CompletarAcoes(raiz));
                resumo.Add(CaberRotulos(raiz));

                PrefabUtility.SaveAsPrefabAsset(raiz, Hud, out bool gravou);
                if (!gravou) resumo.Add("PREFAB: SaveAsPrefabAsset RECUSOU");
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(raiz);
            }

            AssetDatabase.SaveAssets();

            string quebra = System.Environment.NewLine + "  ";
            Debug.Log($"{Marcador} Concluído:" + quebra + string.Join(quebra, resumo));
        }

        // ── Os ícones de habilidade ───────────────────────────────────────────

        /// <summary>
        /// Dá a cada <c>HabilidadeDef</c> o ícone do <c>ItemDef</c> que a usa. <b>Derivado da
        /// referência real</b>, não do nome do arquivo.
        /// </summary>
        private static string AutorarIconesDeHabilidade()
        {
            var itens = AssetDatabase.FindAssets("t:ItemDef")
                .Select(AssetDatabase.GUIDToAssetPath)
                .Select(AssetDatabase.LoadAssetAtPath<ItemDef>)
                .Where(d => d != null && d.Base != null && d.Base.Habilidade != null)
                .ToArray();

            // Uma habilidade pode ser usada por mais de um item (os 3 tiers de uma família
            // apontam para habilidades distintas hoje, mas nada impede o contrário). O
            // primeiro item que a usa decide — e todos os que a usam são da mesma família.
            var iconePara = new Dictionary<HabilidadeDef, Sprite>();
            foreach (var item in itens)
            {
                if (item.Icone == null) continue;
                if (!iconePara.ContainsKey(item.Base.Habilidade))
                    iconePara[item.Base.Habilidade] = item.Icone;
            }

            var todas = AssetDatabase.FindAssets("t:HabilidadeDef")
                .Select(AssetDatabase.GUIDToAssetPath)
                .Select(AssetDatabase.LoadAssetAtPath<HabilidadeDef>)
                .Where(h => h != null)
                .ToArray();

            int postos = 0;
            var semFonte = new List<string>();

            foreach (var h in todas)
            {
                if (!iconePara.TryGetValue(h, out var icone))
                {
                    if (h.Icone == null) semFonte.Add(h.name);
                    continue;
                }

                if (h.Icone == icone) continue;

                h.Icone = icone;
                EditorUtility.SetDirty(h);
                postos++;
            }

            string faltando = semFonte.Count == 0 ? ""
                : $" — {semFonte.Count} sem item que a use: {string.Join(", ", semFonte)}";

            return $"ícones de habilidade: {postos} de {todas.Length} autorado(s){faltando}";
        }

        // ── A barra de artefatos ──────────────────────────────────────────────

        private static string CompletarArtefatos(GameObject raiz)
        {
            var barra = raiz.GetComponentInChildren<BarraDeArtefatos>(true);
            if (barra == null) return "artefatos: BarraDeArtefatos não achada";

            var so = new SerializedObject(barra);
            var slots = so.FindProperty("slots");
            if (slots == null) return "artefatos: campo 'slots' não existe mais";

            int criados = 0, ligados = 0;

            for (int i = 0; i < slots.arraySize; i++)
            {
                var entrada = slots.GetArrayElementAtIndex(i);

                // O 'grupo' já está ligado e é a raiz visual do slot. Derivar dele em vez de
                // procurar por nome: é a referência que a barra realmente usa.
                var grupo = entrada.FindPropertyRelative("grupo").objectReferenceValue as CanvasGroup;
                if (grupo == null) continue;

                var pai = grupo.transform;

                ligados += Ligar(entrada, "icone",
                                 GarantirImagem(pai, "Icone", ref criados,
                                                new Vector2(0f, 0.5f), new Vector2(26f, 26f),
                                                new Vector2(16f, 0f)));

                ligados += Ligar(entrada, "preenchimentoRecarga",
                                 GarantirRecarga(pai, ref criados));

                ligados += Ligar(entrada, "rotuloTecla",
                                 GarantirTexto(pai, "Tecla", $"F{i + 1}", ref criados));
            }

            so.ApplyModifiedPropertiesWithoutUndo();

            return $"artefatos: {criados} objeto(s) criado(s), {ligados} referência(s) ligada(s) " +
                   "— ícone, recarga e tecla dos 4 slots (o código já os pintava; faltava a fiação)";
        }

        // ── A barra de ações ──────────────────────────────────────────────────

        private static string CompletarAcoes(GameObject raiz)
        {
            var barra = raiz.GetComponentInChildren<BarraDeAcoes>(true);
            if (barra == null) return "ações: BarraDeAcoes não achada";

            var so = new SerializedObject(barra);
            int criados = 0, ligados = 0;

            var pai = barra.transform;

            // 1. O ícone da arma, ao lado do nome dela.
            var iconeArma = GarantirImagem(pai, "IconeDaArma", ref criados,
                                           new Vector2(0f, 1f), new Vector2(28f, 28f),
                                           new Vector2(16f, -16f));

            var propIcone = so.FindProperty("iconeDaArma");
            if (propIcone != null && propIcone.objectReferenceValue != iconeArma)
            {
                propIcone.objectReferenceValue = iconeArma;
                ligados++;
            }

            // 2. O slot 0 — a habilidade da arma. Os pedaços JÁ existiam soltos na hierarquia:
            //    NomeDaHabilidade (com CanvasGroup) e Recarga. Montá-los no array é o que faz o
            //    Update() achar slots[0] e finalmente animar a recarga.
            var nomeHab = pai.Find("NomeDaHabilidade");
            var recarga = pai.Find("Recarga");

            if (nomeHab == null) return "ações: 'NomeDaHabilidade' não está na hierarquia";

            var slots = so.FindProperty("slots");
            if (slots == null) return "ações: campo 'slots' não existe mais";

            if (slots.arraySize == 0) slots.arraySize = 1;
            var slot0 = slots.GetArrayElementAtIndex(0);

            ligados += Ligar(slot0, "grupo", nomeHab.GetComponent<CanvasGroup>());
            ligados += Ligar(slot0, "nomeDaHabilidade", nomeHab.GetComponent<Text>());

            ligados += Ligar(slot0, "icone",
                             GarantirImagem(pai, "IconeDaHabilidade", ref criados,
                                            new Vector2(0f, 0f), new Vector2(24f, 24f),
                                            new Vector2(16f, 16f)));

            // A Recarga precisa ser Filled: fillAmount não faz nada em Simple.
            var imgRecarga = recarga != null ? recarga.GetComponent<Image>() : null;
            if (imgRecarga == null) imgRecarga = GarantirRecarga(pai, ref criados);
            else if (imgRecarga.type != Image.Type.Filled)
            {
                imgRecarga.type = Image.Type.Filled;
                imgRecarga.fillMethod = Image.FillMethod.Radial360;
                EditorUtility.SetDirty(imgRecarga);
            }

            ligados += Ligar(slot0, "preenchimentoRecarga", imgRecarga);
            ligados += Ligar(slot0, "rotuloTecla", GarantirTexto(pai, "Tecla", "Q", ref criados));

            so.ApplyModifiedPropertiesWithoutUndo();

            return $"ações: {criados} objeto(s) criado(s), {ligados} referência(s) ligada(s) — " +
                   "slots[0] montado, então o Update() finalmente acha a recarga da habilidade";
        }

        // ── Rótulos que cabem ─────────────────────────────────────────────────

        /// <summary>
        /// Liga <c>BestFit</c> nos rótulos de tecla e nos rótulos de botão, com piso no
        /// mínimo legível.
        ///
        /// <para><b>Medido em 2026-09-02</b> pelo <c>LayoutDaUiTests</c>, o primeiro teste deste
        /// projeto a carregar o HUD e medir o layout de verdade: os rótulos "1".."8" da barra de
        /// itens pedem <b>39 unidades</b> de altura e têm <b>31</b>. Estavam cortados desde que a
        /// barra existe, e nenhum dos 129 testes EditMode podia ver isso — eles leem YAML, e
        /// "não cabe" só existe depois do passo de layout.</para>
        ///
        /// <para>Crescer a caixa invadiria o ícone do slot; encolher a fonte sem piso a tornaria
        /// ilegível. Piso de <b>24</b> porque é o mínimo na referência 1920×1080
        /// (<c>TextoLegivelTests</c>); teto no próprio <c>fontSize</c>, para nunca aumentar.</para>
        /// </summary>
        private static string CaberRotulos(GameObject raiz)
        {
            const int MinimoLegivel = 24;

            int n = 0, total = 0;

            foreach (var txt in raiz.GetComponentsInChildren<Text>(true))
            {
                // Rótulo de tecla, ou rótulo DENTRO de um botão. Os dois têm caixa fixa
                // desenhada por quem montou a tela, e texto que veio depois — é a receita de
                // "não coube". O do Colapso ("Despertar no último refúgio", fonte 48 em 87
                // unidades) foi o caso que a régua achou depois dos treze primeiros.
                bool ehTecla = txt.name == "Tecla";
                bool ehRotuloDeBotao = txt.transform.parent != null &&
                                       txt.transform.parent.GetComponent<Selectable>() != null;

                if (!ehTecla && !ehRotuloDeBotao) continue;
                total++;

                if (txt.resizeTextForBestFit && txt.resizeTextMinSize == MinimoLegivel) continue;

                Undo.RecordObject(txt, "Rótulo de tecla que cabe");

                txt.resizeTextForBestFit = true;
                txt.resizeTextMinSize = MinimoLegivel;
                txt.resizeTextMaxSize = Mathf.Max(MinimoLegivel, txt.fontSize);

                EditorUtility.SetDirty(txt);
                n++;
            }

            return n == 0
                ? $"rótulos: {total} conferido(s), nada a mudar"
                : $"rótulos: {n} de {total} com BestFit (piso {MinimoLegivel}) — " +
                  "estavam cortados pela própria caixa";
        }

        // ── Utilidades ────────────────────────────────────────────────────────

        private static int Ligar(SerializedProperty entrada, string campo, Object valor)
        {
            var prop = entrada.FindPropertyRelative(campo);
            if (prop == null || valor == null) return 0;
            if (prop.objectReferenceValue == valor) return 0;

            prop.objectReferenceValue = valor;
            return 1;
        }

        private static RectTransform Filho(Transform pai, string nome, ref int criados)
        {
            var achado = pai.Find(nome) as RectTransform;
            if (achado != null) return achado;

            var go = new GameObject(nome, typeof(RectTransform));
            go.transform.SetParent(pai, worldPositionStays: false);
            criados++;
            return (RectTransform)go.transform;
        }

        private static Image GarantirImagem(Transform pai, string nome, ref int criados,
                                            Vector2 ancora, Vector2 tamanho, Vector2 posicao)
        {
            var rt = Filho(pai, nome, ref criados);

            rt.anchorMin = rt.anchorMax = ancora;
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.sizeDelta = tamanho;
            rt.anchoredPosition = posicao;

            var img = rt.GetComponent<Image>();
            if (img == null) img = rt.gameObject.AddComponent<Image>();

            // Nasce DESLIGADA: sem arma equipada não há ícone, e uma Image branca vazia
            // desenharia um quadrado sólido em cima da barra.
            img.enabled = false;
            img.preserveAspect = true;
            img.raycastTarget = false;

            EditorUtility.SetDirty(img);
            return img;
        }

        private static Image GarantirRecarga(Transform pai, ref int criados)
        {
            var img = GarantirImagem(pai, "Recarga", ref criados,
                                     new Vector2(0f, 0.5f), new Vector2(26f, 26f),
                                     new Vector2(16f, 0f));

            // Filled/Radial360: é o que faz fillAmount virar um relógio de recarga em vez de
            // não fazer nada. Image.Type.Simple ignora fillAmount por completo.
            img.type = Image.Type.Filled;
            img.fillMethod = Image.FillMethod.Radial360;
            img.fillOrigin = (int)Image.Origin360.Top;
            img.fillClockwise = true;
            img.fillAmount = 1f;
            img.enabled = true;
            img.color = new Color(0f, 0f, 0f, 0.55f);

            var folha = AssetDatabase.LoadAllAssetsAtPath(Folha).OfType<Sprite>()
                .FirstOrDefault(s => s.name == "slot_vazio");
            if (folha != null) img.sprite = folha;

            EditorUtility.SetDirty(img);
            return img;
        }

        private static Text GarantirTexto(Transform pai, string nome, string conteudo,
                                          ref int criados)
        {
            var rt = Filho(pai, nome, ref criados);

            rt.anchorMin = rt.anchorMax = new Vector2(0f, 0f);
            rt.pivot = new Vector2(0.5f, 0.5f);
            // A caixa acompanha a fonte: 24 px de altura de letra não cabe num retângulo de
            // 18, e o BestFit encolheria de volta para o ilegível.
            rt.sizeDelta = new Vector2(32f, 30f);
            rt.anchoredPosition = new Vector2(12f, 12f);

            var txt = rt.GetComponent<Text>();
            if (txt == null) txt = rt.gameObject.AddComponent<Text>();

            txt.text = conteudo;
            txt.alignment = TextAnchor.MiddleCenter;
            txt.raycastTarget = false;

            // Arial.ttf LANÇA ArgumentException na Unity 6 -- o '??' não protegeria, porque a
            // exceção acontece antes de haver o que testar.
            if (txt.font == null)
                txt.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");

            // 24 é o mínimo legível na referência de 1920x1080 -- o TextoLegivelTests pegou
            // esta linha em 14 e estava certo: o rótulo de tecla é o que o jogador procura no
            // meio da luta, e um "F3" que não se lê não serve para nada.
            txt.fontSize = 24;
            txt.color = new Color(0.92f, 0.86f, 0.55f, 1f);

            EditorUtility.SetDirty(txt);
            return txt;
        }
    }
}
