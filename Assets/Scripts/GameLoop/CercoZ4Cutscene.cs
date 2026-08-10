using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using FavelaAmarela.Runtime.Enemies;

namespace FavelaAmarela.Runtime.GameLoop
{
    /// <summary>
    /// Empacota o rigidbody de um Cultista instanciado na cutscene junto da sua
    /// posição-alvo no cerco. Usado só pela corrotina de aproximação.
    /// </summary>
    internal readonly struct AtorEmCerco
    {
        public readonly Rigidbody2D Rb;
        public readonly Vector2 Alvo;

        public AtorEmCerco(Rigidbody2D rb, Vector2 alvo)
        {
            Rb = rb;
            Alvo = alvo;
        }
    }

    /// <summary>
    /// Camada Runtime (MonoBehaviour). Diretor da cutscene de cerco: instancia
    /// Cultistas e Espectros ao redor de Damião na Praça do Cerco (Zona 4) antes
    /// do chão ceder (ver <see cref="QuedaZ4Z5Trigger"/>). Cuida só da encenação
    /// visual deste momento roteirizado — o Cultista tem seu <see cref="CultistaAI"/>
    /// desativado durante a cutscene (senão ele tentaria patrulhar sozinho), e o
    /// Espectro é dirigido via <see cref="EspectroAI"/>.
    /// </summary>
    [AddComponentMenu("Favela Amarela/GameLoop/Cerco Z4 Cutscene")]
    public sealed class CercoZ4Cutscene : MonoBehaviour
    {
        [Header("Prefabs")]
        [Tooltip("Prefab do Cultista Amarelo (precisa ter Rigidbody2D e CultistaAI).")]
        [SerializeField] private GameObject cultistaPrefab;
        [Tooltip("Prefab do Espectro (precisa ter Rigidbody2D e EspectroAI).")]
        [SerializeField] private GameObject espectroPrefab;

        [Header("Posições do Cerco (offset relativo a Damião)")]
        [SerializeField] private Vector2[] slotsCultista;
        [SerializeField] private Vector2[] slotsEspectro;

        [Header("Timing")]
        [Tooltip("Quanto mais longe do slot final os atores aparecem, pra dar sensação de aproximação.")]
        [SerializeField] private float distanciaSpawnExtra = 3f;
        [SerializeField] private float tempoManifestacao = 0.4f;
        [SerializeField] private float duracaoAproximacao = 1.5f;

        // Atores (Cultistas + Espectros) instanciados nesta cutscene, rastreados para
        // limpeza após a queda — eram set-piece da Zona 4 e não devem persistir na Zona 5.
        private readonly List<GameObject> _atoresInstanciados = new List<GameObject>();

        private void Awake()
        {
            if (cultistaPrefab == null)
                Debug.LogError("[CercoZ4Cutscene] Prefab de Cultista não atribuído no Inspector.", this);
            if (espectroPrefab == null)
                Debug.LogError("[CercoZ4Cutscene] Prefab de Espectro não atribuído no Inspector.", this);
        }

        /// <summary>
        /// Toca a cutscene de cerco ao redor de <paramref name="centro"/> (posição
        /// de Damião no instante em que o gatilho da queda disparou).
        /// </summary>
        public IEnumerator Tocar(Vector2 centro)
        {
            var cultistas = InstanciarCultistas(centro);
            var espectros = InstanciarEspectros(centro, out var alvosEspectro);

            foreach (var espectro in espectros)
            {
                espectro.Manifestar();
            }

            yield return new WaitForSeconds(tempoManifestacao);

            for (int i = 0; i < espectros.Count; i++)
            {
                espectros[i].IniciarCerco(alvosEspectro[i]);
            }

            yield return AproximarCultistas(cultistas, duracaoAproximacao);
        }

        /// <summary>
        /// Destrói os atores instanciados nesta cutscene. Chamado pela
        /// <see cref="QuedaZ4Z5Trigger"/> após a queda — eles eram um set-piece da
        /// Zona 4 e não devem persistir/perseguir na Zona 5 (senão o Espectro encalha
        /// na barreira de anomalia).
        /// </summary>
        public void LimparAtores()
        {
            foreach (var ator in _atoresInstanciados)
            {
                if (ator != null) Destroy(ator);
            }
            _atoresInstanciados.Clear();
        }

        private List<AtorEmCerco> InstanciarCultistas(Vector2 centro)
        {
            var instancias = new List<AtorEmCerco>();
            if (cultistaPrefab == null) return instancias;

            foreach (var slot in slotsCultista)
            {
                Vector2 alvo = centro + slot;
                Vector2 origem = centro + slot * distanciaSpawnExtra;

                var instancia = Instantiate(cultistaPrefab, origem, Quaternion.identity);
                _atoresInstanciados.Add(instancia);
                var ai = instancia.GetComponent<CultistaAI>();
                if (ai != null) ai.enabled = false;

                instancias.Add(new AtorEmCerco(instancia.GetComponent<Rigidbody2D>(), alvo));
            }

            return instancias;
        }

        private List<EspectroAI> InstanciarEspectros(Vector2 centro, out List<Vector2> alvos)
        {
            var instancias = new List<EspectroAI>();
            alvos = new List<Vector2>();
            if (espectroPrefab == null) return instancias;

            foreach (var slot in slotsEspectro)
            {
                var instancia = Instantiate(espectroPrefab, centro + slot * distanciaSpawnExtra, Quaternion.identity);
                _atoresInstanciados.Add(instancia);
                var ai = instancia.GetComponent<EspectroAI>();
                if (ai == null) continue;

                instancias.Add(ai);
                alvos.Add(centro + slot);
            }

            return instancias;
        }

        private static IEnumerator AproximarCultistas(List<AtorEmCerco> cultistas, float duracao)
        {
            var origens = new Vector2[cultistas.Count];
            for (int i = 0; i < cultistas.Count; i++)
            {
                origens[i] = cultistas[i].Rb.position;
            }

            float tempo = 0f;
            while (tempo < duracao)
            {
                tempo += Time.deltaTime;
                float t = duracao > 0f ? tempo / duracao : 1f;

                for (int i = 0; i < cultistas.Count; i++)
                {
                    cultistas[i].Rb.position = Vector2.Lerp(origens[i], cultistas[i].Alvo, t);
                }

                yield return null;
            }
        }
    }
}
