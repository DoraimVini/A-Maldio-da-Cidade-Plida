using UnityEngine;
using FavelaAmarela.Runtime.Itens;

namespace FavelaAmarela.Runtime.Progression
{
    /// <summary>
    /// Concede <b>Exposição</b> ao jogador quando este ator é derrotado — para os atores que
    /// <b>não são <c>EnemyBase</c></b>.
    ///
    /// <para><b>O buraco que isto fecha (2026-08-28).</b> A concessão de Exposição mora dentro
    /// do <c>EnemyBase</c>, e o argumento escrito lá é bom: inimigo comum novo concede de graça,
    /// sem lista para manter à mão. Só que o elenco medido conta <b>nove prefabs e apenas dois
    /// <c>EnemyBase</c></b> (Cultista e Byakhee). O <b>Abdul Alhazred — o primeiro chefe do
    /// jogo — implementa <c>IDanificavel</c> direto</b> e portanto concedia <b>zero</b>. Derrotar
    /// o chefe que fecha a Tumba não movia o nível do jogador um ponto.</para>
    ///
    /// <para><b>Por que ligar em <see cref="IFonteDeEspolio"/> em vez de criar outra
    /// interface.</b> "Sei avisar que fui derrotado" é exatamente o contrato que o
    /// <c>DropAoAbater</c> já exige, e pelo mesmo motivo. Duas interfaces com a mesma semântica
    /// divergiriam: um chefe futuro implementaria uma e não a outra, e largaria espólio sem dar
    /// nível — falha silenciosa, do tipo que este repositório coleciona.</para>
    ///
    /// <para><b>Por que não mover a concessão do <c>EnemyBase</c> para cá.</b> Porque aí todo
    /// Cultista passaria a depender de alguém lembrar de pôr o componente — trocaria uma lista
    /// curta (os chefes) por uma longa (o elenco inteiro). Os dois caminhos coexistem, e
    /// <c>EconomiaDeExposicaoTests</c> guarda a soma: <b>todo prefab que larga espólio tem de
    /// conceder Exposição por um dos dois</b>.</para>
    /// </summary>
    [AddComponentMenu("Favela Amarela/Progressão/Exposição ao Abater")]
    public sealed class ExposicaoAoAbater : MonoBehaviour
    {
        [Tooltip("Exposição concedida ao ser derrotado. A curva de níveis pede 100 para o " +
                 "nível 2 e 300 para o 3 — um chefe precisa valer dezenas de inimigos comuns " +
                 "para a vitória ser sentida na ficha, não só no espólio.")]
        [Min(0)]
        [SerializeField] private int exposicao = 100;

        /// <summary>Quanto este ator concede. Exposto para os guardas de Editor conferirem.</summary>
        public int Exposicao => exposicao;

        private IFonteDeEspolio _fonte;

        private void Awake()
        {
            _fonte = GetComponent<IFonteDeEspolio>();

            if (_fonte == null)
            {
                Debug.LogError($"[ExposicaoAoAbater] '{name}' não implementa IFonteDeEspolio — " +
                               "abatê-lo não concederá Exposição nenhuma.", this);
                return;
            }

            _fonte.OnAbatido += Conceder;
        }

        private void OnDestroy()
        {
            if (_fonte != null) _fonte.OnAbatido -= Conceder;
        }

        private void Conceder()
        {
            if (exposicao <= 0) return;

            // Cena de teste pode não ter ProgressionBridge; abater sem progressão não pode
            // derrubar a partida.
            ProgressionBridge.Instancia?.AdicionarExposicao(exposicao);
        }
    }
}
