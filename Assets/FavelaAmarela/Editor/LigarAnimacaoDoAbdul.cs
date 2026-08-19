using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace FavelaAmarela.EditorTools
{
    /// <summary>
    /// Troca a arte do Abdul Alhazred pelo <b>Mage</b> do Horror Enemy Pack (AshDeal) e liga a
    /// animação dele.
    ///
    /// <para><b>Por que trocar a folha antiga:</b> <c>abdul_alhazred_spritesheet.png</c> é
    /// <b>totalmente opaca</b> — o xadrez de transparência ficou achatado dentro do PNG, e em
    /// jogo ele renderizava como um quadrado de 4×4 unidades com fundo claro. Era arte gerada
    /// por IA, exportada já achatada. Ver
    /// <c>Docs/KnowledgeBundle/systems/arte_e_animacao.md</c>.</para>
    ///
    /// <para><b>Consequência assumida:</b> os 7 clipes antigos (<c>Abdul_idle.anim</c> etc.)
    /// apontam para fatias daquela folha por <c>fileID</c> e ficam <b>órfãos</b>. Não foram
    /// apagados — são arte do Vini e a decisão de descartar é dele —, mas deixam de ser
    /// referenciados por qualquer coisa.</para>
    ///
    /// <para><b>Licença:</b> Horror Pixel Art Enemy Pack, por AshDeal (ashdeal.itch.io) — uso
    /// pessoal <b>e comercial</b> e modificação permitidos; proibido revender ou redistribuir.
    /// Os termos completos estão copiados em <c>LICENCA_AshDeal.txt</c>, ao lado da arte:
    /// guardar o texto junto do asset é mais seguro para um edital do que um link.</para>
    /// </summary>
    public static class LigarAnimacaoDoAbdul
    {
        private const string Pasta = "Assets/FavelaAmarela/Art/Enemies/Abdul";
        private const string Folha = Pasta + "/Abdul_Mage_Sheet.png";
        private const string Controlador = Pasta + "/Abdul_AC_Mage.controller";
        private const string Prefab = "Assets/FavelaAmarela/Art/Enemies/Abdul_Alhazred.prefab";
        private const string Prefixo = "abdul";

        /// <summary>
        /// 10 quadros por segundo. O <c>attack</c> tem 10 quadros, então a conjuração dura
        /// exatamente 1 segundo — tempo de leitura suficiente para o jogador reagir ao aviso,
        /// que é o que a luta pede.
        /// </summary>
        private const float Fps = 10f;

        /// <summary>
        /// Ordem das faixas na folha, <b>conferida na arte</b> e não só no ReadMe do pacote:
        /// os quadros 0–3 mostram a figura parada, 8–17 os braços erguidos com ondas
        /// concêntricas, 20–28 o corpo se desfazendo. Bate com o que o ReadMe declara
        /// (4 + 4 + 10 + 2 + 9 = 29 = 3248 ÷ 112).
        /// </summary>
        private static readonly MontadorDeAnimacao.Faixa[] Faixas =
        {
            new MontadorDeAnimacao.Faixa("idle",   0,  4, true),
            // Sem consumidor: o Abdul não anda nem teleporta em lugar nenhum do AbdulFSM.
            // Fatiado mesmo assim, para a folha ficar íntegra se alguém der movimento a ele.
            new MontadorDeAnimacao.Faixa("walk",   4,  4, true),
            // Em loop porque a FSM pode ficar conjurando além da duração do clipe; sem loop
            // ele congelaria de braços erguidos.
            new MontadorDeAnimacao.Faixa("attack", 8, 10, true),
            new MontadorDeAnimacao.Faixa("hit",   18,  2, false),
            // Não repete de propósito: segura o último quadro, o corpo fica caído.
            new MontadorDeAnimacao.Faixa("death", 20,  9, false),
        };

        private const int LarguraDoQuadro = 112;
        private const int AlturaDoQuadro = 48;

        [MenuItem("Tools/FavelaAmarela/Ligar animacao do Abdul (Mage)")]
        public static void Executar()
        {
            if (!MontadorDeAnimacao.FatiarFolha(Folha, Prefixo, LarguraDoQuadro, AlturaDoQuadro, Faixas))
                return;

            var grupos = MontadorDeAnimacao.AgruparPorNome(Folha, Prefixo);
            if (grupos == null) return;

            var loopPorNome = Faixas.ToDictionary(f => f.Nome, f => f.Loop);
            var clipes = new Dictionary<string, AnimationClip>();
            var resumo = new List<string>();

            foreach (var par in grupos.OrderBy(p => p.Key))
            {
                bool loop = loopPorNome.TryGetValue(par.Key, out bool l) && l;
                clipes[par.Key] = MontadorDeAnimacao.MontarClipe(
                    Pasta, $"Abdul_{par.Key}_Mage", par.Value, loop, Fps);
                resumo.Add($"{par.Key}: {par.Value.Count} quadro(s), {(loop ? "loop" : "1x")}");
            }

            var ctrl = MontadorDeAnimacao.MontarControlador(
                Controlador, clipes, "idle", new[] { "hit" });

            var primeiro = grupos.TryGetValue("idle", out var idle) && idle.Count > 0
                ? idle[0] : null;

            resumo.Add(MontadorDeAnimacao.PorAnimatorNoPrefab(Prefab, ctrl, primeiro));

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            Debug.Log("[AnimacaoAbdul] Concluído:\n  " + string.Join("\n  ", resumo));
        }
    }
}
