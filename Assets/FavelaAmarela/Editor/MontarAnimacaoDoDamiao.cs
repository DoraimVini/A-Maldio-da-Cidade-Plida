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

        /// <summary>
        /// Altura comum às 9 tiras. <b>88, não 84</b> — corrigido em 2026-08-21.
        ///
        /// <para>Era 84 até o contorno e a elipse de sombra expandirem cada quadro em 2 px de
        /// cada lado. A constante ficou para trás, e rodar esta ferramenta <b>fatiaria as
        /// folhas erradas</b>, cortando 4 px de cada quadro.</para>
        /// </summary>
        private const int AlturaDoQuadro = 88;

        /// <summary>Margem em px sob os pés, ocupada pela elipse de sombra.</summary>
        private const float MargemDaSombra = 2f;

        /// <summary>
        /// Altura alvo do quadro, em unidades de mundo. <b>2,20, não 0,75</b> — corrigido em
        /// 2026-08-21.
        ///
        /// <para><b>Esta ferramenta era uma mina.</b> O 0,75 antigo era do sprite de 32×48, e
        /// <c>EscalaDoDamiao</c> levou o Damião a 2,20 a pedido do Vini (ele era o menor do
        /// elenco). Como as duas ferramentas escrevem o mesmo <c>localScale</c> e ninguém as
        /// reconciliou, rodar esta <b>desfaria</b> a escala — devolvendo o Damião a 0,2857 sem
        /// erro nenhum. Somado à altura de quadro errada e ao pivô zerado, eram três regressões
        /// silenciosas numa execução só.</para>
        ///
        /// <para>O valor tem que continuar igual ao de <c>EscalaDoDamiao.AlturaAlvo</c>.
        /// <c>AnimacaoDoDamiaoTests</c> guarda o resultado.</para>
        /// </summary>
        private const float AlturaVisualAlvo = 2.2f;

        private static readonly float EscalaNova = AlturaVisualAlvo / (AlturaDoQuadro / 32f);

        private sealed class Tira
        {
            public string Nome;      // sufixo do arquivo e da fatia: damiao_<Nome>_<n>
            public string Campo;     // campo do AnimadorDoDamiao a preencher
            public int Quadros;
            public bool Loop;
        }

        private static readonly Tira[] Tiras =
        {
            new Tira { Nome = "idle",        Campo = "idle",           Quadros = 4, Loop = true  },
            new Tira { Nome = "run_down",     Campo = "correrBaixo",    Quadros = 5, Loop = true  },
            new Tira { Nome = "run_up",       Campo = "correrCima",     Quadros = 5, Loop = true  },
            new Tira { Nome = "run_left",     Campo = "correrEsquerda", Quadros = 6, Loop = true  },
            new Tira { Nome = "run_right",    Campo = "correrDireita",  Quadros = 6, Loop = true  },
            new Tira { Nome = "slice_down",   Campo = "golpeBaixo",     Quadros = 3, Loop = false },
            new Tira { Nome = "slice_up",     Campo = "golpeCima",      Quadros = 3, Loop = false },
            new Tira { Nome = "slice_left",   Campo = "golpeEsquerda",  Quadros = 3, Loop = false },
            new Tira { Nome = "slice_right",  Campo = "golpeDireita",   Quadros = 3, Loop = false },
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
                // Pivô na linha do chão: a elipse de sombra ocupa MargemDaSombra px abaixo dos
                // pés, então a base do quadro não é onde ele pisa. Vai junto da fatiagem —
                // corrigir depois falhava calado (ver FatiarFolha).
                var pivo = new Vector2(0.5f, MargemDaSombra / (float)AlturaDoQuadro);

                // Medidas DERIVADAS da textura, nunca escritas a mao. Ate 2026-08-22 a
                // largura de cada tira era uma constante nesta classe, e ficou 4 px curta
                // quando o contorno expandiu os quadros em 2 px de cada lado. Rodar a
                // ferramenta com a constante velha refatiava TODOS os quadros desalinhados --
                // o sprite do Damiao aparecia cortado, e nada no console acusava.
                //
                // Largura do quadro = largura da folha / numero de quadros. Se a arte mudar de
                // tamanho, a conta acompanha sozinha.
                var textura = AssetDatabase.LoadAssetAtPath<Texture2D>(caminho);
                if (textura == null)
                {
                    resumo.Add($"{t.Nome}: textura nao carregou ({caminho})");
                    continue;
                }

                if (textura.width % t.Quadros != 0)
                {
                    resumo.Add($"{t.Nome}: largura {textura.width} nao divide em {t.Quadros} " +
                               "quadros inteiros -- a folha nao bate com a contagem de quadros");
                    continue;
                }

                int larguraDoQuadro = textura.width / t.Quadros;

                if (!MontadorDeAnimacao.FatiarFolha(caminho, Prefixo, larguraDoQuadro, textura.height,
                                                    faixa, pivo: pivo))
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
                resumo.Add($"{t.Campo}: {sprites.Count} quadro(s) de {larguraDoQuadro}×{textura.height}");
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
        // AjustarEscalaPreservandoOColisor foi REMOVIDO em 2026-08-21.
        //
        // Ele escrevia localScale e o tamanho do BoxCollider2D — os mesmos campos que
        // EscalaDoDamiao e RevisarColisores escrevem, com fórmulas próprias e desatualizadas.
        // Rodar esta ferramenta devolvia o Damião a 0,2857 (o tamanho de antes do Vini pedir
        // que ele crescesse), sem erro nenhum. Dois donos para o mesmo campo é como um deles
        // desfaz o outro em silêncio.
        //
        // Agora: escala é de EscalaDoDamiao, colisor é de RevisarColisores, e esta ferramenta
        // cuida só de fatiar e ligar a animação.

    }
}
