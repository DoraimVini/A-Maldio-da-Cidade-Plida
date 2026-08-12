using System;

namespace FavelaAmarela.Core.Combat
{
    /// <summary>
    /// Calcula a ficha final de atributos somando a ficha base com os bônus dos equipamentos.
    /// POCO puro, não depende de Unity.
    /// </summary>
    public sealed class FichaDeEquipamentos
    {
        private FichaDeAtributos _fichaBase;

        // Bônus vindos dos equipamentos
        private float _bonusAtaque;
        private float _bonusDefesa;
        private float _bonusConjuracao;
        private float _bonusResistenciaAnomala;
        private float _bonusVitalidadeMax;

        public FichaDeEquipamentos(FichaDeAtributos fichaBase)
        {
            _fichaBase = fichaBase ?? throw new ArgumentNullException(nameof(fichaBase));
        }

        /// <summary>Atualiza a ficha base do personagem.</summary>
        public void SetFichaBase(FichaDeAtributos novaBase)
        {
            _fichaBase = novaBase ?? throw new ArgumentNullException(nameof(novaBase));
        }

        /// <summary>
        /// Aplica os bônus provindos de um traje ou relíquia.
        /// </summary>
        public void AplicarBonus(float ataque, float defesa, float conjuracao, float resistenciaAnomala, float vitalidadeMax = 0f)
        {
            _bonusAtaque += ataque;
            _bonusDefesa += defesa;
            _bonusConjuracao += conjuracao;
            _bonusResistenciaAnomala += resistenciaAnomala;
            _bonusVitalidadeMax += vitalidadeMax;
        }

        /// <summary>
        /// Reseta todos os bônus acumulados. Útil ao recalcular quando o jogador troca de equipamento.
        /// </summary>
        public void LimparBonus()
        {
            _bonusAtaque = 0f;
            _bonusDefesa = 0f;
            _bonusConjuracao = 0f;
            _bonusResistenciaAnomala = 0f;
            _bonusVitalidadeMax = 0f;
        }

        /// <summary>
        /// Retorna a ficha final, somando a base aos bônus atuais.
        /// </summary>
        public FichaDeAtributos CalcularFichaFinal()
        {
            return new FichaDeAtributos(
                vitalidadeMax: Math.Max(1f, _fichaBase.VitalidadeMax + _bonusVitalidadeMax),
                ataque: Math.Max(0f, _fichaBase.Ataque + _bonusAtaque),
                defesa: Math.Max(0f, _fichaBase.Defesa + _bonusDefesa),
                conjuracao: Math.Max(0f, _fichaBase.Conjuracao + _bonusConjuracao),
                resistenciaAnomala: Math.Max(0f, _fichaBase.ResistenciaAnomala + _bonusResistenciaAnomala)
            );
        }
    }
}
