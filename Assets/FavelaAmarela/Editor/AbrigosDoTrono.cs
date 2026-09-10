using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using FavelaAmarela.Runtime.Enemies;
using FavelaAmarela.Runtime.Itens;

namespace FavelaAmarela.EditorTools
{
    /// <summary>
    /// Monta os <b>abrigos</b> dos três pontos focais do Trono de Aldebaran.
    ///
    /// <para><b>A mecânica (pedido do Vini, 2026-09-10):</b> <i>"cada artefato gera um escudo
    /// por vez e você tem que se proteger dentro"</i>. Esta ferramenta põe em cena o que o
    /// <see cref="EscudoDeReliquia"/> precisa: o componente no altar, a cúpula como filho, e a
    /// ligação no <see cref="ReiEmAmareloAI"/>.</para>
    ///
    /// <para><b>Por que ferramenta e não código de runtime.</b> A cúpula precisa dos 12 quadros
    /// do <c>Escudo_Magico</c> atribuídos a um <c>AnimadorEmLaco</c>, e <c>quadros</c> é campo
    /// serializado privado sem setter. Montar aqui deixa a referência <b>gravada no YAML da
    /// cena</b> — que é onde se vai procurar quando algo não acende — em vez de existir só em
    /// memória.</para>
    ///
    /// <para><b>Arte reaproveitada, medida antes:</b> o <c>Escudo_Magico</c> do Abdul tem
    /// 64 × 96 px a PPU 32 (2 × 3 un), pivô central, e <b>alpha médio de 20%</b> — dá para ver
    /// Damião lá dentro, que é a condição para um abrigo em que se entra. À escala 2,5 a cúpula
    /// fica com 5 × 7,5 un: alta de propósito, porque os dois altares das pontas estão a 20 un
    /// um do outro e a câmera só mostra 20 — a cúpula aparece por cima da borda do quadro antes
    /// de o altar aparecer.</para>
    ///
    /// <para>Idempotente: rodar de novo refaz as cúpulas do zero.</para>
    /// </summary>
    public static class AbrigosDoTrono
    {
        private const string Marcador = "[AbrigosDoTrono]";
        private const string Cena = "Assets/Scenes/Castelo_Carcosa.unity";
        private const string Quadros = "Assets/FavelaAmarela/Art/Enemies/Abdul/Escudo/Escudo_Magico_";
        private const string NomeDoVisual = "Escudo";

        /// <summary>Escala da cúpula. Ver o resumo da classe.</summary>
        private const float EscalaDaCupula = 2.5f;

        /// <summary>
        /// Altura do centro da cúpula acima do chão do altar.
        ///
        /// <para>Sai da conta, não do olho: a cúpula tem 7,5 un à escala 2,5, logo 3,75 de
        /// meia-altura; o abrigo testado vai até 1,0 un ao sul do altar. Pondo o centro em
        /// 2,75, a <b>base desenhada da cúpula encontra exatamente a borda sul da elipse
        /// testada</b> — o que se vê é o que se testa.</para>
        /// </summary>
        private const float AlturaDoCentro = 2.75f;

