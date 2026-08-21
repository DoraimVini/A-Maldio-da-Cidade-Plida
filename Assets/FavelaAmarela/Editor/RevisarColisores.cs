using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEngine;

namespace FavelaAmarela.EditorTools
{
    /// <summary>
    /// Normaliza as pegadas de colisão do elenco e dá um colisor ao Byakhee.
    ///
    /// <para><b>O achado que motivou tudo (2026-08-20): o Byakhee não tinha colisor
    /// nenhum.</b> Só um <c>Rigidbody2D</c>. E <c>MaoFisicaBridge.ResolverGolpe</c> resolve o
    /// golpe por <c>Physics2D.OverlapCircle</c> — sem colisor, <b>o chefe era impossível de
    /// acertar</b>. É a causa do "aparentemente o Damião não causou dano na Byakhee" relatado
    /// no playtest, e metade do "combate sem feel": você batia no vazio.</para>
    ///
    /// <para><b>As pegadas nunca foram calibradas entre si.</b> Medidas em unidades de mundo
    /// (<c>size × localScale</c>) antes desta rodada: Damião <b>1,467</b>, Abdul 1,200, Rei
    /// 1,000, Yug-Neth 0,600, Cultista 0,576, Espectro 0,416. O Damião tinha <b>2,5× a pegada
    /// do Cultista</b> — dois humanos do mesmo rig — e o colisor dele era <b>mais largo que a
    /// própria figura</b> (0,84) e 4,7× a largura dos pés (0,31). Por isso ele entalava.</para>
    ///
    /// <para><b>Por que encolher é seguro para o combate:</b> os inimigos aplicam dano por
    /// <c>Vector2.Distance</c> + <c>IDanificavel</c>, <b>não</b> por sobreposição de colisor
    /// (conferido em <c>ByakheeAI</c>, <c>AbdulAlhazredAI</c>, <c>CoisaDoCemiterioAI</c>).
    /// A pegada do jogador só governa o que barra movimento. Mudá-la não altera o quanto ele
    /// apanha.</para>
    ///
    /// <para><b>Sobre cápsula, que o Vini perguntou:</b> cápsula <i>em pé</i> é forma de jogo
    /// de plataforma lateral, onde o eixo alto é altura real. No isométrico o Y da tela é
    /// <b>profundidade</b>, então a pegada é uma área no chão e o que importa é ela ser
    /// <b>achatada na proporção 2:1 da grade</b> — que é o que esta ferramenta aplica. Trocar
    /// <c>BoxCollider2D</c> por <c>CapsuleCollider2D</c> ganharia só os cantos arredondados
    /// (menos enrosco em quina); ficou de fora <b>de propósito</b> na véspera da build, porque
    /// troca de tipo de componente muda fileID e pode quebrar referência serializada. O ganho
    /// é pequeno perto do risco. O Byakhee leva cápsula porque o colisor dele é novo — não há
    /// o que quebrar.</para>
    ///
    /// <para><b>Hitbox/hurtbox continuam sem existir</b>, e as camadas para isso já estão
    /// declaradas no projeto sem uso nenhum: <c>PlayerHitbox</c> (11), <c>EnemyHitbox</c> (12),
    /// <c>PlayerHurtbox</c> (13), <c>EnemyHurtbox</c> (14) — zero prefabs, zero cenas, zero
    /// código. Separar as três camadas é a resposta certa para o combate a médio prazo, e é
    /// refatoração de pipeline de dano; não cabe na véspera.</para>
    /// </summary>
    public static class RevisarColisores
    {
        /// <summary>
        /// Pegada humana em unidades de mundo. <b>0,60 × 0,30</b> fica entre a largura dos pés
        /// (0,31, estreita demais para barrar parede de forma confiável) e a da figura inteira
        /// (0,84, que é o corpo, não o chão que ele ocupa). O 2:1 acompanha o losango da grade
        /// isométrica.
        /// </summary>
        private static readonly Vector2 PegadaHumana = new Vector2(0.60f, 0.30f);

        /// <summary>
        /// Corpo do Byakhee em unidades de mundo. Generoso de propósito: o golpe é resolvido por
        /// <c>OverlapCircle</c>, e um chefe voador difícil de encostar lê como um chefe que não
        /// responde. O quadro dele tem 5,12 un e as asas ocupam quase tudo.
        /// </summary>
        private static readonly Vector2 CorpoDoByakhee = new Vector2(2.0f, 3.0f);

        private const string Pasta = "Assets/FavelaAmarela/Art";

