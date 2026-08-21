using System.IO;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEngine;

namespace FavelaAmarela.EditorTools
{
    /// <summary>
    /// Ajusta o tamanho do Damião em mundo pelo <c>localScale</c> do prefab.
    ///
    /// <para><b>Por que aqui e não na PPU:</b> o prefab traz
    /// <c>localScale = 0,2857</c> (dois sétimos). Com a sprite em 2,62 unidades, isso o deixa
    /// com <b>0,75 un</b> \u2014 menor que o Cultista (1,80), que o Espectro (1,95) e seis vezes menor
    /// que o Byakhee. O protagonista era o menor do elenco. Mexer na PPU não resolveria: a
    /// escala do prefab multiplicaria por cima e ele chegaria a 1,50, ainda abaixo dos
    /// cultistas.</para>
    ///
    /// <para><b>O colisor acompanha, e isso é aceitável aqui.</b> <c>localScale</c> na raiz
    /// escala o <c>BoxCollider2D</c> junto: 1,75 × 0,84 = <b>1,47</b> de pegada. Os corredores do
    /// Castelo têm 4 de largura, então cabe com folga. Foi por isso que a conta foi feita antes
    /// de escolher o número, e não depois.</para>
    ///
    /// <para><b>Escala não conserta legibilidade.</b> O ocre do Damião tem luminância 0,313 —
    /// no meio da faixa — então nenhuma cor mais clara que ele alcança 3:1, e nenhuma mais
    /// escura o faz sem virar quase preto. Um Damião maior continua se fundindo na areia. Quem
    /// resolve isso é contorno e sombra na sprite, não tamanho.</para>
    /// </summary>
    public static class EscalaDoDamiao
    {
        private const string Prefab = "Assets/FavelaAmarela/Art/Characters/Damiao/Player_Damiao.prefab";

        /// <summary>
        /// Altura da sprite em unidades (84 px a PPU 32). Usada para converter "quero N unidades"
        /// em escala.
        /// </summary>
        private const float AlturaDaSprite = 88f / 32f;

        /// <summary>
        /// Altura da <b>figura</b> em px, sem a elipse de sombra (opaco de y=3 a y=83 no quadro
        /// de 88). É por ela que Damião e Cultista se comparam: as duas folhas têm margens de
        /// sombra diferentes, então igualar altura de imagem deixaria os corpos desiguais.
        /// </summary>
        private const float AlturaDaFiguraEmPx = 81f;

        /// <summary>
        /// Alvo em unidades de mundo para o quadro inteiro (a figura fica em ~2,12; os 0,08
        /// restantes são a margem da sombra).
        ///
        /// <para><b>Correção de 2026-08-20.</b> Este comentário dizia que 2,2 "põe o Damião logo
        /// acima do Cultista (1,80) e do Espectro (1,95) — proporção de protagonista contra
        /// capangas". <b>Era justificativa inventada.</b> O 1,80 do Cultista não era decisão de
        /// design: era só o que o <c>localScale 1.8</c> do prefab dele produzia sobre uma folha
        /// antiga de 32 px. Eu li um acidente como calibragem e construí uma teoria de leitura
        /// de cena em cima dele.</para>
        ///
        /// <para>O Vini corrigiu: <i>"o Damião e o Cultista têm que ser do mesmo tamanho"</i>.
        /// Os dois são humanos e saíram do mesmo rig. O Cultista foi realinhado à figura do
        /// Damião em <c>MontarAnimacaoDoCultista</c>, e <c>AnimacaoDoCultistaTests</c> guarda a
        /// igualdade. O 2,2 daqui continua válido — o que era falso era a comparação.</para>
        /// </summary>
        private const float AlturaAlvo = 2.2f;

        /// <summary>
        /// Altura da figura em unidades — o alvo de verdade, e o número que
        /// <c>MontarAnimacaoDoCultista</c> copia para o Cultista medir igual.
        /// </summary>
        private const float AlturaDaFiguraAlvo = 2.12f;

        [MenuItem("Tools/FavelaAmarela/Escala: ajustar o Damião")]
        public static void Executar()
        {
            // Pela FIGURA, não pelo quadro. Antes de 2026-08-21 esta conta usava 84 px de
            // altura de quadro, que ficou obsoleta quando o contorno expandiu as folhas para 88
            // — e a ferramenta seguia gravando um número derivado de um quadro que não existe
            // mais.
            float escala = AlturaDaFiguraAlvo / (AlturaDaFiguraEmPx / 32f);

            var raiz = PrefabUtility.LoadPrefabContents(Prefab);
            if (raiz == null)
            {
                Debug.LogError($"[EscalaDoDamiao] Prefab não carregou: {Prefab}");
                return;
            }

            float antes = raiz.transform.localScale.x;

            try
            {
                raiz.transform.localScale = new Vector3(escala, escala, 1f);
                PrefabUtility.SaveAsPrefabAsset(raiz, Prefab, out bool salvou);

                if (!salvou)
                {
                    Debug.LogError("[EscalaDoDamiao] SaveAsPrefabAsset recusou salvar.");
                    return;
                }
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(raiz);
            }

            AssetDatabase.Refresh();

            // Confere no DISCO. O retorno da API já mentiu neste projeto mais de uma vez.
            var m = Regex.Match(File.ReadAllText(Prefab),
                                @"m_LocalScale:\s*\{x:\s*([\d.eE+-]+)");

            if (!m.Success)
            {
                Debug.LogError("[EscalaDoDamiao] Não consegui reler o localScale do prefab.");
                return;
            }

            float gravado = float.Parse(m.Groups[1].Value,
                                        System.Globalization.CultureInfo.InvariantCulture);

            if (Mathf.Abs(gravado - escala) > 0.001f)
            {
                Debug.LogError($"[EscalaDoDamiao] O disco tem {gravado}, esperado {escala}.");
                return;
            }

            Debug.Log($"[EscalaDoDamiao] localScale {antes:0.0000} → {escala:0.0000}. " +
                      $"Altura em mundo {antes * AlturaDaSprite:0.00} → {AlturaAlvo:0.00} un. " +
                      $"Pegada do colisor: {1.75f * escala:0.00} (corredor tem 4).");
        }
    }
}
