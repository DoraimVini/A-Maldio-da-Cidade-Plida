using System.Linq;
using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using FavelaAmarela.Core.Enemies;
using FavelaAmarela.Runtime.Enemies;
using FavelaAmarela.Runtime.Itens;

namespace FavelaAmarela.EditorTools
{
    /// <summary>
    /// <c>Tools/FavelaAmarela/Trono: montar o Rito do Olhar</c> — põe a Z5 do Castelo no desenho
    /// do Rito do Olhar. <b>Idempotente</b>: rodar de novo reposiciona o que já existe em vez de
    /// duplicar.
    ///
    /// <para><b>O layout não é de mão.</b> Saiu de uma busca que confere as seis regras de
    /// geometria do plano (§6.2) contra o chão pintado e o olho real do Rei (os pés, em
    /// (0,1 ; 66,7) — não o pivô do sprite). As posições abaixo são o resultado; o guarda
    /// <c>OTronoDoOlharTests</c> as confere na cena a cada suíte.</para>
    ///
    /// <para><b>O Rei não se move.</b> O plano recomendava pô-lo ao fundo (decisão D1), e ele
    /// <b>já está</b>: o pivô do quadro fica 3,9 un à direita e 4,3 un abaixo da figura
    /// desenhada, e a figura está em pé no fundo da sala desde que o Vini a posicionou. Movê-lo
    /// para (0 ; 68) poria o pé dele fora do chão.</para>
    /// </summary>
    public static class MontarORitoDoOlhar
    {
        private const string Cena = "Assets/Scenes/Castelo_Carcosa.unity";
        private const string PrefabDoRei = "Assets/FavelaAmarela/Art/Enemies/ReiEmAmarelo.prefab";
        private const string PastaDoAltar = "Assets/FavelaAmarela/Art/Props/AltarDeReliquia";
        private const string PastaDosNobres = "Assets/FavelaAmarela/Art/Environment/CasteloCarcosa";
        private const string MaterialDoDither = "Assets/FavelaAmarela/Art/Materials/OcclusionDither.mat";

        /// <summary>Centro do Altar de Selamento (a elipse de 4 × 2).</summary>
        public static readonly Vector2 CentroDoAltar = new Vector2(0f, 62f);

        /// <summary>Base (pivô) de cada Nobre, em escala 2. A pegada fica em [x ± 1,2] × [y, y + 2].</summary>
        public static readonly Vector2[] Nobres =
        {
            new Vector2(-4.5f, 62.0f),
            new Vector2(4.5f, 62.0f),
            new Vector2(-8.5f, 63.0f),
            new Vector2(8.5f, 63.0f),
            new Vector2(-5.0f, 58.5f),
        };

        /// <summary>Os altares de fragmento: dois ao lado do Rei, um perto da entrada.</summary>
        public static readonly (string id, Vector2 posicao)[] AltaresDeFragmento =
        {
            (RitoDoRei.Necronomicon, new Vector2(-5.5f, 66.0f)),
            (RitoDoRei.Patua, new Vector2(5.5f, 66.0f)),
            (RitoDoRei.Anel, new Vector2(0f, 57.3f)),
        };

        private const float EscalaDoNobre = 2f;

