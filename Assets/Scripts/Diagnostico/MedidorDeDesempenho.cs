// Mesma guarda do ConsoleDeCarcosa: numa build de RELEASE este arquivo compila para NADA.
#if UNITY_EDITOR || DEVELOPMENT_BUILD

using System;
using Unity.Profiling;
using UnityEngine;

namespace FavelaAmarela.Runtime.Diagnostico
{
    /// <summary>
    /// Mede desempenho <b>enquanto o jogo é jogado</b> — e não em análise estática.
    ///
    /// <para><b>A lacuna que isto fecha (2026-08-30).</b> A auditoria varreu 51 métodos de
    /// <c>Update</c> e não achou alocação nem busca em hot path. Isso elimina <i>uma classe</i>
    /// de problema, não responde desempenho: não diz quantos objetos existem, quantos colisores
    /// a física resolve, quanto a GPU trabalha, nem — o que mais importa — <b>quão ruim é o pior
    /// quadro</b>.</para>
    ///
    /// <para><b>Por que milissegundos e não FPS.</b> FPS engana porque não é linear: cair de 120
    /// para 60 custa 8 ms; de 60 para 30 custa 16,7 ms. E a <b>média</b> esconde justamente o que
    /// o jogador sente — um jogo a "60 de média" que trava 200 ms quando um Cultista morre parece
    /// quebrado. Por isso o painel mostra <b>mediana, pior quadro e a fração acima de
    /// 16,7 ms</b>.</para>
    ///
    /// <para><b>Não mede com o jogo parado</b>, e é decisivo: o console congela o tempo ao abrir.
    /// Medir ali seria medir uma tela parada. A janela deslizante guarda os últimos segundos de
    /// jogo <i>de verdade</i>, então abrir o console mostra o que acabou de acontecer.</para>
    ///
    /// <para><b>O tempo de quadro não depende do Profiler.</b> Vem de
    /// <c>Time.unscaledDeltaTime</c>, que sempre funciona. Os contadores de desenho e de lixo
    /// vêm do <c>ProfilerRecorder</c> — que, pela documentação da 6.4, funciona <i>"in Editor and
    /// Player builds, including Release Players"</i> — e aparecem como <b>—</b> quando o nome do
    /// contador não resolve, em vez de mentir um zero.</para>
    /// </summary>
    public sealed class MedidorDeDesempenho : MonoBehaviour
    {
        /// <summary>Quadros guardados: ~5 s a 60 Hz. Curto o bastante para refletir "agora".</summary>
        private const int Janela = 300;

        /// <summary>O orçamento de 60 quadros por segundo, em milissegundos.</summary>
        public const float OrcamentoDe60 = 1000f / 60f;

        private static MedidorDeDesempenho _instancia;

        /// <summary>Instância única. Nula fora de Play.</summary>
        public static MedidorDeDesempenho Instancia => _instancia;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        public static void GarantirInstancia()
        {
            if (_instancia != null) return;

            var go = new GameObject("MedidorDeDesempenho (automático)");
            go.AddComponent<MedidorDeDesempenho>();
        }

        private readonly float[] _quadros = new float[Janela];
        private int _escritos;
        private int _proximo;

        private ProfilerRecorder _desenhos;
        private ProfilerRecorder _lotes;
        private ProfilerRecorder _lixo;

        private void Awake()
        {
            if (_instancia != null && _instancia != this) { Destroy(gameObject); return; }

            _instancia = this;
            DontDestroyOnLoad(gameObject);

            // Nomes de contador variam entre versões; cada um é conferido por Valid antes de
            // ser lido, e o painel mostra "—" quando não resolve.
            _desenhos = ProfilerRecorder.StartNew(ProfilerCategory.Render, "Draw Calls Count");
            _lotes = ProfilerRecorder.StartNew(ProfilerCategory.Render, "Batches Count");
            _lixo = ProfilerRecorder.StartNew(ProfilerCategory.Memory, "GC Allocated In Frame");
        }

