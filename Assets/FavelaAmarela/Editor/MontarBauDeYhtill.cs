using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using FavelaAmarela.Core.Loot;
using FavelaAmarela.Core.Persistencia;
using FavelaAmarela.Inventario;
using FavelaAmarela.Runtime.GameLoop;
using FavelaAmarela.Runtime.Itens;

namespace FavelaAmarela.EditorTools
{
    /// <summary>
    /// Monta o <b>Baú de Yhtill</b>: a recompensa material da quest da Cassilda.
    ///
    /// <para><b>O pedido do Vini (2026-09-01):</b> <i>"vamos por um baú na cena da Cassilda,
    /// para ganharmos alguma coisa depois da quest dela."</i></para>
    ///
    /// <para><b>O que isto conserta.</b> "A Canção Incompleta" entrega o <b>Patuá das Luas
    /// Gêmeas</b> — que é uma <i>relíquia de rito</i>, exigida lá no Rei em Amarelo. Do ponto de
    /// vista de quem está jogando a Fase 1, a maior quest do Santuário devolve um item que não
    /// muda nada no minuto seguinte. A recompensa existia e não era <b>sentida</b>.</para>
    ///
    /// <para><b>O broquel é garantido, e isso é a decisão de design aqui.</b> A quest é opcional
    /// e acontece <b>antes</b> do Byakhee, que bate 26 contra a Defesa 6 do Damião — cinco
    /// golpes até o Colapso. A mitigação é subtrativa, então cada ponto de Defesa muda essa
    /// conta diretamente. Quem explora o Santuário entra no chefe mais defendido; quem pula,
    /// enfrenta a luta que o Vini já reportou como dura demais. <b>É exploração pagando em
    /// sobrevivência</b>, que é a promessa do pivô para combate + exploração.</para>
    ///
    /// <para>O baú não conhece a Cassilda: ele lê a chave de save
    /// <c>Quest.Cassilda.Concluida</c>. Ver <see cref="BauDeRecompensa"/>.</para>
    /// </summary>
    public static class MontarBauDeYhtill
    {
        private const string Marcador = "[BauDeYhtill]";
        private const string Cena = "Assets/Scenes/Santuario_Yhtill.unity";
        private const string PastaDasTabelas = "Assets/FavelaAmarela/Config/Drops";
        private const string PastaDosItens = "Assets/FavelaAmarela/Config/Resources/Itens";
        private const string PastaDosProps = "Assets/FavelaAmarela/Art/Props";

        private const string NomeDaTabela = "Drop_BauDeYhtill";
        private const string NomeDoBau = "Bau_DeYhtill";
        private const string ChaveDoBau = "Mundo.Bau.Yhtill";

        /// <summary>
        /// O conteúdo. <b>Garantido</b> significa que ignora chance e nível — é o que faz a
        /// recompensa da quest ser uma promessa, e não um sorteio que pode sair vazio depois de
        /// o jogador recolher três fragmentos pelo deserto.
        /// </summary>
        private static readonly (string Arquivo, bool Garantido, float Chance, int Min, int Max)[]
            Conteudo =
        {
            // O broquel SEMPRE. Ver o sumário da classe: é Defesa antes do Byakhee.
            ("Item_Escudo_BroquelDeCouro", true, 1f, 1, 1),

            // Um degrau de arma, provavelmente. As três com o mesmo peso: qual sai é variedade
            // entre partidas. O teto de 3 impede que saiam as três.
            ("Item_Arma_AlfanjeDasRuinasPalidas", false, 0.45f, 1, 1),
            ("Item_Arma_MacaDeAldebaran", false, 0.45f, 1, 1),
            ("Item_Arma_EstileteDeYhtill", false, 0.45f, 1, 1),

            // A Raiz de Yhtill é daqui — o consumível leva o nome do lugar. Coerência barata
            // que o jogador nota sem que ninguém explique.
            ("Item_Consumivel_RaizDeYhtill", false, 0.8f, 1, 2),
        };

