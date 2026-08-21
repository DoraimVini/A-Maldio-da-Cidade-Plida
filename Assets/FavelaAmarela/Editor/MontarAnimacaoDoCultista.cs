using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using FavelaAmarela.Runtime.Enemies;

namespace FavelaAmarela.EditorTools
{
    /// <summary>
    /// Fatia as folhas novas do Cultista e liga no <see cref="AnimadorDoCultista"/>.
    ///
    /// <para><b>Por que a arte é nova:</b> a folha antiga
    /// (<c>Assets/Sprites/Cultistas/Cultista_Spritesheet_16x32.png</c>) estava destruída — fundo
    /// opaco e buracos na própria figura, 15,9% de transparência — e o <c>.aseprite</c> de
    /// origem tinha o mesmo dano. Não havia o que recuperar. A substituta sai do mesmo pacote de
    /// onde veio o Damião (<i>4 directional character</i>), recolorida com o gradiente
    /// roxo→amarelo-doente, o que dá casamento exato de estilo, escala e projeção.</para>
    ///
    /// <para><b>A escala é o ponto delicado.</b> O prefab trazia <c>localScale 1.8</c>,
    /// calibrado para a arte antiga de 32 px de altura (32/32 × 1,8 = 1,80 un). A arte nova tem
    /// <b>86 px</b>: mantida a escala, o Cultista iria a <b>4,84 un</b> — mais que o dobro do
    /// Damião (2,20). Era exatamente a queixa do Vini no playtest, <i>"o cultista está
    /// visivelmente maior que o Damião"</i>, e a razão de eu ter errado antes foi ignorar o
    /// <c>localScale</c> e comparar só pixels.</para>
    ///
    /// <para><b>O colisor não pode encolher junto.</b> <c>localScale</c> na raiz escala o
    /// <c>BoxCollider2D</c>, então cair de 1,8 para 0,67 reduziria a pegada de 0,576 para 0,214
    /// de mundo — a hitbox do inimigo mudaria como efeito colateral de uma troca de arte, e o
    /// combate ficaria diferente sem ninguém ter pedido. O tamanho do colisor é recalculado para
    /// preservar o volume em mundo, como <c>MontarAnimacaoDoDamiao</c> já fazia.</para>
    /// </summary>
    public static class MontarAnimacaoDoCultista
    {
        private const string Pasta = "Assets/FavelaAmarela/Art/Enemies/Cultista";
        private const string Prefab = "Assets/FavelaAmarela/Art/Enemies/Cultista.prefab";
        private const string Prefixo = "cultista";

        private const int LarguraDoQuadro = 78;
        private const int AlturaDoQuadro = 86;

        /// <summary>
        /// Altura da <b>figura</b> do Cultista em px, medida no quadro sem contar a elipse de
        /// sombra (opaco de y=3 a y=81 no quadro de 86).
        /// </summary>
        private const float AlturaDaFiguraEmPx = 79f;

        /// <summary>
        /// Altura da figura do <b>Damião</b> em unidades de mundo: 81 px de figura a
        /// <c>localScale 0,8381</c>. É o alvo — Cultista e Damião são os dois humanos, saídos do
        /// mesmo rig, e têm que medir o mesmo.
        ///
        /// <para><b>Correção de 2026-08-20.</b> A versão anterior desta ferramenta mirava
        /// <b>1,80 un</b>, deixando o Cultista visivelmente menor. Aquele número não era decisão
        /// de design: era o que o <c>localScale 1.8</c> do prefab produzia sobre a arte antiga de
        /// 32 px, e eu o preservei achando que estava respeitando uma calibragem existente.
        /// Cheguei a escrever em <c>EscalaDoDamiao</c> uma justificativa inventada ("2,20 põe o
        /// Damião logo acima do Cultista") para racionalizar um número herdado por acidente.
        /// O Vini corrigiu: os dois são humanos, medem igual.</para>
        ///
        /// <para>Compara-se a <b>figura</b>, não o quadro: as duas folhas têm margens de sombra
        /// diferentes (88 px contra 86), então igualar altura de imagem deixaria os corpos
        /// desiguais.</para>
        /// </summary>
        private const float AlturaDaFiguraDoDamiao = 2.12f;

        /// <summary>
        /// Margem em px abaixo dos pés, ocupada pela elipse de sombra. Mesma receita do Damião —
        /// as duas folhas saíram do mesmo gerador — e é o que tira o pivô do zero.
        /// </summary>
        private const float MargemDaSombra = 2f;

