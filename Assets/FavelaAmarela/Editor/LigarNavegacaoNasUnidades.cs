using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
using FavelaAmarela.Runtime.Navegacao;

namespace FavelaAmarela.EditorTools
{
    /// <summary>
    /// Põe o <see cref="SeguidorDeCaminho"/> nas unidades que andam pelo chão.
    ///
    /// <para><b>Por que uma ferramenta, e não "é só arrastar o componente".</b> O
    /// <c>SeguidorDeCaminho</c> é <b>opcional por design</b>: sem ele o movimento degrada para
    /// a linha reta de sempre, em vez de a unidade travar. Essa escolha é boa e traz um risco
    /// conhecido — código que existe, compila, passa nos testes e <b>não está em prefab
    /// nenhum</b>. É o modo de falha assinatura deste repositório, e já cobrou caro nove
    /// vezes.</para>
    ///
    /// <para><c>NavegacaoNasUnidadesTests</c> guarda o resultado: quem anda pelo chão tem de
    /// ter o componente, e quem voa não pode ter.</para>
    /// </summary>
    public static class LigarNavegacaoNasUnidades
    {
        private const string Marcador = "[Navegacao]";

        /// <summary>
        /// Quem anda pelo chão e precisa contornar. Lista escrita à mão de propósito: <b>quem
        /// contorna e quem não contorna é decisão de design</b>, não dedução.
        /// </summary>
        private static readonly (string Caminho, string Razao)[] AndamNoChao =
        {
            ("Assets/FavelaAmarela/Art/Enemies/Cultista.prefab",
             "a tropa do jogo, e a que mais persegue — onze deles no Deserto"),

            ("Assets/FavelaAmarela/Art/Enemies/CoisaDoCemiterio.prefab",
             "caça por faro, e faro não atravessa parede"),

            ("Assets/FavelaAmarela/Art/Enemies/EspectroHali.prefab",
             "cerca o jogador; cercar sem contornar é encostar no muro"),

            ("Assets/FavelaAmarela/Art/Enemies/EsqueletoInvocado.prefab",
             "invocado em fluxo dentro da arena do Abdul, com as Pedras no caminho"),

            ("Assets/FavelaAmarela/Art/Characters/MiGo/YugNeth.prefab",
             "O COMPANHEIRO. É quem mais custa perder atrás de um muro: ele some, o jogador " +
             "não entende por quê, e a barra dele continua acusando presença"),
        };

        /// <summary>
        /// Quem <b>não</b> deve contornar, com a razão. Estar aqui é decisão registrada; estar
        /// fora das duas listas é esquecimento, e o guarda grita.
        /// </summary>
        private static readonly (string Caminho, string Razao)[] NaoContornam =
        {
            ("Assets/FavelaAmarela/Art/Enemies/Byakhee.prefab",
             "VOA. Contornar obstáculo de chão inverteria a identidade da luta — os rasantes e " +
             "o mergulho existem justamente porque ele ignora o terreno"),

            ("Assets/FavelaAmarela/Art/Enemies/ConeDeGelo.prefab",
             "é projétil: linha reta É o comportamento correto"),

            ("Assets/FavelaAmarela/Art/Enemies/Abdul_Alhazred.prefab",
             "não se desloca — luta parado no centro da arena, conjurando"),

            ("Assets/FavelaAmarela/Art/Enemies/ReiEmAmarelo.prefab",
             "não se desloca; o ritual acontece em torno dele"),

            ("Assets/FavelaAmarela/Art/Enemies/PedraDePoder.prefab",
             "é cenário quebrável, não anda"),
        };

        [MenuItem("Tools/FavelaAmarela/Navegação: ligar nas unidades que andam")]
        public static void Executar()
        {
            var resumo = new List<string>();

            foreach (var (caminho, razao) in AndamNoChao)
                resumo.Add(Aplicar(caminho, razao));

            foreach (var (caminho, razao) in NaoContornam)
                resumo.Add($"{Path.GetFileNameWithoutExtension(caminho)}: SEM navegação por " +
                           $"decisão — {razao}");

            AssetDatabase.SaveAssets();

            string quebra = System.Environment.NewLine + "  ";
            Debug.Log($"{Marcador} Concluído:" + quebra + string.Join(quebra, resumo));
        }

        private static string Aplicar(string caminho, string razao)
        {
            string nome = Path.GetFileNameWithoutExtension(caminho);

            if (!File.Exists(caminho)) return $"{nome}: PREFAB AUSENTE";

            var raiz = PrefabUtility.LoadPrefabContents(caminho);

            try
            {
                if (raiz.GetComponent<SeguidorDeCaminho>() != null)
                    return $"{nome}: já tinha";

                raiz.AddComponent<SeguidorDeCaminho>();

                PrefabUtility.SaveAsPrefabAsset(raiz, caminho, out bool gravou);

                return gravou
                    ? $"{nome}: SeguidorDeCaminho acrescentado — {razao}"
                    : $"{nome}: SaveAsPrefabAsset RECUSOU";
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(raiz);
            }
        }
    }
}
