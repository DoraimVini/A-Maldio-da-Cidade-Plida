using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace FavelaAmarela.EditorTools
{
    /// <summary>
    /// Troca a arte do Rei em Amarelo pelo <b>Moonstone Keeper</b> (SUCART) e liga a animação.
    ///
    /// <para><b>O que havia antes:</b> um recorte do spritesheet "Necromancer" da Inbox —
    /// arquétipo certo, cores erradas, e sem animação nenhuma. O Moonstone Keeper é uma figura
    /// alta encapuzada com sigilo brilhante, quase monocromática, com 0,56 × 2,19 unidades de
    /// corpo — <b>três vezes a altura do Damião na mesma largura</b>. A silhueta imponente é o
    /// que o Rei pede.</para>
    ///
    /// <para><b>Uma folha por animação, não uma só.</b> Os quadros vêm soltos (200×150 com muito
    /// vazio), então são recortados no <b>mesmo bbox global</b> e empacotados em cinco folhas de
    /// uma linha. O bbox tem de ser global: recortar cada animação no próprio contorno faria o
    /// personagem <b>pular de posição</b> ao trocar de clipe.</para>
    ///
    /// <para><b>Cuidado com o teto de import:</b> <c>idle</c> (2805px) e <c>queda</c> (3135px)
    /// passam do padrão de 2048 da Unity, que reescalaria em silêncio e borraria a arte. Daí o
    /// <c>maxTextureSize</c> de 4096.</para>
    ///
    /// <para><b>Licença:</b> o pacote da SUCART <b>não traz arquivo de termos</b> — só PNG e
    /// GIF. Está em uso sob a autorização geral do Vini para os assets baixados, mas os termos
    /// precisam ser capturados da página do autor antes da submissão. Ver
    /// <c>LICENCA_PENDENTE.txt</c> ao lado da arte.</para>
    /// </summary>
    public static class LigarAnimacaoDoReiEmAmarelo
    {
        private const string Pasta = "Assets/FavelaAmarela/Art/Enemies/ReiEmAmarelo";
        private const string Controlador = Pasta + "/ReiEmAmarelo_AC.controller";
        private const string Prefixo = "rei";

        /// <summary>
        /// 10 quadros por segundo. O <c>idle</c> tem 17 quadros: a 10 fps o ciclo dura 1,7 s —
        /// respiração lenta, que é como uma Aparição Primordial deve se mover.
        /// </summary>
        private const float Fps = 10f;

        /// <summary>Altura do quadro. Global: o recorte vertical é o mesmo em toda animação.</summary>
        private const int AlturaDoQuadro = 129;

        /// <summary>
        /// Nome da animação → (quadros, largura do quadro, repete).
        ///
        /// <para><b>A largura varia por animação, a altura não.</b> Com a largura global de
        /// 165px — ditada pelo efeito dos ataques — <c>idle</c> daria uma folha de 2805px e
        /// <c>queda</c> 3135px, acima do teto de import de 2048 da Unity, que <b>reescala em
        /// silêncio</b> e borra pixel art. Cada folha é recortada na própria largura, mas
        /// <b>simétrica em torno do mesmo eixo</b>: com pivô BottomCenter o personagem cai
        /// sempre no mesmo ponto e não pula ao trocar de clipe.</para>
        ///
        /// <para><c>queda</c> tem 17 dos 19 quadros originais: mesmo recortada ela daria
        /// 2242px. Perder 2 quadros de uma queda a 10 fps é imperceptível; borrar a arte
        /// inteira não é.</para>
        ///
        /// <para><c>selar</c> e <c>desvelo</c> repetem porque a FSM permanece nesses estados
        /// enquanto o ciclo do ritual corre; sem loop o Rei congelaria no último quadro no meio
        /// do ato. <c>queda</c> não repete de propósito — segura o último quadro.</para>
        /// </summary>
        private static readonly (string nome, int quadros, int largura, bool loop)[] Animacoes =
        {
            ("idle",    17,  88, true),
            ("selar",   11, 166, true),
            ("desvelo", 10, 166, true),
            ("dano",     3,  92, false),
            ("queda",   17, 118, false),
        };

        [MenuItem("Tools/FavelaAmarela/Ligar animacao do Rei em Amarelo")]
        public static void Executar()
        {
            var clipes = new Dictionary<string, AnimationClip>();
            var resumo = new List<string>();
            Sprite primeiro = null;

            foreach (var (nome, quadros, largura, loop) in Animacoes)
            {
                string folha = $"{Pasta}/Rei_{nome}.png";

                var faixa = new[] { new MontadorDeAnimacao.Faixa(nome, 0, quadros, loop) };

                if (!MontadorDeAnimacao.FatiarFolha(folha, Prefixo, largura, AlturaDoQuadro, faixa))
                    continue;

                var grupos = MontadorDeAnimacao.AgruparPorNome(folha, Prefixo);
                if (grupos == null || !grupos.TryGetValue(nome, out var sprites)) continue;

                clipes[nome] = MontadorDeAnimacao.MontarClipe(
                    Pasta, $"Rei_{nome}", sprites, loop, Fps);

                if (nome == "idle" && sprites.Count > 0) primeiro = sprites[0];

                resumo.Add($"{nome}: {sprites.Count} quadro(s), {(loop ? "loop" : "1x")}");
            }

            if (clipes.Count == 0)
            {
                Debug.LogError("[AnimacaoRei] Nenhum clipe montado — as folhas Rei_*.png estão " +
                               "em " + Pasta + "?");
                return;
            }

            var ctrl = MontadorDeAnimacao.MontarControlador(
                Controlador, clipes, "idle", new[] { "dano" });

            string prefab = AssetDatabase.FindAssets("ReiEmAmarelo t:Prefab")
                .Select(AssetDatabase.GUIDToAssetPath)
                .FirstOrDefault(p => p.EndsWith("/ReiEmAmarelo.prefab"));

            resumo.Add(string.IsNullOrEmpty(prefab)
                ? "prefab do Rei em Amarelo não encontrado"
                : MontadorDeAnimacao.PorAnimatorNoPrefab(prefab, ctrl, primeiro));

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            Debug.Log("[AnimacaoRei] Concluído:\n  " + string.Join("\n  ", resumo));
        }
    }
}
