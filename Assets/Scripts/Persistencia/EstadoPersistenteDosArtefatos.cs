using UnityEngine;
using FavelaAmarela.Core.Artefatos;
using FavelaAmarela.Core.Persistencia;
using FavelaAmarela.Player;

namespace FavelaAmarela.Runtime.Persistencia
{
    /// <summary>
    /// Camada Runtime. Faz os <b>quatro slots de Artefato</b> sobreviverem à troca de cena.
    ///
    /// <para>Formato: os ids na ordem dos slots, separados por vírgula, com slot vazio
    /// virando campo em branco (<c>"necronomicon,,coroa_de_ossos,"</c>). É legível ao depurar
    /// e a <b>posição importa</b> — o jogador escolheu qual Artefato fica em qual tecla, e
    /// devolver tudo embaralhado seria quase tão ruim quanto perder.</para>
    /// </summary>
    [RequireComponent(typeof(ArtefatosBridge))]
    [AddComponentMenu("Favela Amarela/Persistência/Estado Persistente dos Artefatos")]
    public sealed class EstadoPersistenteDosArtefatos : MonoBehaviour, IPersistente
    {
        private ArtefatosBridge _artefatos;

        /// <inheritdoc />
        public string ChaveDePersistencia => ChavesDeSave.ArtefatosEquipados;

        private void Awake() => _artefatos = GetComponent<ArtefatosBridge>();

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
            if (_artefatos == null) return "";

            var ids = new string[InventarioDeArtefatos.TotalDeSlots];
            for (int i = 0; i < ids.Length; i++)
                ids[i] = _artefatos.Inventario.IdNoSlot(i) ?? "";

            return string.Join(",", ids);
        }

        /// <inheritdoc />
        public void AplicarEstado(string estado)
        {
            if (string.IsNullOrWhiteSpace(estado) || _artefatos == null) return;

            var ids = estado.Split(',');

            for (int slot = 0; slot < InventarioDeArtefatos.TotalDeSlots && slot < ids.Length; slot++)
            {
                string id = ids[slot];
                if (string.IsNullOrWhiteSpace(id)) continue;

                // Artefato que não existe mais (save de outra versão) é ignorado: o slot fica
                // vazio em vez de derrubar o load inteiro.
                _artefatos.Equipar(id, slot);
            }
        }
    }
}
