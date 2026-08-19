using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;

namespace FavelaAmarela.EditorTools
{
    /// <summary>
    /// Miolo compartilhado de "folha de sprites → clipes → controller → <c>Animator</c> no
    /// prefab". Serve o Byakhee e o Abdul; o Rei em Amarelo é o próximo.
    ///
    /// <para><b>Por que virou peça própria:</b> a segunda ligação de animação ia ser uma cópia
    /// da primeira, e a terceira, outra. Três cópias divergem — a correção entra numa e as
    /// outras ficam para trás em silêncio. Mesmo motivo de <see cref="PaletaDaInterface"/> e
    /// de <c>MontarBootstrapDaCena</c>.</para>
    ///
    /// <para><b>Convenção de nome de fatia:</b> <c>&lt;prefixo&gt;_&lt;animacao&gt;_&lt;n&gt;</c>
    /// (ex.: <c>abdul_attack_3</c>). Os clipes saem <b>dos nomes</b>, não de uma tabela escrita
    /// à mão: acrescentar quadros à folha passa a funcionar sozinho, e não há tabela paralela
    /// para envelhecer.</para>
    /// </summary>
    public static class MontadorDeAnimacao
    {
        /// <summary>Uma animação a recortar de uma folha de fatias iguais e enfileiradas.</summary>
        public struct Faixa
        {
            /// <summary>Nome da animação, sem prefixo (ex.: <c>attack</c>).</summary>
            public string Nome;

            /// <summary>Índice do primeiro quadro na folha.</summary>
            public int Inicio;

            /// <summary>Quantidade de quadros.</summary>
            public int Quantidade;

            /// <summary>Se o clipe repete.</summary>
            public bool Loop;

            public Faixa(string nome, int inicio, int quantidade, bool loop)
            {
                Nome = nome; Inicio = inicio; Quantidade = quantidade; Loop = loop;
            }
        }

        /// <summary>
        /// Fatia uma folha de quadros de tamanho fixo, nomeando cada fatia pela faixa a que
        /// pertence. Aplica as regras de pixel art do projeto (PPU 32, Point, sem compressão).
        ///
        /// <para>O pivô é <b>BottomCenter</b>: <c>DynamicYSort</c> ordena por
        /// <c>transform.position.y + offsetPes</c>, e com <c>offsetPes = 0</c> isso só está
        /// certo se o pivô estiver nos pés.</para>
        /// </summary>
        /// <param name="tamanhoMaximo">
        /// Teto de resolução do import. O padrão da Unity é <b>2048</b>, e uma folha mais larga
        /// que isso é <b>reescalada em silêncio</b> — pixel art borrada, sem aviso nenhum.
        ///
        /// <para><b>Aviso a quem for usar isto para escapar do teto:</b> subir este valor
        /// <b>não resolveu</b> no caso do Rei em Amarelo. Nem <c>importer.maxTextureSize</c> nem
        /// <c>SetPlatformTextureSettings("DefaultTexturePlatform")</c> chegaram a escrever o
        /// <c>maxTextureSize</c> do topo do <c>.meta</c>, que continuou em 2048 — conferido no
        /// arquivo, não no log. A saída foi eliminar a causa: empacotar folhas que já cabem.</para>
        /// </param>
        public static bool FatiarFolha(string caminho, string prefixo, int larguraDoQuadro,
                                       int alturaDoQuadro, IEnumerable<Faixa> faixas,
                                       int tamanhoMaximo = 2048)
        {
            var importer = AssetImporter.GetAtPath(caminho) as TextureImporter;
            if (importer == null)
            {
                Debug.LogError($"[MontadorDeAnimacao] Textura não encontrada em '{caminho}'.");
                return false;
            }

            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Multiple;
            importer.spritePixelsPerUnit = 32f;
            importer.filterMode = FilterMode.Point;
            importer.textureCompression = TextureImporterCompression.Uncompressed;
            importer.alphaIsTransparency = true;
            importer.mipmapEnabled = false;
            importer.maxTextureSize = tamanhoMaximo;

            // DefaultTexturePlatform PRECISA estar na lista: os blocos por plataforma nascem com
            // `overridden: 0`, ou seja, inertes — quem governa de fato é o default. Sem ele,
            // atribuir `importer.maxTextureSize` deixava o topo do .meta em 2048 e a Unity
            // reescalava as folhas de 2805px e 3135px do Rei em silêncio, borrando a pixel art.
            foreach (string plataforma in new[]
                     { "DefaultTexturePlatform", "Standalone", "WebGL", "WindowsStoreApps" })
            {
                var ps = importer.GetPlatformTextureSettings(plataforma);
                ps.textureCompression = TextureImporterCompression.Uncompressed;
                ps.maxTextureSize = tamanhoMaximo;
                importer.SetPlatformTextureSettings(ps);
            }

            var fatias = new List<SpriteMetaData>();

            foreach (var f in faixas)
            {
                for (int i = 0; i < f.Quantidade; i++)
                {
                    int indice = f.Inicio + i;

                    fatias.Add(new SpriteMetaData
                    {
                        name = $"{prefixo}_{f.Nome}_{i}",
                        // A Unity conta o eixo Y de baixo para cima; a folha é de uma linha só.
                        rect = new Rect(indice * larguraDoQuadro, 0, larguraDoQuadro, alturaDoQuadro),
                        alignment = (int)SpriteAlignment.BottomCenter,
                        pivot = new Vector2(0.5f, 0f),
                    });
                }
            }

            // A API antiga (spritesheet) continua sendo a que funciona em batch mode; o
            // SpriteDataProvider exige o Editor gráfico aberto.
#pragma warning disable CS0618
            importer.spritesheet = fatias.ToArray();
#pragma warning restore CS0618

            importer.SaveAndReimport();

            Debug.Log($"[MontadorDeAnimacao] '{System.IO.Path.GetFileName(caminho)}' fatiada em " +
                      $"{fatias.Count} sprite(s) de {larguraDoQuadro}×{alturaDoQuadro}.");
            return true;
        }

