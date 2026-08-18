using UnityEngine;
using FavelaAmarela.Core.Persistencia;
using FavelaAmarela.Progression;
using FavelaAmarela.Runtime.Progression;

namespace FavelaAmarela.Runtime.Persistencia
{
    /// <summary>
    /// Camada Runtime. Faz o <b>nível de Exposição, os pontos e os Ecos desbloqueados</b>
    /// sobreviverem à troca de cena e ao save em disco.
    ///
    /// <para><b>Buraco que motivou (auditoria 2026-08-11):</b> igual ao inventário — o
    /// <c>GetSaveData()</c> existia e nunca era chamado. Perder o nível ao recarregar é pior que
    /// perder itens, porque o nível também <b>gateia o loot</b> (ver
    /// <c>systems/loot_e_drop.md</c>).</para>
    ///
    /// <para><b>Mudou em 2026-08-18 (Fase 3):</b> passou a falar com o
    /// <see cref="ProgressionBridge"/> em vez do antigo <c>ProgressionManager</c>. O catálogo de
    /// <c>EcoDef</c> saiu daqui e foi para o bridge — quem resolve id→asset é quem guarda os ids,
    /// e ter as duas pontas montando o próprio dicionário era duplicação esperando divergir.</para>
    /// </summary>
    [AddComponentMenu("Favela Amarela/Persistência/Estado Persistente da Progressão")]
    public sealed class EstadoPersistenteDaProgressao : MonoBehaviour, IPersistente
    {
        /// <inheritdoc />
        public string ChaveDePersistencia => ChavesDeSave.Progressao;

        private void Start()
        {
            var gerenciador = GerenciadorDeSave.Instancia;
            if (gerenciador == null) return;

            gerenciador.Registrar(this);

            if (gerenciador.Registro.TentarObter(ChaveDePersistencia, out var estado))
                AplicarEstado(estado);
        }

        private void OnDestroy() => GerenciadorDeSave.Instancia?.Desregistrar(this);

        /// <inheritdoc />
        public string CapturarEstado()
        {
            var bridge = ProgressionBridge.Instancia;
            if (bridge == null) return "";

            return JsonUtility.ToJson(bridge.CapturarSaveData());
        }

        /// <inheritdoc />
        public void AplicarEstado(string estado)
        {
            if (string.IsNullOrWhiteSpace(estado)) return;

            var bridge = ProgressionBridge.Instancia;
            if (bridge == null) return;

            ProgressionSaveData dados;
            try
            {
                dados = JsonUtility.FromJson<ProgressionSaveData>(estado);
            }
            catch (System.Exception e)
            {
                Debug.LogWarning("[EstadoPersistenteDaProgressao] Save ilegível, progressão " +
                                 $"mantida no padrão: {e.Message}", this);
                return;
            }

            if (dados == null) return;

            bridge.RestaurarSaveData(dados);
        }
    }
}
