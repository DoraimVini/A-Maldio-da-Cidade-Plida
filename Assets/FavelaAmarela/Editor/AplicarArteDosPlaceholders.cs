using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace FavelaAmarela.EditorTools
{
    /// <summary>
    /// Troca o sprite embutido da Unity (<c>fileID: 10905</c>, o "Knob") pela arte real nos
    /// cinco prefabs que ainda o usavam: Pedra de Poder, Cone de Gelo, Esqueleto Invocado,
    /// Necronomicon e Yug-Neth.
    ///
    /// <para><b>Por que uma ferramenta só:</b> o Vini pediu para abrir a Unity uma vez. Import
    /// de textura, prefab e cena precisam do Editor; fazer em três passadas obrigaria três
    /// aberturas.</para>
    ///
    /// <para><b>O Yug-Neth não precisava de arte nova.</b> <c>yug_neth_idle.png</c> (40×50, um
    /// Mi-Go completo) já estava no projeto desde sempre, importado e sem estar ligado a nada —
    /// o mesmo modo de falha que este projeto repete: <b>a peça existe e não está ligada</b>.
    /// Os outros quatro foram autorados agora porque nenhum dos pacotes de terceiros tinha
    /// esqueleto em pé, livro nem cristal.</para>
    ///
    /// <para><b>Regra que guiou os números abaixo: preservar todo volume de jogo.</b> Cada
    /// colisor mantém exatamente o mesmo tamanho <b>em unidades de mundo</b> que tinha antes —
    /// só o tamanho local muda, para compensar a escala nova. Trocar arte não é hora de
    /// reequilibrar hitbox; isso seria uma mudança de design escondida numa tarefa de arte.</para>
    ///
    /// <para><b>Cuidado com o pivô.</b> O Knob tem pivô no centro; a arte do projeto usa
    /// <c>Bottom</c> (0.5, 0), como Damião e Yug-Neth. Trocar o sprite move a arte para cima em
    /// relação ao transform — por isso a Pedra de Poder ganha <c>offset</c> no colisor. De
    /// quebra isso <b>conserta</b> o Y-sort: <see cref="FavelaAmarela.Runtime.Rendering.DynamicYSort"/>
    /// ordena por <c>transform.position.y + offsetPes</c>, e com <c>offsetPes = 0</c> (o valor
    /// gravado nos cinco) só está certo se o pivô estiver nos pés. Com o Knob, ordenavam pelo
    /// meio do sprite.</para>
    ///
    /// <para>O Cone de Gelo é a exceção: pivô <c>Center</c>, porque
    /// <c>ConeDeGelo.Lancar</c> gira o <c>transform</c> para a direção de viagem e a rotação
    /// acontece em torno do pivô. Com pivô nos pés, a lasca giraria em volta da própria cauda.
    /// O sprite dele é autorado apontando para <b>+X</b>, que é o zero daquele
    /// <c>Atan2</c>.</para>
    ///
    /// <para>Idempotente: rodar de novo reescreve os mesmos valores.</para>
    /// </summary>
    public static class AplicarArteDosPlaceholders
    {
        private const string Enemies = "Assets/FavelaAmarela/Art/Enemies/";
        private const string Items = "Assets/FavelaAmarela/Art/Items/";
        private const string MiGo = "Assets/FavelaAmarela/Art/Characters/MiGo/";

        /// <summary>
        /// Plataformas cujo bloco de import precisa ser escrito à mão. São as que a Unity
        /// serializa neste projeto — as mesmas que aparecem nos <c>.meta</c> já existentes.
        /// </summary>
        private static readonly string[] Plataformas = { "Standalone", "WebGL", "WindowsStoreApps" };

        /// <summary>Um prefab a corrigir, com tudo que muda nele.</summary>
        private sealed class Alvo
        {
            /// <summary>Caminho do .prefab.</summary>
            public string Prefab;

            /// <summary>Caminho do .png a atribuir.</summary>
            public string Sprite;

            /// <summary>Pivô a gravar no importador.</summary>
            public SpriteAlignment Pivo;

            /// <summary>Escala nova da raiz (a antiga fora calibrada para o Knob, de 32px a PPU 100).</summary>
            public float Escala;

            /// <summary>Tamanho local do BoxCollider2D — escolhido para manter o volume de mundo.</summary>
            public Vector2 Colisor;

            /// <summary>Offset local do colisor.</summary>
            public Vector2 Offset;

            /// <summary>Volume de mundo que deve resultar. Só para conferência no log.</summary>
            public Vector2 MundoEsperado;
        }

        private static readonly Alvo[] Alvos =
        {
            // Companheiro Mi-Go. A arte já existia; só faltava ligar.
            new Alvo
            {
                Prefab = MiGo + "YugNeth.prefab",
                Sprite = MiGo + "yug_neth_idle.png",
                Pivo = SpriteAlignment.BottomCenter,
                Escala = 0.5f,
                Colisor = new Vector2(1.2f, 1.2f),
                Offset = Vector2.zero,
                MundoEsperado = new Vector2(0.6f, 0.6f),
            },

            // Inimigo humanoide: mesma altura de Damião (0.5 × 0.75 no mundo).
            new Alvo
            {
                Prefab = Enemies + "EsqueletoInvocado.prefab",
                Sprite = Enemies + "EsqueletoInvocado.png",
                Pivo = SpriteAlignment.BottomCenter,
                Escala = 0.5f,
                Colisor = new Vector2(0.832f, 1.088f),
                Offset = Vector2.zero,
                MundoEsperado = new Vector2(0.416f, 0.544f),
            },

            // Cenário destrutível da arena do Abdul. O colisor já era de altura humana
            // (1.0 × 1.35 no mundo) — quem montou dimensionou para uma pedra de verdade.
            // O offset sobe porque o corpo inteiro precisa ser acertável, e porque o pivô
            // saiu do centro.
            new Alvo
            {
                Prefab = Enemies + "PedraDePoder.prefab",
                Sprite = Enemies + "PedraDePoder.png",
                Pivo = SpriteAlignment.BottomCenter,
                Escala = 0.9f,
                Colisor = new Vector2(1.1111f, 1.5f),
                Offset = new Vector2(0f, 0.75f),
                MundoEsperado = new Vector2(1.0f, 1.35f),
            },

            // Projétil: pivô no centro, sprite apontando para +X.
            new Alvo
            {
                Prefab = Enemies + "ConeDeGelo.prefab",
                Sprite = Enemies + "ConeDeGelo.png",
                Pivo = SpriteAlignment.Center,
                Escala = 0.4f,
                Colisor = new Vector2(1.5f, 0.75f),
                Offset = Vector2.zero,
                MundoEsperado = new Vector2(0.6f, 0.3f),
            },

            // Relíquia no chão. O gatilho é o raio de coleta, bem maior que o livro.
            new Alvo
            {
                Prefab = Items + "Necronomicon.prefab",
                Sprite = Items + "Necronomicon.png",
                Pivo = SpriteAlignment.BottomCenter,
                Escala = 0.4f,
                Colisor = new Vector2(2.1f, 2.625f),
                Offset = new Vector2(0f, 0.25f),
                MundoEsperado = new Vector2(0.84f, 1.05f),
            },
        };

        [MenuItem("Tools/FavelaAmarela/Aplicar arte dos placeholders")]
        public static void Aplicar()
        {
            var resumo = new List<string>();

            foreach (var alvo in Alvos)
                resumo.Add(Processar(alvo));

            resumo.Add(CorrigirInstanciaDoYugNeth());

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            Debug.Log("[ArtePlaceholders] Concluído:\n  " + string.Join("\n  ", resumo));
        }

        private static string Processar(Alvo alvo)
        {
            string nome = System.IO.Path.GetFileNameWithoutExtension(alvo.Prefab);

            var sprite = ConfigurarImport(alvo.Sprite, alvo.Pivo);
            if (sprite == null) return $"{nome}: sprite não carregou ({alvo.Sprite})";

            var raiz = PrefabUtility.LoadPrefabContents(alvo.Prefab);
            if (raiz == null) return $"{nome}: prefab não abriu";

            try
            {
                var sr = raiz.GetComponent<SpriteRenderer>();
                if (sr == null) return $"{nome}: sem SpriteRenderer na raiz";

                sr.sprite = sprite;

                // A cor volta a branco: os cinco estavam TINGIDOS para dar alguma leitura ao
                // Knob (que é branco). Com arte de verdade, o tingimento só sujaria a paleta.
                sr.color = Color.white;

                raiz.transform.localScale = new Vector3(alvo.Escala, alvo.Escala, 1f);

                var box = raiz.GetComponent<BoxCollider2D>();
                if (box != null)
                {
                    box.size = alvo.Colisor;
                    box.offset = alvo.Offset;
                }

                PrefabUtility.SaveAsPrefabAsset(raiz, alvo.Prefab);
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(raiz);
            }

            Vector2 mundo = alvo.Colisor * alvo.Escala;
            return $"{nome}: sprite={System.IO.Path.GetFileName(alvo.Sprite)} " +
                   $"pivô={alvo.Pivo} escala={alvo.Escala} " +
                   $"colisor mundo=({mundo.x:0.###}, {mundo.y:0.###}) " +
                   $"esperado=({alvo.MundoEsperado.x:0.###}, {alvo.MundoEsperado.y:0.###})";
        }

        /// <summary>
        /// Aplica as regras de import de pixel art do projeto (skill
        /// <c>favela-pixelart-standards</c>): PPU 32, Point, sem compressão, sem mipmap.
        /// Sem isso o sprite entra a PPU 100 e borrado, e fica de outro tamanho.
        /// </summary>
        private static Sprite ConfigurarImport(string caminho, SpriteAlignment pivo)
        {
            var importer = AssetImporter.GetAtPath(caminho) as TextureImporter;
            if (importer == null)
            {
                Debug.LogError($"[ArtePlaceholders] Textura não encontrada em '{caminho}'.");
                return null;
            }

            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.spritePixelsPerUnit = 32f;
            importer.filterMode = FilterMode.Point;
            importer.textureCompression = TextureImporterCompression.Uncompressed;
            importer.alphaIsTransparency = true;
            importer.mipmapEnabled = false;

            // A propriedade acima só escreve o DefaultTexturePlatform. Os blocos por
            // plataforma nascem com compressão 1, e embora hoje sejam inertes (vêm com
            // `overridden: 0`, então o default é que vale), todo sprite já existente no
            // projeto está serializado com 0 em todas. Gravar explícito mantém a forma
            // uniforme e não deixa a compressão voltar caso alguém ligue um override.
            foreach (string plataforma in Plataformas)
            {
                var ps = importer.GetPlatformTextureSettings(plataforma);
                ps.textureCompression = TextureImporterCompression.Uncompressed;
                importer.SetPlatformTextureSettings(ps);
            }

            // O pivô só é gravável por TextureImporterSettings; a propriedade solta do
            // importer não cobre spriteAlignment.
            var s = new TextureImporterSettings();
            importer.ReadTextureSettings(s);
            s.spriteAlignment = (int)pivo;
            importer.SetTextureSettings(s);

            importer.SaveAndReimport();

            return AssetDatabase.LoadAssetAtPath<Sprite>(caminho);
        }

        /// <summary>
        /// O Yug-Neth é o único dos cinco que está <b>colocado</b> numa cena
        /// (<c>Tumba_De_Alhazred</c>) — os outros quatro nascem por <c>Instantiate</c> em
        /// runtime, então corrigir o prefab basta para eles.
        ///
        /// <para>E aquela instância <b>sobrescreve a escala</b> (1.0348, 1.2947). Override de
        /// instância ganha do prefab: sem mexer aqui, a correção da escala simplesmente não
        /// apareceria na única cena onde ele está — o Mi-Go entraria com mais de 2 unidades de
        /// altura, quase o triplo do Damião.</para>
        /// </summary>
        private static string CorrigirInstanciaDoYugNeth()
        {
            const string caminhoDaCena = "Assets/Scenes/Tumba_De_Alhazred.unity";

            if (!System.IO.File.Exists(caminhoDaCena))
                return "Tumba_De_Alhazred: cena ausente";

            var cena = EditorSceneManager.OpenScene(caminhoDaCena, OpenSceneMode.Single);

            int ajustados = 0;
            foreach (var t in Object.FindObjectsByType<Transform>(FindObjectsInactive.Include))
            {
                if (t.name != "YugNeth") continue;

                Undo.RecordObject(t, "Ajustar escala do Yug-Neth");
                t.localScale = new Vector3(0.5f, 0.5f, 1f);
                EditorUtility.SetDirty(t);
                ajustados++;
            }

            if (ajustados > 0)
            {
                EditorSceneManager.MarkSceneDirty(cena);
                EditorSceneManager.SaveScene(cena);
            }

            return $"Tumba_De_Alhazred: {ajustados} instância(s) de YugNeth com escala 0.5";
        }
    }
}
