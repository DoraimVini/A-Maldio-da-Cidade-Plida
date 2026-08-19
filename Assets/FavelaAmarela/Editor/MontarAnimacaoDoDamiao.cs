using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using FavelaAmarela.Player;

namespace FavelaAmarela.EditorTools
{
    /// <summary>
    /// Ferramenta de Editor. Fatia as 9 tiras de Damião (preparadas fora da Unity a partir do
    /// pacote "4 directional character", recolorido de vermelho para o ouro de Carcosa) e
    /// preenche o <see cref="AnimadorDoDamiao"/> no <c>Player_Damiao.prefab</c>.
    ///
    /// <para><b>Dimensões medidas, não estimadas.</b> As 9 tiras foram recortadas no mesmo bbox
    /// vertical global (84px, de <c>y=0</c> a <c>y=83</c> na arte fonte) para o personagem não
    /// pular de posição ao trocar de ciclo — mas cada uma tem sua própria largura de quadro,
    /// porque poses diferentes (correndo, golpeando) ocupam larguras diferentes. Refletido na
    /// tabela abaixo.</para>
    ///
    /// <para><b>Escala recalculada para preservar o colisor.</b> A escala antiga (0.5) foi
    /// calibrada para <c>Damiao_Robe_Idle.png</c> (32×48 px). A nova arte tem quadros de 84px de
    /// altura — sem recalcular, o personagem dobraria de tamanho. A escala nova é escolhida para
    /// a altura visual continuar ~0,75 unidade (a mesma de antes), e o <c>BoxCollider2D</c> tem o
    /// tamanho local recalculado para o volume de mundo (0,5 × 0,5, como já era) não mudar —
    /// mesma regra aplicada aos 5 prefabs de placeholder mais cedo nesta sessão.</para>
    /// </summary>
    public static class MontarAnimacaoDoDamiao
    {
        private const string Pasta = "Assets/FavelaAmarela/Art/Characters/Damiao/Animado";
        private const string Prefab = "Assets/FavelaAmarela/Art/Characters/Damiao/Player_Damiao.prefab";
        private const string Prefixo = "damiao";

        /// <summary>Altura comum a todas as 9 tiras — o bbox vertical global usado no recorte.</summary>
        private const int AlturaDoQuadro = 84;

        /// <summary>Altura visual alvo, em unidades, igual à do sprite antigo (32×48 @ escala 0.5).</summary>
        private const float AlturaVisualAlvo = 0.75f;

        private static readonly float EscalaNova = AlturaVisualAlvo / (AlturaDoQuadro / 32f);

        private sealed class Tira
        {
            public string Nome;      // sufixo do arquivo e da fatia: damiao_<Nome>_<n>
            public string Campo;     // campo do AnimadorDoDamiao a preencher
            public int Quadros;
            public int Largura;
            public bool Loop;
        }

        private static readonly Tira[] Tiras =
        {
            new Tira { Nome = "idle",        Campo = "idle",           Quadros = 4, Largura = 34, Loop = true  },
            new Tira { Nome = "run_down",     Campo = "correrBaixo",    Quadros = 5, Largura = 36, Loop = true  },
            new Tira { Nome = "run_up",       Campo = "correrCima",     Quadros = 5, Largura = 36, Loop = true  },
            new Tira { Nome = "run_left",     Campo = "correrEsquerda", Quadros = 6, Largura = 50, Loop = true  },
            new Tira { Nome = "run_right",    Campo = "correrDireita",  Quadros = 6, Largura = 50, Loop = true  },
            new Tira { Nome = "slice_down",   Campo = "golpeBaixo",     Quadros = 3, Largura = 74, Loop = false },
            new Tira { Nome = "slice_up",     Campo = "golpeCima",      Quadros = 3, Largura = 74, Loop = false },
            new Tira { Nome = "slice_left",   Campo = "golpeEsquerda",  Quadros = 3, Largura = 78, Loop = false },
            new Tira { Nome = "slice_right",  Campo = "golpeDireita",   Quadros = 3, Largura = 78, Loop = false },
        };

