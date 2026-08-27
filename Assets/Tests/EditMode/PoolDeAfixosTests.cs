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
    /// Guarda o <b>pool de afixos autorado</b>.
    ///
    /// <para><b>O que mudou de gravidade em 2026-08-27.</b> Cinco <c>StatType</c> são
    /// decorativos: <c>RCMaxima</c>, <c>Velocidade</c> e <c>Furtividade</c> não têm consumidor
    /// nenhum; <c>DefesaAnomalia</c> é <b>exibida na ficha e não aplicada no combate</b>; e
    /// <c>RMMaxima</c> só funciona como consumível. Enquanto o loot entregava apenas itens
    /// autorados à mão, isso era dívida conhecida — o <c>atributos_e_build.md</c> registra que
    /// "um item com +Furtividade pode ser autorado, salvo e equipado sem efeito algum".</para>
    ///
    /// <para>Com afixos rolados virou <b>defeito ativo</b>: o gerador pode pôr esse número em
    /// qualquer item, e o jogador lê, ocupa o slot e não recebe nada. Um item que mente é pior
    /// que um item fraco.</para>
    /// </summary>
    public sealed class PoolDeAfixosTests
    {
        private const string Pasta = "Assets/FavelaAmarela/Config/Resources/Afixos";

        /// <summary>
        /// Os <c>StatType</c> sem consumidor no jogo. A fonte da verdade sobre isto é
        /// <c>PainelDeFicha.AtributoConsomeBonus</c>, cruzada com o código por
        /// <c>AtributosConsumidosTests</c> — esta lista é o espelho dela do lado do loot.
        /// </summary>
        private static readonly HashSet<StatType> Decorativos = new HashSet<StatType>
        {
            StatType.RCMaxima,       // Resiliência do Companheiro nunca foi ligada
            StatType.Velocidade,     // zero menções em Assets/Scripts
            StatType.Furtividade,    // autorado no Anel, mas nenhum sistema de stealth lê
            StatType.DefesaAnomalia, // exibido na ficha, NÃO aplicado no combate — o pior caso
            StatType.RMMaxima,       // só funciona como consumível, não como passiva
        };

        private static AfixoDef[] Todos() =>
            Directory.Exists(Pasta)
                ? Directory.GetFiles(Pasta, "*.asset", SearchOption.AllDirectories)
                           .Select(c => c.Replace(Path.DirectorySeparatorChar, '/'))
                           .Select(AssetDatabase.LoadAssetAtPath<AfixoDef>)
                           .Where(a => a != null)
                           .OrderBy(a => a.name)
                           .ToArray()
                : new AfixoDef[0];

        [Test]
        public void OPool_NaoEstaVazio()
        {
            Assert.IsNotEmpty(Todos(),
                $"Nenhum AfixoDef em '{Pasta}'. O gerador existiria e todo item sairia sem " +
                "modificador. Conserto: 'Tools/FavelaAmarela/Itens: montar o pool de afixos'.");
        }

        /// <summary>
        /// <b>O guarda mais importante deste arquivo.</b> Um afixo decorativo produz um item
        /// que mente: o jogador lê o número na ficha e não recebe efeito nenhum.
        /// </summary>
        [Test]
        public void NenhumAfixo_RolaUmAtributoSemEfeito()
        {
            var mentirosos = Todos()
                .Where(a => Decorativos.Contains(a.Stat))
                .Select(a => $"{a.name} rola {a.Stat}")
                .ToList();

            Assert.IsEmpty(mentirosos,
                "Afixo(s) que produzem item mentiroso:" + Environment.NewLine + "  " +
                string.Join(Environment.NewLine + "  ", mentirosos) + Environment.NewLine +
                "Esses StatType não têm consumidor no jogo — o jogador vê o número, ocupa o " +
                "slot e não recebe nada. Ou implemente o atributo, ou tire-o do pool.");
        }

        /// <summary>
        /// Os afixos estão sob <c>Resources/</c>? Fora de lá o catálogo não os enxerga em
        /// runtime, e o sintoma é mudo: o asset existe no disco, não entra no pool, e nada
        /// aparece no console.
        /// </summary>
        [Test]
        public void OPool_ViveSobResources()
        {
            StringAssert.Contains("/Resources/", Pasta,
                "O pool precisa estar sob Resources para CatalogoDeAfixos.Recarregar achá-lo.");

            foreach (var a in Todos())
            {
                string caminho = AssetDatabase.GetAssetPath(a);
                StringAssert.Contains("/Resources/", caminho,
                    $"'{a.name}' está fora de Resources — invisível em runtime.");
            }
        }

        [Test]
        public void TodoAfixo_TemIdUnico()
        {
            var todos = Todos();

            var vazios = todos.Where(a => string.IsNullOrWhiteSpace(a.Id))
                              .Select(a => a.name).ToList();
            Assert.IsEmpty(vazios, "Afixo(s) sem Id: " + string.Join(", ", vazios) +
                ". O Id vai para o save junto do valor rolado.");

            var duplicados = todos.GroupBy(a => a.Id)
                                  .Where(g => g.Count() > 1)
                                  .Select(g => g.Key)
                                  .ToList();

            Assert.IsEmpty(duplicados, "Id(s) duplicado(s): " + string.Join(", ", duplicados));
        }

        [Test]
        public void TodoAfixo_TemFaixaEPesoUteis()
        {
            var quebrados = new List<string>();

            foreach (var a in Todos())
            {
                if (a.Peso <= 0f)
                    quebrados.Add($"{a.name}: peso {a.Peso} — nunca sairia no sorteio");

                if (a.ValorMin == 0f && a.ValorMax == 0f)
                    quebrados.Add($"{a.name}: faixa 0–0 — o afixo cai e não faz nada");

                if (a.NivelMinimoDoItem < 1)
                    quebrados.Add($"{a.name}: nível mínimo {a.NivelMinimoDoItem} < 1");
            }

            Assert.IsEmpty(quebrados,
                "Afixo(s) inúteis:" + Environment.NewLine + "  " +
                string.Join(Environment.NewLine + "  ", quebrados));
        }

        /// <summary>
        /// Rótulo é texto <b>visível ao jogador</b> e segue a skill <c>favela-lore-enforcer</c>:
        /// o vocabulário é o de Carcosa, não o genérico de RPG. O grau já tem os nomes dele
        /// (Inerte, Marcado, Impregnado, Relíquia) — afixo não repete raridade.
        /// </summary>
        [Test]
        public void NenhumRotulo_UsaVocabularioGenericoDeRPG()
        {
            string[] proibidos = { "comum", "raro", "épico", "epico", "lendário", "lendario",
                                   "incomum", "mítico", "mitico", "tier", "level", "hp", "mana" };

            var genericos = new List<string>();

            foreach (var a in Todos())
            {
                if (string.IsNullOrWhiteSpace(a.Rotulo))
                {
                    genericos.Add($"{a.name}: sem rótulo — o item sairia sem nome de afixo");
                    continue;
                }

                string rotulo = a.Rotulo.ToLowerInvariant();

                foreach (var proibido in proibidos)
                    if (rotulo.Contains(proibido))
                        genericos.Add($"{a.name}: rótulo '{a.Rotulo}' usa '{proibido}'");
            }

            Assert.IsEmpty(genericos,
                "Rótulo(s) fora do vocabulário diegético:" + Environment.NewLine + "  " +
                string.Join(Environment.NewLine + "  ", genericos));
        }

        /// <summary>
        /// Todo grau gerável tem de ter afixo disponível no nível de entrada, senão o grau
        /// promete algo que o pool não entrega e o item sai igual a um Inerte.
        /// </summary>
        [Test]
        public void ExisteAfixoDeNivelUm_DeCadaTipo()
        {
            var todos = Todos();

            Assert.IsTrue(todos.Any(a => a.Tipo == TipoDeAfixo.Prefixo && a.NivelMinimoDoItem <= 1),
                "Nenhum prefixo disponível no nível 1: todo item Marcado de nível baixo sairia " +
                "sem afixo, indistinguível de um Inerte.");

            Assert.IsTrue(todos.Any(a => a.Tipo == TipoDeAfixo.Sufixo && a.NivelMinimoDoItem <= 1),
                "Nenhum sufixo no nível 1: item Impregnado de nível baixo sairia com metade do " +
                "que o grau promete.");
        }
    }
}
