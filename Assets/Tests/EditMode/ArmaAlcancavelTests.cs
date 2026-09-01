using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using FavelaAmarela.Inventario;

namespace FavelaAmarela.Tests.EditMode
{
    /// <summary>
    /// Guarda que <b>toda arma autorada tenha como chegar ao jogador</b> — ou que a ausência
    /// seja decisão registrada.
    ///
    /// <para><b>Por que este guarda existe (2026-09-01).</b> A escada de tiers criou seis armas
    /// novas de uma vez. Sem fonte, elas seriam <b>conteúdo morto</b>: existem no disco, passam
    /// em toda checagem de asset, aparecem na Forja do Debugger — e nenhum jogador as vê. É o
    /// modo de falha assinatura deste repositório, e ele já cobrou dez vezes: o
    /// <c>GeradorDeItem</c> sem chamador, a tabela de drop do Abdul apontando para nada, o
    /// <c>OnVitoria</c> do Rei sem assinantes, a barra de vida sem sprite.</para>
    ///
    /// <para><b>Duas listas, e a segunda é o ponto.</b> Uma arma sem fonte não faz o teste
    /// falhar se estiver declarada como pendente <b>com a razão</b>. O que faz falhar é o
    /// silêncio — arma que ninguém decidiu de onde vem.</para>
    /// </summary>
    public sealed class ArmaAlcancavelTests
    {
        /// <summary>
        /// Armas que ainda não têm fonte, e por quê. Sair desta lista é o trabalho; entrar nela
        /// sem razão, não.
        /// </summary>
        /// <remarks>
        /// <b>Vazia desde 2026-09-01, e isso é a notícia.</b> As três armas T3 (Alfanje do Rei,
        /// Cravo do Sinal Amarelo, Estilete da Máscara Pálida) ficaram aqui declaradas como
        /// inalcançáveis com uma razão real: <i>"o Rei em Amarelo é SELADO por rito, não abatido
        /// — ele não dispara OnAbatido, então não larga espólio pelo caminho normal"</i>.
        ///
        /// <para>A saída foi a que a própria nota previa (<i>"ou uma recompensa do
        /// selamento"</i>): o <c>ReiEmAmareloAI</c> passou a implementar
        /// <c>IFonteDeEspolio</c> e dispara <c>OnAbatido</c> no selamento. Foi o mesmo conserto
        /// que o Abdul recebeu em 28/08 — quem larga espólio é quem sabe avisar que foi
        /// derrotado, não quem herda de uma classe.</para>
        ///
        /// <para><b>Mantida, e não apagada</b>, porque é o contrato deste par de testes: arma
        /// nova sem fonte entra aqui <b>com a razão escrita</b>, ou o
        /// <see cref="TodaArma_TemFonteOuRazaoRegistrada"/> falha.</para>
        /// </remarks>
        private static readonly (string Id, string Porque)[] SemFonteAinda =
        {
        };

        private static ItemDef[] ArmasAutoradas() =>
            AssetDatabase.FindAssets("t:ItemDef")
                .Select(AssetDatabase.GUIDToAssetPath)
                .Select(AssetDatabase.LoadAssetAtPath<ItemDef>)
                .Where(d => d != null && d.Tipo == ItemType.Arma)
                .OrderBy(d => d.name)
                .ToArray();

        /// <summary>Todos os ids de item que alguma tabela de drop pode entregar.</summary>
        private static HashSet<string> IdsComFonte()
        {
            var ids = new HashSet<string>();

            foreach (var tabela in AssetDatabase.FindAssets("t:TabelaDeDrop")
                         .Select(AssetDatabase.GUIDToAssetPath)
                         .Select(AssetDatabase.LoadAssetAtPath<TabelaDeDrop>)
                         .Where(t => t != null))
            {
                foreach (var c in tabela.ProjetarCandidatos())
                    ids.Add(c.ItemDefId);
            }

            return ids;
        }

        [Test]
        public void TodaArma_TemFonteOuRazaoRegistrada()
        {
            var comFonte = IdsComFonte();
            var pendentes = SemFonteAinda.Select(p => p.Id).ToHashSet();

            var orfas = ArmasAutoradas()
                .Where(d => !comFonte.Contains(d.Id) && !pendentes.Contains(d.Id))
                .Select(d => $"{d.name} ('{d.Nome}') — nenhuma tabela a entrega, e não está " +
                             "declarada como pendente")
                .ToList();

            Assert.IsEmpty(orfas,
                "Arma(s) que o jogador nunca vai encontrar:" + Environment.NewLine + "  " +
                string.Join(Environment.NewLine + "  ", orfas) + Environment.NewLine +
                "Ou acrescente a uma TabelaDeDrop, ou declare em " +
                "ArmaAlcancavelTests.SemFonteAinda COM A RAZÃO.");
        }

