using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using FavelaAmarela.Runtime.Persistencia;

namespace FavelaAmarela.EditorTools
{
    /// <summary>
    /// Ferramenta de Editor. Carimba uma chave de persistência em todo
    /// <see cref="ObjetoPersistente"/> da cena aberta que ainda não tenha uma, e salva.
    ///
    /// <para>Existe porque a geração automática (<c>OnValidate</c>) não pode marcar a cena
    /// como suja sozinha — <c>EditorUtility.SetDirty</c> mora no namespace
    /// <c>UnityEditor</c>, que o assembly de Runtime não referencia. Aqui, no assembly de
    /// Editor, isso é seguro.</para>
    ///
    /// <para><b>Nunca sobrescreve chave existente</b>: regenerar uma chave é justamente o
    /// que transformaria o save do objeto em lixo órfão.</para>
    /// </summary>
    public static class GerarChavesDePersistencia
    {
        [MenuItem("Tools/FavelaAmarela/Gerar chaves de persistência")]
        public static void Executar()
        {
            var objetos = Object.FindObjectsByType<ObjetoPersistente>(
                FindObjectsInactive.Include);

            if (objetos.Length == 0)
            {
                Debug.Log("[GerarChavesDePersistencia] Nenhum ObjetoPersistente na cena.");
                return;
            }

            int gerados = 0;
            foreach (var obj in objetos)
            {
                if (!obj.GarantirChave()) continue;

                EditorUtility.SetDirty(obj);
                gerados++;
                Debug.Log($"[GerarChavesDePersistencia] '{obj.name}' → {obj.Chave}", obj);
            }

            if (gerados == 0)
            {
                Debug.Log($"[GerarChavesDePersistencia] Os {objetos.Length} objetos já tinham chave.");
                return;
            }

            var cena = EditorSceneManager.GetActiveScene();
            EditorSceneManager.MarkSceneDirty(cena);
            EditorSceneManager.SaveScene(cena);

            Debug.Log($"[GerarChavesDePersistencia] {gerados} chave(s) geradas e cena salva.");
        }
    }
}

