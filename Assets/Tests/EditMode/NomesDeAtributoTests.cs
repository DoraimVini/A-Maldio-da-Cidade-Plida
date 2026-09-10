using System;
using System.Collections.Generic;
using System.Linq;
using FavelaAmarela.Inventario;
using NUnit.Framework;

namespace FavelaAmarela.Tests.EditMode
{
    /// <summary>
    /// Nenhum nome de enum chega à tela do jogador.
    ///
    /// <para><b>O que motivou (2026-09-10):</b> o inventário mostrava <c>MaoSecundaria</c> num
    /// slot e a ficha mostrava <c>VitMaxima</c>, <c>DanoCritico</c>, <c>Precisao</c> — C# vazando
    /// para quem joga, contra a skill <c>favela-lore-enforcer</c>. <see cref="NomesDeAtributo"/> é
    /// a fonte única; um valor novo no enum sem tradução aqui reprova antes de aparecer.</para>
    /// </summary>
    public sealed class NomesDeAtributoTests
    {
        [Test]
        public void TodoStatType_TemNomeQueNaoEhOEnum()
        {
            // "Velocidade" e "Furtividade" são palavras e servem como estão; o que denuncia enum
            // cru é o CamelCase — "VitMaxima", "DanoCritico" — uma maiúscula depois da primeira.
            var crus = Enum.GetValues(typeof(StatType)).Cast<StatType>()
                .Where(s => NomesDeAtributo.De(s) == s.ToString() && s.ToString().Skip(1).Any(char.IsUpper))
                .Select(s => s.ToString())
                .ToList();

            Assert.IsEmpty(crus,
                "StatType sem nome para o jogador (cairia como enum cru na ficha): " +
                string.Join(", ", crus) + ". Acrescente em NomesDeAtributo.De(StatType).");
        }

        [Test]
        public void TodoSlotDoCorpo_TemNomeQueNaoEhOEnum()
        {
            var crus = Enum.GetValues(typeof(EquipmentSlot)).Cast<EquipmentSlot>()
                .Where(s => s != EquipmentSlot.Nenhum)
                .Where(s => NomesDeAtributo.De(s) == s.ToString() && s.ToString().Any(char.IsUpper) &&
                            s.ToString().Skip(1).Any(char.IsUpper))   // "MaoSecundaria": CamelCase é enum cru
                .Select(s => s.ToString())
                .ToList();

            Assert.IsEmpty(crus,
                "EquipmentSlot em CamelCase cru na tela: " + string.Join(", ", crus) +
                ". Acrescente em NomesDeAtributo.De(EquipmentSlot).");

            Assert.AreEqual("Mão Secundária", NomesDeAtributo.De(EquipmentSlot.MaoSecundaria));
        }

        [Test]
        public void NomesNaoRepetem()
        {
            var nomes = Enum.GetValues(typeof(StatType)).Cast<StatType>().Select(NomesDeAtributo.De).ToList();
            var repetidos = nomes.GroupBy(n => n).Where(g => g.Count() > 1).Select(g => g.Key).ToList();

            Assert.IsEmpty(repetidos,
                "Dois atributos com o mesmo nome para o jogador: " + string.Join(", ", repetidos) +
                " — a ficha mostraria duas linhas iguais com números diferentes.");
        }
    }
}