        /// <summary>
        /// O outro lado: uma arma declarada pendente que <b>ganhou</b> fonte tem de sair da
        /// lista. Senão a lista vira ficção, e o próximo a lê acredita.
        /// </summary>
        [Test]
        public void NenhumaPendente_JaTemFonte()
        {
            var comFonte = IdsComFonte();

            var resolvidas = SemFonteAinda
                .Where(p => comFonte.Contains(p.Id))
                .Select(p => $"{p.Id} já tem fonte e continua listada como pendente")
                .ToList();

            Assert.IsEmpty(resolvidas,
                "Lista de pendências desatualizada:" + Environment.NewLine + "  " +
                string.Join(Environment.NewLine + "  ", resolvidas) + Environment.NewLine +
                "Remova-as de ArmaAlcancavelTests.SemFonteAinda.");
        }

        /// <summary>
        /// O <b>primeiro degrau</b> precisa estar ao alcance de quem termina a Fase 1 — é o que
        /// transforma derrotar o Byakhee em progressão de item, e não só em espólio de rito.
        /// </summary>
        [Test]
        public void OPrimeiroDegrau_CaiAoFecharAFase1()
        {
            var byakhee = AssetDatabase.LoadAssetAtPath<TabelaDeDrop>(
                "Assets/FavelaAmarela/Config/Drops/Drop_Byakhee.asset");

            Assert.IsNotNull(byakhee, "Drop_Byakhee não existe.");

            var ids = byakhee.ProjetarCandidatos().Select(c => c.ItemDefId).ToHashSet();

            var t2 = new[]
            {
                "Item_Arma_AlfanjeDasRuinasPalidas",
                "Item_Arma_CravoDeAldebaran",
                "Item_Arma_EstileteDeYhtill",
            };

            var faltando = t2.Where(i => !ids.Contains(i)).ToList();

            Assert.IsEmpty(faltando,
                "O Byakhee deixou de largar T2: " + string.Join(", ", faltando) +
                Environment.NewLine + "O jogador chega nele no nível 3 com uma arma T1 do baú. " +
                "Sem o degrau aqui, fechar a Fase 1 não melhora a arma de ninguém.");
        }

        /// <summary>
        /// O <b>último degrau</b> tem de cair no desfecho. Antes de 2026-09-01 o Rei em Amarelo
        /// era o único confronto do Vertical Slice que largava <b>zero</b> equipamento — ele não
        /// é <c>EnemyBase</c> nem <c>IDanificavel</c> (não tem barra de vida, por design), e
        /// ficava de fora do espólio por construção.
        /// </summary>
        [Test]
        public void OUltimoDegrau_CaiAoSelarORei()
        {
            var rei = AssetDatabase.LoadAssetAtPath<TabelaDeDrop>(
                "Assets/FavelaAmarela/Config/Drops/Drop_ReiEmAmarelo.asset");

            Assert.IsNotNull(rei, "Drop_ReiEmAmarelo não existe — o desfecho voltou a não " +
                                  "largar nada.");

            var candidatos = rei.ProjetarCandidatos();
            var ids = candidatos.Select(x => x.ItemDefId).ToHashSet();

            var t3 = new[]
            {
                "Item_Arma_AlfanjeDoRei",
                "Item_Arma_CravoDoSinalAmarelo",
                "Item_Arma_EstileteDaMascaraPalida",
            };

            var faltando = t3.Where(i => !ids.Contains(i)).ToList();

            Assert.IsEmpty(faltando,
                "O Rei deixou de largar T3: " + string.Join(", ", faltando));

            // GARANTIDO, e não sorteado. O Rei é selado UMA vez e a cena acaba: sorteio só é
            // justo quando se repete, porque só aí o azar tem como ser corrigido jogando.
            var sorteadas = candidatos
                .Where(x => t3.Contains(x.ItemDefId) && !x.Garantido)
                .Select(x => x.ItemDefId)
                .ToList();

            Assert.IsEmpty(sorteadas,
                "Arma(s) T3 dependendo de sorte no Rei: " + string.Join(", ", sorteadas) +
                Environment.NewLine + "O rito acontece uma vez só — quem tiver azar fica sem, " +
                "e não existe segunda tentativa para corrigir.");
        }

        /// <summary>
        /// E a tabela precisa estar <b>ligada</b> ao prefab. Criar tabela não é ligar tabela —
        /// o <c>Drop_Abdul</c> passou um mês inteiro apontando para nada.
        /// </summary>
        [Test]
        public void OReiTemODropAoAbaterLigado()
        {
            const string caminho = "Assets/FavelaAmarela/Art/Enemies/ReiEmAmarelo.prefab";
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(caminho);

            Assert.IsNotNull(prefab, $"Prefab ausente: {caminho}");

            var drop = prefab.GetComponentInChildren<FavelaAmarela.Runtime.Itens.DropAoAbater>(true);

            Assert.IsNotNull(drop,
                "O prefab do Rei não tem DropAoAbater: a tabela existe e nada a consulta.");

            var so = new UnityEditor.SerializedObject(drop);
            var tabela = so.FindProperty("tabela");

            Assert.IsNotNull(tabela?.objectReferenceValue,
                "O DropAoAbater do Rei está sem tabela — o componente existe e não entrega nada.");
        }
    }
}
