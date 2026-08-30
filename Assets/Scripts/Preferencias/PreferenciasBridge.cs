using System;
using System.IO;
using UnityEngine;
using FavelaAmarela.Core.Preferencias;

namespace FavelaAmarela.Runtime.Preferencias
{
    /// <summary>
    /// Adaptador das <see cref="PreferenciasDoJogador"/>: persiste em disco e <b>aplica no
    /// motor</b>.
    ///
    /// <para><b>Nasce sozinha</b>, como <c>ProgressionBridge</c>, <c>ItemDatabase</c> e o
    /// <c>HUDController</c>. Preferência que dependesse de um componente posto em cada cena
    /// valeria só nas cenas onde alguém lembrou — e o volume voltaria ao padrão ao trocar de
    /// fase, em silêncio.</para>
    ///
    /// <para><b>Arquivo próprio, separado do save.</b> <c>preferencias.json</c> ao lado do save,
    /// não dentro dele: começar uma peregrinação nova apaga o progresso e <b>não pode</b> zerar
    /// o volume que a pessoa ajustou.</para>
    /// </summary>
    [AddComponentMenu("Favela Amarela/Preferências/Bridge de Preferências")]
    public sealed class PreferenciasBridge : MonoBehaviour
    {
        private const string NomeDoArquivo = "preferencias.json";

        private static PreferenciasBridge _instancia;

        /// <summary>Instância única. Nula fora de Play — todo chamador deve tolerar isso.</summary>
        public static PreferenciasBridge Instancia => _instancia;

        /// <summary>As preferências correntes. Nunca nula depois do <c>Awake</c>.</summary>
        public PreferenciasDoJogador Preferencias { get; private set; }

        /// <summary>
        /// Nasce antes de qualquer cena — inclusive o menu principal, que é justamente onde o
        /// jogador vai procurar as opções.
        /// </summary>
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        public static void GarantirInstancia()
        {
            if (_instancia != null) return;

            var go = new GameObject("PreferenciasBridge (automático)");
            go.AddComponent<PreferenciasBridge>();   // o Awake faz o DontDestroyOnLoad
        }

        private void Awake()
        {
            if (_instancia != null && _instancia != this) { Destroy(gameObject); return; }

            _instancia = this;
            DontDestroyOnLoad(gameObject);

            Preferencias = new PreferenciasDoJogador();
            Carregar();

            Preferencias.OnMudou += Aplicar;
            Aplicar();   // o disco já foi lido; agora o motor obedece
        }

        private void OnDestroy()
        {
            if (Preferencias != null) Preferencias.OnMudou -= Aplicar;
            if (_instancia == this) _instancia = null;
        }

        private string Caminho => Path.Combine(Application.persistentDataPath, NomeDoArquivo);

        // ── Aplicar no motor ──────────────────────────────────────────────────

        /// <summary>
        /// Traduz a preferência em chamadas de motor, e grava. É o <b>único</b> lugar do projeto
        /// que toca <c>QualitySettings.vSyncCount</c>, <c>Application.targetFrameRate</c> e
        /// <c>Screen.fullScreen</c> — espalhá-los produziria o mesmo desacordo silencioso que os
        /// dois números de dano por inimigo produziam.
        /// </summary>
        private void Aplicar()
        {
            // A ordem importa: com vSyncCount != 0 a Unity IGNORA o targetFrameRate (doc da
            // 6.4). Escrever os dois sempre, na ordem certa, evita que o motor fique num estado
            // que a interface não descreve.
            QualitySettings.vSyncCount = Preferencias.SincronizacaoVertical ? 1 : 0;
            Application.targetFrameRate = Preferencias.LimiteEfetivoDeQuadros;

            if (Screen.fullScreen != Preferencias.TelaCheia)
                Screen.fullScreen = Preferencias.TelaCheia;

            Salvar();
        }

        // ── Disco ─────────────────────────────────────────────────────────────

        [Serializable]
        private sealed class Arquivo
        {
            public float volumeGeral = 0.8f;
            public bool telaCheia = true;
            public bool sincronizacaoVertical = true;
            public int limiteDeQuadros = PreferenciasDoJogador.SemLimiteDeQuadros;
        }

        private void Carregar()
        {
            if (!File.Exists(Caminho)) return;   // primeira execução: padrões de fábrica

            try
            {
                var dados = JsonUtility.FromJson<Arquivo>(File.ReadAllText(Caminho));
                if (dados == null) return;

                // Num evento só: quatro chamadas separadas reconfigurariam o motor quatro vezes
                // e a janela piscaria no arranque.
                Preferencias.Restaurar(dados.volumeGeral, dados.telaCheia,
                                       dados.sincronizacaoVertical, dados.limiteDeQuadros);
            }
            catch (Exception e)
            {
                // Preferência corrompida não pode impedir o jogo de abrir. Cai no padrão e
                // avisa uma vez -- o arquivo será reescrito no primeiro Aplicar.
                Debug.LogWarning($"[Preferencias] '{NomeDoArquivo}' ilegível ({e.Message}); " +
                                 "voltando ao padrão.", this);
            }
        }

        private void Salvar()
        {
            try
            {
                File.WriteAllText(Caminho, JsonUtility.ToJson(new Arquivo
                {
                    volumeGeral = Preferencias.VolumeGeral,
                    telaCheia = Preferencias.TelaCheia,
                    sincronizacaoVertical = Preferencias.SincronizacaoVertical,
                    limiteDeQuadros = Preferencias.LimiteDeQuadros,
                }, prettyPrint: true));
            }
            catch (IOException e)
            {
                Debug.LogWarning($"[Preferencias] não consegui gravar '{NomeDoArquivo}': " +
                                 e.Message, this);
            }
        }
    }
}
