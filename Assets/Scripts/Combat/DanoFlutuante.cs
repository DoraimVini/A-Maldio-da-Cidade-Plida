using UnityEngine;

namespace FavelaAmarela.Runtime.Combat
{
    /// <summary>
    /// Número de dano flutuante em world space — sobe e desvanece até se autodestruir.
    ///
    /// <para><b>Natureza provisória:</b> isto é um <b>diagnóstico visual</b> de combate,
    /// pedido enquanto não existem animações de golpe/impacto. É o jeito mais direto de
    /// confirmar que dano, mitigação e cadência estão funcionando. Quando as animações e
    /// o VFX de impacto entrarem, este componente pode ser trocado por feedback diegético
    /// (flash de sprite, partícula) sem afetar o Core.</para>
    ///
    /// <para>Usa <c>TextMesh</c> legado propositalmente: renderiza em world space sem
    /// depender de nenhum asset importado (TextMeshPro exigiria os Essential Resources) e
    /// sem Canvas. A fonte built-in na Unity 6 é <c>LegacyRuntime.ttf</c> — o nome antigo
    /// (<c>Arial.ttf</c>) foi removido nas versões recentes.</para>
    /// </summary>
    [AddComponentMenu("")] // interno: criado só por código, não deve aparecer no menu
    public sealed class DanoFlutuante : MonoBehaviour
    {
        // Constantes de game-feel, calibradas para a escala de 32 PPU do projeto.
        private const float DuracaoSegundos = 0.9f;
        private const float AlturaSubida = 1.1f;
        private const float DeslocamentoLateralMax = 0.25f;
        private const int TamanhoFonte = 48;
        private const float TamanhoCaractere = 0.06f;

        // Ordem de renderização alta para o número ficar acima dos sprites do mundo
        // (o Y-sorting do projeto usa sortingOrder por -worldCenter.y, ver LevelBlockoutGenerator).
        private const int OrdemDeRenderizacao = 32000;

        private static Font _fonteCache;

        private TextMesh _texto;
        private MeshRenderer _renderer;
        private Vector3 _origem;
        private Vector3 _deslocamento;
        private Color _corBase;
        private float _tempo;

        /// <summary>
        /// Cria e dispara um número de dano flutuante na posição de mundo indicada.
        /// Alocação por golpe (não por frame) — fora de hot path, dentro da Regra de Ouro 1.
        /// </summary>
        /// <param name="posicaoMundo">Onde o número nasce (normalmente a posição do alvo).</param>
        /// <param name="valor">Dano final já mitigado; exibido arredondado.</param>
        /// <param name="cor">Cor do número (convenção: alvo inimigo x Damião).</param>
        public static void Mostrar(Vector3 posicaoMundo, float valor, Color cor)
        {
            var fonte = ObterFonte();
            if (fonte == null) return; // erro já reportado por ObterFonte

            var go = new GameObject("DanoFlutuante");
            go.transform.position = posicaoMundo;

            var texto = go.AddComponent<TextMesh>();
            texto.font = fonte;
            texto.text = Mathf.RoundToInt(valor).ToString();
            texto.fontSize = TamanhoFonte;
            texto.characterSize = TamanhoCaractere;
            texto.anchor = TextAnchor.LowerCenter;
            texto.alignment = TextAlignment.Center;
            texto.color = cor;

            var renderer = go.GetComponent<MeshRenderer>();
            renderer.sharedMaterial = fonte.material;
            renderer.sortingOrder = OrdemDeRenderizacao;

            var flutuante = go.AddComponent<DanoFlutuante>();
            flutuante.Inicializar(posicaoMundo, cor, texto, renderer);
        }

        private static bool _fonteIndisponivel;

        private static Font ObterFonte()
        {
            if (_fonteCache != null) return _fonteCache;
            if (_fonteIndisponivel) return null; // já falhou antes; não repete o log

            // Unity 6: a fonte embutida chama-se "LegacyRuntime.ttf" — o nome antigo
            // ("Arial.ttf") não só foi removido como faz GetBuiltinResource LANÇAR
            // ArgumentException (ver FonteBuiltinTests). O try/catch garante que este
            // diagnóstico visual nunca derrube o combate se a Unity mudar de novo.
            try
            {
                _fonteCache = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            }
            catch (System.Exception e)
            {
                Debug.LogError($"[DanoFlutuante] Falha ao obter a fonte built-in: {e.Message}. " +
                               "Números de dano desativados.");
                _fonteIndisponivel = true;
                return null;
            }

            if (_fonteCache == null)
            {
                Debug.LogError("[DanoFlutuante] Fonte built-in 'LegacyRuntime.ttf' não encontrada; " +
                               "números de dano desativados.");
                _fonteIndisponivel = true;
            }

            return _fonteCache;
        }

        private void Inicializar(Vector3 origem, Color cor, TextMesh texto, MeshRenderer renderer)
        {
            _origem = origem;
            _corBase = cor;
            _texto = texto;
            _renderer = renderer;

            // Espalha os números lateralmente para golpes em sequência não empilharem.
            _deslocamento = new Vector3(Random.Range(-DeslocamentoLateralMax, DeslocamentoLateralMax), 0f, 0f);
        }

        private void Update()
        {
            _tempo += Time.deltaTime;
            float t = _tempo / DuracaoSegundos;

            if (t >= 1f)
            {
                Destroy(gameObject);
                return;
            }

            // Sobe desacelerando (ease-out) e desvanece na segunda metade da vida.
            float subida = AlturaSubida * (1f - (1f - t) * (1f - t));
            transform.position = _origem + _deslocamento + new Vector3(0f, subida, 0f);

            float alfa = t < 0.5f ? 1f : 1f - ((t - 0.5f) / 0.5f);
            _texto.color = new Color(_corBase.r, _corBase.g, _corBase.b, alfa);
        }
    }
}