        /// <summary>
        /// Lê os sprites de uma folha já fatiada e agrupa por animação, a partir do nome.
        /// <c>abdul_attack_3</c> → grupo <c>attack</c>, índice 3.
        /// </summary>
        public static Dictionary<string, List<Sprite>> AgruparPorNome(string caminho, string prefixo)
        {
            var sprites = AssetDatabase.LoadAllAssetsAtPath(caminho).OfType<Sprite>().ToList();

            if (sprites.Count == 0)
            {
                Debug.LogError($"[MontadorDeAnimacao] Nenhum sprite em '{caminho}'. " +
                               "A folha está fatiada (spriteMode Multiple)?");
                return null;
            }

            var grupos = new Dictionary<string, List<(int ordem, Sprite s)>>();

            foreach (var s in sprites)
            {
                var m = Regex.Match(s.name, $@"^{Regex.Escape(prefixo)}_(.+)_(\d+)$");
                if (!m.Success)
                {
                    Debug.LogWarning($"[MontadorDeAnimacao] Fatia '{s.name}' fora do padrão " +
                                     $"'{prefixo}_<animacao>_<n>' — ficou de fora.");
                    continue;
                }

                string nome = m.Groups[1].Value;
                if (!grupos.ContainsKey(nome)) grupos[nome] = new List<(int, Sprite)>();
                grupos[nome].Add((int.Parse(m.Groups[2].Value), s));
            }

            return grupos.ToDictionary(
                p => p.Key,
                p => p.Value.OrderBy(t => t.ordem).Select(t => t.s).ToList());
        }

        /// <summary>Cria (ou reescreve) um clipe de troca de sprite.</summary>
        public static AnimationClip MontarClipe(string pasta, string arquivo, List<Sprite> quadros,
                                                bool loop, float fps)
        {
            string caminho = $"{pasta}/{arquivo}.anim";

            var clipe = AssetDatabase.LoadAssetAtPath<AnimationClip>(caminho);
            if (clipe == null)
            {
                clipe = new AnimationClip();
                AssetDatabase.CreateAsset(clipe, caminho);
            }

            clipe.frameRate = fps;

            var binding = new EditorCurveBinding
            {
                type = typeof(SpriteRenderer),
                path = "",                    // o SpriteRenderer está na raiz do prefab
                propertyName = "m_Sprite",
            };

            var chaves = new ObjectReferenceKeyframe[quadros.Count];
            for (int i = 0; i < quadros.Count; i++)
                chaves[i] = new ObjectReferenceKeyframe { time = i / fps, value = quadros[i] };

            AnimationUtility.SetObjectReferenceCurve(clipe, binding, chaves);

            var cfg = AnimationUtility.GetAnimationClipSettings(clipe);
            cfg.loopTime = loop;
            AnimationUtility.SetAnimationClipSettings(clipe, cfg);

            EditorUtility.SetDirty(clipe);
            return clipe;
        }

