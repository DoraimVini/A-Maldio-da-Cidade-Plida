using System;
using System.Collections.Generic;

namespace FavelaAmarela.Core.Artefatos
{
    /// <summary>
    /// Os Artefatos de Damião — <b>posse</b> e <b>porte</b>, que são coisas diferentes.
    ///
    /// <para><b>Posse</b> não tem limite nem custo: um Artefato recolhido fica aqui para
    /// sempre e <b>não ocupa espaço no Bolsão Frio</b>. <b>Porte</b> são os quatro slots:
    /// um Artefato só vale — passiva e habilidade — enquanto estiver encaixado num deles.
    /// Os demais ficam <b>dormentes</b>, guardados e sem efeito.</para>
    ///
    /// <para>O limite de quatro existe para ser uma escolha. Quando o catálogo passar de
    /// quatro (e vai: o desenho prevê mais Artefatos), portar um significa adormecer outro —
    /// mas nunca perdê-lo.</para>
    ///
    /// <para>Guarda <b>ids</b> de Artefato, não os assets — o Core não conhece
    /// <c>ScriptableObject</c>. A tradução id→asset é do adaptador Runtime.</para>
    /// </summary>
    public sealed class InventarioDeArtefatos
    {
        /// <summary>Quantidade de slots de porte. Quatro, por decisão de design.</summary>
        public const int TotalDeSlots = 4;

        private readonly string[] _slots = new string[TotalDeSlots];

        // Sem teto de propósito: o número total de Artefatos do jogo ainda não foi decidido,
        // e um limite chutado aqui viraria perda silenciosa de progresso ao ser atingido.
        private readonly List<string> _possuidos = new List<string>();

        /// <summary>Disparado a cada mudança de posse ou de composição dos slots.</summary>
        public event Action OnMudou;

        /// <summary>Número de slots de porte (sempre <see cref="TotalDeSlots"/>).</summary>
        public int Capacidade => TotalDeSlots;

        /// <summary>Todos os Artefatos que Damião possui, portados ou dormentes.</summary>
        public IReadOnlyList<string> Possuidos => _possuidos;

        /// <summary>Id do Artefato no slot, ou <c>null</c> se vazio ou índice inválido.</summary>
        public string IdNoSlot(int slot)
            => EhValido(slot) ? _slots[slot] : null;

        /// <summary>
        /// Se este Artefato está <b>encaixado num slot</b> — portanto ativo, com passiva e
        /// habilidade valendo.
        ///
        /// <para><b>Não confundir com <see cref="Possui"/>.</b> O rito do Rei em Amarelo
        /// (<c>PontoFocalDeReliquia</c>) exige porte, não posse: a relíquia tem de estar na
        /// mão para responder ao ponto focal.</para>
        /// </summary>
        public bool Contem(string artefatoId)
        {
            if (string.IsNullOrEmpty(artefatoId)) return false;

            for (int i = 0; i < _slots.Length; i++)
                if (_slots[i] == artefatoId) return true;

            return false;
        }

        /// <summary>
        /// Se Damião <b>tem</b> este Artefato, portado ou dormente. É o que uma porta selada
        /// pergunta: carregar o tomo basta, não importa em que slot ele está.
        /// </summary>
        public bool Possui(string artefatoId)
            => !string.IsNullOrEmpty(artefatoId) && _possuidos.Contains(artefatoId);

        /// <summary>
        /// Registra a posse de um Artefato e, se houver slot livre, já o porta — recolher uma
        /// relíquia e ela não aparecer em lugar nenhum seria desconcertante.
        /// </summary>
        /// <returns>true se a posse era nova; false se já possuía (recolher duas vezes não duplica).</returns>
        public bool Adquirir(string artefatoId)
        {
            if (string.IsNullOrEmpty(artefatoId)) return false;
            if (_possuidos.Contains(artefatoId)) return false;

            _possuidos.Add(artefatoId);

            int livre = PrimeiroSlotLivre();
            if (livre >= 0)
            {
                // Atribuição direta: Equipar dispararia um segundo OnMudou para a mesma aquisição.
                _slots[livre] = artefatoId;
            }

            OnMudou?.Invoke();
            return true;
        }

