using System.Globalization;
using UnityEngine;
using FavelaAmarela.Core.Persistencia;
using FavelaAmarela.Runtime.Combat;

namespace FavelaAmarela.Runtime.Persistencia
{
    /// <summary>
    /// Camada Runtime (MonoBehaviour). Faz a Vitalidade de Yug-Neth sobreviver à troca de
    /// cena — sem isto, toda vez que ele atravessa de cena
    /// (<c>FavelaAmarela.Runtime.GameLoop.TravessiaDoCompanheiro</c>) nasce com Vitalidade
    /// cheia, mesmo tendo levado dano ou estando incapacitado, porque a instância antiga é
    /// destruída junto com a cena de origem e a nova não tem como saber disso.
    ///
    /// <para><b>A incapacitação não precisa de chave própria.</b> Ela só acontece quando a
    /// Vitalidade chega a zero (<c>VitalidadeBridge.OnAbatido</c>), e <c>YugNethAI</c> já
    /// escuta esse evento. Restaurar a Vitalidade salva como zero dispara
    /// <c>HandleAbatido</c> pelo caminho normal — ele nasce incapacitado de graça, sem
    /// duplicar o estado em duas chaves que poderiam dessincronizar.</para>
    ///
    /// <para>Vive no <b>prefab</b> do Yug-Neth: tanto a instância cativa da Tumba quanto
    /// qualquer instância recriada por <c>TravessiaDoCompanheiro</c> se registram e se
    /// restauram sozinhas — mesmo padrão de <see cref="EstadoPersistenteDoJogador"/>, só que
    /// sem arma (ele não empunha nada).</para>
    /// </summary>
    [RequireComponent(typeof(VitalidadeBridge))]
    [AddComponentMenu("Favela Amarela/Persistência/Estado Persistente do Companheiro")]
    public sealed class EstadoPersistenteDoCompanheiro : MonoBehaviour, IPersistente
    {
        private VitalidadeBridge _vitalidade;

        /// <inheritdoc />
        public string ChaveDePersistencia => ChavesDeSave.YugNethVitalidadeAtual;

        private void Awake() => _vitalidade = GetComponent<VitalidadeBridge>();

        private void Start()
        {
            // Registro no Start, não no Awake: o GerenciadorDeSave pode ainda não ter
            // acordado, e a ordem de Awake entre GameObjects não é garantida — mesma
            // armadilha documentada em EstadoPersistenteDoJogador.
            var gerenciador = GerenciadorDeSave.Instancia;
            if (gerenciador == null) return;

            gerenciador.Registrar(this);

            if (gerenciador.Registro.TentarObter(ChaveDePersistencia, out var estado))
                AplicarEstado(estado);
        }

        private void OnDestroy() => GerenciadorDeSave.Instancia?.Desregistrar(this);

        /// <inheritdoc />
        public string CapturarEstado()
            => _vitalidade?.Vitalidade != null
                ? _vitalidade.Vitalidade.Atual.ToString(CultureInfo.InvariantCulture)
                : "";

        /// <inheritdoc />
        public void AplicarEstado(string estado)
        {
            if (!float.TryParse(estado, NumberStyles.Float, CultureInfo.InvariantCulture, out float atual))
                return;

            var vitalidade = _vitalidade?.Vitalidade;
            if (vitalidade == null) return;

            // Nasce cheio; só precisamos tirar o que faltava — mesmo raciocínio do jogador.
            // Se o valor salvo for zero, isto dispara OnAbatido pelo caminho normal, e
            // YugNethAI.HandleAbatido cuida da incapacitação sozinho.
            float diferenca = vitalidade.Atual - atual;
            if (diferenca > 0f) vitalidade.Ferir(diferenca);
        }
    }
}
