using System;
using System.Collections.Generic;

namespace FavelaAmarela.Core.Persistencia
{
    /// <summary>
    /// POCO puro. O save <b>em memória</b>: um mapa de <b>chave de persistência</b> →
    /// estado serializado. É a fonte da verdade durante a partida; o arquivo em disco
    /// (<see cref="EstadoDeSave"/>) é só a fotografia dele.
    ///
    /// <para><b>Por que chaves e não referências:</b> a Unity recria cenas e objetos do zero
    /// a cada carregamento, então nada garante que o "Baú" da cena nova é o mesmo objeto que
    /// o jogador abriu antes. A chave é o documento de identidade que costura os dois.</para>
    ///
    /// <para><b>Nunca use nome de objeto ou caminho de hierarquia como chave.</b> Renomear
    /// o objeto ou movê-lo de pai mudaria a chave, o save não seria encontrado, e o jogador
    /// perderia progresso silenciosamente — o baú voltaria a aparecer fechado. Use o GUID
    /// imutável gerado uma única vez (ver <c>ObjetoPersistente</c> no Runtime).</para>
    ///
    /// <para><b>Degradação graciosa</b> (nunca lançar por dado estranho): chave nula ou
    /// vazia é ignorada; chave presente no save cujo objeto não existe mais na cena é
    /// simplesmente ignorada; objeto novo sem chave no save assume o estado padrão.</para>
    /// </summary>
    public sealed class RegistroDeSave
    {
        private readonly Dictionary<string, string> _valores;

        /// <summary>Quantas chaves estão registradas.</summary>
        public int Contagem => _valores.Count;

        /// <summary>Cria um registro vazio (partida nova).</summary>
        public RegistroDeSave()
        {
            _valores = new Dictionary<string, string>(StringComparer.Ordinal);
        }

        /// <summary>
        /// Grava (ou sobrescreve) o estado de uma chave. Chave nula/vazia é ignorada em
        /// silêncio — é dado malformado, não motivo para derrubar o save inteiro.
        /// </summary>
        public void Definir(string chave, string valor)
        {
            if (string.IsNullOrWhiteSpace(chave)) return;
            _valores[chave] = valor;
        }

        /// <summary>Lê o estado de uma chave. Retorna false se ela não existe.</summary>
        public bool TentarObter(string chave, out string valor)
        {
            if (string.IsNullOrWhiteSpace(chave))
            {
                valor = null;
                return false;
            }

            return _valores.TryGetValue(chave, out valor);
        }

        /// <summary>Lê o estado de uma chave, ou <paramref name="padrao"/> se ela não existe.</summary>
        public string ObterOuPadrao(string chave, string padrao = null)
            => TentarObter(chave, out var valor) ? valor : padrao;

        /// <summary>Se a chave existe no registro.</summary>
        public bool Contem(string chave)
            => !string.IsNullOrWhiteSpace(chave) && _valores.ContainsKey(chave);

        /// <summary>Remove uma chave. Retorna se havia algo para remover.</summary>
        public bool Remover(string chave)
            => !string.IsNullOrWhiteSpace(chave) && _valores.Remove(chave);

        /// <summary>Esvazia o registro (começar uma partida nova).</summary>
        public void Limpar() => _valores.Clear();

        /// <summary>Converte para o formato de disco.</summary>
        public EstadoDeSave ParaEstado()
        {
            var estado = new EstadoDeSave();
            foreach (var par in _valores)
                estado.Entradas.Add(new EntradaDeSave(par.Key, par.Value));

            return estado;
        }

        /// <summary>
        /// Reconstrói o registro a partir de um estado lido do disco. Tolera arquivo nulo,
        /// lista nula, entradas nulas e chaves repetidas (a última vence) — um save
        /// corrompido em parte não pode impedir a partida de carregar.
        /// </summary>
        public static RegistroDeSave DeEstado(EstadoDeSave estado)
        {
            var registro = new RegistroDeSave();
            if (estado?.Entradas == null) return registro;

            foreach (var entrada in estado.Entradas)
            {
                if (entrada == null) continue;
                registro.Definir(entrada.Chave, entrada.Valor);
            }

            return registro;
        }
    }
}
