using UnityEngine;
using FavelaAmarela.Core.Persistencia;
using FavelaAmarela.Inventario;

namespace FavelaAmarela.Runtime.Persistencia
{
    /// <summary>
    /// Camada Runtime. Faz a <b>mochila e o equipamento</b> sobreviverem à troca de cena e ao
    /// save em disco.
    ///
    /// <para><b>Buraco que motivou (auditoria 2026-08-11):</b> o
    /// <c>InventoryManager.GetSaveData()</c> existia desde sempre e <b>nunca era chamado por
    /// ninguém</b> — nada ligava o inventário ao <see cref="GerenciadorDeSave"/>. Na prática
    /// tudo que o jogador carregava se perdia ao recarregar, em silêncio.</para>
    ///
    /// <para>Segue o padrão Observer do <see cref="IPersistente"/>: só sabe ler e escrever o
    /// próprio estado; quem grava é o gerenciador.</para>
    /// </summary>
    [AddComponentMenu("Favela Amarela/Persistência/Estado Persistente do Inventário")]
    public sealed class EstadoPersistenteDoInventario : MonoBehaviour, IPersistente
    {
        /// <inheritdoc />
        public string ChaveDePersistencia => ChavesDeSave.Inventario;

        private void Start()
        {
            // Registro no Start, não no Awake: a ordem de Awake entre GameObjects não é
            // garantida e o gerenciador pode ainda não existir.
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
            var inventario = InventoryManager.Instance;
            if (inventario == null) return "";

            return JsonUtility.ToJson(inventario.GetSaveData());
        }

        /// <inheritdoc />
        public void AplicarEstado(string estado)
        {
            if (string.IsNullOrWhiteSpace(estado)) return;

            var inventario = InventoryManager.Instance;
            if (inventario == null) return;

            // JSON corrompido ou de uma versão antiga não pode derrubar o load: o inventário
            // fica como estava (vazio), que é degradação graciosa.
            InventorySaveData dados;
            try
            {
                dados = JsonUtility.FromJson<InventorySaveData>(estado);
            }
            catch (System.Exception e)
            {
                Debug.LogWarning($"[EstadoPersistenteDoInventario] Save ilegível, inventário mantido vazio: {e.Message}", this);
                return;
            }

            if (dados != null) inventario.LoadFromSaveData(dados);
        }
    }
}