        [MenuItem("Tools/FavelaAmarela/Montar Animação do Damião")]
        public static void Executar()
        {
            var camposPreenchidos = new Dictionary<string, List<Sprite>>();
            var resumo = new List<string>();

            foreach (var t in Tiras)
            {
                string caminho = $"{Pasta}/Damiao_{t.Nome}.png";

                var faixa = new[] { new MontadorDeAnimacao.Faixa(t.Nome, 0, t.Quadros, t.Loop) };
                if (!MontadorDeAnimacao.FatiarFolha(caminho, Prefixo, t.Largura, AlturaDoQuadro, faixa))
                {
                    resumo.Add($"{t.Nome}: falhou ao fatiar ({caminho})");
                    continue;
                }

                var grupos = MontadorDeAnimacao.AgruparPorNome(caminho, Prefixo);
                if (grupos == null || !grupos.TryGetValue(t.Nome, out var sprites))
                {
                    resumo.Add($"{t.Nome}: sem sprites agrupados");
                    continue;
                }

                camposPreenchidos[t.Campo] = sprites;
                resumo.Add($"{t.Campo}: {sprites.Count} quadro(s) de {t.Largura}×{AlturaDoQuadro}");
            }

            var raiz = PrefabUtility.LoadPrefabContents(Prefab);
            try
            {
                var animador = raiz.GetComponent<AnimadorDoDamiao>()
                               ?? raiz.AddComponent<AnimadorDoDamiao>();

                var so = new SerializedObject(animador);
                foreach (var par in camposPreenchidos)
                {
                    var prop = so.FindProperty(par.Key);
                    if (prop == null)
                    {
                        Debug.LogWarning($"[AnimacaoDamiao] Campo '{par.Key}' não existe em " +
                                         "AnimadorDoDamiao.");
                        continue;
                    }

                    prop.arraySize = par.Value.Count;
                    for (int i = 0; i < par.Value.Count; i++)
                        prop.GetArrayElementAtIndex(i).objectReferenceValue = par.Value[i];
                }
                so.ApplyModifiedPropertiesWithoutUndo();

                AjustarEscalaPreservandoOColisor(raiz);

                var sr = raiz.GetComponent<SpriteRenderer>();
                if (sr != null && camposPreenchidos.TryGetValue("idle", out var idle) && idle.Count > 0)
                    sr.sprite = idle[0];

                PrefabUtility.SaveAsPrefabAsset(raiz, Prefab);
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(raiz);
            }

            resumo.Add(CorrigirInstanciasEmCena());

            Debug.Log("[AnimacaoDamiao] Concluído:\n  " + string.Join("\n  ", resumo) +
                      $"\n  escala: {EscalaNova:0.###}");
        }

        /// <summary>
        /// <c>Deserto_Hali</c> e <c>Playtest_RuinasPalidas</c> sobrescrevem a escala do
        /// Player_Damiao com valores <b>não-uniformes</b> e específicos de cada cena
        /// (<c>0.677×0.749</c> e <c>0.841×0.949</c> — nenhum dos dois é o <c>0.5</c> do prefab).
        /// Calibrados para o sprite antigo (32×48), aplicados à arte nova eles esticariam o
        /// personagem de forma desigual nos dois eixos.
        ///
        /// <para>Não achei evidência de que os valores fossem intencionais (não são números
        /// redondos, não coincidem entre as duas cenas) — mesmo padrão do override do Yug-Neth
        /// corrigido mais cedo nesta sessão. Corrigido para a escala uniforme nova, com o valor
        /// exato registrado aqui para reverter fácil se alguém confirmar que era proposital.</para>
        /// </summary>
        private static string CorrigirInstanciasEmCena()
        {
            var cenas = new[] { "Assets/Scenes/Deserto_Hali.unity",
                                 "Assets/Scenes/Playtest_RuinasPalidas.unity" };
            var linhas = new List<string>();

            foreach (var caminho in cenas)
            {
                if (!System.IO.File.Exists(caminho)) { linhas.Add($"{caminho}: ausente"); continue; }

                var cena = EditorSceneManager.OpenScene(caminho, OpenSceneMode.Single);
                int ajustados = 0;

                foreach (var t in Object.FindObjectsByType<Transform>(FindObjectsInactive.Include))
                {
                    if (t.name != "Player_Damiao") continue;

                    Vector3 antes = t.localScale;
                    Undo.RecordObject(t, "Ajustar escala do Damião");
                    t.localScale = new Vector3(EscalaNova, EscalaNova, 1f);
                    EditorUtility.SetDirty(t);
                    ajustados++;
                    linhas.Add($"{System.IO.Path.GetFileNameWithoutExtension(caminho)}: " +
                               $"escala ({antes.x:0.###},{antes.y:0.###}) → {EscalaNova:0.###}");
                }

                if (ajustados > 0)
                {
                    EditorSceneManager.MarkSceneDirty(cena);
                    EditorSceneManager.SaveScene(cena);
                }
            }

            return string.Join(" | ", linhas);
        }

        /// <summary>
        /// Troca a escala da raiz para <see cref="EscalaNova"/> e recalcula o tamanho local do
        /// <c>BoxCollider2D</c> para o volume de mundo continuar o mesmo — mesma regra aplicada
        /// aos 5 prefabs de placeholder (Pedra de Poder, Esqueleto Invocado etc.) mais cedo
        /// nesta sessão: trocar arte não é hora de reequilibrar hitbox.
        /// </summary>
        private static void AjustarEscalaPreservandoOColisor(GameObject raiz)
        {
            var box = raiz.GetComponent<BoxCollider2D>();
            Vector2 volumeDeMundo = Vector2.zero;
            bool temColisor = box != null;

            if (temColisor)
            {
                Vector3 escalaAntiga = raiz.transform.localScale;
                volumeDeMundo = new Vector2(box.size.x * escalaAntiga.x, box.size.y * escalaAntiga.y);
            }

            raiz.transform.localScale = new Vector3(EscalaNova, EscalaNova, 1f);

            if (temColisor)
                box.size = new Vector2(volumeDeMundo.x / EscalaNova, volumeDeMundo.y / EscalaNova);
        }
    }
}
