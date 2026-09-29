using System;
using UnityEngine;
using FavelaAmarela.Core.Enemies;

namespace FavelaAmarela.Runtime.Enemies
{
    /// <summary>
    /// Os números do Rito do Olhar como o Inspector os mostra — para calibrar a luta no prefab do
    /// Rei <b>sem recompilar</b>, que é o que o plano promete.
    ///
    /// <para>O Core guarda os números em <see cref="ParametrosDoRito"/>, com propriedades — a Unity
    /// não serializa propriedade. Esta classe é o espelho serializável, e os padrões dela são os
    /// mesmos do Core (calibrados por simulação em <c>ORitoCabeNoTempoTests</c>). Mudar um
    /// número aqui muda o jogo e <b>não</b> muda aquele teste: a simulação usa os padrões do Core.
    /// Por isso a calibração que ficar deve voltar para o Core.</para>
    /// </summary>
    [Serializable]
    public sealed class ConfiguracaoDoRito
    {
        [Header("O laço")]
        [Tooltip("Pontos de selo por segundo, exposto no Altar.")]
        [Min(0.1f)] public float seloPorSegundo = 1f;

        [Tooltip("Resiliência recuperada por segundo, coberto (Ancoragem).")]
        [Min(0f)] public float ancoragemPorSegundo = 3f;

        [Header("Dreno exposto, por fase (RM/s)")]
        [Min(0f)] public float drenoChegada = 6f;
        [Min(0f)] public float drenoMascara = 7f;
        [Min(0f)] public float drenoPeca = 8f;
        [Min(0f)] public float drenoVerbo = 8f;

        [Header("Marcos do selo")]
        [Range(1f, 99f)] public float marcoMascara = 20f;
        [Range(1f, 99f)] public float marcoPeca = 40f;
        [Range(1f, 99f)] public float marcoVerbo = 65f;
        [Range(1f, 99f)] public float marcoQueda = 90f;

        [Tooltip("Segundos da Queda: o selo fecha sozinho nesse tempo.")]
        [Min(1f)] public float duracaoDaQueda = 30f;

        [Header("Relíquias")]
        [Tooltip("Necronomicon: multiplica o avanço do selo.")]
        [Min(0.1f)] public float bonusDoNecronomicon = 1.3f;
        [Tooltip("Patuá das Luas Gêmeas: multiplica a Ancoragem.")]
        [Min(0.1f)] public float bonusDoPatua = 2f;
        [Tooltip("Anel do Sinal Amarelo: multiplica o dreno (menor que 1 protege).")]
        [Min(0.1f)] public float bonusDoAnel = 0.8f;

        [Header("A Máscara (Fase 2)")]
        [Range(10f, 180f)] public float larguraDoCone = 70f;
        [Range(0f, 90f)] public float amplitudeDaVarredura = 60f;
        [Min(1f)] public float duracaoDaVarredura = 8f;
        [Min(1)] public int varredurasEntreAberturas = 3;
        [Min(0.1f)] public float duracaoDaAbertura = 2f;

        [Header("A Peça (Fase 3)")]
        [Min(0.1f)] public float tempoDeLeitura = 1.5f;
        [Min(0f)] public float seloPorFragmento = 8f;
        [Min(0f)] public float custoDoFragmento = 15f;
        [Min(0f)] public float reaparecimentoDoFragmento = 10f;

        [Header("O Verbo (Fase 4)")]
        [Min(1f)] public float intervaloDoVerbo = 8f;
        [Min(0f)] public float custoDoVerbo = 8f;
        [Min(0)] public int coberturasQueResistem = 1;

        /// <summary>Monta os parâmetros do Core com estes números.</summary>
        public ParametrosDoRito Criar() => new ParametrosDoRito
        {
            SeloPorSegundo = seloPorSegundo,
            AncoragemPorSegundo = ancoragemPorSegundo,
            DrenoChegada = drenoChegada,
            DrenoMascara = drenoMascara,
            DrenoPeca = drenoPeca,
            DrenoVerbo = drenoVerbo,
            MarcoMascara = marcoMascara,
            MarcoPeca = marcoPeca,
            MarcoVerbo = marcoVerbo,
            MarcoQueda = marcoQueda,
            DuracaoDaQueda = duracaoDaQueda,
            BonusDoNecronomicon = bonusDoNecronomicon,
            BonusDoPatua = bonusDoPatua,
            BonusDoAnel = bonusDoAnel,
            Olhar = new ParametrosDoOlhar
            {
                LarguraDoCone = larguraDoCone,
                Amplitude = amplitudeDaVarredura,
                DuracaoDaVarredura = duracaoDaVarredura,
                VarredurasEntreAberturas = varredurasEntreAberturas,
                DuracaoDaAbertura = duracaoDaAbertura,
            },
            Fragmentos = new ParametrosDosFragmentos
            {
                TempoDeLeitura = tempoDeLeitura,
                Selo = seloPorFragmento,
                Custo = custoDoFragmento,
                Reaparecimento = reaparecimentoDoFragmento,
            },
            Verbo = new ParametrosDoVerbo
            {
                Intervalo = intervaloDoVerbo,
                Custo = custoDoVerbo,
                CoberturasQueResistem = coberturasQueResistem,
            },
        };
    }
}
