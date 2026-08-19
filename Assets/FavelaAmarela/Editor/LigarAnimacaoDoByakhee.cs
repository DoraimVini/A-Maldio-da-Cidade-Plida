using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;

namespace FavelaAmarela.EditorTools
{
    /// <summary>
    /// Monta os clipes de animação do Byakhee a partir das 26 fatias já nomeadas na folha,
    /// cria o <c>Byakhee_AC</c> e põe o <c>Animator</c> no prefab.
    ///
    /// <para><b>Por que o Byakhee e não o Abdul.</b> O Abdul parecia estar mais perto do fim
    /// (já tem 7 clipes e um controller), mas a folha dele é <b>totalmente opaca</b> — o xadrez
    /// de transparência está achatado dentro do PNG. Enquanto ela não for reexportada do
    /// <c>.ase</c>, ele renderiza como um quadrado de 4×4 unidades com fundo claro; e quando for
    /// reexportada, <b>os 7 clipes morrem junto</b>, porque cada keyframe aponta para uma fatia
    /// dela por <c>fileID</c>. A folha do Byakhee tem transparência correta, então aqui o
    /// trabalho não se perde. Ver <c>Docs/KnowledgeBundle/systems/arte_e_animacao.md</c>.</para>
    ///
    /// <para><b>Os clipes saem dos nomes das fatias</b>, não de uma tabela escrita à mão: quem
    /// fatiou a folha já as nomeou por animação (<c>byakhee_espreita_0</c>…), em terminologia
    /// diegética. Agrupar pelo prefixo mantém a ferramenta correta se alguém acrescentar
    /// quadros — e evita que uma tabela paralela envelheça em silêncio.</para>
    ///
    /// <para>Idempotente: reescreve os mesmos assets nos mesmos caminhos.</para>
    /// </summary>
    public static class LigarAnimacaoDoByakhee
    {
        private const string Folha = "Assets/FavelaAmarela/Art/Enemies/Byakhee_Spritesheet.png";
        private const string Pasta = "Assets/FavelaAmarela/Art/Enemies/Byakhee";
        private const string Controlador = Pasta + "/Byakhee_AC.controller";

        /// <summary>12 quadros por segundo: a folha tem 4 a 6 quadros por animação, e a 12 fps
        /// um ciclo de 6 dura meio segundo — batida de asa plausível sem parecer acelerado.</summary>
        private const float Fps = 12f;

        /// <summary>
        /// Quais animações repetem. <c>garras</c> e <c>grito</c> repetem porque a FSM pode
        /// permanecer no estado além da duração do clipe — sem loop, o boss congelaria no
        /// último quadro no meio da investida.
        /// </summary>
        private static readonly Dictionary<string, bool> EmLoop = new Dictionary<string, bool>
        {
            { "espreita", true },
            { "rasante",  true },
            { "garras",   true },
            { "grito",    true },
            { "dano",     false },
            { "derrota",  false },   // segura o último quadro: o corpo fica caído
        };

        [MenuItem("Tools/FavelaAmarela/Ligar animacao do Byakhee")]
        public static void Executar()
        {
            var grupos = AgruparFatias();
            if (grupos == null) return;

            if (!AssetDatabase.IsValidFolder(Pasta))
                AssetDatabase.CreateFolder("Assets/FavelaAmarela/Art/Enemies", "Byakhee");

            var clipes = new Dictionary<string, AnimationClip>();
            var resumo = new List<string>();

            foreach (var par in grupos.OrderBy(p => p.Key))
            {
                bool loop = EmLoop.TryGetValue(par.Key, out bool l) && l;
                var clipe = MontarClipe(par.Key, par.Value, loop);
                clipes[par.Key] = clipe;
                resumo.Add($"{par.Key}: {par.Value.Count} quadro(s), {(loop ? "loop" : "1x")}");
            }

            var ctrl = MontarControlador(clipes);
            resumo.Add(PorAnimatorNoPrefab(ctrl));

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            Debug.Log("[AnimacaoByakhee] Concluído:\n  " + string.Join("\n  ", resumo));
        }

