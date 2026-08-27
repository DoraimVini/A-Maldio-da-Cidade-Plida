using System.Collections.Generic;
using UnityEngine;

namespace FavelaAmarela.Inventario
{
    /// <summary>
    /// Todos os <see cref="AfixoDef"/> do projeto, carregados de <c>Resources</c>.
    ///
    /// <para><b>Varre a pasta, não mantém lista.</b> Uma lista de afixos escrita à mão seria a
    /// décima a envelhecer neste repositório — e o sintoma seria mudo: o afixo existiria no
    /// disco, não estaria no pool, e ninguém veria erro nenhum. Afixo novo entra sozinho.</para>
    ///
    /// <para>Segue o mesmo caminho do <c>ItemDatabase</c>: <c>Resources.LoadAll</c>, que é a
    /// razão de os assets precisarem viver sob uma pasta <c>Resources/</c> para existirem em
    /// runtime.</para>
    /// </summary>
    public static class CatalogoDeAfixos
    {
        private static AfixoDef[] _cache;

        /// <summary>Todos os afixos autorados. Carregado uma vez e reusado.</summary>
        public static IReadOnlyList<AfixoDef> Todos
        {
            get
            {
                if (_cache == null) Recarregar();
                return _cache;
            }
        }

        /// <summary>
        /// Acha um afixo pelo <c>Id</c> gravado no save. Devolve <c>null</c> quando o afixo foi
        /// removido do projeto depois de já ter caído num item — caso em que o item continua
        /// com o modificador (o VALOR está no save), só perde o rótulo no nome. É a degradação
        /// certa: tirar o efeito puniria o jogador por uma decisão de autoria.
        /// </summary>
        public static AfixoDef PorId(string id)
        {
            if (string.IsNullOrEmpty(id)) return null;

            var todos = Todos;
            for (int i = 0; i < todos.Count; i++)
                if (todos[i] != null && todos[i].Id == id) return todos[i];

            return null;
        }

        /// <summary>
        /// Relê o disco. Chamado sozinho na primeira consulta; público para o Item Creator
        /// poder atualizar o pool depois de criar um afixo, sem reiniciar o Play Mode.
        /// </summary>
        public static void Recarregar()
        {
            _cache = Resources.LoadAll<AfixoDef>("");

            if (_cache == null || _cache.Length == 0)
            {
                // Não é erro: um projeto sem afixos autorados gera itens Inertes, que é um
                // estado válido do jogo. Mas vale o aviso, porque "nenhum item sai com
                // modificador" é indistinguível de um bug quando se está jogando.
                Debug.LogWarning("[CatalogoDeAfixos] Nenhum AfixoDef encontrado em Resources. " +
                                 "Todo item gerado sairá sem modificador.");
                _cache = new AfixoDef[0];
            }
        }
    }
}
