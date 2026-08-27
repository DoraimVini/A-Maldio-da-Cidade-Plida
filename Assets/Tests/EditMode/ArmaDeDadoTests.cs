using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using NUnit.Framework;
using UnityEditor;
using FavelaAmarela.Inventario;

namespace FavelaAmarela.Tests.EditMode
{
    /// <summary>
    /// O contrato mínimo de <b>toda arma do catálogo</b>: se o jogador consegue equipar, tem de
    /// virar uma arma que causa dano.
    ///
    /// <para><b>O buraco que este arquivo fechou (2026-08-27).</b> A <c>WeaponFactory</c> era o
    /// único ponto de produção onde um item equipado virava arma jogável, e tinha <b>zero</b>
    /// cobertura. Pior: <c>Criar</c> devolvia <c>null</c> por "degradação graciosa" para valor
    /// desconhecido — uma arma sumindo do dicionário viraria Damião desarmado <b>sem uma linha
    /// no console</b>. O <c>ArmasDaTumbaTests</c> não pegava: ele instanciava as classes na mão
    /// e nunca passava pela fábrica.</para>
    ///
    /// <para>A fábrica saiu com a migração para dado; o contrato ficou, e agora aponta para
    /// <c>BaseDeArma.ConstruirArma</c> — o único lugar que monta uma arma. <b>Este guarda é o
    /// que faz uma arma nova, criada pelo Item Creator, não nascer inerte.</b></para>
    /// </summary>
    public sealed class ArmaDeDadoTests
    {
        private const string PastaDosItens = "Assets/FavelaAmarela/Config/Resources/Itens";

        private static ItemDef[] ArmasAutoradas() =>
            Directory.GetFiles(PastaDosItens, "*.asset", SearchOption.AllDirectories)
                     .Select(c => c.Replace(Path.DirectorySeparatorChar, '/'))
                     .Select(AssetDatabase.LoadAssetAtPath<ItemDef>)
                     .Where(d => d != null && d.Tipo == ItemType.Arma)
                     .OrderBy(d => d.name)
                     .ToArray();

        [Test]
        public void OCatalogo_TemArmas()
        {
            Assert.IsNotEmpty(ArmasAutoradas(),
                $"Nenhuma arma em '{PastaDosItens}'. Ou o catálogo mudou de lugar — e aí este " +
                "guarda passaria verde sem verificar nada — ou o jogo ficou sem arsenal.");
        }

        /// <summary>
        /// O contrato central. Uma arma que não constrói é Damião equipando e continuando
        /// desarmado, sem erro no console.
        /// </summary>
        [Test]
        public void TodaArmaAutorada_ViraUmaArmaEmpunhavel()
        {
            var quebradas = new List<string>();

            foreach (var def in ArmasAutoradas())
            {
                if (def.Base == null)
                {
                    quebradas.Add($"{def.name}: sem BaseDeArma — não há o que construir");
                    continue;
                }

                if (def.Base.ConstruirArma() == null)
                    quebradas.Add($"{def.name}: a família '{def.Base.name}' devolveu null " +
                                  "(HabilidadeDef vazia?)");
            }

            Assert.IsEmpty(quebradas,
                "Arma(s) que o jogador consegue equipar e que não viram arma nenhuma:" +
                Environment.NewLine + "  " +
                string.Join(Environment.NewLine + "  ", quebradas) + Environment.NewLine +
                "Em jogo isso é equipar e continuar desarmado, sem erro no console.");
        }

        /// <summary>
        /// Toda arma tem de causar dano e ocupar tempo. Sem isto, um asset com todos os números
        /// zerados — o erro mais provável ao autorar pelo Item Creator — passaria como "arma
        /// existe e não é nula".
        /// </summary>
        [Test]
        public void TodaArmaAutorada_CausaDanoNoGolpeBasico()
        {
            var inertes = new List<string>();

            foreach (var def in ArmasAutoradas())
            {
                var arma = def.Base != null ? def.Base.ConstruirArma() : null;
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
        /// Nenhuma arma pode ter cadência zero: um golpe sem recarga é dano infinito por
        /// segundo, e é o valor que um campo recém-criado no Inspector tem por padrão.
        /// </summary>
        [Test]
        public void NenhumaArma_TemCadenciaZero()
        {
            var infinitas = new List<string>();

            foreach (var def in ArmasAutoradas())
            {
                var arma = def.Base != null ? def.Base.ConstruirArma() : null;
                if (arma == null) continue;

                if (arma.CanActivate(0f))
                    infinitas.Add($"{def.name}: pronta de novo em 0 s");
            }

            Assert.IsEmpty(infinitas,
                "Arma(s) sem cadência nenhuma: " + string.Join(", ", infinitas) +
                ". Dano por segundo infinito trivializa qualquer chefe.");
        }

        /// <summary>
        /// Arma sem nome desenha um rótulo vazio na barra de ações — o jogador vê um espaço em
        /// branco onde deveria estar a arma que ele acabou de achar.
        /// </summary>
        [Test]
        public void NenhumaArma_FicaSemNomeNaBarraDeAcoes()
        {
            var anonimas = new List<string>();

            foreach (var def in ArmasAutoradas())
            {
                var arma = def.Base != null ? def.Base.ConstruirArma() : null;
                if (arma == null) continue;

                if (string.IsNullOrWhiteSpace(arma.NomeDaArma) ||
                    arma.NomeDaArma == "Arma sem nome")
                    anonimas.Add(def.name);
            }

            Assert.IsEmpty(anonimas,
                "Arma(s) sem nome autorado: " + string.Join(", ", anonimas) +
                ". A barra de ações desenharia um rótulo vazio.");
        }
    }
}
