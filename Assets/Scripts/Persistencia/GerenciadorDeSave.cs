using System.Collections.Generic;
using System.IO;
using UnityEngine;
using FavelaAmarela.Core.Persistencia;

namespace FavelaAmarela.Runtime.Persistencia
{
    /// <summary>
    /// Camada Runtime (MonoBehaviour). <b>Save Manager central</b>: mantém o
    /// <see cref="RegistroDeSave"/> da partida, coleta o estado de todos os
    /// <see cref="IPersistente"/> registrados e grava tudo num único arquivo JSON.
    ///
    /// <para><b>Sobrevive à troca de cena</b> (<c>DontDestroyOnLoad</c>) — é o que resolve o
    /// problema concreto de a arma da Tumba sumir ao sair da dungeon: o registro continua em
    /// memória enquanto as cenas vão e voltam.</para>
    ///
    /// <para><b>JSON, nunca <c>PlayerPrefs</c></b> (Regra de Ouro 9): <c>PlayerPrefs</c>
    /// existe para configuração de usuário (volume, resolução), não para progresso — é
    /// lento para volume, não guarda estrutura e é trivial de adulterar.</para>
    /// </summary>
    [DisallowMultipleComponent]
    [AddComponentMenu("Favela Amarela/Persistência/Gerenciador de Save")]
    public sealed class GerenciadorDeSave : MonoBehaviour
    {
        [Tooltip("Nome do arquivo de save dentro de Application.persistentDataPath.")]
        [SerializeField] private string nomeDoArquivo = "partida.json";

        [Tooltip("Grava em disco automaticamente a cada captura. Desligue para salvar só " +
                 "em pontos definidos (Refúgios de Luz).")]
        [SerializeField] private bool gravarEmDiscoAoCapturar = false;

        [Tooltip("Lê o save do disco ao iniciar. Desligue só para depurar uma partida limpa " +
                 "sem apagar o arquivo.")]
        [SerializeField] private bool carregarDoDiscoAoIniciar = true;

        private static GerenciadorDeSave _instancia;

        private readonly List<IPersistente> _registrados = new List<IPersistente>();
        private RegistroDeSave _registro = new RegistroDeSave();

        /// <summary>Instância viva, ou null se nenhuma cena a criou ainda.</summary>
        public static GerenciadorDeSave Instancia => _instancia;

        /// <summary>Registro em memória da partida corrente.</summary>
        public RegistroDeSave Registro => _registro;

        private string CaminhoDoArquivo => Path.Combine(Application.persistentDataPath, nomeDoArquivo);

        /// <summary>
        /// Cria o gerenciador automaticamente antes de qualquer cena carregar.
        ///
        /// <para><b>Por que não depender de um objeto posto na cena:</b> exigir que alguém
        /// lembre de arrastar o componente faz uma cena nova nascer <b>quebrada por
        /// padrão</b> — nada é capturado e nenhum erro aparece. Aconteceu de verdade em
        /// 2026-07-31: a persistência foi instalada na Tumba e o Deserto ficou sem, então a
        /// arma continuava sumindo. Com <c>BeforeSceneLoad</c>, ele existe sempre, em toda
        /// cena, inclusive nas que ainda não foram criadas.</para>
        ///
        /// <para>Um gerenciador colocado à mão numa cena continua funcionando: o guarda de
        /// instância única no <c>Awake</c> faz o duplicado se destruir.</para>
        /// </summary>
        /// <summary>
        /// Garante que o gerenciador exista. Idempotente — a <c>RaizPersistente</c> também chama.
        /// </summary>
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        public static void GarantirInstancia()
        {
            if (_instancia != null) return;

            var go = new GameObject("GerenciadorDeSave (automático)");
            go.AddComponent<GerenciadorDeSave>(); // o Awake abaixo faz o DontDestroyOnLoad
        }

