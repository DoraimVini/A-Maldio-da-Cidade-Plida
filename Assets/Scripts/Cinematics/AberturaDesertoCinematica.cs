using System.Collections;
using UnityEngine;
using FavelaAmarela.Player;
using FavelaAmarela.Runtime.Environment;
using FavelaAmarela.Runtime.UI;

namespace FavelaAmarela.Runtime.Cinematics
{
    [AddComponentMenu("Favela Amarela/Cinematics/Abertura Deserto")]
    public class AberturaDesertoCinematica : MonoBehaviour
    {
        [Header("Dependências")]
        [SerializeField] private PlayerMovement playerMovement;
        [SerializeField] private ScreenFader screenFader;
        [SerializeField] private TempestadeAmbiente tempestadeAmbiente;
        
        [Header("Câmeras (GameObjects com Cinemachine Camera)")]
        [SerializeField] private GameObject cameraShot2_Pes;
        [SerializeField] private GameObject cameraShot3_Garganta;
        [SerializeField] private GameObject cameraShot4_Saida;
        [SerializeField] private GameObject cameraShot6_Avança;
        [SerializeField] private GameObject cameraGameplay;

        [Header("Atores")]
        [SerializeField] private Transform playerTransform;
        [SerializeField] private GameObject[] espectrosHali;

        [Header("Áudio")]
        [SerializeField] private AudioSource audioSource;
        [SerializeField] private AudioClip somVentoBaixo;
        [SerializeField] private AudioClip somVentoRajada;
        [SerializeField] private AudioClip somSintetizadorCarcosa;
        [SerializeField] private AudioClip somFrequencia783Hz;
        [SerializeField] private AudioClip droneExploracao;

        [Header("Configurações de Movimento")]
        [SerializeField] private float velocidadeMovimentoCinematica = 2f;
        [SerializeField] private Vector2 direcaoMovimentoShot2 = Vector2.right;
        [SerializeField] private Vector2 direcaoMovimentoShot6 = Vector2.up;

        private void Start()
        {
            StartCoroutine(PlayCinematicRoutine());
        }

