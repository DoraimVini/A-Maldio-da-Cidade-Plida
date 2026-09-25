using System.IO;
using UnityEditor;
using UnityEngine;
using FavelaAmarela.Runtime.Quests;
using FavelaAmarela.Runtime.Rendering;

namespace FavelaAmarela.EditorTools
{
    /// <summary>
    /// Ferramenta de Editor. Cria/atualiza o <b>prefab da Rainha Cassilda</b> — visual
    /// placeholder + todo o conteúdo já decidido (saudação, pedido, falas por fragmento e o
    /// recital das duas estrofes finais, decisão do Vini de 2026-08-02) — como um asset
    /// único, para não ter o mesmo texto autorado em dois lugares diferentes.
    ///
    /// <para><b>O que fica de fora do prefab, de propósito:</b> <c>caixaDeTexto</c> e
    /// <c>painelDeEscolha</c> são referências de <b>cena</b> (Canvas do Santuário) — um
    /// asset de prefab não pode apontar para um objeto de cena. Essas duas ficam para
    /// <c>MontarSantuarioDeYhtill</c>, que já é a ferramenta que instancia Cassilda dentro
    /// do Santuário e resolve o resto do wiring da quest (mesma separação que
    /// <c>MontarPatuaDasLuasGemeas</c> usa para a recompensa).</para>
    ///
    /// <para><b>Sprite real desde 2026-08-02:</b> se <c>Cassilda_Sprite.png</c> existir na
    /// pasta do prefab, usa ele (corrigindo o pivô do import para <c>BottomCenter</c> —
    /// mesmo padrão de Damião e Yug-Neth, sem o que o Y-sort desalinha). Sem o arquivo, cai
    /// no losango amarelo-pálido placeholder — nunca trava por falta de arte.</para>
    ///
    /// <para>Idempotente: reaproveita o asset se já existir e só atualiza os campos.</para>
    /// </summary>
    public static class MontarPrefabDaCassilda
    {
        private const string PastaPrefab = "Assets/FavelaAmarela/Art/Characters/Cassilda";
        private const string CaminhoPrefab = PastaPrefab + "/Cassilda.prefab";
        private const string CaminhoSprite = PastaPrefab + "/Cassilda_Sprite.png";
        private const string CaminhoPrefabPatua = "Assets/FavelaAmarela/Art/Items/Patua_DasLuasGemeas.prefab";

        [MenuItem("Tools/FavelaAmarela/Montar Prefab da Cassilda")]
        public static void Executar()
        {
            var go = AssetDatabase.LoadAssetAtPath<GameObject>(CaminhoPrefab) != null
                ? Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>(CaminhoPrefab))
                : new GameObject("Cassilda");
            go.name = "Cassilda";

            GarantirVisual(go);
            GarantirColisor(go);
            var npc = GarantirComponente(go);
            PreencherConteudo(npc);

            if (!AssetDatabase.IsValidFolder(PastaPrefab))
                Directory.CreateDirectory(PastaPrefab);

            var prefab = PrefabUtility.SaveAsPrefabAsset(go, CaminhoPrefab);
            Object.DestroyImmediate(go);

            AssetDatabase.SaveAssets();
            Debug.Log($"[Cassilda] Prefab pronto em {CaminhoPrefab}.", prefab);
        }

        private static void GarantirVisual(GameObject go)
        {
            var sr = go.GetComponent<SpriteRenderer>();
            if (sr == null) sr = go.AddComponent<SpriteRenderer>();

            CorrigirImportacaoDoSprite();
            var spriteReal = AssetDatabase.LoadAssetAtPath<Sprite>(CaminhoSprite);

            if (spriteReal != null)
            {
                sr.sprite = spriteReal;
                sr.color = Color.white; // arte real: sem tingimento por cima
            }
            else if (sr.sprite == null)
            {
                // Placeholder: losango nas cores das vestes de Yhtill, até a arte existir.
                sr.sprite = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/Knob.psd");
                sr.color = new Color(0.93f, 0.89f, 0.55f);
            }

            if (go.GetComponent<DynamicYSort>() == null) go.AddComponent<DynamicYSort>();
        }

