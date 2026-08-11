using System.Collections.Generic;
using UnityEngine;
using FavelaAmarela.Core.Persistencia;
using FavelaAmarela.Progression;

namespace FavelaAmarela.Runtime.Persistencia
{
    /// <summary>
    /// Camada Runtime. Faz o <b>nível de Exposição, os pontos e os Ecos desbloqueados</b>
    /// sobreviverem à troca de cena e ao save em disco.
    ///
    /// <para><b>Buraco que motivou (auditoria 2026-08-11):</b> igual ao inventário — o
    /// <c>ProgressionManager.GetSaveData()</c> existia e nunca era chamado. Perder o nível ao
    /// recarregar é pior que perder itens, porque agora o nível também <b>gateia o loot</b>
    /// (ver <c>systems/loot_e_drop.md</c>).</para>
    /// </summary>
    [AddComponentMenu("Favela Amarela/Persistência/Estado Persistente da Progressão")]
    public sealed class EstadoPersistenteDaProgressao : MonoBehaviour, IPersistente
    {
        [Tooltip("Todos os EcoDef do jogo, para resolver os ids do save de volta em assets. " +
                 "Vazio = carrega de Resources/Ecos. [ASSET]")]
        [SerializeField] private EcoDef[] catalogoDeEcos;

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
            var progressao = ProgressionManager.Instance;
            if (progressao == null) return "";

            return JsonUtility.ToJson(progressao.GetSaveData());
        }

        /// <inheritdoc />
        public void AplicarEstado(string estado)
        {
            if (string.IsNullOrWhiteSpace(estado)) return;

            var progressao = ProgressionManager.Instance;
            if (progressao == null) return;

            ProgressionSaveData dados;
            try
            {
                dados = JsonUtility.FromJson<ProgressionSaveData>(estado);
            }
            catch (System.Exception e)
            {
                Debug.LogWarning($"[EstadoPersistenteDaProgressao] Save ilegível, progressão mantida no padrão: {e.Message}", this);
                return;
            }

            if (dados == null) return;

            progressao.RestoreFromSaveData(dados, MontarDicionario());
        }

        /// <summary>
        /// Mapa id→<c>EcoDef</c>. O save guarda ids, e o <c>ProgressionManager</c> precisa dos
        /// assets de volta — um Eco que não exista mais é simplesmente ignorado por ele.
        /// </summary>
        private Dictionary<string, EcoDef> MontarDicionario()
        {
            var fonte = catalogoDeEcos != null && catalogoDeEcos.Length > 0
                ? catalogoDeEcos
                : Resources.LoadAll<EcoDef>("Ecos");

            var mapa = new Dictionary<string, EcoDef>();
            foreach (var eco in fonte)
            {
                if (eco == null || string.IsNullOrEmpty(eco.Id)) continue;
                mapa[eco.Id] = eco;
            }

            return mapa;
        }
    }
}