        private void Awake()
        {
            if (_instancia != null && _instancia != this)
            {
                // Já existe um (o automático, ou o de uma cena anterior): este é redundante.
                Destroy(gameObject);
                return;
            }

            _instancia = this;
            DontDestroyOnLoad(gameObject);

            // Lê o save do disco AQUI, no Awake, e não sob demanda.
            //
            // Bug que motivou (auditoria 2026-08-11): CarregarDoDisco() existia desde o
            // início e NUNCA era chamado por ninguém. O jogo gravava o arquivo (Refúgio de
            // Luz, troca de cena) e nunca o lia de volta — trocar de cena funcionava só
            // porque o registro vive em memória neste objeto DontDestroyOnLoad, mas fechar
            // e reabrir o jogo perdia tudo, sem um aviso sequer.
            //
            // O Awake é o momento certo: roda antes do Start dos objetos de cena, que é
            // onde cada IPersistente se registra e consulta o registro.
            if (carregarDoDiscoAoIniciar) CarregarDoDisco();
        }

        private void OnDestroy()
        {
            if (_instancia == this) _instancia = null;
        }

        /// <summary>
        /// Inscreve um objeto para ter estado salvo. Chamado pelo próprio objeto no
        /// <c>Start</c> (não no <c>Awake</c>: o gerenciador pode ainda não existir).
        /// Idempotente.
        /// </summary>
        public void Registrar(IPersistente persistente)
        {
            if (persistente == null) return;
            if (string.IsNullOrWhiteSpace(persistente.ChaveDePersistencia))
            {
                Debug.LogError($"[GerenciadorDeSave] '{persistente.GetType().Name}' tentou se " +
                               "registrar sem chave — não será salvo.");
                return;
            }

            if (_registrados.Contains(persistente)) return;
            _registrados.Add(persistente);
        }

        /// <summary>Cancela a inscrição (o objeto foi destruído ou a cena descarregou).</summary>
        public void Desregistrar(IPersistente persistente)
        {
            if (persistente == null) return;
            _registrados.Remove(persistente);
        }

        /// <summary>
        /// Pede o estado de todos os inscritos e o grava no registro em memória. Chamado
        /// antes de trocar de cena e ao salvar de verdade.
        /// </summary>
        public void CapturarTudo()
        {
            for (int i = 0; i < _registrados.Count; i++)
            {
                var p = _registrados[i];
                if (p == null) continue;
                _registro.Definir(p.ChaveDePersistencia, p.CapturarEstado());
            }

            // Onde o jogador estava. Não vem de um IPersistente porque não pertence a objeto
            // nenhum da cena — é fato sobre a partida, e é o que o "Continuar" do menu lê
            // para saber para onde voltar.
            var cena = UnityEngine.SceneManagement.SceneManager.GetActiveScene();
            if (cena.IsValid() && !string.IsNullOrEmpty(cena.name))
                _registro.Definir(ChavesDeSave.CenaAtual, cena.name);

            if (gravarEmDiscoAoCapturar) GravarEmDisco();
        }

        /// <summary>
        /// Cena onde a partida salva parou, ou <c>null</c> se não houver save utilizável.
        /// O menu principal usa isto no "Continuar".
        /// </summary>
        public string CenaSalva => _registro.TentarObter(ChavesDeSave.CenaAtual, out var nome) &&
                                   !string.IsNullOrWhiteSpace(nome)
            ? nome
            : null;

        /// <summary>
        /// Reaplica a todos os inscritos o estado que existir no registro. Objeto sem chave
        /// no save mantém o estado padrão — é o fallback gracioso para conteúdo novo.
        /// </summary>
        public void AplicarTudo()
        {
            for (int i = 0; i < _registrados.Count; i++)
            {
                var p = _registrados[i];
                if (p == null) continue;

                if (_registro.TentarObter(p.ChaveDePersistencia, out var estado))
                    p.AplicarEstado(estado);
            }
        }

        /// <summary>Serializa o registro para o arquivo JSON.</summary>
        public void GravarEmDisco()
        {
            try
            {
                string json = JsonUtility.ToJson(_registro.ParaEstado(), prettyPrint: true);
                File.WriteAllText(CaminhoDoArquivo, json);
            }
            catch (IOException e)
            {
                // Disco cheio/arquivo travado não pode derrubar a partida em andamento.
                Debug.LogError($"[GerenciadorDeSave] Falha ao gravar o save: {e.Message}", this);
            }
        }

