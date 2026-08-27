using UnityEngine;
using FavelaAmarela.Core.Stealth;

namespace FavelaAmarela.Runtime.Enemies
{
    public class EnemyPerception : MonoBehaviour
    {
        [Header("Audição")]
        [SerializeField] private float raioAudicao = 10f;
        [SerializeField] private float taxaSuspeita = 0.4f;
        [Range(0f, 1f)] [SerializeField] private float limiarAlerta = 0.3f;
        [Range(0f, 1f)] [SerializeField] private float limiarCaca = 0.7f;

        /// <summary>
        /// Por quanto tempo um som continua "sendo ouvido" depois de chegar. Precisa ser
        /// maior que o intervalo de emissão do jogador (<c>PlayerMovement</c> emite a cada
        /// 0,15 s); caso contrário a suspeita decai na maioria dos frames e nunca sobe.
        /// </summary>
        private const float MemoriaDoSom = 0.35f;

        private SoundBroadcastService _soundBroadcaster;
        private float _suspeita;
        private Vector2? _ultimaOrigemConhecida;
        private float _tempoDesdeUltimoSom = float.MaxValue;
        private bool _jaEntrouAlerta;
        private bool _jaEntrouCaca;

        public event System.Action<float> OnSuspeitaChanged;
        public event System.Action OnEntrouAlerta;
        public event System.Action OnEntrouCaca;
        public event System.Action OnPerdeuAlvo;

        public float Suspeita => _suspeita;
        public Vector2? UltimaOrigemConhecida => _ultimaOrigemConhecida;

        public void Bind(SoundBroadcastService broadcaster)
        {
            if (broadcaster == null) return;

            // Desassina antes de assinar: uma segunda injeção (bootstrap repetido, cena
            // recarregada) acumularia handlers e faria o mesmo som contar várias vezes.
            if (_soundBroadcaster != null)
                _soundBroadcaster.OnSomEmitido -= HandleSomEmitido;

            _soundBroadcaster = broadcaster;
            _soundBroadcaster.OnSomEmitido += HandleSomEmitido;
        }

        private void Start()
        {
            // Sem o serviço de som este inimigo é surdo: nunca entra em Alerta nem em Caça,
            // e só reage se for golpeado (via Hurt -> Chase). Isso é indistinguível de "IA
            // quebrada" em playtest, então falha alto em vez de em silêncio.
            if (_soundBroadcaster == null)
                Debug.LogError($"[EnemyPerception] '{name}' não recebeu o serviço de som — " +
                               "vai ignorar o jogador até ser atacado. Confirme que existe um " +
                               "GameManager na cena (é ele quem injeta no bootstrap).", this);
        }

        private void Update()
        {
            // O som chega em rajadas (uma a cada 0,15 s), mas este Update roda todo frame.
            // Guardar "ouvi agora" num bool zerado ao fim do frame fazia a suspeita subir em
            // 1 frame e cair nos ~8 seguintes — saldo negativo, e o inimigo nunca percebia
            // o jogador. A janela de memória cobre o intervalo entre duas emissões.
            _tempoDesdeUltimoSom += Time.deltaTime;
            bool estaOuvindo = _tempoDesdeUltimoSom <= MemoriaDoSom;

            float delta = taxaSuspeita * Time.deltaTime;
            _suspeita = Mathf.Clamp01(_suspeita + (estaOuvindo ? delta : -delta));
            OnSuspeitaChanged?.Invoke(_suspeita);

            if (!_jaEntrouAlerta && _suspeita >= limiarAlerta)
            {
                _jaEntrouAlerta = true;
                OnEntrouAlerta?.Invoke();
            }
            if (!_jaEntrouCaca && _suspeita >= limiarCaca)
            {
                _jaEntrouCaca = true;
                OnEntrouCaca?.Invoke();
            }

            // Desarma os dois limiares ao cair abaixo do de Alerta. Antes esta condição
            // exigia _jaEntrouCaca: quem só chegasse a Alerta e esfriasse ficava com
            // _jaEntrouAlerta travado em true para sempre e nunca mais reagia a um novo
            // ruído — surdo pelo resto da partida.
            if ((_jaEntrouAlerta || _jaEntrouCaca) && _suspeita < limiarAlerta)
            {
                _jaEntrouAlerta = false;
                _jaEntrouCaca = false;
                OnPerdeuAlvo?.Invoke();
            }
        }

        /// <summary>
        /// Ouve um ruído — comparando a distância com o <b>alcance do próprio som</b>, e não só
        /// com a acuidade deste inimigo.
        ///
        /// <para><b>O defeito que isto conserta é o maior do jogo (2026-08-27).</b> A versão
        /// anterior comparava a distância <b>apenas</b> com <c>raioAudicao</c> (10 no Cultista)
        /// e <b>descartava <c>som.RaioEfetivo</c></b>. Consequência: agachado (raio 2,0) e
        /// correndo (8,5) eram ouvidos <b>exatamente igual</b>. Modo Furtivo, corrida e o
        /// abafamento da tempestade não tinham efeito nenhum em produção.</para>
        ///
        /// <para>Num jogo cuja percepção é 100% sonora, isso significa que <b>a furtividade —
        /// o pilar do jogo — nunca funcionou</b>.</para>
        ///
        /// <para><b>E o código certo já existia, testado.</b>
        /// <c>CultistaFSM.ReceberEstimuloSonoro</c> compara com <c>raioEfetivo</c> desde
        /// sempre — mas ela só é instanciada em teste. O caminho vivo é
        /// <c>CultistaAI</c> + <c>EnemyPerception</c>, que é este. Um POCO testado e morto: o
        /// modo de falha da casa, na sua forma mais cara.</para>
        ///
        /// <para><b>Por que o mínimo dos dois:</b> o som carrega até o raio dele, mas nenhum
        /// inimigo ouve além da própria acuidade. Hoje <c>raioAudicao</c> (10) é maior que o
        /// ruído mais alto do jogo (8,5), então o teto não morde — ele existe como botão de
        /// "este aqui é surdo", não como limitador ativo.</para>
        /// </summary>
        private void HandleSomEmitido(SomEmitido som)
        {
            if (som.RaioEfetivo <= 0f) return;

            float alcance = Mathf.Min(som.RaioEfetivo, raioAudicao);

            if (Vector2.Distance(transform.position, som.Origem) > alcance) return;

            _tempoDesdeUltimoSom = 0f;
            _ultimaOrigemConhecida = som.Origem;
        }

        public void PerderAlvo()
        {
            _suspeita = 0f;
            _ultimaOrigemConhecida = null;
            _tempoDesdeUltimoSom = float.MaxValue;
            _jaEntrouAlerta = false;
            _jaEntrouCaca = false;
            OnSuspeitaChanged?.Invoke(0f);
            OnPerdeuAlvo?.Invoke();
        }

        private void OnDestroy()
        {
            if (_soundBroadcaster != null)
                _soundBroadcaster.OnSomEmitido -= HandleSomEmitido;
        }
    }
}