        [MenuItem("Tools/FavelaAmarela/Itens: montar o Baú de Yhtill (quest da Cassilda)")]
        public static void Executar()
        {
            var resumo = new List<string> { MontarATabela() };

            var cena = EditorSceneManager.OpenScene(Cena, OpenSceneMode.Single);
            resumo.Add(PorOBauNaCena(cena));

            EditorSceneManager.MarkSceneDirty(cena);
            EditorSceneManager.SaveScene(cena);
            AssetDatabase.SaveAssets();

            string quebra = System.Environment.NewLine + "  ";
            Debug.Log($"{Marcador} Concluído:" + quebra + string.Join(quebra, resumo));
        }

        // ── A tabela ──────────────────────────────────────────────────────────

        private static string MontarATabela()
        {
            string caminho = $"{PastaDasTabelas}/{NomeDaTabela}.asset";
            var tabela = AssetDatabase.LoadAssetAtPath<TabelaDeDrop>(caminho);

            bool criada = tabela == null;
            if (criada)
            {
                tabela = ScriptableObject.CreateInstance<TabelaDeDrop>();
                AssetDatabase.CreateAsset(tabela, caminho);
            }

            var so = new SerializedObject(tabela);
            var entradas = so.FindProperty("entradas");

            if (entradas == null || !entradas.isArray)
                return $"{NomeDaTabela}: campo 'entradas' não existe mais no TabelaDeDrop";

            var jaPresentes = new HashSet<Object>();
            for (int i = 0; i < entradas.arraySize; i++)
            {
                var item = entradas.GetArrayElementAtIndex(i).FindPropertyRelative("Item");
                if (item?.objectReferenceValue != null) jaPresentes.Add(item.objectReferenceValue);
            }

            var postos = new List<string>();

            foreach (var (arquivo, garantido, chance, min, max) in Conteudo)
            {
                var def = AssetDatabase.LoadAssetAtPath<ItemDef>($"{PastaDosItens}/{arquivo}.asset");

                if (def == null) { postos.Add($"{arquivo} AUSENTE"); continue; }

                // Idempotente: rodar duas vezes não empilha o mesmo item.
                if (jaPresentes.Contains(def)) continue;

                entradas.arraySize++;
                var nova = entradas.GetArrayElementAtIndex(entradas.arraySize - 1);

                nova.FindPropertyRelative("Item").objectReferenceValue = def;
                nova.FindPropertyRelative("Grau").enumValueIndex = (int)GrauDeImpregnacao.Marcado;
                nova.FindPropertyRelative("Garantido").boolValue = garantido;
                nova.FindPropertyRelative("Chance").floatValue = chance;
                nova.FindPropertyRelative("QuantidadeMin").intValue = min;
                nova.FindPropertyRelative("QuantidadeMax").intValue = max;
                nova.FindPropertyRelative("NivelMinimo").intValue = 1;

                postos.Add(def.name + (garantido ? " [GARANTIDO]" : $" ({chance:P0})"));
            }

            var teto = so.FindProperty("tetoDeItens");
            if (teto != null) teto.intValue = 3;

            // O piso de nível é responsabilidade do DefinirNivelDasTabelasDeDrop, que é a fonte
            // única disso. Aqui só se garante que não fique em zero se a tabela nasceu agora.
            var nivel = so.FindProperty("nivelDoItem");
            if (nivel != null && nivel.intValue < 1) nivel.intValue = 3;

            so.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(tabela);
            AssetDatabase.SaveAssetIfDirty(tabela);

            string oQue = postos.Count == 0 ? "nada a acrescentar (já tinha tudo)"
                                            : string.Join(" + ", postos);

            return $"{NomeDaTabela}{(criada ? " [CRIADA]" : "")}: {oQue}, teto 3";
        }

        // ── O baú, na cena ────────────────────────────────────────────────────

