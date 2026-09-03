using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using FavelaAmarela.Inventario;

namespace FavelaAmarela.EditorTools
{
    /// <summary>
    /// Cria os <see cref="HabilidadeDef"/> das três armas da Tumba e os liga às famílias.
    ///
    /// <para><b>Os números vêm do CÓDIGO, não da documentação.</b> O
    /// <c>armas_da_tumba.md</c> diz 40/25/60 de dano básico; os construtores dizem
    /// <b>40/30/45</b>. O <c>CLAUDE.md</c> §3.1 regra 4 é explícito: o código é a verdade para
    /// <i>como funciona</i>. Migrar com os números do documento mudaria o balanceamento de
    /// duas armas em silêncio, no meio de uma refatoração — que é a pior hora possível para
    /// alguém descobrir que o jogo ficou diferente.</para>
    ///
    /// <para>A equivalência é <b>provada por teste</b> antes de as classes C# saírem:
    /// <c>EquivalenciaDaMigracaoTests</c> compara campo a campo o <c>ArmaResult</c> da arma a
    /// dado com o da classe que ela substitui.</para>
    /// </summary>
    public static class MontarHabilidadesDasArmas
    {
        private const string PastaDasHabilidades = "Assets/FavelaAmarela/Config/Habilidades";
        private const string PastaDasBases = "Assets/FavelaAmarela/Config/Armas";

        [MenuItem("Tools/FavelaAmarela/Armas: montar as habilidades a dado")]
        public static void Executar()
        {
            GarantirPasta();

            var resumo = new List<string>
            {
                MontarMaca(),
                MontarEstilete(),
                MontarAlfanje(),
            };

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            Debug.Log("[HabilidadesDasArmas] Concluído:\n  " + string.Join("\n  ", resumo));
        }

        private static void GarantirPasta()
        {
            if (AssetDatabase.IsValidFolder(PastaDasHabilidades)) return;
            AssetDatabase.CreateFolder("Assets/FavelaAmarela/Config", "Habilidades");
        }

        // ── As três armas, com os números dos construtores ────────────────────

        /// <summary>Maça de Aklo — o anti-mago. Interrompe conjuração na habilidade.</summary>
        private static string MontarMaca() => Montar(
            arquivo: "Habilidade_MacaDeAklo",
            baseDeArma: "BaseArma_Maca",
            nomeDaArma: "Maça de Aklo",
            nomeDaHabilidade: "Calar o Aklo",
            duracaoBasico: 0.35f, cooldownBasico: 0.5f,
            duracaoHabilidade: 0.4f, cooldownHabilidade: 6f,
            basico: new[] { Dano(40f) },
            habilidade: new[] { Dano(30f), Interrupcao() });

        /// <summary>Estilete de Irem — dano por permanência, não por pico.</summary>
        private static string MontarEstilete() => Montar(
            arquivo: "Habilidade_EstileteDeIrem",
            baseDeArma: "BaseArma_LaminaFina",
            nomeDaArma: "Estilete de Irem",
            nomeDaHabilidade: "Ferida de Aklo",
            duracaoBasico: 0.25f, cooldownBasico: 0.3f,
            duracaoHabilidade: 0.3f, cooldownHabilidade: 5f,
            // O básico ABRE 1 acúmulo: é isso que torna o teto alcançável. Com cooldown de
            // 0,3 s, manter a pressão sobe a contagem depressa; a habilidade sozinha
            // (cooldown 5 s) levaria quase um minuto para chegar lá.
            basico: new[] { Dano(30f), Sangramento(1, 4f, 5f) },
            habilidade: new[] { Dano(15f), Sangramento(3, 4f, 5f) });

        /// <summary>Alfanje de Alhazred — força bruta e espaço.</summary>
        private static string MontarAlfanje() => Montar(
            arquivo: "Habilidade_AlfanjeDeAlhazred",
            baseDeArma: "BaseArma_Alfanje",
            nomeDaArma: "Alfanje de Alhazred",
            nomeDaHabilidade: "Golpe do Deserto",
            duracaoBasico: 0.45f, cooldownBasico: 0.7f,
            duracaoHabilidade: 0.5f, cooldownHabilidade: 5f,
            basico: new[] { Dano(45f) },
            habilidade: new[] { Dano(40f), Atordoamento(2f), Repulsao(6f) });

        // ── Construtores de efeito, para a tabela acima ler como design ───────

        private static EfeitoAutorado Dano(float v) =>
            new EfeitoAutorado { Tipo = TipoDeEfeito.Dano, Valor = v };

        private static EfeitoAutorado Atordoamento(float segundos) =>
            new EfeitoAutorado { Tipo = TipoDeEfeito.Atordoamento, Valor = segundos };

        private static EfeitoAutorado Repulsao(float forca) =>
            new EfeitoAutorado { Tipo = TipoDeEfeito.Repulsao, Valor = forca };

        private static EfeitoAutorado Interrupcao() =>
            new EfeitoAutorado { Tipo = TipoDeEfeito.Interrupcao };

        private static EfeitoAutorado Sangramento(int acumulos, float porSegundo, float duracao) =>
            new EfeitoAutorado
            {
                Tipo = TipoDeEfeito.Sangramento,
                Acumulos = acumulos,
                Valor = porSegundo,
                Duracao = duracao,
            };

        private static string Montar(string arquivo, string baseDeArma,
                                     string nomeDaArma, string nomeDaHabilidade,
                                     float duracaoBasico, float cooldownBasico,
                                     float duracaoHabilidade, float cooldownHabilidade,
                                     EfeitoAutorado[] basico, EfeitoAutorado[] habilidade)
        {
            string caminho = $"{PastaDasHabilidades}/{arquivo}.asset";

            var def = AssetDatabase.LoadAssetAtPath<HabilidadeDef>(caminho);
            bool existia = def != null;

            if (!existia)
            {
                def = ScriptableObject.CreateInstance<HabilidadeDef>();
                AssetDatabase.CreateAsset(def, caminho);
            }

            def.NomeDaArma = nomeDaArma;
            def.NomeDaHabilidade = nomeDaHabilidade;
            def.DuracaoBasico = duracaoBasico;
            def.CooldownBasico = cooldownBasico;
            def.DuracaoHabilidade = duracaoHabilidade;
            def.CooldownHabilidade = cooldownHabilidade;
            def.EfeitosDoBasico = new List<EfeitoAutorado>(basico);
            def.EfeitosDaHabilidade = new List<EfeitoAutorado>(habilidade);

            EditorUtility.SetDirty(def);
            AssetDatabase.SaveAssetIfDirty(def);

            // Ligar na família é o que faz a habilidade existir para o jogo. Uma HabilidadeDef
            // criada e não ligada seria mais uma peça que existe e não está em lugar nenhum --
            // o modo de falha dominante deste repositório.
            string caminhoBase = $"{PastaDasBases}/{baseDeArma}.asset";
            var familia = AssetDatabase.LoadAssetAtPath<BaseDeArma>(caminhoBase);

            if (familia == null)
                return $"{arquivo}: habilidade {(existia ? "atualizada" : "criada")}, mas a " +
                       $"família '{baseDeArma}' NÃO FOI ENCONTRADA — não está ligada a nada";

            familia.Habilidade = def;
            EditorUtility.SetDirty(familia);
            AssetDatabase.SaveAssetIfDirty(familia);

            return $"{arquivo}: {basico.Length} efeito(s) no básico, " +
                   $"{habilidade.Length} na habilidade → ligada em {baseDeArma}";
        }
    }
}