        private IEnumerator PlayCinematicRoutine()
        {
            // Bloqueia input do jogador
            if (playerMovement != null)
                playerMovement.MovimentoBloqueado = true;

            // Desliga todas as câmeras específicas e liga a do Shot 2
            cameraShot2_Pes?.SetActive(true);
            cameraShot3_Garganta?.SetActive(false);
            cameraShot4_Saida?.SetActive(false);
            cameraShot6_Avança?.SetActive(false);
            cameraGameplay?.SetActive(false);

            // Esconde espectros inicialmente
            foreach (var espectro in espectrosHali)
            {
                if (espectro != null) espectro.SetActive(false);
            }

            // Garante tela preta inicial
            if (screenFader != null)
                yield return StartCoroutine(screenFader.FadeTo(1f, 0f));

            // ==========================================================
            // Shot 1 — Negro e Vento (0s–4s)
            // ==========================================================
            
            // 1s de silêncio absoluto
            yield return new WaitForSeconds(1f);
            
            // Vento distante começa
            TocarSom(somVentoBaixo, true);
            yield return new WaitForSeconds(2f); // Aos 3s

            // Rajada súbita
            TocarSom(somVentoRajada, false);
            if (tempestadeAmbiente != null)
                tempestadeAmbiente.DefinirFaixa(0.8f, 0.9f);
                
            yield return new WaitForSeconds(1f); // Termina os 4s iniciais

            // Fade in para a cena (Shot 2)
            if (screenFader != null)
                StartCoroutine(screenFader.FadeTo(0f, 1f));

            // ==========================================================
            // Shot 2 — Os Pés de Damião (4s–9s)
            // ==========================================================
            if (tempestadeAmbiente != null)
                tempestadeAmbiente.DefinirFaixa(0.3f, 0.4f);

            float t = 0;
            while (t < 5f)
            {
                MoverPlayer(direcaoMovimentoShot2);
                t += Time.deltaTime;
                yield return null;
            }

            // ==========================================================
            // Shot 3 — A Garganta (9s–18s)
            // ==========================================================
            cameraShot2_Pes?.SetActive(false);
            cameraShot3_Garganta?.SetActive(true);
            
            TocarSom(somSintetizadorCarcosa, false);

            t = 0;
            while (t < 9f)
            {
                MoverPlayer(direcaoMovimentoShot2); // Continua andando
                t += Time.deltaTime;
                yield return null;
            }

            // ==========================================================
            // Shot 4 — A Saída da Garganta (18s–27s)
            // ==========================================================
            cameraShot3_Garganta?.SetActive(false);
            cameraShot4_Saida?.SetActive(true);

            // Parar vento por 2s
            if (audioSource != null) audioSource.Stop();
            if (tempestadeAmbiente != null) tempestadeAmbiente.DefinirFaixa(0f, 0f);

            yield return new WaitForSeconds(2f);

            // Maior rajada
            TocarSom(somVentoRajada, false);
            if (tempestadeAmbiente != null) tempestadeAmbiente.DefinirFaixa(0.9f, 1f);

            yield return new WaitForSeconds(7f);

            // ==========================================================
            // Shot 5 — A Rajada e os Espectros (27s–42s)
            // ==========================================================
            // Espectros emergem
            foreach (var espectro in espectrosHali)
            {
                if (espectro != null) espectro.SetActive(true);
            }

            // Todos sons param, 7.83Hz começa
            if (audioSource != null) audioSource.Stop();
            TocarSom(somFrequencia783Hz, true);

            // Damião congela (não movemos ele neste loop)
            yield return new WaitForSeconds(15f);

            // Espectros desaparecem
            foreach (var espectro in espectrosHali)
            {
                if (espectro != null) Destroy(espectro);
            }

            // ==========================================================
            // Shot 6 — Damião Avança (42s–52s)
            // ==========================================================
            cameraShot4_Saida?.SetActive(false);
            cameraShot6_Avança?.SetActive(true);
            
            if (audioSource != null) audioSource.Stop();
            TocarSom(droneExploracao, true);

            if (tempestadeAmbiente != null) tempestadeAmbiente.DefinirFaixa(0.2f, 0.3f);

            t = 0;
            while (t < 10f)
            {
                // Passo hesitante, avança para o deserto
                MoverPlayer(direcaoMovimentoShot6);
                t += Time.deltaTime;
                yield return null;
            }

            // ==========================================================
            // Shot 7 — Fade e Controle (52s–60s)
            // ==========================================================
            cameraShot6_Avança?.SetActive(false);
            cameraGameplay?.SetActive(true);

            if (tempestadeAmbiente != null) tempestadeAmbiente.DefinirFaixa(0.1f, 0.3f); // Calmaria ("Entrada")

            // Devolve controle ao jogador
            if (playerMovement != null)
                playerMovement.MovimentoBloqueado = false;
        }

        private void MoverPlayer(Vector2 dir)
        {
            if (playerTransform != null)
            {
                // Como não estamos injetando no Input System, caso queira que o Animator
                // toque a animação de andar, será necessário ter um bridge de animação.
                Vector2 move = new Vector2(dir.x, dir.y) * (velocidadeMovimentoCinematica * Time.deltaTime);

                // Rigidbody2D.position, e NÃO transform.position. O projeto está com
                // Auto Sync Transforms DESLIGADO (Physics2DSettings): escrever no transform de
                // um objeto com corpo não move o colisor na hora, e as consultas continuam
                // vendo a posição anterior até o próximo passo de física. Durante a cinemática
                // isso punha o Damião atravessando geometria com o colisor atrasado.
                var corpo = playerTransform.GetComponent<Rigidbody2D>();
                if (corpo != null) corpo.position += move;
                else playerTransform.position += (Vector3)move;
            }
        }

        private void TocarSom(AudioClip clip, bool loop)
        {
            if (audioSource != null && clip != null)
            {
                if (loop)
                {
                    audioSource.clip = clip;
                    audioSource.loop = true;
                    audioSource.Play();
                }
                else
                {
                    audioSource.PlayOneShot(clip);
                }
            }
        }
    }
}