        [MenuItem("Tools/FavelaAmarela/Trono: montar o Rito do Olhar")]
        public static void Executar()
        {
            var log = new StringBuilder("[RitoDoOlhar]\n");

            LimparOPrefabDoRei(log);

            var cena = EditorSceneManager.GetActiveScene();
            if (cena.path != Cena) cena = EditorSceneManager.OpenScene(Cena, OpenSceneMode.Single);

            var z5 = GameObject.Find("Z5_TronoDeAldebaran");
            var rei = Object.FindAnyObjectByType<ReiEmAmareloAI>(FindObjectsInactive.Include);
            if (z5 == null || rei == null)
            {
                Debug.LogError("[RitoDoOlhar] Sem Z5_TronoDeAldebaran ou sem Rei na cena do Castelo.");
                return;
            }

            var material = rei.GetComponent<SpriteRenderer>().sharedMaterial;
            var altar = MontarAltar(z5.transform, material, log);
            var nobres = MontarNobres(z5.transform, log);
            var fragmentos = PosicionarAltaresDeFragmento(z5.transform, log);
            var eco = MontarEco(z5.transform, log);

            var so = new SerializedObject(rei);
            so.FindProperty("altar").objectReferenceValue = altar;
            Lista(so.FindProperty("coberturas"), nobres);
            Lista(so.FindProperty("altaresDeFragmento"), fragmentos);
            so.FindProperty("eco").objectReferenceValue = eco;
            so.FindProperty("materialDosTracos").objectReferenceValue = material;
            so.FindProperty("fonteDaTela").objectReferenceValue = PaletaDaInterface.Fonte;
            so.ApplyModifiedPropertiesWithoutUndo();
            log.AppendLine($"   Rei ligado: altar, {nobres.Length} Nobres, {fragmentos.Length} altares de fragmento, Eco.");

            // A instância do Rei na cena ainda carregava overrides de campos da luta antiga
            // (os 'escudos', a ficha…): campos que não existem mais, gravados à toa na cena.
            if (PrefabUtility.IsAnyPrefabInstanceRoot(rei.gameObject))
            {
                PrefabUtility.RemoveUnusedOverrides(new[] { rei.gameObject }, InteractionMode.AutomatedAction);
                log.AppendLine("   Overrides órfãos da instância do Rei removidos.");
            }

            EditorSceneManager.MarkSceneDirty(cena);
            EditorSceneManager.SaveScene(cena);
            Debug.Log(log.ToString());
        }

        // ── O Rei ────────────────────────────────────────────────────────────

        /// <summary>
        /// Tira do prefab o que era da luta antiga: o espólio (decisão D3 — o jogo termina no
        /// selamento), a Exposição por abate e a barra de vida. O Rei não tem carne.
        /// </summary>
        private static void LimparOPrefabDoRei(StringBuilder log)
        {
            var raiz = PrefabUtility.LoadPrefabContents(PrefabDoRei);
            try
            {
                int tirados = 0;
                foreach (var tipo in new[] { typeof(DropAoAbater), typeof(FavelaAmarela.Runtime.Progression.ExposicaoAoAbater) })
                {
                    var c = raiz.GetComponent(tipo);
                    if (c == null) continue;
                    Object.DestroyImmediate(c, true);
                    tirados++;
                }

                var barra = raiz.GetComponentInChildren<FavelaAmarela.Runtime.UI.BarraDeVidaFlutuante>(true);
                if (barra != null)
                {
                    Object.DestroyImmediate(barra.gameObject, true);
                    tirados++;
                }

                PrefabUtility.SaveAsPrefabAsset(raiz, PrefabDoRei);
                log.AppendLine($"   Prefab do Rei: {tirados} peça(s) da luta antiga removida(s).");
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(raiz);
            }
        }

        // ── O Altar de Selamento ─────────────────────────────────────────────

        private static AltarDeSelamento MontarAltar(Transform z5, Material material, StringBuilder log)
        {
            var go = Filho(z5, "Altar_De_Selamento");
            go.transform.position = CentroDoAltar;

            var altar = Garantir<AltarDeSelamento>(go);
            var feixe = Garantir<AnimadorDeAltarDeReliquia>(go);
            var quadros = Enumerable.Range(0, 10)
                .Select(i => AssetDatabase.LoadAssetAtPath<Sprite>($"{PastaDoAltar}/Altar_Reliquia_Aceso_{i:00}.png"))
                .ToArray();
            var sof = new SerializedObject(feixe);
            Lista(sof.FindProperty("quadros"), quadros);
            sof.ApplyModifiedPropertiesWithoutUndo();

            var apagado = AssetDatabase.LoadAssetAtPath<Sprite>($"{PastaDoAltar}/Altar_Reliquia_Inativo.png");

            // A pedra fica na borda do FUNDO da elipse: o Damião, dentro da área, está na frente
            // dela e o Y-sort o desenha por cima.
            var pedraGo = Filho(go.transform, "Pedra");
            pedraGo.transform.localPosition = new Vector3(0f, 0.9f, 0f);
            pedraGo.transform.localScale = new Vector3(1.3f, 1.3f, 1f);
            var pedra = Garantir<SpriteRenderer>(pedraGo);
            pedra.sprite = apagado;
            pedra.sharedMaterial = material;
            pedra.sortingOrder = Mathf.RoundToInt(-pedraGo.transform.position.y * 10f);

            var circulo = MontarCirculo(go.transform, material);

            var so = new SerializedObject(altar);
            so.FindProperty("pedra").objectReferenceValue = pedra;
            so.FindProperty("spriteApagado").objectReferenceValue = apagado;
            so.FindProperty("circulo").objectReferenceValue = circulo;
            so.ApplyModifiedPropertiesWithoutUndo();

            log.AppendLine($"   Altar de Selamento em {CentroDoAltar}.");
            return altar;
        }