        /// <summary>
        /// Corrige o pivô de importação do sprite para <c>BottomCenter</c> — o padrão usado
        /// por Damião e Yug-Neth (<c>pivot: {0.5, 0}</c>). Sem isto, o import padrão da
        /// Unity fica em <c>BottomLeft</c>, o que desloca a Cassilda meio sprite para a
        /// direita da posição real e desalinha o Y-sort (que assume pé no centro-base).
        /// PPU e filtro Point já vêm certos do import automático (skill
        /// favela-pixelart-standards) — só o pivô precisa de correção manual.
        /// </summary>
        private static void CorrigirImportacaoDoSprite()
        {
            if (!(AssetImporter.GetAtPath(CaminhoSprite) is TextureImporter importer)) return;

            // spriteAlignment/spritePivot não existem como propriedade direta do
            // TextureImporter nesta versão — só via TextureImporterSettings.
            var settings = new TextureImporterSettings();
            importer.ReadTextureSettings(settings);

            if (settings.spriteAlignment == (int)SpriteAlignment.BottomCenter)
                return; // já corrigido, evita reimport à toa

            settings.spriteAlignment = (int)SpriteAlignment.BottomCenter;
            settings.spritePivot = new Vector2(0.5f, 0f);
            importer.SetTextureSettings(settings);
            importer.SaveAndReimport();

            Debug.Log("[Cassilda] Pivô do sprite corrigido para BottomCenter.");
        }

        private static void GarantirColisor(GameObject go)
        {
            var col = go.GetComponent<CircleCollider2D>();
            if (col == null) col = go.AddComponent<CircleCollider2D>();
            col.isTrigger = true;
            col.radius = 1.2f;
        }

        private static CassildaNPC GarantirComponente(GameObject go)
        {
            var npc = go.GetComponent<CassildaNPC>();
            return npc != null ? npc : go.AddComponent<CassildaNPC>();
        }

