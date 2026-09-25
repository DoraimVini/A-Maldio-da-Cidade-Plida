using System;

namespace FavelaAmarela.Core.Artefatos
{
    /// <summary>
    /// Os Artefatos que Damião carrega ativos. São <b>quatro slots</b>, fixos: um Artefato só
    /// vale — passiva e habilidade — enquanto estiver encaixado aqui.
    ///
    /// <para>O limite existe para ser uma escolha. Quando o catálogo passar de quatro (e vai:
    /// o desenho prevê mais Artefatos), carregar um significa deixar outro para trás.</para>
    ///
    /// <para>Guarda <b>ids</b> de Artefato, não os assets — o Core não conhece
    /// <c>ScriptableObject</c>. A tradução id→asset é do adaptador Runtime.</para>
    /// </summary>
    public sealed class InventarioDeArtefatos
    {
        /// <summary>Quantidade de slots. Quatro, por decisão de design.</summary>
        public const int TotalDeSlots = 4;

        private readonly string[] _slots = new string[TotalDeSlots];

        /// <summary>Disparado a cada mudança de composição dos slots.</summary>
        public event Action OnMudou;

        /// <summary>Número de slots (sempre <see cref="TotalDeSlots"/>).</summary>
        public int Capacidade => TotalDeSlots;

        /// <summary>Id do Artefato no slot, ou <c>null</c> se vazio ou índice inválido.</summary>
        public string IdNoSlot(int slot)
            => EhValido(slot) ? _slots[slot] : null;

        /// <summary>Se este Artefato já está encaixado em algum slot.</summary>
        public bool Contem(string artefatoId)
        {
            if (string.IsNullOrEmpty(artefatoId)) return false;

            for (int i = 0; i < _slots.Length; i++)
                if (_slots[i] == artefatoId) return true;

            return false;
        }

        /// <summary>
        /// Encaixa um Artefato no slot, devolvendo o que estava lá (ou <c>null</c>).
        /// <b>Recusa duplicata:</b> o mesmo Artefato em dois slots daria duas vezes a passiva e
        /// gastaria um slot à toa.
        /// </summary>
        /// <returns>Id do Artefato deslocado, ou <c>null</c> se o slot estava vazio ou a operação falhou.</returns>
        public string Equipar(string artefatoId, int slot)
        {
            if (!EhValido(slot) || string.IsNullOrEmpty(artefatoId)) return null;
            if (_slots[slot] == artefatoId) return null;
            if (Contem(artefatoId)) return null;

            string anterior = _slots[slot];
            _slots[slot] = artefatoId;
            OnMudou?.Invoke();
            return anterior;
        }

        /// <summary>Esvazia o slot e devolve o que saiu (ou <c>null</c>).</summary>
        public string Desequipar(int slot)
        {
            if (!EhValido(slot) || _slots[slot] == null) return null;

            string retirado = _slots[slot];
            _slots[slot] = null;
            OnMudou?.Invoke();
            return retirado;
        }

        /// <summary>Primeiro slot livre, ou -1 se todos estiverem ocupados.</summary>
        public int PrimeiroSlotLivre()
        {
            for (int i = 0; i < _slots.Length; i++)
                if (_slots[i] == null) return i;

            return -1;
        }

        private static bool EhValido(int slot) => slot >= 0 && slot < TotalDeSlots;
    }
}
