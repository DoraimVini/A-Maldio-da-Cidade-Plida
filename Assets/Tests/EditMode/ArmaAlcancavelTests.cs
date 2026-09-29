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
        /// <b>2026-09-28: as três T3 voltaram para cá</b>, com o Rito do Olhar — o Rei deixou de
        /// largar espólio (decisão D3). O histórico abaixo fica como estava.
        ///
        /// <para><b>Vazia de 2026-09-01 a 2026-09-28.</b></para> As três armas T3 (Alfanje do Rei,
        /// Maça do Sinal Amarelo, Estilete da Máscara Pálida) ficaram aqui declaradas como
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
            // 2026-09-28, Rito do Olhar (decisão D3 do plano): o Rei é selado e o jogo volta ao
            // Menu 5 s depois. O espólio caía na tela do desfecho, onde ninguém podia usá-lo —
            // era fonte no papel. O Drop_ReiEmAmarelo fica no projeto para quando houver um
            // "depois do Rei" (ver TabelasSemConsumidor).
            ("Item_Arma_AlfanjeDoRei", "o Rei não larga espólio desde o Rito do Olhar (D3)"),
            ("Item_Arma_MacaDoSinalAmarelo", "o Rei não larga espólio desde o Rito do Olhar (D3)"),
            ("Item_Arma_EstileteDaMascaraPalida", "o Rei não larga espólio desde o Rito do Olhar (D3)"),
        };

        /// <summary>
        /// Tabelas guardadas que nada consulta, e por quê. Não contam como fonte: tabela que
        /// ninguém rola não entrega arma a ninguém.
        /// </summary>
        private static readonly (string Nome, string Porque)[] TabelasSemConsumidor =
        {
            ("Drop_ReiEmAmarelo", "o Rei perdeu o DropAoAbater no Rito do Olhar (D3); guardada para uso futuro"),
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
                         .Where(t => t != null)
                         .Where(t => TabelasSemConsumidor.All(x => x.Nome != t.name)))
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
                "Item_Arma_MacaDeAldebaran",
                "Item_Arma_EstileteDeYhtill",
            };

            var faltando = t2.Where(i => !ids.Contains(i)).ToList();

            Assert.IsEmpty(faltando,
                "O Byakhee deixou de largar T2: " + string.Join(", ", faltando) +
                Environment.NewLine + "O jogador chega nele no nível 3 com uma arma T1 do baú. " +
                "Sem o degrau aqui, fechar a Fase 1 não melhora a arma de ninguém.");
        }

        /// <summary>
        /// <b>O Rei não larga espólio</b> (decisão D3 do Rito do Olhar, 2026-09-28): selá-lo
        /// termina o jogo, e o que caísse ali ficaria na tela do desfecho. Guarda contra alguém
        /// religar o <c>DropAoAbater</c> por reflexo — o Rei nem implementa mais
        /// <c>IFonteDeEspolio</c>, e o componente reclamaria em todo Awake.
        /// </summary>
        [Test]
        public void ORei_NaoLargaEspolio()
        {
            const string caminho = "Assets/FavelaAmarela/Art/Enemies/ReiEmAmarelo.prefab";
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(caminho);

            Assert.IsNotNull(prefab, $"Prefab ausente: {caminho}");
            Assert.IsNull(prefab.GetComponentInChildren<FavelaAmarela.Runtime.Itens.DropAoAbater>(true),
                "O prefab do Rei voltou a ter DropAoAbater. O Rei é selado e o jogo termina — " +
                "se isso mudou, mude também a decisão D3 no plano do Rito do Olhar.");
        }
    }
}
