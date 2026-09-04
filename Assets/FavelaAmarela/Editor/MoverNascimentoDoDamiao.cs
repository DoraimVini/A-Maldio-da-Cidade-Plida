using System.Linq;
using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace FavelaAmarela.EditorTools
{
    /// <summary>
    /// Move o nascimento do Damião no Deserto de Hali para junto do <c>Refugio_Entrada</c>,
    /// encostado na parede sul.
    ///
    /// <para><b>Por que ali.</b> O Vini pediu <i>"o ponto mais próximo da parede no Deserto de
    /// Hali, perto do refúgio de luz"</i>. Dos três refúgios da cena, o
    /// <c>Refugio_Entrada</c> (-24, -22) é o único perto de uma parede: 9 unidades do
    /// <c>Limite_Sul</c> (y = -31), contra 19 do <c>Limite_Oeste</c>. Os outros dois
    /// (Santuário em -26, 18 e Portões em -4, 26) ficam no miolo do mapa.</para>
    ///
    /// <para><b>Por que uma ferramenta e não edição de YAML.</b> O Damião é um
    /// <c>PrefabInstance</c>, e posição de instância vive em <c>m_Modifications</c> com
    /// <c>propertyPath</c> separado por eixo — não num <c>m_LocalPosition</c> de
    /// <c>Transform</c>. Um <c>grep</c> por <c>m_LocalPosition</c> na cena não acha o objeto, e
    /// escrever ali por regex é como se colam três componentes no GameObject errado.</para>
    ///
    /// <para><b>A folga da parede é deliberada.</b> O <c>Limite_Sul</c> é uma caixa de altura 1
    /// centrada em y = -31, então a face interna dela está em y = -30,5. Nascer colado geraria
    /// sobreposição no primeiro <c>FixedUpdate</c> e a física empurraria o Damião — o
    /// <see cref="FolgaDaParede"/> existe para isso.</para>
    /// </summary>
    public static class MoverNascimentoDoDamiao
    {
        private const string Cena = "Assets/Scenes/Deserto_Hali.unity";
        private const string Jogador = "Player_Damiao";
        private const string Refugio = "Refugio_Entrada";
        private const string Parede = "Limite_Sul";

        /// <summary>Distância entre o Damião e a face interna da parede, em unidades.</summary>
        private const float FolgaDaParede = 2.5f;

        [MenuItem("Tools/FavelaAmarela/Cena: nascimento do Damião no Deserto")]
        public static void Executar()
        {
            if (!Application.isBatchMode &&
                !EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
            {
                Debug.Log("[NascimentoDoDamiao] Cancelado — havia cena modificada por salvar.");
                return;
            }

            var cena = EditorSceneManager.OpenScene(Cena, OpenSceneMode.Single);
            var tudo = cena.GetRootGameObjects()
                .SelectMany(r => r.GetComponentsInChildren<Transform>(true))
                .ToList();

            Transform Achar(string nome)
            {
                var t = tudo.FirstOrDefault(x => x.name == nome);
                if (t == null)
                    Debug.LogError($"[NascimentoDoDamiao] Não achei '{nome}' em {Cena}. " +
                                   "Nada foi movido.");
                return t;
            }

            var jogador = Achar(Jogador);
            var refugio = Achar(Refugio);
            var parede = Achar(Parede);
            if (jogador == null || refugio == null || parede == null) return;

            // A face INTERNA da parede, e não o centro dela: o colisor tem altura.
            var colisorDaParede = parede.GetComponent<Collider2D>();
            float faceInterna = colisorDaParede != null
                ? colisorDaParede.bounds.max.y
                : parede.position.y + 0.5f;

            var antes = jogador.position;
            var depois = new Vector3(refugio.position.x,
                                     faceInterna + FolgaDaParede,
                                     antes.z);

            jogador.position = depois;

            var log = new StringBuilder();
            log.AppendLine("[NascimentoDoDamiao]");
            log.AppendLine($"   {Refugio}: {refugio.position}");
            log.AppendLine($"   {Parede}: centro y={parede.position.y:0.##}, " +
                           $"face interna y={faceInterna:0.##}");
            log.AppendLine($"   Damião: {antes} -> {depois}");
            log.AppendLine($"   distância até o refúgio: " +
                           $"{Vector3.Distance(depois, refugio.position):0.##}");
            log.AppendLine($"   distância até a face da parede: {FolgaDaParede:0.##}");

            EditorSceneManager.MarkSceneDirty(cena);
            EditorSceneManager.SaveScene(cena);
            log.AppendLine($"   cena salva: {Cena}");

            Debug.Log(log.ToString());
        }
    }
}
