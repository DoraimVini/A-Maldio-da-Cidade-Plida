using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace FavelaAmarela.EditorTools
{
    /// <summary>
    /// Monta os clipes de animação do Byakhee a partir das 26 fatias já nomeadas na folha,
    /// cria o <c>Byakhee_AC</c> e põe o <c>Animator</c> no prefab.
    ///
    /// <para><b>Diferença para o Abdul:</b> a folha do Byakhee <b>já vinha fatiada e nomeada</b>
    /// por quem a importou (<c>byakhee_espreita_0</c>…, em terminologia diegética), então aqui
    /// não há passo de fatiamento — só o agrupamento por nome. O miolo compartilhado está em
    /// <see cref="MontadorDeAnimacao"/>.</para>
    ///
    /// <para>Idempotente: reescreve os mesmos assets nos mesmos caminhos.</para>
    /// </summary>
    public static class LigarAnimacaoDoByakhee
    {
        private const string Folha = "Assets/FavelaAmarela/Art/Enemies/Byakhee_Spritesheet.png";
        private const string Pasta = "Assets/FavelaAmarela/Art/Enemies/Byakhee";
        private const string Controlador = Pasta + "/Byakhee_AC.controller";
        private const string Prefab = "Assets/FavelaAmarela/Art/Enemies/Byakhee.prefab";
        private const string Prefixo = "byakhee";

        /// <summary>12 quadros por segundo: a folha tem 4 a 6 quadros por animação, e a 12 fps
        /// um ciclo de 6 dura meio segundo — batida de asa plausível sem parecer acelerado.</summary>
        private const float Fps = 12f;

        /// <summary>
        /// Quais animações repetem. <c>garras</c> e <c>grito</c> repetem porque a FSM pode
        /// permanecer no estado além da duração do clipe — sem loop, o boss congelaria no
        /// último quadro no meio da investida. <c>derrota</c> não repete de propósito: segura o
        /// último quadro, o corpo fica caído.
        /// </summary>
        private static readonly Dictionary<string, bool> EmLoop = new Dictionary<string, bool>
        {
            { "espreita", true },
            { "rasante",  true },
            { "garras",   true },
            { "grito",    true },
            { "dano",     false },
            { "derrota",  false },
        };

        [MenuItem("Tools/FavelaAmarela/Ligar animacao do Byakhee")]
        public static void Executar()
        {
            // Corrige o pivô ANTES de agrupar. A folha veio fatiada com pivô Center, mas o
            // BoxCollider2D do prefab (size 2.625×2.633, offset 0,2.19) só faz sentido com pivô
            // no rodapé: com Center ele ficava 1,3 unidade ACIMA da arte. E o offsetPes = 0 do
            // DynamicYSort só está certo se o pivô estiver nos pés.
            MontadorDeAnimacao.CorrigirPivoDasFatias(Folha, SpriteAlignment.BottomCenter);

            var grupos = MontadorDeAnimacao.AgruparPorNome(Folha, Prefixo);
            if (grupos == null) return;

            if (!AssetDatabase.IsValidFolder(Pasta))
                AssetDatabase.CreateFolder("Assets/FavelaAmarela/Art/Enemies", "Byakhee");

            var clipes = new Dictionary<string, AnimationClip>();
            var resumo = new List<string>();

            foreach (var par in grupos.OrderBy(p => p.Key))
            {
                bool loop = EmLoop.TryGetValue(par.Key, out bool l) && l;
                clipes[par.Key] = MontadorDeAnimacao.MontarClipe(
                    Pasta, $"Byakhee_{par.Key}", par.Value, loop, Fps);
                resumo.Add($"{par.Key}: {par.Value.Count} quadro(s), {(loop ? "loop" : "1x")}");
            }

            var ctrl = MontadorDeAnimacao.MontarControlador(
                Controlador, clipes, "espreita", new[] { "dano" });

            // Sprite inicial deixado como está: a folha do Byakhee já tinha um quadro válido
            // atribuído no prefab, e trocá-lo não acrescenta nada.
            resumo.Add(MontadorDeAnimacao.PorAnimatorNoPrefab(Prefab, ctrl, null));

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            Debug.Log("[AnimacaoByakhee] Concluído:\n  " + string.Join("\n  ", resumo));
        }
    }
}