        /// <summary>A elipse da área, desenhada no chão: acima do piso, abaixo de qualquer ator.</summary>
        private static LineRenderer MontarCirculo(Transform altar, Material material)
        {
            var go = Filho(altar, "Circulo");
            go.transform.localPosition = Vector3.zero;
            var lr = Garantir<LineRenderer>(go);
            const int pontos = 48;
            lr.useWorldSpace = false;
            lr.loop = true;
            lr.positionCount = pontos;
            for (int i = 0; i < pontos; i++)
            {
                float t = i / (float)pontos * Mathf.PI * 2f;
                lr.SetPosition(i, new Vector3(AbrigoDeReliquia.SemiEixoXPadrao * Mathf.Cos(t),
                                              AbrigoDeReliquia.SemiEixoYPadrao * Mathf.Sin(t), 0f));
            }
            lr.widthMultiplier = 0.08f;
            lr.numCornerVertices = 0;
            lr.alignment = LineAlignment.View;
            lr.sharedMaterial = material;
            lr.sortingOrder = -1999;
            lr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            lr.receiveShadows = false;
            return lr;
        }

        // ── Os Nobres ────────────────────────────────────────────────────────

        private static CoberturaDoTrono[] MontarNobres(Transform z5, StringBuilder log)
        {
            var sprites = Enumerable.Range(0, 3)
                .Select(i => AssetDatabase.LoadAssetAtPath<Sprite>($"{PastaDosNobres}/Nobre_Fossilizado_{i}.png"))
                .ToArray();
            int obstaculo = LayerMask.NameToLayer("Obstacle");
            var saida = new CoberturaDoTrono[Nobres.Length];
            var dither = AssetDatabase.LoadAssetAtPath<Material>(MaterialDoDither);
            if (dither == null)
                Debug.LogError($"[RitoDoOlhar] Sem '{MaterialDoDither}' — os Nobres ficam sem silhueta.");

            for (int i = 0; i < Nobres.Length; i++)
            {
                var go = Filho(z5, $"Nobre_Do_Trono_{i}");
                go.layer = obstaculo;
                go.transform.position = Nobres[i];
                go.transform.localScale = new Vector3(EscalaDoNobre, EscalaDoNobre, 1f);

                var sr = Garantir<SpriteRenderer>(go);
                sr.sprite = sprites[i % sprites.Length];
                sr.flipX = Nobres[i].x > 0f;   // viram-se para o Rei, no centro
                sr.sortingOrder = Mathf.RoundToInt(-Nobres[i].y * 10f);

                // A pegada: a base do Nobre, acima do pivô (o Z2 a deixou abaixo — lá não importa;
                // aqui ela é a sombra, e tem de coincidir com a pedra desenhada).
                var box = Garantir<BoxCollider2D>(go);
                box.offset = new Vector2(0f, 0.5f);
                box.size = new Vector2(1.2f, 1.0f);
                box.isTrigger = false;
                box.enabled = true;

                // A silhueta do Damião do outro lado da estátua (skill de isometria, regra 6): o
                // material do dither + o componente que o dirige. O componente só sabe do Damião
                // por gatilho, e o gatilho tem de estar NESTE objeto (o Nobre não tem Rigidbody2D
                // para receber a mensagem de um filho). Cápsula, e não outra caixa: a
                // CoberturaDoTrono lê a pegada por GetComponent<BoxCollider2D>.
                sr.sharedMaterial = dither;
                Garantir<FavelaAmarela.Runtime.Rendering.OcclusaoDitherFade>(go);
                var gatilho = Garantir<CapsuleCollider2D>(go);
                gatilho.isTrigger = true;
                gatilho.direction = CapsuleDirection2D.Vertical;
                gatilho.offset = new Vector2(0f, 1.1f);
                gatilho.size = new Vector2(1.5f, 1.9f);

                saida[i] = Garantir<CoberturaDoTrono>(go);
                log.AppendLine($"   Nobre {i} em {Nobres[i]} (escala {EscalaDoNobre}, com dither).");
            }
            return saida;
        }