        private static void PreencherConteudo(CassildaNPC npc)
        {
            var so = new SerializedObject(npc);

            so.FindProperty("totalDeFragmentos").intValue = 3;

            var patua = AssetDatabase.LoadAssetAtPath<GameObject>(CaminhoPrefabPatua);
            if (patua != null) so.FindProperty("prefabPatua").objectReferenceValue = patua;
            else Debug.LogWarning($"[Cassilda] Prefab do Patuá não encontrado em " +
                                   $"{CaminhoPrefabPatua} — rode 'Montar Patua das Luas Gemeas' antes.");

            so.FindProperty("falaDeSaudacao").stringValue =
                "Você cheira a Hali, forasteiro. E a algo mais — a morte recente. " +
                "Bem-vindo ao Santuário de Yhtill. Que reste ainda um santuário para ser chamado assim.";

            so.FindProperty("falaDoPedido").stringValue =
                "Meus nobres partiram há tempo não mensurável. Deixaram diários, cartas, " +
                "fragmentos de nossas vidas antes de Carcosa. Traga-os. Para que eu possa cantar " +
                "a canção de cada nome deles direito.";

            var falasPorFragmento = so.FindProperty("falasPorFragmento");
            falasPorFragmento.arraySize = FalasDeEntrega.Length;
            for (int i = 0; i < FalasDeEntrega.Length; i++)
                falasPorFragmento.GetArrayElementAtIndex(i).stringValue = FalasDeEntrega[i];

            so.FindProperty("falaDeEspera").stringValue = "Ainda faltam {0}. Quando os tiver, volte.";

            var opcoesEncontro = so.FindProperty("opcoesDoPrimeiroEncontro");
            opcoesEncontro.arraySize = 3;
            opcoesEncontro.GetArrayElementAtIndex(0).stringValue = "Onde estou?";
            opcoesEncontro.GetArrayElementAtIndex(1).stringValue = "Você está presa aqui?";
            opcoesEncontro.GetArrayElementAtIndex(2).stringValue = "(Ficar em silêncio)";

            var reacoesEncontro = so.FindProperty("reacoesDoPrimeiroEncontro");
            reacoesEncontro.arraySize = 3;
            reacoesEncontro.GetArrayElementAtIndex(0).stringValue =
                "No coração de Carcosa, onde os sóis gêmeos esqueceram como se pôr. Este é o " +
                "Santuário de Yhtill — o que resta da corte do Rei em Amarelo antes que ele " +
                "deixasse de ser apenas um personagem de peça e passasse a ser um fato.";
            reacoesEncontro.GetArrayElementAtIndex(1).stringValue =
                "Presa. Sim. Essa palavra serve. A geometria de Carcosa tem predileção por " +
                "ironias: a rainha que fundou este santuário não pode sair dele. Mas meus " +
                "nobres partiram. Foram buscar fragmentos de uma saída que não existe. Ou " +
                "talvez existe — simplesmente não voltaram para me contar.";
            reacoesEncontro.GetArrayElementAtIndex(2).stringValue =
                "Silêncio. Isso é raro aqui. A maioria dos que chegam aqui ou gritam ou choram. " +
                "Sente-se, forasteiro. Ou não. A geometria de Carcosa não se importa com sua postura.";

            so.FindProperty("falaDeAberturaDoRecital").stringValue =
                "Vaine ouviu o final e não escreveu — achou que me pouparia. Mas em Carcosa o " +
                "silêncio é a pior das maldições: os nomes deles não descansam enquanto a canção " +
                "não terminar.\n\nE eu não a tenho mais, forasteiro. Cantei estes versos por " +
                "tantas eras que gastei as palavras até o osso. Não consigo mais chamá-las... " +
                "mas eu as reconheço. Se você disser certo, eu vou saber.";

            so.FindProperty("falaDeRecapitulacao").stringValue =
                "\"Ao longo da costa as ondas de nuvem se quebram,\nOs sóis gêmeos afundam por " +
                "trás do lago,\nAs sombras se alongam\nEm Carcosa.\n\nEstranha é a noite em que " +
                "as estrelas negras sobem,\nE estranhas luas circulam pelos céus...\nMas ainda " +
                "mais estranha é\na Perdida Carcosa.\"\n\nAté aqui, eu me lembro. É daqui em " +
                "diante que a canção me escapa.";

            so.FindProperty("perguntaEstrofe3").stringValue =
                "As ondas de nuvem. Os sóis gêmeos. As estranhas luas. Depois delas vêm as " +
                "Híades — e o que elas cantam?";

            var opcoes3 = so.FindProperty("opcoesEstrofe3");
            opcoes3.arraySize = 3;
            opcoes3.GetArrayElementAtIndex(0).stringValue =
                "Onde batem os farrapos do Rei, / devem reinar sobre as cinzas da / Fosca Carcosa.";
            opcoes3.GetArrayElementAtIndex(1).stringValue =
                "Onde batem os farrapos do Rei, / devem morrer não ouvidas na / Fosca Carcosa.";
            opcoes3.GetArrayElementAtIndex(2).stringValue =
                "Onde os deuses caem e sangram, / devem afundar para sempre no lago da / Perdida Carcosa.";

            so.FindProperty("falaDeAcertoEstrofe3").stringValue =
                "Sim... morrer não ouvidas. Como eles morreram.";

            so.FindProperty("perguntaEstrofe4").stringValue =
                "Falta o último suspiro. A minha parte. O que a minha alma pede?";

            var opcoes4 = so.FindProperty("opcoesEstrofe4");
            opcoes4.arraySize = 3;
            opcoes4.GetArrayElementAtIndex(0).stringValue =
                "Canção de minha alma, minha voz se ergue; / queima tu, iluminada, como brasas " +
                "não extintas / vão arder e viver na / Eterna Carcosa.";
            opcoes4.GetArrayElementAtIndex(1).stringValue =
                "Canção de minha alma, a corte está morta; / que os reis lamentem, como servos " +
                "sem coroa / vão secar e morrer na / Fosca Carcosa.";
            opcoes4.GetArrayElementAtIndex(2).stringValue =
                "Canção de minha alma, minha voz está morta; / morre tu, não cantada, como as " +
                "lágrimas não choradas / vão secar e morrer na / Perdida Carcosa.";

            so.FindProperty("falaDeErroNoRecital").stringValue =
                "Não. Essa não é a nossa melodia.\n\nAlguma sombra sussurrou mentira no seu " +
                "caminho. Ouça de novo o que eles escreveram — e tente outra vez.";

            so.FindProperty("falaDoLamentoFinal").stringValue =
                "\"Nas luas que não piscam, Seraphel se foi rápida.\nMorthis pisou devagar até " +
                "não mais poder.\nVaine escreveu até onde ainda era seguro escrever.\nQue as " +
                "sombras descansem na areia de Hali — e que Aldaron, onde quer que esteja, " +
                "ainda seja lembrado por um nome.\"\n\nA canção está completa. Os nomes deles " +
                "têm permissão para secar e morrer, finalmente.";

            so.FindProperty("falaDeConclusao").stringValue =
                "Tome. O Patuá das Luas Gêmeas — feito com fios das vestes de Yhtill. Ele " +
                "desacelera o que o escuro faz com a sua mente. Use as pausas que ele lhe dá.";

            so.ApplyModifiedPropertiesWithoutUndo();
        }

        /// <summary>Falas de Cassilda ao receber cada fragmento, na ordem dos índices.</summary>
        private static readonly string[] FalasDeEntrega =
        {
            "Seraphel... Ela sempre escrevia rápido, como se tivesse medo de esquecer. " +
            "Obrigada por trazer a letra dela de volta para mim.",

            "Morthis. Ele era prático acima de qualquer coisa. 'Andar devagar.' Sim. Era o " +
            "único conselho que ele tinha. Funciona, de certa forma.",

            "Vaine. Ela não era obrigada a ir. Era a mais jovem da corte. Eu deveria tê-la " +
            "impedido.\n\nNão a culpe, se a encontrar. Ela não escolheu mal. Ela simplesmente… " +
            "escolheu seguir.",
        };
    }
}