        private static readonly float EscalaNova =
            AlturaDaFiguraDoDamiao / (AlturaDaFiguraEmPx / 32f);

        private struct Tira
        {
            public string Nome;
            public string Campo;
            public int Quadros;
            public bool Loop;
        }

        private static readonly Tira[] Tiras =
        {
            new Tira { Nome = "idle",   Campo = "idle",   Quadros = 4, Loop = true },
            new Tira { Nome = "walk",   Campo = "walk",   Quadros = 5, Loop = true },
            new Tira { Nome = "attack", Campo = "attack", Quadros = 3, Loop = false },
            new Tira { Nome = "death",  Campo = "death",  Quadros = 4, Loop = false },
        };

        [MenuItem("Tools/FavelaAmarela/Montar Animação do Cultista")]
        public static void Executar()
        {
            var camposPreenchidos = new Dictionary<string, List<Sprite>>();
            var resumo = new List<string>();

            foreach (var t in Tiras)
            {
                string caminho = $"{Pasta}/Cultista_{t.Nome}.png";

                var faixa = new[] { new MontadorDeAnimacao.Faixa(t.Nome, 0, t.Quadros, t.Loop) };
                // Pivô na linha do chão, não na base do quadro: o gerador desenha a elipse de
                // sombra centrada MargemDaSombra px acima da base, e é o centro dela que marca
                // onde o Cultista pisa. Vai junto da fatiagem, numa escrita só — corrigir
                // depois falhava calado (ver FatiarFolha).
                var pivo = new Vector2(0.5f, MargemDaSombra / AlturaDoQuadro);

                if (!MontadorDeAnimacao.FatiarFolha(caminho, Prefixo,
                                                    LarguraDoQuadro, AlturaDoQuadro, faixa,
                                                    pivo: pivo))
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
                resumo.Add($"{t.Campo}: {sprites.Count} quadro(s) de " +
                           $"{LarguraDoQuadro}×{AlturaDoQuadro}");
            }

            var raiz = PrefabUtility.LoadPrefabContents(Prefab);
            try
            {
                var animador = raiz.GetComponent<AnimadorDoCultista>()
                               ?? raiz.AddComponent<AnimadorDoCultista>();

                var so = new SerializedObject(animador);
                foreach (var par in camposPreenchidos)
                {
                    var prop = so.FindProperty(par.Key);
                    if (prop == null)
                    {
                        Debug.LogWarning($"[AnimacaoCultista] Campo '{par.Key}' não existe em " +
                                         "AnimadorDoCultista.");
                        continue;
                    }

                    prop.arraySize = par.Value.Count;
                    for (int i = 0; i < par.Value.Count; i++)
                        prop.GetArrayElementAtIndex(i).objectReferenceValue = par.Value[i];
                }
                so.ApplyModifiedPropertiesWithoutUndo();

                AjustarEscalaPreservandoOColisor(raiz, resumo);

                var sr = raiz.GetComponent<SpriteRenderer>();
                if (sr != null && camposPreenchidos.TryGetValue("idle", out var idle) && idle.Count > 0)
                    sr.sprite = idle[0];

                PrefabUtility.SaveAsPrefabAsset(raiz, Prefab);
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(raiz);
            }

            Debug.Log("[AnimacaoCultista] Concluído:\n  " + string.Join("\n  ", resumo));
        }

        private static void AjustarEscalaPreservandoOColisor(GameObject raiz, List<string> resumo)
        {
            var box = raiz.GetComponent<BoxCollider2D>();
            Vector2 volumeDeMundo = Vector2.zero;
            bool temColisor = box != null;

            Vector3 escalaAntiga = raiz.transform.localScale;

            if (temColisor)
                volumeDeMundo = new Vector2(box.size.x * escalaAntiga.x,
                                            box.size.y * escalaAntiga.y);

            raiz.transform.localScale = new Vector3(EscalaNova, EscalaNova, 1f);

            if (temColisor)
                box.size = new Vector2(volumeDeMundo.x / EscalaNova, volumeDeMundo.y / EscalaNova);

            resumo.Add($"escala {escalaAntiga.x:0.###} → {EscalaNova:0.###} " +
                       $"(figura {escalaAntiga.x * AlturaDaFiguraEmPx / 32f:0.00} → " +
                       $"{AlturaDaFiguraDoDamiao:0.00} un, igual à do Damião)");

            if (temColisor)
                resumo.Add($"colisor {volumeDeMundo.x / escalaAntiga.x:0.###} → {box.size.x:0.###} " +
                           $"(volume de mundo preservado em {volumeDeMundo.x:0.###})");
        }
    }
}
