using FavelaAmarela.Inventario;

namespace FavelaAmarela.Inventario
{
    /// <summary>
    /// Lê o que está na Mão Secundária.
    ///
    /// <para>Existe para haver <b>um</b> lugar que sabe o índice do slot e resolve o item. Dois
    /// consumidores (a mitigação de dano e a recarga da habilidade) lendo o inventário por
    /// conta própria seria mais uma divergência para manter à mão — e o índice do slot já é
    /// contrato duro em outro ponto do projeto (<c>MaoFisicaBridge.SlotDeArma</c> é 0, guardado
    /// por <c>MaoSecundariaTests</c>).</para>
    /// </summary>
    public static class MaoSecundaria
    {
        /// <summary>O item na Mão Secundária, ou <c>null</c>.</summary>
        public static ItemDef Equipada()
        {
            var inv = InventoryManager.Instance;
            if (inv?.Equipment == null) return null;

            int indice = inv.Equipment.IndiceDoSlot(EquipmentSlot.MaoSecundaria);
            if (indice < 0) return null;

            return inv.Equipment.GetSlot(indice)?.Def;
        }

        /// <summary>Chance de aparar um golpe, ou 0 quando não há escudo.</summary>
        public static float ChanceDeBloqueio()
        {
            var def = Equipada();
            return def != null && def.Funcao == FuncaoDeMaoSecundaria.Escudo
                ? def.PotenciaDaMaoSecundaria
                : 0f;
        }

        /// <summary>Fração do dano aparada quando o bloqueio acontece.</summary>
        public static float ReducaoAoBloquear()
        {
            var def = Equipada();
            return def != null && def.Funcao == FuncaoDeMaoSecundaria.Escudo
                ? def.ReducaoAoBloquear
                : 0f;
        }

        /// <summary>
        /// Fração da recarga descontada da habilidade da arma, ou 0 quando não há foco.
        /// Limitada a 0,8 — recarga zero faria a habilidade virar o ataque básico.
        /// </summary>
        public static float DescontoDeRecarga()
        {
            var def = Equipada();
            if (def == null || def.Funcao != FuncaoDeMaoSecundaria.Foco) return 0f;

            float d = def.PotenciaDaMaoSecundaria;
            return d < 0f ? 0f : (d > 0.8f ? 0.8f : d);
        }
    }
}