        private static readonly string[] Humanos =
        {
            Pasta + "/Characters/Damiao/Player_Damiao.prefab",
            Pasta + "/Enemies/Cultista.prefab",
            Pasta + "/Enemies/Abdul_Alhazred.prefab",
            Pasta + "/Enemies/EspectroHali.prefab",
        };

        private const string PrefabByakhee = Pasta + "/Enemies/Byakhee.prefab";

        [MenuItem("Tools/FavelaAmarela/Colisores: revisar as pegadas")]
        public static void Executar()
        {
            var resumo = new List<string>();

            foreach (var caminho in Humanos)
                resumo.Add(AjustarPegadaHumana(caminho));

            resumo.Add(GarantirColisorDoByakhee());

            Debug.Log("[RevisarColisores] Concluído:\n  " + string.Join("\n  ", resumo));
        }

        /// <summary>
        /// Põe a pegada em <see cref="PegadaHumana"/> <b>em unidades de mundo</b>, dividindo
        /// pela escala do prefab. Sem essa divisão, prefabs com escalas diferentes (0,5 do
        /// Yug-Neth contra 1,3 do Espectro) acabariam com pegadas diferentes em jogo, que é
        /// exatamente a bagunça que esta ferramenta desfaz.
        /// </summary>
        private static string AjustarPegadaHumana(string caminho)
        {
            if (!File.Exists(caminho)) return $"{Path.GetFileName(caminho)}: prefab ausente";

            var raiz = PrefabUtility.LoadPrefabContents(caminho);
            if (raiz == null) return $"{Path.GetFileName(caminho)}: não carregou";

            string linha;

            try
            {
                var box = raiz.GetComponent<BoxCollider2D>();
                if (box == null)
                {
                    return $"{Path.GetFileName(caminho)}: sem BoxCollider2D — não mexi " +
                           "(pode ser intencional; confira à mão)";
                }

                float escala = raiz.transform.localScale.x;
                if (Mathf.Approximately(escala, 0f)) escala = 1f;

                Vector2 antes = new Vector2(box.size.x * escala, box.size.y * escala);

                box.size = new Vector2(PegadaHumana.x / escala, PegadaHumana.y / escala);

                PrefabUtility.SaveAsPrefabAsset(raiz, caminho, out bool salvou);
                if (!salvou) return $"{Path.GetFileName(caminho)}: SaveAsPrefabAsset recusou";

                linha = $"{Path.GetFileName(caminho)}: pegada {antes.x:0.000}×{antes.y:0.000} → " +
                        $"{PegadaHumana.x:0.00}×{PegadaHumana.y:0.00} (escala {escala:0.000})";
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(raiz);
            }

            return linha;
        }

        /// <summary>
        /// Dá ao Byakhee o colisor que ele nunca teve. <b>Trigger</b>, não sólido: o filtro do
        /// golpe já usa <c>useTriggers = true</c>, então trigger basta para ser acertado — e um
        /// chefe voador sólido empurraria o jogador e enroscaria nas paredes da arena.
        /// </summary>
        private static string GarantirColisorDoByakhee()
        {
            if (!File.Exists(PrefabByakhee)) return "Byakhee.prefab: ausente";

            var raiz = PrefabUtility.LoadPrefabContents(PrefabByakhee);
            if (raiz == null) return "Byakhee.prefab: não carregou";

            bool jaTinha;

            try
            {
                jaTinha = raiz.GetComponent<Collider2D>() != null;

                var capsula = raiz.GetComponent<CapsuleCollider2D>();
                if (capsula == null) capsula = raiz.AddComponent<CapsuleCollider2D>();

                float escala = raiz.transform.localScale.x;
                if (Mathf.Approximately(escala, 0f)) escala = 1f;

                capsula.direction = CapsuleDirection2D.Vertical;
                capsula.size = new Vector2(CorpoDoByakhee.x / escala, CorpoDoByakhee.y / escala);
                capsula.isTrigger = true;

                PrefabUtility.SaveAsPrefabAsset(raiz, PrefabByakhee, out bool salvou);
                if (!salvou) return "Byakhee.prefab: SaveAsPrefabAsset recusou";
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(raiz);
            }

            // Confere no disco: !u!70 é o CapsuleCollider2D.
            bool noDisco = Regex.IsMatch(File.ReadAllText(PrefabByakhee), @"!u!70\b");

            return noDisco
                ? $"Byakhee.prefab: cápsula {CorpoDoByakhee.x:0.0}×{CorpoDoByakhee.y:0.0} " +
                  $"(trigger) — {(jaTinha ? "já tinha algum colisor" : "ANTES NÃO TINHA COLISOR NENHUM")}"
                : "Byakhee.prefab: a cápsula não apareceu no YAML depois de salvar";
        }
    }
}
