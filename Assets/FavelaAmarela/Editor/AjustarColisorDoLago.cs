using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace FavelaAmarela.EditorTools
{
    /// <summary>
    /// Utilitário de Editor (uso único): troca o <c>BoxCollider2D</c> do Lago Negro de Hali por
    /// um <c>PolygonCollider2D</c> que acompanha o contorno real da água.
    ///
    /// <para><b>Por que existe:</b> a sprite <c>Entrada_LagoNegroDeHali.png</c> é uma borda
    /// rochosa isométrica em losango com a água afundada no meio e um cais de madeira saindo
    /// por cima dela. Um <c>BoxCollider2D</c> não acompanha esse contorno — cobre a rocha
    /// junto com a água, ou fica pequeno demais e deixa água sem colisor. O Vini tentou ajustar
    /// à mão pelo Inspector (o offset/size que ficaram no diff da cena, com incrementos de
    /// centésimos) e não conseguiu — era o formato de collider errado para a forma, não um
    /// valor errado.</para>
    ///
    /// <para><b>Como os pontos foram gerados:</b> segmentação por pixel da sprite (região escura
    /// e opaca = água), limpeza por abertura morfológica + maior componente conexo (remove as
    /// pontas rochosas do topo, que têm luminância parecida com a água) e simplificação do
    /// contorno (<c>cv2.approxPolyDP</c>) para 22 pontos. Convertidos para o espaço local do
    /// sprite usando o pivô <b>Bottom</b> (alignment 7 no import) e <c>spritePixelsToUnits: 32</c>
    /// — a mesma convenção que já produzia <c>oldSize: {x: 4, y: 2.90625}</c> no
    /// <c>BoxCollider2D</c> antigo (128×93px ÷ 32), confirmando a conversão. O recorte do cais
    /// fica de fora do polígono de propósito: ele é madeira sobre a água, caminhável.</para>
    ///
    /// <para>Ferramenta de uso único — os pontos são fixos para esta sprite. Não generaliza para
    /// outros props.</para>
    /// </summary>
    public static class AjustarColisorDoLago
    {
        private const string Cena = "Assets/Scenes/Deserto_Hali.unity";
        private const string NomeDoObjeto = "Lago_De_Hali";

        // Pontos em espaço local do sprite (pivô Bottom, PPU 32), extraídos do contorno real da
        // água. Ordem preserva o sentido do contorno original — Unity não exige winding
        // específico para PolygonCollider2D de caminho único.
        private static readonly Vector2[] PontosDaAgua =
        {
            new Vector2(-1.6875f, 1.625f),
            new Vector2(-1.6875f, 1.4688f),
            new Vector2(-0.9375f, 1.0625f),
            new Vector2(-0.6562f, 1.0625f),
            new Vector2(-0.5f,    0.9375f),
            new Vector2(-0.4375f, 0.7188f),
            new Vector2(-0.1562f, 0.6875f),
            new Vector2(0.4375f,  0.7812f),
            new Vector2(0.375f,   1.2188f),
            new Vector2(0.5938f,  1.2812f),
            new Vector2(0.6562f,  1.4062f),
            new Vector2(0.9688f,  1.3125f),
            new Vector2(1.3438f,  1.4062f),
            new Vector2(1.4062f,  1.7812f),
            new Vector2(1.0625f,  1.875f),
            new Vector2(0.9375f,  2.0f),
            new Vector2(0.6562f,  2.0312f),
            new Vector2(0.4375f,  2.25f),
            new Vector2(-0.4688f, 2.25f),
            new Vector2(-0.7188f, 2.0312f),
            new Vector2(-1.1562f, 1.9375f),
            new Vector2(-1.3125f, 1.7812f),
        };

        [MenuItem("Tools/FavelaAmarela/Ajustar Colisor do Lago de Hali")]
        public static void Ajustar()
        {
            var cena = EditorSceneManager.OpenScene(Cena, OpenSceneMode.Single);

            var lago = GameObject.Find(NomeDoObjeto);
            if (lago == null)
            {
                Debug.LogError($"[AjustarColisorDoLago] '{NomeDoObjeto}' não encontrado em {Cena}.");
                return;
            }

            var boxAntigo = lago.GetComponent<BoxCollider2D>();
            if (boxAntigo != null)
            {
                Undo.DestroyObjectImmediate(boxAntigo);
            }

            var poligono = lago.GetComponent<PolygonCollider2D>();
            if (poligono == null)
                poligono = Undo.AddComponent<PolygonCollider2D>(lago);

            poligono.pathCount = 1;
            poligono.SetPath(0, PontosDaAgua);
            poligono.offset = Vector2.zero;
            poligono.isTrigger = false;

            EditorUtility.SetDirty(lago);
            EditorSceneManager.MarkSceneDirty(cena);
            EditorSceneManager.SaveScene(cena);

            Debug.Log($"[AjustarColisorDoLago] '{NomeDoObjeto}': BoxCollider2D → PolygonCollider2D " +
                      $"com {PontosDaAgua.Length} pontos. Cena salva.");
        }
    }
}
