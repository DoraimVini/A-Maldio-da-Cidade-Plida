using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using FavelaAmarela.Runtime.Environment;
using FavelaAmarela.Runtime.GameLoop;

namespace FavelaAmarela.EditorTools
{
    /// <summary>
    /// Ferramenta de Editor. Cria os <b>setores de tempestade</b> do Deserto de Hali —
    /// os volumes que fazem a intensidade variar por região, conforme a tabela de
    /// <c>systems/level_design_deserto_hali.md</c> §3.
    ///
    /// <para>Até aqui o Deserto inteiro usava a faixa padrão do <see cref="TempestadeAmbiente"/>.
    /// São estes volumes que transformam a tempestade de "efeito ambiente" em <b>geografia
    /// jogável</b>: o centro vira alívio de stealth (o vento abafa tudo), o leste vira
    /// travessia às cegas, e a Entrada/Santuário/Portões viram zonas de respiro.</para>
    ///
    /// <para>Idempotente: reaproveita os volumes pelo nome e só reescreve os valores.</para>
    /// </summary>
    public static class MontarSetoresDoDeserto
    {
        private const string CenaDeserto = "Assets/Scenes/Deserto_Hali.unity";
        private const string NomeRaiz = "Setores_Tempestade";

        /// <summary>
        /// Um setor: nome, centro, tamanho e faixa de intensidade.
        ///
        /// <para>A faixa vem da <b>visibilidade</b> da tabela de design (intensidade ≈
        /// 1 − visibilidade), com uma banda estreita em volta para o oscilador ter o que
        /// respirar — a tempestade nunca é um valor fixo, são rajadas.</para>
        ///
        /// <para>Os retângulos <b>ladrilham o mapa sem sobrepor</b>. Importa porque o
        /// <see cref="TempestadeZonaTrigger"/> age no <c>OnTriggerEnter2D</c>: com volumes
        /// sobrepostos, qual "vence" dependeria da ordem de entrada.</para>
        /// </summary>
        private readonly struct Setor
        {
            public readonly string Nome;
            public readonly Vector2 Centro, Tamanho;
            public readonly float Min, Max;
            public readonly string Porque;

            public Setor(string nome, Vector2 centro, Vector2 tamanho, float min, float max, string porque)
            {
                Nome = nome; Centro = centro; Tamanho = tamanho; Min = min; Max = max; Porque = porque;
            }
        }

        // Mapa jogável: x ∈ [-21,5 ; 21,5], y ∈ [-15,5 ; 15,5].
        // Bandas: Oeste [-21,5;-9] · Centro [-9;9] · Leste [9;21,5]
        //         Sul [-15,5;-8] · Meio [-8;10] · Norte [10;15,5]
        private static readonly Setor[] Setores =
        {
            new Setor("Setor_Entrada", new Vector2(0f, -11.75f), new Vector2(43f, 7.5f),
                0.00f, 0.08f, "Calmaria — área de orientação, 100% de visibilidade, sem penalidade"),

            new Setor("Setor_TumbaDeAlhazred", new Vector2(-15.25f, -1f), new Vector2(12.5f, 14f),
                0.22f, 0.38f, "Moderada (~70% vis.) — passos abafados"),

            new Setor("Setor_SantuarioDeYhtill", new Vector2(-15.25f, 10.75f), new Vector2(12.5f, 9.5f),
                0.02f, 0.12f, "Calma sobrenatural (90% vis.) — a tempestade para nas bordas"),

            new Setor("Setor_DesertoCentral", new Vector2(0f, 1f), new Vector2(18f, 18f),
                0.45f, 0.65f, "Forte (~45% vis.) — passos silenciosos: o alívio de stealth do percurso"),

            new Setor("Setor_PortoesDasRuinas", new Vector2(0f, 12.75f), new Vector2(18f, 5.5f),
                0.00f, 0.10f, "Calmaria ominosa (95% vis.) — a luta do Byakhee exige enxergar"),

            new Setor("Setor_LesteTemploSerpente", new Vector2(15.25f, 3.75f), new Vector2(12.5f, 23.5f),
                0.78f, 0.95f, "Tempestade máxima (~15% vis.) — navegação às cegas"),
        };

        [MenuItem("Tools/FavelaAmarela/Montar setores de tempestade do Deserto")]
        public static void Executar()
        {
            // Salva sem perguntar. `SaveCurrentModifiedScenesIfUserWantsTo` abre um
            // diálogo MODAL, e uma ferramenta disparada pela ponte MCP trava a Unity
            // inteira esperando um clique que ninguém vê.
            var cenaAtiva = EditorSceneManager.GetActiveScene();
            if (cenaAtiva.isDirty && !string.IsNullOrEmpty(cenaAtiva.path))
                EditorSceneManager.SaveScene(cenaAtiva);

            string cenaOriginal = cenaAtiva.path;
            var cena = EditorSceneManager.OpenScene(CenaDeserto, OpenSceneMode.Single);

            var ambiente = Object.FindAnyObjectByType<TempestadeAmbiente>(FindObjectsInactive.Include);
            if (ambiente == null)
            {
                Debug.LogError("[Setores] Não há TempestadeAmbiente na cena — rode antes " +
                               "'Tools/FavelaAmarela/Montar Deserto de Hali'. Nada feito.");
                return;
            }

            var raiz = GameObject.Find(NomeRaiz);
            if (raiz == null)
            {
                raiz = new GameObject(NomeRaiz);
                Undo.RegisterCreatedObjectUndo(raiz, "Criar raiz dos setores");
            }

            foreach (var s in Setores)
                MontarSetor(s, raiz.transform, ambiente);

            EditorSceneManager.MarkSceneDirty(cena);
            EditorSceneManager.SaveScene(cena);

            if (!string.IsNullOrEmpty(cenaOriginal) && cenaOriginal != CenaDeserto)
                EditorSceneManager.OpenScene(cenaOriginal, OpenSceneMode.Single);

            Debug.Log($"[Setores] {Setores.Length} setores de tempestade montados no Deserto.");
        }

        private static void MontarSetor(Setor s, Transform raiz, TempestadeAmbiente ambiente)
        {
            var t = raiz.Find(s.Nome);
            GameObject go;
            if (t != null)
            {
                go = t.gameObject;
            }
            else
            {
                go = new GameObject(s.Nome);
                Undo.RegisterCreatedObjectUndo(go, "Criar setor de tempestade");
                go.transform.SetParent(raiz, false);
            }

            go.transform.position = s.Centro;

            var col = go.GetComponent<BoxCollider2D>();
            if (col == null) col = go.AddComponent<BoxCollider2D>();
            col.isTrigger = true;   // é volume de ambiente, não parede
            col.size = s.Tamanho;
            col.offset = Vector2.zero;

            var trigger = go.GetComponent<TempestadeZonaTrigger>();
            if (trigger == null) trigger = go.AddComponent<TempestadeZonaTrigger>();

            var so = new SerializedObject(trigger);
            so.FindProperty("tempestadeAmbiente").objectReferenceValue = ambiente;
            so.FindProperty("minimo").floatValue = s.Min;
            so.FindProperty("maximo").floatValue = s.Max;
            so.ApplyModifiedPropertiesWithoutUndo();

            EditorUtility.SetDirty(go);
            Debug.Log($"[Setores] {s.Nome}: faixa {s.Min:0.00}–{s.Max:0.00} — {s.Porque}", go);
        }
    }
}
