using UnityEngine;

namespace FavelaAmarela.Runtime.Combat
{
    /// <summary>
    /// Camada Runtime (MonoBehaviour). Marcador puro: "esta unidade está do lado de Damião".
    /// Não tem lógica nem estado — existe só para que as armas do jogador saibam <b>não</b>
    /// acertá-la (ver <c>MaoFisicaBridge</c>).
    ///
    /// <para><b>Por que um marcador e não uma checagem de tipo:</b> a alternativa seria a
    /// arma perguntar "é o Yug-Neth?", o que amarraria o sistema de combate a um personagem
    /// específico. Com o marcador, qualquer companheiro futuro fica protegido só de ganhar
    /// este componente — composição, como manda a Regra de Ouro 3.</para>
    ///
    /// <para><b>Não confunde com invulnerabilidade.</b> Um aliado continua podendo levar
    /// dano de inimigos: é disso que depende a incapacitação do Yug-Neth
    /// (<c>systems/companheiro_mi_go.md</c>). Este marcador barra <b>só</b> o golpe do
    /// jogador. Para tornar alguém temporariamente imune a tudo, use
    /// <c>VitalidadeBridge.IgnorarDano</c>.</para>
    /// </summary>
    [AddComponentMenu("Favela Amarela/Combate/Aliado")]
    public sealed class Aliado : MonoBehaviour
    {
    }
}