        // ── Os altares de fragmento ─────────────────────────────────────────

        private static PontoFocalDeReliquia[] PosicionarAltaresDeFragmento(Transform z5, StringBuilder log)
        {
            var existentes = z5.GetComponentsInChildren<PontoFocalDeReliquia>(true);
            var saida = new PontoFocalDeReliquia[AltaresDeFragmento.Length];

            for (int i = 0; i < AltaresDeFragmento.Length; i++)
            {
                var (id, posicao) = AltaresDeFragmento[i];
                var ponto = existentes.FirstOrDefault(p => p.ArtefatoId == id);
                if (ponto == null)
                {
                    Debug.LogError($"[RitoDoOlhar] Sem o altar de relíquia '{id}' na Z5.");
                    continue;
                }

                ponto.transform.position = posicao;

                // O escudo da luta antiga sai: o componente já não existe, e o filho que era a
                // cúpula desenhada ficaria no altar como um enfeite sem sentido.
                var escudo = ponto.transform.Find("Escudo");
                if (escudo != null) Object.DestroyImmediate(escudo.gameObject);
                RemoverScriptsAusentes(ponto.gameObject);

                var sr = ponto.GetComponent<SpriteRenderer>();
                if (sr != null) sr.sortingOrder = Mathf.RoundToInt(-posicao.y * 10f);

                saida[i] = ponto;
                log.AppendLine($"   Altar de fragmento '{id}' em {posicao}.");
            }
            return saida;
        }

        private static void RemoverScriptsAusentes(GameObject go)
        {
            int n = GameObjectUtility.RemoveMonoBehavioursWithMissingScript(go);
            if (n > 0) Debug.Log($"[RitoDoOlhar] '{go.name}': {n} componente(s) sem script removido(s).");
        }

        // ── O Eco da Queda ───────────────────────────────────────────────────

        /// <summary>
        /// Um Eco só, e não os três do plano: todos se manifestariam no mesmo lugar (nas costas
        /// do Damião), e três drenos empilhados seriam o triplo sem nenhuma leitura nova. Nasce
        /// inativo; o Rei o liga na Queda. Clonado de um Eco da Biblioteca para herdar os quadros.
        /// </summary>
        private static EcoDeCarcosa MontarEco(Transform z5, StringBuilder log)
        {
            var existente = z5.Find("Eco_Da_Queda");
            GameObject go;
            if (existente != null)
            {
                go = existente.gameObject;
            }
            else
            {
                var modelo = Object.FindObjectsByType<EcoDeCarcosa>(FindObjectsInactive.Include)
                                   .FirstOrDefault(e => !e.transform.IsChildOf(z5));
                if (modelo == null)
                {
                    Debug.LogError("[RitoDoOlhar] Nenhum Eco de Carcosa na cena para clonar.");
                    return null;
                }
                go = Object.Instantiate(modelo.gameObject, z5);
                go.name = "Eco_Da_Queda";
            }

            go.transform.position = CentroDoAltar + new Vector2(0f, -3f);
            var eco = go.GetComponent<EcoDeCarcosa>();
            var so = new SerializedObject(eco);
            so.FindProperty("tempoMaximoImovel").floatValue = 1.5f;
            so.ApplyModifiedPropertiesWithoutUndo();
            go.SetActive(false);

            log.AppendLine("   Eco da Queda: imobilidade 1,5 s, inativo até a Fase 5.");
            return eco;
        }

        // ── Utilidades ───────────────────────────────────────────────────────

        private static GameObject Filho(Transform pai, string nome)
        {
            var t = pai.Find(nome);
            if (t != null) return t.gameObject;
            var go = new GameObject(nome);
            go.transform.SetParent(pai, false);
            return go;
        }

        // Sem '??': componente ausente da Unity é um "null falso" que o operador não reconhece,
        // e a ferramenta seguia sem adicionar nada (foi o erro da primeira execução).
        private static T Garantir<T>(GameObject go) where T : Component
        {
            var c = go.GetComponent<T>();
            return c != null ? c : go.AddComponent<T>();
        }

        private static void Lista(SerializedProperty prop, Object[] valores)
        {
            prop.arraySize = valores.Length;
            for (int i = 0; i < valores.Length; i++)
                prop.GetArrayElementAtIndex(i).objectReferenceValue = valores[i];
        }
    }
}