        /// <summary>
        /// Um estado por clipe, com <paramref name="estadoPadrao"/> como default.
        ///
        /// <para><b>Sem teia de transições com condições, de propósito:</b> quem manda no estado
        /// é a FSM do chefe, que já tem um ponto único de troca. Duplicar essa lógica em
        /// condições do Animator criaria duas fontes de verdade que divergem em silêncio. Os
        /// adaptadores chamam <c>Animator.Play</c> direto.</para>
        ///
        /// <para>As únicas transições são as de <paramref name="voltamAoPadrao"/>: clipes de
        /// interrupção curta (levar dano) que precisam devolver o controle sozinhos.</para>
        /// </summary>
        public static AnimatorController MontarControlador(string caminho,
            Dictionary<string, AnimationClip> clipes, string estadoPadrao,
            IEnumerable<string> voltamAoPadrao)
        {
            var ctrl = AssetDatabase.LoadAssetAtPath<AnimatorController>(caminho);
            if (ctrl == null) ctrl = AnimatorController.CreateAnimatorControllerAtPath(caminho);

            var maquina = ctrl.layers[0].stateMachine;

            // Zera para ser idempotente: rodar de novo não empilha estados repetidos.
            foreach (var st in maquina.states.ToArray()) maquina.RemoveState(st.state);

            var estados = new Dictionary<string, AnimatorState>();
            foreach (var par in clipes.OrderBy(p => p.Key))
            {
                var st = maquina.AddState(par.Key);
                st.motion = par.Value;
                st.writeDefaultValues = false;
                estados[par.Key] = st;
            }

            AnimatorState padrao = null;
            if (estados.TryGetValue(estadoPadrao, out padrao))
                maquina.defaultState = padrao;

            if (padrao != null)
            {
                foreach (var nome in voltamAoPadrao)
                {
                    if (!estados.TryGetValue(nome, out var origem)) continue;

                    var t = origem.AddTransition(padrao);
                    t.hasExitTime = true;
                    t.exitTime = 1f;
                    t.duration = 0f;
                }
            }

            EditorUtility.SetDirty(ctrl);
            return ctrl;
        }

        /// <summary>
        /// Põe o <c>Animator</c> na raiz do prefab e aponta para o controller. Também fixa o
        /// sprite inicial: sem isso, o prefab fica exibindo a arte antiga fora do Play Mode.
        /// </summary>
        public static string PorAnimatorNoPrefab(string caminhoDoPrefab, AnimatorController ctrl,
                                                 Sprite spriteInicial)
        {
            if (!System.IO.File.Exists(caminhoDoPrefab)) return $"{caminhoDoPrefab}: ausente";

            var raiz = PrefabUtility.LoadPrefabContents(caminhoDoPrefab);
            try
            {
                var anim = raiz.GetComponent<Animator>();
                if (anim == null) anim = raiz.AddComponent<Animator>();

                anim.runtimeAnimatorController = ctrl;
                anim.applyRootMotion = false;
                anim.updateMode = AnimatorUpdateMode.Normal;
                anim.cullingMode = AnimatorCullingMode.CullUpdateTransforms;

                if (spriteInicial != null)
                {
                    var sr = raiz.GetComponent<SpriteRenderer>();
                    if (sr != null) sr.sprite = spriteInicial;
                }

                PrefabUtility.SaveAsPrefabAsset(raiz, caminhoDoPrefab);
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(raiz);
            }

            return $"Animator ligado em {caminhoDoPrefab}";
        }
    }
}
