using System;
using System.Collections.Generic;
using FavelaAmarela.Core.Abilities;

namespace FavelaAmarela.Core.Factories
{
    /// <summary>
    /// Fábrica universal de armas. Converte o enumerador (usado nos assets de inventário)
    /// na instância real da classe de combate (POCO), restaurando o dano das armas.
    /// </summary>
    public static class WeaponFactory
    {
        private static readonly Dictionary<TipoArmaFisica, Func<IArmaComHabilidade>> _factory =
            new Dictionary<TipoArmaFisica, Func<IArmaComHabilidade>>
        {
            { TipoArmaFisica.MaoVazia, () => null },
            { TipoArmaFisica.CravoDeAklo, () => new CravoDeAklo() },
            { TipoArmaFisica.EstileteDeIrem, () => new EstileteDeIrem() },
            { TipoArmaFisica.AlfanjeDeAlhazred, () => new AlfanjeDeAlhazred() },
        };

        public static IArmaComHabilidade Criar(TipoArmaFisica tipo)
        {
            if (_factory.TryGetValue(tipo, out var factory))
                return factory();
            
            // Degradação graciosa
            return null;
        }
    }
}
