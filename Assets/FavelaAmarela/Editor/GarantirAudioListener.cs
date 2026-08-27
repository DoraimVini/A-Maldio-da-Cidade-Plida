using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace FavelaAmarela.EditorTools
{
    /// <summary>
    /// Garante um <c>AudioListener</c> — e <b>um só</b> — em cada cena do Build Settings.
    ///
    /// <para><b>O defeito (2026-08-27).</b> Quatro cenas não tinham nenhum, incluindo
    /// <c>Deserto_Hali</c>, que é a <b>Fase 1 do Vertical Slice</b>. Elas têm
    /// <c>MixerDeAudio</c> e <c>AudioDeStealth</c> montados: tudo toca e <b>nada é ouvido</b>.
    /// A própria Unity vinha gritando isso no console — <i>"There are no audio listeners in the
    /// scene"</i>, repetido a cada som — e ninguém tinha olhado.</para>
    ///
    /// <para><b>Onde ele vai, e por quê.</b> Nas cenas que já têm um, ele está na
    /// <c>Main Camera</c>. Mantemos a convenção, mas com uma ressalva medida: a câmera fica em
    /// <c>z = −10</c> e o rolloff do <c>MixerDeAudio</c> é linear de 3 a 24. Isso significa que
    /// um som <b>exatamente sob o jogador</b> está a 10 unidades do ouvinte e toca a
    /// <b>0,667</b> do volume — <i>nenhum som do jogo toca cheio, nunca</i> —, o raio audível no
    /// plano cai de 24 para 21,8, e o pan estéreo comprime de 90° para 26,6°.</para>
    ///
    /// <para>Corrigir isso é mexer no <c>MixerDeAudio</c> ou na câmera, e muda como o jogo
    /// <i>soa</i> — fica registrado aqui e é decisão do Vini. <b>Ter um ouvinte é
    /// pré-requisito para essa conversa sequer fazer sentido.</b></para>
    ///
    /// <para><b>Dois listeners é pior que nenhum:</b> a Unity avisa e desliga um deles
    /// arbitrariamente. Por isso a ferramenta também remove excedentes.</para>
    /// </summary>
    public static class GarantirAudioListener
    {
        [MenuItem("Tools/FavelaAmarela/Áudio: garantir um AudioListener por cena")]
        public static void Executar()
        {
            var resumo = new List<string>();

            foreach (var caminho in CenasDoBuild())
            {
                if (!File.Exists(caminho))
                {
                    resumo.Add($"{Path.GetFileName(caminho)}: ausente no disco");
                    continue;
                }

                var cena = EditorSceneManager.OpenScene(caminho, OpenSceneMode.Single);
                resumo.Add(Ajustar(caminho, cena));
            }

            AssetDatabase.SaveAssets();
            Debug.Log("[AudioListener] Concluído:\n  " + string.Join("\n  ", resumo));
        }

        /// <summary>
        /// As cenas vêm do <b>Build Settings</b>, não de uma lista escrita à mão. Cena nova
        /// entra sozinha — é a nona vez que este projeto troca lista por derivação.
        /// </summary>
        private static IEnumerable<string> CenasDoBuild() =>
            EditorBuildSettings.scenes.Where(s => s.enabled).Select(s => s.path);

        private static string Ajustar(string caminho, UnityEngine.SceneManagement.Scene cena)
        {
            string nome = Path.GetFileName(caminho);

            var listeners = Object.FindObjectsByType<AudioListener>(
                FindObjectsInactive.Include, FindObjectsSortMode.None);

            if (listeners.Length == 1) return $"{nome}: já tinha 1 (em '{listeners[0].name}')";

            if (listeners.Length > 1)
            {
                // A Unity desliga um arbitrariamente e avisa. Fica o da câmera, se houver.
                var manter = listeners.FirstOrDefault(l => l.GetComponent<Camera>() != null)
                             ?? listeners[0];

                int removidos = 0;
                foreach (var l in listeners)
                {
                    if (l == manter) continue;
                    Object.DestroyImmediate(l);
                    removidos++;
                }

                Salvar(cena);
                return $"{nome}: {removidos} listener(s) excedente(s) removido(s), " +
                       $"ficou o de '{manter.name}'";
            }

            var camera = Object.FindAnyObjectByType<Camera>(FindObjectsInactive.Include);
            if (camera == null)
                return $"{nome}: SEM CÂMERA e sem listener — nada a fazer sem decidir onde pôr";

            camera.gameObject.AddComponent<AudioListener>();
            Salvar(cena);

            return $"{nome}: listener CRIADO em '{camera.name}' — a cena era muda";
        }

        private static void Salvar(UnityEngine.SceneManagement.Scene cena)
        {
            EditorSceneManager.MarkSceneDirty(cena);
            if (!EditorSceneManager.SaveScene(cena))
                Debug.LogError($"[AudioListener] SaveScene RECUSOU '{cena.path}'.");
        }
    }
}