        /// <summary>
        /// Lê o arquivo JSON para o registro em memória. Arquivo ausente ou corrompido
        /// resulta numa partida nova, não numa exceção.
        /// </summary>
        public void CarregarDoDisco()
        {
            if (!File.Exists(CaminhoDoArquivo))
            {
                _registro = new RegistroDeSave();
                return;
            }

            try
            {
                string json = File.ReadAllText(CaminhoDoArquivo);
                _registro = RegistroDeSave.DeEstado(JsonUtility.FromJson<EstadoDeSave>(json));
            }
            catch (System.Exception e)
            {
                Debug.LogError($"[GerenciadorDeSave] Save corrompido ou ilegível ({e.Message}). " +
                               "Começando com registro vazio.", this);
                _registro = new RegistroDeSave();
            }
        }

        /// <summary>Esvazia o registro em memória (partida nova). Não apaga o arquivo.</summary>
        public void LimparRegistro() => _registro.Limpar();

        /// <summary>
        /// Se existe um arquivo de save em disco. O menu usa isto para decidir se mostra
        /// "Continuar" — botão morto ensina o jogador a desconfiar da interface.
        /// </summary>
        public bool ExisteSaveEmDisco => File.Exists(CaminhoDoArquivo);

        /// <summary>
        /// Apaga o save do disco <b>e</b> o registro em memória — "Nova peregrinação".
        ///
        /// <para>Os dois juntos de propósito: limpar só o arquivo deixaria o registro em
        /// memória vivo (o gerenciador é <c>DontDestroyOnLoad</c>), e a partida "nova"
        /// nasceria com o progresso da anterior.</para>
        /// </summary>
        public void ApagarSave()
        {
            LimparRegistro();

            try
            {
                if (File.Exists(CaminhoDoArquivo)) File.Delete(CaminhoDoArquivo);
            }
            catch (IOException e)
            {
                // Arquivo travado não pode impedir o jogador de começar de novo: o registro
                // em memória já foi limpo, então a partida nova é válida de qualquer forma.
                Debug.LogError($"[GerenciadorDeSave] Falha ao apagar o save: {e.Message}", this);
            }
        }

        // ── Flags de acontecimento (write-through) ───────────────────────────────
        //
        // Para estado que muda UMA vez e não volta atrás — baú aberto, boss resolvido,
        // item recolhido — o objeto grava a chave no instante em que o fato acontece, em
        // vez de esperar o CapturarTudo() perguntar.
        //
        // Por quê: CapturarTudo() só enxerga quem está carregado e registrado. Um pickup
        // que já se desativou, um objeto destruído, ou qualquer coisa numa cena
        // descarregada seria pulado em silêncio — e "silêncio" aqui significa progresso
        // perdido sem erro nenhum. Gravar na hora do fato não tem esse buraco.

        /// <summary>
        /// Marca que um acontecimento já ocorreu. Seguro chamar mesmo sem gerenciador vivo
        /// (só não persiste) — um pickup não deve quebrar por causa de save ausente.
        /// </summary>
        public static void MarcarAconteceu(string chave)
            => _instancia?._registro.Definir(chave, "1");

        /// <summary>Se um acontecimento já foi marcado. Falso quando não há gerenciador.</summary>
        public static bool JaAconteceu(string chave)
            => _instancia != null && _instancia._registro.Contem(chave);

        /// <summary>
        /// Grava um valor sob uma chave, para estado que não cabe num "aconteceu ou não" —
        /// ex.: o desfecho de Abdul, que é "derrotado" <b>ou</b> "poupado".
        /// </summary>
        public static void DefinirValor(string chave, string valor)
            => _instancia?._registro.Definir(chave, valor);

        /// <summary>
        /// Lê o valor de uma chave, ou <paramref name="padrao"/> se ela não existe (ou se
        /// não há gerenciador vivo). Um <c>null</c> de volta significa "nunca aconteceu".
        /// </summary>
        public static string ObterValor(string chave, string padrao = null)
            => _instancia != null ? _instancia._registro.ObterOuPadrao(chave, padrao) : padrao;
    }
}