        private void OnDestroy()
        {
            // ProfilerRecorder aloca recurso não gerenciado; não descartar vaza a cada troca
            // de cena que recriasse este objeto.
            if (_desenhos.Valid) _desenhos.Dispose();
            if (_lotes.Valid) _lotes.Dispose();
            if (_lixo.Valid) _lixo.Dispose();

            if (_instancia == this) _instancia = null;
        }

        private void Update()
        {
            // Jogo parado (console aberto, menu de pausa) não é jogo: medir ali encheria a
            // janela de quadros vazios e apagaria justamente o trecho que interessa.
            if (Time.timeScale <= 0f) return;

            _quadros[_proximo] = Time.unscaledDeltaTime * 1000f;
            _proximo = (_proximo + 1) % Janela;
            if (_escritos < Janela) _escritos++;
        }

        // ── O que o painel lê ─────────────────────────────────────────────────

        /// <summary>Quantos quadros a janela já tem. Zero = nada medido ainda.</summary>
        public int QuadrosMedidos => _escritos;

        /// <summary>Milissegundos do quadro mais recente.</summary>
        public float UltimoQuadroMs =>
            _escritos == 0 ? 0f : _quadros[(_proximo - 1 + Janela) % Janela];

        /// <summary>
        /// <b>Mediana</b>, e não média: uma única travada de 300 ms move a média e não move a
        /// mediana — que é o ponto, porque o pico já é reportado à parte.
        /// </summary>
        public float MedianaMs => Percentil(0.50f);

        /// <summary>O quadro no 99º percentil — o "pior caso" que o jogador sente de verdade.</summary>
        public float PiorMs => Percentil(0.99f);

        /// <summary>Fração dos quadros que estourou o orçamento de 60 por segundo.</summary>
        public float FracaoAcimaDoOrcamento
        {
            get
            {
                if (_escritos == 0) return 0f;

                int acima = 0;
                for (int i = 0; i < _escritos; i++)
                    if (_quadros[i] > OrcamentoDe60) acima++;

                return acima / (float)_escritos;
            }
        }

        /// <summary>Quadros por segundo derivados da mediana. Só para leitura humana.</summary>
        public float QuadrosPorSegundo => MedianaMs > 0.001f ? 1000f / MedianaMs : 0f;

        /// <summary>Chamadas de desenho, ou <c>-1</c> se o contador não existe nesta versão.</summary>
        public long Desenhos => _desenhos.Valid ? _desenhos.LastValue : -1;

        /// <summary>Lotes de renderização, ou <c>-1</c>.</summary>
        public long Lotes => _lotes.Valid ? _lotes.LastValue : -1;

        /// <summary>
        /// Bytes alocados no último quadro, ou <c>-1</c>. <b>Em regime isto deveria ser zero</b>:
        /// lixo por quadro é o que produz as travadas do coletor.
        /// </summary>
        public long LixoNoQuadro => _lixo.Valid ? _lixo.LastValue : -1;

        /// <summary>Esquece a janela — para medir um trecho específico do zero.</summary>
        public void Zerar()
        {
            Array.Clear(_quadros, 0, _quadros.Length);
            _escritos = 0;
            _proximo = 0;
        }

        /// <summary>
        /// Percentil por cópia ordenada. Aloca — e por isso <b>nunca</b> é chamado do
        /// <c>Update</c>, só quando o painel está aberto, que é quando o jogo já está parado.
        /// </summary>
        private float Percentil(float fracao)
        {
            if (_escritos == 0) return 0f;

            var copia = new float[_escritos];
            Array.Copy(_quadros, copia, _escritos);
            Array.Sort(copia);

            int indice = Mathf.Clamp(Mathf.RoundToInt(fracao * (_escritos - 1)), 0, _escritos - 1);
            return copia[indice];
        }
    }
}

#endif