        private static string PorOBauNaCena(UnityEngine.SceneManagement.Scene cena)
        {
            var raizes = cena.GetRootGameObjects();

            var existente = raizes
                .SelectMany(r => r.GetComponentsInChildren<BauDeRecompensa>(true))
                .FirstOrDefault(b => b.name == NomeDoBau);

            var cassilda = raizes
                .SelectMany(r => r.GetComponentsInChildren<Transform>(true))
                .FirstOrDefault(t => t.name.Contains("Cassilda"));

            if (cassilda == null) return "baú: 'Cassilda' não encontrada na cena — nada posto";

            var tabela = AssetDatabase.LoadAssetAtPath<TabelaDeDrop>(
                $"{PastaDasTabelas}/{NomeDaTabela}.asset");

            if (tabela == null) return "baú: tabela ausente — nada posto";

            GameObject go;
            bool novo = existente == null;

            if (novo)
            {
                go = new GameObject(NomeDoBau, typeof(SpriteRenderer), typeof(BoxCollider2D));
                go.transform.SetParent(cassilda.parent, worldPositionStays: true);

                // Ao LADO da rainha, não em cima: o baú tem de ser visto de onde se conversa
                // com ela, sem disputar o prompt de interação com o diálogo.
                go.transform.position = cassilda.position + new Vector3(2.2f, -0.6f, 0f);
            }
            else
            {
                go = existente.gameObject;
            }

            var col = go.GetComponent<BoxCollider2D>() ?? go.AddComponent<BoxCollider2D>();
            col.isTrigger = true;
            col.size = new Vector2(1f, 1f);

            var sr = go.GetComponent<SpriteRenderer>() ?? go.AddComponent<SpriteRenderer>();
            var fechado = AssetDatabase.LoadAssetAtPath<Sprite>($"{PastaDosProps}/bau_fechado.png");
            var aberto = AssetDatabase.LoadAssetAtPath<Sprite>($"{PastaDosProps}/bau_aberto.png");

            if (fechado != null) sr.sprite = fechado;
            sr.sortingOrder = Mathf.RoundToInt(-go.transform.position.y * 10f);

            var bau = go.GetComponent<BauDeRecompensa>() ?? go.AddComponent<BauDeRecompensa>();

            var soBau = new SerializedObject(bau);
            soBau.FindProperty("chaveDeSaveExigida").stringValue = ChavesDeSave.CassildaConcluida;
            soBau.FindProperty("chaveDeSaveDoBau").stringValue = ChaveDoBau;
            soBau.FindProperty("rotulo").stringValue = "Abrir o baú";
            soBau.FindProperty("spriteDoBau").objectReferenceValue = sr;
            soBau.FindProperty("spriteAberto").objectReferenceValue = aberto;

            // A fala diz POR QUE está fechado, sem dizer "faça a quest": o jogador liga as
            // duas coisas sozinho, e a rainha continua sendo quem conta a história.
            soBau.FindProperty("falaTrancado").stringValue =
                "O fecho não cede. Cassilda ainda tem estrofes presas na garganta.";
            soBau.FindProperty("falaAoAbrir").stringValue =
                "A rainha inclina a cabeça. O baú de Yhtill se abre.";

            soBau.ApplyModifiedPropertiesWithoutUndo();

            // O DropAoAbater é quem materializa. Sem ele o baú abre vazio -- e criar a peça sem
            // ligar a peça é o modo de falha que este repositório mais catalogou.
            var drop = go.GetComponent<DropAoAbater>() ?? go.AddComponent<DropAoAbater>();

            var soDrop = new SerializedObject(drop);
            soDrop.FindProperty("tabela").objectReferenceValue = tabela;
            soDrop.FindProperty("raioDeEspalhamento").floatValue = 0.7f;
            soDrop.ApplyModifiedPropertiesWithoutUndo();

            EditorUtility.SetDirty(go);

            return $"baú: '{NomeDoBau}' {(novo ? "CRIADO" : "atualizado")} em " +
                   $"{go.transform.position} (ao lado da Cassilda), portão " +
                   $"'{ChavesDeSave.CassildaConcluida}', tabela '{NomeDaTabela}'" +
                   (fechado == null ? " — SEM SPRITE (bau_fechado.png não importado)" : "");
        }
    }
}