        /// <summary>
        /// Encaixa um Artefato num slot, devolvendo o que estava lá. O deslocado continua
        /// possuído — vira dormente, não some.
        ///
        /// <para><b>Portar implica possuir:</b> encaixar um Artefato registra a posse dele se
        /// ainda não havia. Equipar é uma afirmação mais forte que ter, então exigir posse
        /// prévia aqui só criaria uma ordem de chamada para o chamador decorar. Quem quer a
        /// política de "só equipa o que já é seu" é a camada Runtime
        /// (<c>ArtefatosBridge.Equipar</c>), que serve a UI.</para>
        ///
        /// <para><b>Recusa duplicata:</b> o mesmo Artefato em dois slots daria duas vezes a
        /// passiva e gastaria um slot à toa.</para>
        /// </summary>
        /// <returns>Id do Artefato deslocado, ou <c>null</c> se o slot estava vazio ou a operação falhou.</returns>
        public string Equipar(string artefatoId, int slot)
        {
            if (!EhValido(slot) || string.IsNullOrEmpty(artefatoId)) return null;
            if (_slots[slot] == artefatoId) return null;
            if (Contem(artefatoId)) return null;

            if (!_possuidos.Contains(artefatoId)) _possuidos.Add(artefatoId);

            string anterior = _slots[slot];
            _slots[slot] = artefatoId;
            OnMudou?.Invoke();
            return anterior;
        }

        /// <summary>
        /// Esvazia o slot e devolve o que saiu. O Artefato <b>continua possuído</b>, apenas
        /// adormece — desequipar nunca descarta.
        /// </summary>
        public string Desequipar(int slot)
        {
            if (!EhValido(slot) || _slots[slot] == null) return null;

            string retirado = _slots[slot];
            _slots[slot] = null;
            OnMudou?.Invoke();
            return retirado;
        }

        /// <summary>
        /// Reconstrói posse e porte a partir de um save. <b>Não é ação diegética</b> — não é
        /// recolher nem equipar, é reconstrução de estado a partir do disco, mesmo papel de
        /// <c>Vitalidade.Restaurar</c>.
        ///
        /// <para>Existe porque <see cref="Adquirir"/> porta no primeiro slot livre por
        /// conveniência, e usá-lo para carregar um save <b>embaralharia a ordem</b> — o jogador
        /// escolheu qual Artefato fica em qual tecla, e devolver tudo trocado seria quase tão
        /// ruim quanto perder.</para>
        ///
        /// <para>Entradas inconsistentes são descartadas em silêncio em vez de derrubar o load:
        /// id portado que não consta como possuído, e duplicata em dois slots.</para>
        /// </summary>
        public void Restaurar(IReadOnlyList<string> possuidos, IReadOnlyList<string> equipadosPorSlot)
        {
            _possuidos.Clear();

            if (possuidos != null)
            {
                for (int i = 0; i < possuidos.Count; i++)
                {
                    string id = possuidos[i];
                    if (!string.IsNullOrEmpty(id) && !_possuidos.Contains(id)) _possuidos.Add(id);
                }
            }

            for (int i = 0; i < _slots.Length; i++) _slots[i] = null;

            if (equipadosPorSlot != null)
            {
                for (int i = 0; i < _slots.Length && i < equipadosPorSlot.Count; i++)
                {
                    string id = equipadosPorSlot[i];
                    if (string.IsNullOrEmpty(id)) continue;
                    if (!_possuidos.Contains(id)) continue;
                    if (SlotDe(id) >= 0) continue;

                    _slots[i] = id;
                }
            }

            OnMudou?.Invoke();
        }

        /// <summary>Artefatos possuídos que <b>não</b> estão em nenhum slot.</summary>
        public List<string> Dormentes()
        {
            var fora = new List<string>();
            for (int i = 0; i < _possuidos.Count; i++)
                if (!Contem(_possuidos[i])) fora.Add(_possuidos[i]);

            return fora;
        }

        /// <summary>Em que slot este Artefato está portado, ou -1 se dormente ou ausente.</summary>
        public int SlotDe(string artefatoId)
        {
            if (string.IsNullOrEmpty(artefatoId)) return -1;

            for (int i = 0; i < _slots.Length; i++)
                if (_slots[i] == artefatoId) return i;

            return -1;
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
