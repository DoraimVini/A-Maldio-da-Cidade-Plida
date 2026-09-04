using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using FavelaAmarela.Runtime.GameLoop;

namespace FavelaAmarela.EditorTools
{
    /// <summary>
    /// Ferramenta de Editor. Fecha o ciclo de ida e volta da Tumba de Alhazred: hoje dá
    /// para entrar na dungeon pelo Deserto, mas não para sair — a Tumba é um beco sem saída
    /// (achado pelo Vini em playtest, 2026-07-31).
    ///
    /// <para>Decisão do Vini: <b>a saída fica na própria porta de entrada</b>, sem inventar
    /// um segundo local. Então esta ferramenta monta as duas pontas:</para>
    /// <list type="number">
    ///   <item>Na <b>Tumba</b>: um <see cref="PortalDeCena"/> na entrada (onde Damião
    ///   aparece ao chegar), destino <c>Deserto_Hali</c>, pedindo chegada em
    ///   <see cref="IdChegada"/>.</item>
    ///   <item>No <b>Deserto</b>: um <see cref="PontoDeChegada"/> com esse identificador,
    ///   colado na entrada da Tumba — é o que faz o jogador sair exatamente onde entrou, em
    ///   vez de no ponto inicial do deserto.</item>
    /// </list>
    ///
    /// <para>Idempotente: reconhece as peças pelo nome e reaproveita em vez de duplicar.</para>
    /// </summary>
    public static class MontarSaidaDaTumba
    {
        private const string CenaTumba = "Assets/Scenes/Tumba_De_Alhazred.unity";
        private const string CenaDeserto = "Assets/Scenes/Deserto_Hali.unity";
        private const string NomeCenaDeserto = "Deserto_Hali";

        private const string IdChegada = "TumbaAlhazred";
        private const string NomeSaida = "Saida_TumbaAlhazred";
        private const string NomeChegada = "Chegada_TumbaAlhazred";
        private const string NomeEntradaNoDeserto = "Entrada_TumbaAlhazred";

        /// <summary>Onde Damião aparece ao entrar na Tumba — a porta, portanto.</summary>
        private static readonly Vector3 EntradaDaTumba = new Vector3(0.84f, 0.98f, 0f);

        /// <summary>Tamanho do volume da porta, em unidades de mundo.</summary>
        private static readonly Vector2 TamanhoDaPorta = new Vector2(1.6f, 1.6f);

        [MenuItem("Tools/FavelaAmarela/Montar saída da Tumba (ida e volta)")]
        public static void Executar()
        {
            // Salva sem perguntar. `SaveCurrentModifiedScenesIfUserWantsTo` abre um
            // diálogo MODAL, e uma ferramenta disparada pela ponte MCP trava a Unity
            // inteira esperando um clique que ninguém vê.
            var cenaAtiva = EditorSceneManager.GetActiveScene();
            if (cenaAtiva.isDirty && !string.IsNullOrEmpty(cenaAtiva.path))
                EditorSceneManager.SaveScene(cenaAtiva);

            string cenaOriginal = cenaAtiva.path;

            MontarPontoDeChegadaNoDeserto();
            MontarSaidaNaTumba();

            if (!string.IsNullOrEmpty(cenaOriginal))
                EditorSceneManager.OpenScene(cenaOriginal, OpenSceneMode.Single);

            Debug.Log("[MontarSaidaDaTumba] Pronto. A Tumba agora tem porta de saída na " +
                      "entrada, e o Deserto recebe o jogador na porta da Tumba.");
        }

        private static void MontarSaidaNaTumba()
        {
            var cena = EditorSceneManager.OpenScene(CenaTumba, OpenSceneMode.Single);

            var go = GameObject.Find(NomeSaida);
            if (go == null)
            {
                go = new GameObject(NomeSaida);
                Undo.RegisterCreatedObjectUndo(go, "Criar saída da Tumba");
            }

            go.transform.position = EntradaDaTumba;

            var col = go.GetComponent<BoxCollider2D>();
            if (col == null) col = go.AddComponent<BoxCollider2D>();
            col.isTrigger = true;
            col.size = TamanhoDaPorta;

            var portal = go.GetComponent<PortalDeCena>();
            if (portal == null) portal = go.AddComponent<PortalDeCena>();
            portal.DefinirCenaDestino(NomeCenaDeserto);
            portal.DefinirChegada(IdChegada);

            EditorSceneManager.MarkSceneDirty(cena);
            EditorSceneManager.SaveScene(cena);
            Debug.Log($"[MontarSaidaDaTumba] Saída posicionada em {EntradaDaTumba} na Tumba.");
        }

        private static void MontarPontoDeChegadaNoDeserto()
        {
            var cena = EditorSceneManager.OpenScene(CenaDeserto, OpenSceneMode.Single);

            // A chegada fica exatamente sobre a entrada — "sai no mesmo lugar que entrou".
            // Cair em cima do portal de entrada não reabre a Tumba: o PortalDeCena tem uma
            // carência de alguns décimos de segundo após carregar, justamente para isso.
            var entrada = GameObject.Find(NomeEntradaNoDeserto);
            if (entrada == null)
            {
                Debug.LogError($"[MontarSaidaDaTumba] Não achei '{NomeEntradaNoDeserto}' no " +
                               "Deserto — sem ela não sei onde fica a porta da Tumba. Nada feito.");
                return;
            }

            var go = GameObject.Find(NomeChegada);
            if (go == null)
            {
                go = new GameObject(NomeChegada);
                Undo.RegisterCreatedObjectUndo(go, "Criar ponto de chegada da Tumba");
            }

            go.transform.position = entrada.transform.position;

            if (go.GetComponent<PontoDeChegada>() == null)
                go.AddComponent<PontoDeChegada>();

            // `identificador` é privado e serializado: escrito por SerializedObject, que é
            // como o resto das ferramentas do projeto mexe em campo serializado.
            var so = new SerializedObject(go.GetComponent<PontoDeChegada>());
            so.FindProperty("identificador").stringValue = IdChegada;
            so.ApplyModifiedPropertiesWithoutUndo();

            EditorSceneManager.MarkSceneDirty(cena);
            EditorSceneManager.SaveScene(cena);
            Debug.Log($"[MontarSaidaDaTumba] Chegada '{IdChegada}' posicionada em " +
                      $"{entrada.transform.position} no Deserto.");
        }
    }
}
