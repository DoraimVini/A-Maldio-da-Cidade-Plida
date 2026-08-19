using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using FavelaAmarela.Inventario;
using FavelaAmarela.Runtime.GameLoop;
using FavelaAmarela.Runtime.Itens;
using FavelaAmarela.Runtime.Rendering;

namespace FavelaAmarela.EditorTools
{
    /// <summary>
    /// Ferramenta de Editor. Espalha os <b>consumíveis</b> pelo Deserto de Hali — o item 2 da
    /// lista do edital, que tinha os <c>ItemDef</c> autorados e o pipeline de consumo pronto,
    /// mas <b>zero instâncias no mundo</b>: o jogador não tinha como obter nenhum.
    ///
    /// <para><b>Modelo de escassez:</b> consumíveis são finitos e encontrados no mapa, não
    /// farmáveis de inimigos comuns. A quantidade aqui e o <c>EmpilhamentoMaximo</c> de cada
    /// <c>ItemDef</c> são os dois diais de balanceamento — ambos dado, nenhum código. O
    /// anti-<em>soft-lock</em> não é recarga: é o <see cref="RefugioDeLuz"/>, que devolve
    /// Resiliência e parte da Vitalidade e é o único ponto de save do jogo. Ver
    /// <c>Docs/KnowledgeBundle/systems/inventario_e_consumiveis.md</c>.</para>
    ///
    /// <para><b>Chave de save derivada, nunca aleatória.</b> O <c>PovoarODeserto</c> usa
    /// <c>ObjetoPersistente.GarantirChave()</c>, que sorteia um GUID novo a cada reconstrução —
    /// rodar a ferramenta de novo troca todas as chaves e faz o progresso já registrado apontar
    /// para o vazio (verificado em 2026-08-12: os 12 inimigos abatidos voltariam a viver). Aqui
    /// a chave vem do id do item mais um índice estável, e os setores são percorridos em ordem
    /// alfabética, então <b>rodar duas vezes produz exatamente as mesmas chaves</b>.</para>
    /// </summary>
    public static class MontarConsumiveisDoDeserto
    {
        private const string CaminhoCena = "Assets/Scenes/Deserto_Hali.unity";
        private const string NomeDoGrupo = "Consumiveis_Deserto";
        private const string PastaDosItens = "Assets/FavelaAmarela/Config/Resources/Itens/";

        // Semente fixa: a distribuição é reproduzível, como no PovoarODeserto.
        private const int Semente = 20260812;

        // Setor de chegada fica vazio, coerente com o PovoarODeserto: é onde o jogador respira.
        private const string SetorDeChegada = "Setor_Entrada";

        /// <summary>Quanto de cada consumível existe no Deserto inteiro. É o dial de escassez.</summary>
        private static readonly (string Asset, int Quantidade, Color Cor)[] Receita =
        {
            // Cura de corpo: a mais comum, porque é o único canal que pode travar o progresso.
            ("Item_Consumivel_AguaDaCacimba.asset", 4, new Color(0.45f, 0.70f, 0.85f)),
            // Cura de mente: o Refúgio já devolve RM cheia, então aqui é conveniência, não rede.
            ("Item_Consumivel_ErvaDeAncoragem.asset", 3, new Color(0.55f, 0.80f, 0.45f)),
            // Cura os dois canais de uma vez: a mais rara, e a que vale guardar para um chefe.
            ("Item_Consumivel_RaizDeYhtill.asset", 2, new Color(0.85f, 0.75f, 0.35f)),
        };

        [MenuItem("Tools/FavelaAmarela/Montar consumíveis do Deserto")]
        public static void Executar()
        {
            var cenaAtiva = EditorSceneManager.GetActiveScene();
            if (cenaAtiva.isDirty && !string.IsNullOrEmpty(cenaAtiva.path))
                EditorSceneManager.SaveScene(cenaAtiva);

            string cenaOriginal = cenaAtiva.path;
            var cena = EditorSceneManager.OpenScene(CaminhoCena, OpenSceneMode.Single);

            int total = Espalhar();

            if (total > 0)
            {
                EditorSceneManager.MarkSceneDirty(cena);
                EditorSceneManager.SaveScene(cena);
            }

            if (!string.IsNullOrEmpty(cenaOriginal) && cenaOriginal != CaminhoCena)
                EditorSceneManager.OpenScene(cenaOriginal, OpenSceneMode.Single);

            Debug.Log($"[Consumiveis] Pronto — {total} consumível(is) no Deserto de Hali.");
        }