        /// <summary>
        /// Lê os sprites da folha e agrupa por prefixo, descartando o sufixo numérico.
        /// <c>byakhee_espreita_3</c> → grupo <c>espreita</c>, índice 3.
        /// </summary>
        private static Dictionary<string, List<Sprite>> AgruparFatias()
        {
            var sprites = AssetDatabase.LoadAllAssetsAtPath(Folha).OfType<Sprite>().ToList();

            if (sprites.Count == 0)
            {
                Debug.LogError($"[AnimacaoByakhee] Nenhum sprite em '{Folha}'. A folha está " +
                               "fatiada (spriteMode Multiple)?");
                return null;
            }

            var grupos = new Dictionary<string, List<(int ordem, Sprite s)>>();

            foreach (var s in sprites)
            {
                var m = System.Text.RegularExpressions.Regex.Match(s.name, @"^byakhee_(.+)_(\d+)$");
                if (!m.Success)
                {
                    Debug.LogWarning($"[AnimacaoByakhee] Fatia '{s.name}' fora do padrão " +
                                     "'byakhee_<animacao>_<n>' — ficou de fora.");
                    continue;
                }

                string nome = m.Groups[1].Value;
                int ordem = int.Parse(m.Groups[2].Value);

                if (!grupos.ContainsKey(nome)) grupos[nome] = new List<(int, Sprite)>();
                grupos[nome].Add((ordem, s));
            }

            return grupos.ToDictionary(
                p => p.Key,
                p => p.Value.OrderBy(t => t.ordem).Select(t => t.s).ToList());
        }

        private static AnimationClip MontarClipe(string nome, List<Sprite> quadros, bool loop)
        {
            string caminho = $"{Pasta}/Byakhee_{nome}.anim";

            var clipe = AssetDatabase.LoadAssetAtPath<AnimationClip>(caminho);
            if (clipe == null)
            {
                clipe = new AnimationClip();
                AssetDatabase.CreateAsset(clipe, caminho);
            }

            clipe.frameRate = Fps;

            var binding = new EditorCurveBinding
            {
                type = typeof(SpriteRenderer),
                path = "",                    // o SpriteRenderer está na raiz do prefab
                propertyName = "m_Sprite",
            };

            var chaves = new ObjectReferenceKeyframe[quadros.Count];
            for (int i = 0; i < quadros.Count; i++)
                chaves[i] = new ObjectReferenceKeyframe { time = i / Fps, value = quadros[i] };

            AnimationUtility.SetObjectReferenceCurve(clipe, binding, chaves);

            var cfg = AnimationUtility.GetAnimationClipSettings(clipe);
            cfg.loopTime = loop;
            AnimationUtility.SetAnimationClipSettings(clipe, cfg);

            EditorUtility.SetDirty(clipe);
            return clipe;
        }

        /// <summary>
        /// Um estado por clipe, <c>espreita</c> como default.
        ///
        /// <para><b>Sem teia de transições de propósito:</b> quem manda no estado é a
        /// <c>ByakheeFSM</c>, que já tem <c>HandleEstadoMudou</c> como ponto único. Duplicar
        /// essa lógica em condições do Animator criaria duas fontes de verdade que divergem em
        /// silêncio. O AI chama <c>Play</c> direto.</para>
        ///
        /// <para>A única transição é <c>dano → espreita</c> por tempo: o clipe de dano é uma
        /// interrupção curta que precisa devolver o controle sozinha. <b>Ressalva conhecida:</b>
        /// se ele levar um golpe em pleno rasante, volta visualmente para <c>espreita</c> até a
        /// próxima troca de estado da FSM.</para>
        /// </summary>
        private static AnimatorController MontarControlador(Dictionary<string, AnimationClip> clipes)
        {
            var ctrl = AssetDatabase.LoadAssetAtPath<AnimatorController>(Controlador);
            if (ctrl == null) ctrl = AnimatorController.CreateAnimatorControllerAtPath(Controlador);

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

            if (estados.TryGetValue("espreita", out var padrao))
                maquina.defaultState = padrao;

            if (estados.TryGetValue("dano", out var dano) && padrao != null)
            {
                var t = dano.AddTransition(padrao);
                t.hasExitTime = true;
                t.exitTime = 1f;
                t.duration = 0f;
            }

            EditorUtility.SetDirty(ctrl);
            return ctrl;
        }

        private static string PorAnimatorNoPrefab(AnimatorController ctrl)
        {
            string caminho = AssetDatabase.FindAssets("Byakhee t:Prefab")
                .Select(AssetDatabase.GUIDToAssetPath)
                .FirstOrDefault(p => p.EndsWith("/Byakhee.prefab"));

            if (string.IsNullOrEmpty(caminho)) return "prefab do Byakhee não encontrado";

            var raiz = PrefabUtility.LoadPrefabContents(caminho);
            try
            {
                var anim = raiz.GetComponent<Animator>();
                if (anim == null) anim = raiz.AddComponent<Animator>();

                anim.runtimeAnimatorController = ctrl;
                anim.applyRootMotion = false;
                anim.updateMode = AnimatorUpdateMode.Normal;
                anim.cullingMode = AnimatorCullingMode.CullUpdateTransforms;

                PrefabUtility.SaveAsPrefabAsset(raiz, caminho);
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(raiz);
            }

            return $"Animator ligado em {caminho}";
        }
    }
}
