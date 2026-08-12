using System.Collections.Generic;
using UnityEngine;
using FavelaAmarela.Core.Artefatos;
using FavelaAmarela.Core.Persistencia;
using FavelaAmarela.Player;

namespace FavelaAmarela.Runtime.Persistencia
{
    /// <summary>
    /// Camada Runtime. Faz os Artefatos sobreviverem à troca de cena — tanto os
    /// <b>portados</b> nos quatro slots quanto os <b>dormentes</b>, que Damião possui sem
    /// estar usando.
    ///
    /// <para>Formato: <c>"portados|possuídos"</c>. Antes da barra, os ids na ordem dos slots
    /// com slot vazio virando campo em branco; depois, todos os possuídos. Exemplo:
    /// <c>"necronomicon,,coroa_de_ossos,|necronomicon,coroa_de_ossos,patua_luas_gemeas"</c> —
    /// o Patuá está guardado, fora dos slots. É legível ao depurar e a <b>posição importa</b>:
    /// o jogador escolheu qual Artefato fica em qual tecla.</para>
    ///
    /// <para><b>Save antigo (sem a barra)</b> é lido como só a lista de portados, e a posse é
    /// deduzida deles. É o formato de antes de 2026-08-12, quando não existia o conceito de
    /// Artefato dormente — nem, na prática, caminho de gameplay para adquirir um.</para>
    /// </summary>
    [RequireComponent(typeof(ArtefatosBridge))]
    [AddComponentMenu("Favela Amarela/Persistência/Estado Persistente dos Artefatos")]
    public sealed class EstadoPersistenteDosArtefatos : MonoBehaviour, IPersistente
    {
        private const char SeparadorDeSecao = '|';
        private const char SeparadorDeId = ',';

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

            var portados = new string[InventarioDeArtefatos.TotalDeSlots];
            for (int i = 0; i < portados.Length; i++)
                portados[i] = _artefatos.Inventario.IdNoSlot(i) ?? "";

            string secaoPortados = string.Join(SeparadorDeId.ToString(), portados);
            string secaoPossuidos = string.Join(SeparadorDeId.ToString(), _artefatos.Inventario.Possuidos);

            return secaoPortados + SeparadorDeSecao + secaoPossuidos;
        }

        /// <inheritdoc />
        public void AplicarEstado(string estado)
        {
            if (string.IsNullOrWhiteSpace(estado) || _artefatos == null) return;

            var secoes = estado.Split(SeparadorDeSecao);

            var portados = secoes[0].Split(SeparadorDeId);
            var possuidos = secoes.Length > 1
                ? Limpar(secoes[1].Split(SeparadorDeId))
                : Limpar(portados); // save antigo: só existia o que estava nos slots

            // Restaurar, e não Adquirir/Equipar: o caminho diegético porta no primeiro slot
            // livre e embaralharia as teclas escolhidas pelo jogador.
            _artefatos.Inventario.Restaurar(possuidos, portados);
        }

        private static List<string> Limpar(string[] ids)
        {
            var limpos = new List<string>(ids.Length);
            for (int i = 0; i < ids.Length; i++)
            {
                if (string.IsNullOrWhiteSpace(ids[i])) continue;
                limpos.Add(ids[i].Trim());
            }
            return limpos;
        }
    }
}