        private static int Espalhar()
        {
            var setores = SetoresOrdenados();
            if (setores.Count == 0)
            {
                Debug.LogWarning("[Consumiveis] Nenhum setor de tempestade na cena — rode antes " +
                                 "'Montar setores de tempestade do Deserto'. Nada foi colocado.");
                return 0;
            }

            // Refaz do zero: sem isso, rodar duas vezes dobraria a quantidade.
            var antigo = GameObject.Find(NomeDoGrupo);
            if (antigo != null) UnityEngine.Object.DestroyImmediate(antigo);

            var grupo = new GameObject(NomeDoGrupo);
            var rng = new System.Random(Semente);
            int total = 0;

            foreach (var (asset, quantidade, cor) in Receita)
            {
                var def = AssetDatabase.LoadAssetAtPath<ItemDef>(PastaDosItens + asset);
                if (def == null)
                {
                    Debug.LogError($"[Consumiveis] ItemDef não encontrado: {PastaDosItens}{asset}");
                    continue;
                }

                for (int i = 0; i < quantidade; i++)
                {
                    // Distribui em rodízio pelos setores, para nenhum ficar sem nada.
                    var setor = setores[(total + i) % setores.Count];
                    var area = ObterArea(setor);
                    if (!area.HasValue) continue;

                    Colocar(def, cor, i, PontoDentro(area.Value, rng), grupo.transform);
                }

                total += quantidade;
                Debug.Log($"[Consumiveis] {quantidade}x '{def.Nome}'.");
            }

            return total;
        }

        /// <summary>
        /// Setores em ordem alfabética, sem o de chegada. A ordenação importa: sem ela a ordem
        /// vem do <c>FindObjectsByType</c> e poderia variar entre execuções, o que embaralharia
        /// qual chave de save corresponde a qual posição.
        /// </summary>
        private static List<TempestadeZonaTrigger> SetoresOrdenados()
        {
            var encontrados = UnityEngine.Object.FindObjectsByType<TempestadeZonaTrigger>(
                FindObjectsInactive.Include);

            var lista = new List<TempestadeZonaTrigger>();
            foreach (var s in encontrados)
                if (s.name != SetorDeChegada) lista.Add(s);

            lista.Sort((a, b) => string.CompareOrdinal(a.name, b.name));
            return lista;
        }

        private static void Colocar(ItemDef def, Color cor, int indice, Vector3 posicao, Transform pai)
        {
            // O id já começa com "consumivel_"; repetir o prefixo no nome do objeto deixaria
            // a hierarquia com "Consumivel_consumivel_...".
            var go = new GameObject($"Coletavel_{def.Id}_{indice}");
            go.transform.SetParent(pai, false);
            go.transform.position = posicao;

            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = def.Icone != null ? def.Icone : SpriteProvisorio();
            sr.color = def.Icone != null ? Color.white : cor;
            go.transform.localScale = def.Icone != null ? Vector3.one : Vector3.one * 0.5f;

            go.AddComponent<DynamicYSort>();

            var col = go.AddComponent<CircleCollider2D>();
            col.isTrigger = true;
            col.radius = 0.6f;

            var coletavel = go.AddComponent<ColetavelDeItem>();

            // Chave DERIVADA do id do item + índice: estável entre execuções, ao contrário do
            // GUID aleatório do PovoarODeserto. É o que permite rodar a ferramenta de novo sem
            // ressuscitar o que o jogador já recolheu.
            coletavel.Configurar(def, quantos: 1, chave: $"Item.Deserto.{def.Id}.{indice}");
        }

        /// <summary>
        /// Sprite embutido da Unity, tingido por tipo de consumível. Os <c>ItemDef</c> de
        /// consumível ainda não têm ícone autorado, e sem isto o coletável nasceria invisível.
        /// Trocar por arte real é só preencher o campo <c>Icone</c> do asset — a ferramenta
        /// passa a usá-lo sozinha.
        /// </summary>
        private static Sprite SpriteProvisorio()
            => AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/Knob.psd");

        private static Bounds? ObterArea(TempestadeZonaTrigger setor)
        {
            var col = setor.GetComponent<Collider2D>();
            if (col == null)
            {
                Debug.LogWarning($"[Consumiveis] '{setor.name}' não tem Collider2D — pulado.");
                return null;
            }
            return col.bounds;
        }

        /// <summary>Ponto aleatório dentro da área, com margem para não nascer colado na borda.</summary>
        private static Vector3 PontoDentro(Bounds area, System.Random rng)
        {
            const float margem = 0.2f;

            float Entre(float a, float b) => Mathf.Lerp(a, b, (float)rng.NextDouble());

            float x = Entre(area.min.x + area.size.x * margem, area.max.x - area.size.x * margem);
            float y = Entre(area.min.y + area.size.y * margem, area.max.y - area.size.y * margem);

            return new Vector3(x, y, 0f);
        }
    }
}
