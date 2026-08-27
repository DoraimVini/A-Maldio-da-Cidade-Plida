using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using NUnit.Framework;
using UnityEditor;
using FavelaAmarela.Core.Abilities;
using FavelaAmarela.Core.Factories;
using FavelaAmarela.Inventario;

namespace FavelaAmarela.Tests.EditMode
{
    /// <summary>
    /// A rede que precisa existir <b>antes</b> de a arma virar dado.
    ///
    /// <para><b>O buraco que isto fecha (medido em 2026-08-27).</b> A <c>WeaponFactory</c> é o
    /// único ponto de produção onde um item equipado vira uma arma jogável — e tinha
    /// <b>zero</b> cobertura de teste. Pior: <c>Criar</c> devolve <c>null</c> por "degradação
    /// graciosa" quando não conhece o valor, então uma arma que sumisse do dicionário viraria
    /// Damião desarmado <b>sem uma linha no console</b>. O <c>ArmasDaTumbaTests</c> não pega
    /// isso: ele instancia as classes na mão (<c>new CravoDeAklo()</c>) e nunca passa pela
    /// fábrica.</para>
    ///
    /// <para>Estes guardas continuam valendo depois da migração para dado — o que muda é de
    /// onde a fábrica lê, não o contrato de que <b>toda arma autorada tem de virar uma arma
    /// que causa dano</b>.</para>
    /// </summary>
    public sealed class ArmaDeDadoTests
    {
        private const string PastaDosItens = "Assets/FavelaAmarela/Config/Resources/Itens";

        /// <summary>
        /// Todo <c>ItemDef</c> do catálogo — varrido da pasta, não listado à mão. Item novo
        /// entra sozinho neste guarda.
        /// </summary>
        private static IEnumerable<ItemDef> TodosOsItens()
        {
            if (!Directory.Exists(PastaDosItens)) yield break;

            foreach (var caminho in Directory.GetFiles(PastaDosItens, "*.asset",
                                                       SearchOption.AllDirectories)
                                             .Select(c => c.Replace(Path.DirectorySeparatorChar, '/'))
                                             .OrderBy(c => c))
            {
                var def = AssetDatabase.LoadAssetAtPath<ItemDef>(caminho);
                if (def != null) yield return def;
            }
        }

        private static ItemDef[] ArmasAutoradas() =>
            TodosOsItens().Where(d => d.Tipo == ItemType.Arma).ToArray();

        [Test]
        public void OCatalogo_TemArmas()
        {
            Assert.IsNotEmpty(ArmasAutoradas(),
                $"Nenhuma arma em '{PastaDosItens}'. Ou o catálogo mudou de lugar — e aí este " +
                "guarda passaria verde sem verificar nada — ou o jogo ficou sem arsenal.");
        }

        /// <summary>
        /// O contrato central: uma arma que o jogador pode equipar <b>tem</b> de virar uma arma
        /// empunhável. Hoje isso depende do enum bater com o dicionário da fábrica; depois da
        /// migração dependerá do asset de base estar ligado. O guarda é o mesmo nos dois mundos.
        /// </summary>
        [Test]
        public void TodaArmaAutorada_ViraUmaArmaEmpunhavel()
        {
            var quebradas = new List<string>();

            foreach (var def in ArmasAutoradas())
            {
                var arma = WeaponFactory.Criar(def.ArmaFisica);

                if (arma == null)
                    quebradas.Add($"{def.name} (ArmaFisica: {def.ArmaFisica}) → a fábrica " +
                                  "devolveu null");
            }

            Assert.IsEmpty(quebradas,
                "Arma(s) autorada(s) que o jogador consegue equipar e que não viram arma " +
                "nenhuma:" + Environment.NewLine + "  " +
                string.Join(Environment.NewLine + "  ", quebradas) + Environment.NewLine +
                "Em jogo isso é o Damião equipar e continuar desarmado, sem erro no console.");
        }

        /// <summary>
        /// Toda arma tem de causar dano. Sem isto, um asset com todos os números zerados — o
        /// erro mais provável ao migrar para dado — passaria como "arma existe e é não-nula".
        /// </summary>
        [Test]
        public void TodaArmaAutorada_CausaDanoNoGolpeBasico()
        {
            var inertes = new List<string>();

            foreach (var def in ArmasAutoradas())
            {
                var arma = WeaponFactory.Criar(def.ArmaFisica);
                if (arma == null) continue;   // já coberto pelo guarda acima

                var golpe = arma.Execute();

                if (!golpe.Success) inertes.Add($"{def.name}: Execute devolveu Success=false");
                else if (golpe.Dano <= 0f) inertes.Add($"{def.name}: dano {golpe.Dano}");
                else if (golpe.DurationSeconds <= 0f)
                    inertes.Add($"{def.name}: duração {golpe.DurationSeconds}s — o golpe não " +
                                "ocupa tempo nenhum, então a FSM nunca entra em Atacando");
            }

            Assert.IsEmpty(inertes,
                "Arma(s) que golpeiam sem efeito:" + Environment.NewLine + "  " +
                string.Join(Environment.NewLine + "  ", inertes));
        }

        /// <summary>
        /// Cobre o caminho oposto: valor do enum que a fábrica não conhece. Hoje ela engole e
        /// devolve <c>null</c>; este guarda garante que ninguém acrescente um valor ao enum e
        /// esqueça de registrar a arma — que é exatamente o erro que a migração vai convidar.
        /// </summary>
        [Test]
        public void TodoValorDoEnum_TemArmaRegistrada()
        {
            var semFabrica = new List<string>();

            foreach (TipoArmaFisica tipo in Enum.GetValues(typeof(TipoArmaFisica)))
            {
                // MaoVazia é null de propósito: desarmado é um estado, não uma arma.
                if (tipo == TipoArmaFisica.MaoVazia) continue;

                if (WeaponFactory.Criar(tipo) == null) semFabrica.Add(tipo.ToString());
            }

            Assert.IsEmpty(semFabrica,
                "Valor(es) de TipoArmaFisica sem arma registrada na WeaponFactory: " +
                string.Join(", ", semFabrica) + ". A fábrica devolve null em silêncio para " +
                "valor desconhecido, então isto não apareceria em jogo como erro — só como " +
                "uma arma que não faz nada.");
        }

        /// <summary>
        /// <c>MaoVazia</c> tem de continuar devolvendo <c>null</c>. É o que faz a
        /// <c>MaoFisicaBridge</c> cair no <c>_maoVazia</c> local, com dano 0 por decisão de
        /// design (ver <c>armas_da_tumba.md</c>: Damião começa desarmado).
        /// </summary>
        [Test]
        public void MaoVazia_NaoEUmaArma()
        {
            Assert.IsNull(WeaponFactory.Criar(TipoArmaFisica.MaoVazia),
                "MaoVazia passou a devolver uma arma — desarmado deixaria de ser um estado e " +
                "o Damião começaria o jogo batendo.");
        }
    }
}