        [MenuItem("Tools/FavelaAmarela/Trono: montar os abrigos das relíquias")]
        public static void Executar()
        {
            Scene cena = EditorSceneManager.OpenScene(Cena, OpenSceneMode.Single);

            var quadros = CarregarQuadros();
            if (quadros.Length == 0)
            {
                Debug.LogError($"{Marcador} nenhum quadro de Escudo_Magico encontrado em " +
                               $"'{Quadros}00..11.png' — sem cúpula, o abrigo seria invisível.");
                return;
            }

            var pontos = Object.FindObjectsByType<PontoFocalDeReliquia>(FindObjectsSortMode.None);
            if (pontos.Length == 0)
            {
                Debug.LogError($"{Marcador} nenhum PontoFocalDeReliquia na cena.");
                return;
            }

            var escudos = new List<EscudoDeReliquia>();

            foreach (var ponto in pontos.OrderBy(p => p.name))
            {
                var escudo = ponto.GetComponent<EscudoDeReliquia>()
                             ?? Undo.AddComponent<EscudoDeReliquia>(ponto.gameObject);

                var visual = MontarCupula(ponto.transform, quadros);

                var so = new SerializedObject(escudo);
                so.FindProperty("visual").objectReferenceValue = visual;
                so.FindProperty("nomeDoVisual").stringValue = NomeDoVisual;
                so.ApplyModifiedPropertiesWithoutUndo();

                escudos.Add(escudo);

                Debug.Log($"{Marcador} {ponto.name} ('{ponto.ArtefatoId}') em " +
                          $"{(Vector2)ponto.transform.position} — abrigo " +
                          $"{escudo.SemiEixoX * 2f} x {escudo.SemiEixoY * 2f} un.");
            }

            LigarNoRei(escudos);

            EditorSceneManager.MarkSceneDirty(cena);
            EditorSceneManager.SaveScene(cena);

            Debug.Log($"{Marcador} {escudos.Count} abrigo(s) montado(s) e ligados ao rito.");
        }

        private static Sprite[] CarregarQuadros()
        {
            var lista = new List<Sprite>();

            for (int i = 0; i < 12; i++)
            {
                var s = AssetDatabase.LoadAssetAtPath<Sprite>($"{Quadros}{i:00}.png");
                if (s != null) lista.Add(s);
            }

            return lista.ToArray();
        }

        private static GameObject MontarCupula(Transform altar, Sprite[] quadros)
        {
            var antiga = altar.Find(NomeDoVisual);
            if (antiga != null) Object.DestroyImmediate(antiga.gameObject);

            var go = new GameObject(NomeDoVisual);
            go.transform.SetParent(altar, false);
            go.transform.localPosition = new Vector3(0f, AlturaDoCentro, 0f);
            go.transform.localScale = new Vector3(EscalaDaCupula, EscalaDaCupula, 1f);

            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = quadros[0];

            // "Frente": a cúpula é translúcida e tem de ser vista POR CIMA de quem está dentro.
            // Deixá-la no Y-sort com os atores faria o jogador sumir atrás do próprio abrigo
            // metade das vezes, dependendo de meio passo para o norte ou para o sul.
            sr.sortingLayerName = "Frente";
            sr.sortingOrder = 0;

            var animador = go.AddComponent<FavelaAmarela.Runtime.Enemies.AnimadorEmLaco>();
            var so = new SerializedObject(animador);
            var arr = so.FindProperty("quadros");
            arr.arraySize = quadros.Length;
            for (int i = 0; i < quadros.Length; i++)
                arr.GetArrayElementAtIndex(i).objectReferenceValue = quadros[i];
            so.ApplyModifiedPropertiesWithoutUndo();

            // Nasce apagada: quem acende é o rito, um escudo por vez.
            go.SetActive(false);

            return go;
        }

        private static void LigarNoRei(List<EscudoDeReliquia> escudos)
        {
            var rei = Object.FindFirstObjectByType<ReiEmAmareloAI>();
            if (rei == null)
            {
                Debug.LogWarning($"{Marcador} nenhum ReiEmAmareloAI na cena — os abrigos ficam " +
                                 "montados, mas ninguém os acende.");
                return;
            }

            var so = new SerializedObject(rei);
            var arr = so.FindProperty("escudos");
            arr.arraySize = escudos.Count;
            for (int i = 0; i < escudos.Count; i++)
                arr.GetArrayElementAtIndex(i).objectReferenceValue = escudos[i];
            so.ApplyModifiedPropertiesWithoutUndo();

            Debug.Log($"{Marcador} {escudos.Count} abrigo(s) ligados ao Rei em Amarelo.");
        }
    }
}
