using System;

namespace FavelaAmarela.Core.Abilities
{
    /// <summary>
    /// Identificador universal de armas físicas. Usado para instanciar a 
    /// classe POCO de combate (IArmaComHabilidade) via WeaponFactory.
    /// </summary>
    public enum TipoArmaFisica
    {
        MaoVazia = 0,
        CravoDeAklo = 1,
        EstileteDeIrem = 2,
        AlfanjeDeAlhazred = 3
    }
}
