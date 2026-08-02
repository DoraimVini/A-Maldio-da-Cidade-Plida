using System;

namespace FavelaAmarela.Core.Abilities
{
    /// <summary>
    /// As três armas seladas na Tumba de Alhazred. O baú entrega <b>uma</b> delas por
    /// sorteio — não é escolha do jogador, e é isso que faz a build variar entre partidas.
    /// </summary>
    public enum ArmaDaTumba
    {
        /// <summary>Estaca ritual: habilidade interrompe canalização anômala.</summary>
        CravoDeAklo,
        /// <summary>Adaga rápida: habilidade aplica sangramento (Ferida de Aklo).</summary>
        EstileteDeIrem,
        /// <summary>Cimitarra pesada: habilidade repele e atordoa (Golpe do Deserto).</summary>
        AlfanjeDeAlhazred
    }

    /// <summary>
    /// Sorteia qual das três armas da Tumba o baú entrega. POCO puro com RNG injetável
    /// (determinístico em teste), mesmo padrão de <c>FrasesDeColapso</c>.
    ///
    /// <para>Regra de design: como o baú é RNG e não escolha, <b>o Abdul precisa ser
    /// vencível com qualquer uma das três</b> — nenhuma arma é obrigatória. O sorteio ser
    /// uniforme importa para isso: nenhuma build sai favorecida.</para>
    /// </summary>
    public sealed class SorteioDeArmaDaTumba
    {
        private static readonly ArmaDaTumba[] _pool =
        {
            ArmaDaTumba.CravoDeAklo,
            ArmaDaTumba.EstileteDeIrem,
            ArmaDaTumba.AlfanjeDeAlhazred,
        };

        private readonly Func<double> amostraAleatoria;
        private static readonly Random _randomPadrao = new Random();

        /// <param name="amostraAleatoria">
        /// Fonte de números em [0, 1) para o sorteio. Injetável para testes
        /// determinísticos. Usa <see cref="Random"/> padrão se omitido.
        /// </param>
        public SorteioDeArmaDaTumba(Func<double> amostraAleatoria = null)
        {
            this.amostraAleatoria = amostraAleatoria ?? (() => _randomPadrao.NextDouble());
        }

        /// <summary>Quantidade de armas no pool do baú.</summary>
        public int Quantidade => _pool.Length;

        /// <summary>Sorteia uniformemente uma das armas da Tumba.</summary>
        public ArmaDaTumba Sortear()
        {
            int i = (int)(amostraAleatoria() * _pool.Length);
            if (i < 0) i = 0;
            if (i >= _pool.Length) i = _pool.Length - 1;
            return _pool[i];
        }

        /// <summary>
        /// Instancia a arma correspondente. Fábrica centralizada para o baú (e futuramente
        /// o save) não repetirem o <c>switch</c> de tipos concretos.
        /// </summary>
        public static IArmaComHabilidade Criar(ArmaDaTumba qual) => qual switch
        {
            ArmaDaTumba.CravoDeAklo => new CravoDeAklo(),
            ArmaDaTumba.EstileteDeIrem => new EstileteDeIrem(),
            ArmaDaTumba.AlfanjeDeAlhazred => new AlfanjeDeAlhazred(),
            _ => new CravoDeAklo(),
        };

        /// <summary>Sorteia e já instancia a arma — atalho para o baú.</summary>
        public IArmaComHabilidade SortearECriar() => Criar(Sortear());
    }
}
