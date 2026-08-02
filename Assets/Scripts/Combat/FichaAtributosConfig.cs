using UnityEngine;
using FavelaAmarela.Core.Combat;

namespace FavelaAmarela.Runtime.Combat
{
    /// <summary>
    /// Autoria em asset da <see cref="FichaDeAtributos"/> — a "ficha" de uma unidade,
    /// editável no Inspector como arquivo (um asset por tipo: Ficha_Cultista,
    /// Ficha_Damiao, Ficha_Abdul...). Adaptador Runtime puro: só serializa os 5
    /// atributos e cospe o POCO via <see cref="CriarFicha"/> — nenhuma regra de combate
    /// mora aqui (isso é do Core).
    /// </summary>
    [CreateAssetMenu(fileName = "Ficha_Nova", menuName = "Favela Amarela/Ficha de Atributos")]
    public sealed class FichaAtributosConfig : ScriptableObject
    {
        [Header("Vitalidade (corpo)")]
        [Tooltip("Teto da Vitalidade corpórea. Escala 0–100.")]
        [SerializeField] private float vitalidadeMax = 100f;

        [Header("Ofensivo")]
        [Tooltip("Poder ofensivo físico — dano bruto do golpe corpo-a-corpo da unidade.")]
        [SerializeField] private float ataque = 0f;
        [Tooltip("Poder ofensivo anômalo — dano bruto das magias/conjurações (0 se não conjura).")]
        [SerializeField] private float conjuracao = 0f;

        [Header("Defensivo")]
        [Tooltip("Mitigação física — subtraída do dano físico recebido.")]
        [SerializeField] private float defesa = 0f;
        [Tooltip("Mitigação anômala — subtraída do dano de conjuração recebido (defesa mágica).")]
        [SerializeField] private float resistenciaAnomala = 0f;

        /// <summary>
        /// Cria o POCO de atributos a partir dos valores do asset. Clampa defensivamente
        /// para nunca estourar a validação do <see cref="FichaDeAtributos"/> por um asset
        /// mal preenchido (Regra de Ouro 7 — fallback seguro em vez de exceção).
        /// </summary>
        public FichaDeAtributos CriarFicha() => new FichaDeAtributos(
            vitalidadeMax: Mathf.Max(1f, vitalidadeMax),
            ataque: Mathf.Max(0f, ataque),
            defesa: Mathf.Max(0f, defesa),
            conjuracao: Mathf.Max(0f, conjuracao),
            resistenciaAnomala: Mathf.Max(0f, resistenciaAnomala));
    }
}
