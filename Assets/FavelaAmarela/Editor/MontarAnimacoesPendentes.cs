using UnityEditor;

namespace FavelaAmarela.EditorTools
{
    /// <summary>
    /// Ferramenta de Editor. Chama, em sequência, os três "Montar Animação de ___" pendentes
    /// (Cultista, Espectro, Damião) — para rodar tudo numa única invocação de
    /// <c>-executeMethod</c> em batch mode, sem precisar abrir a Unity três vezes.
    /// </summary>
    public static class MontarAnimacoesPendentes
    {
        [MenuItem("Tools/FavelaAmarela/Montar animações pendentes (Cultista + Espectro + Damião)")]
        public static void Executar()
        {
            MontarAnimacaoDoCultista.Executar();
            MontarAnimacaoDoEspectro.Executar();
            MontarAnimacaoDoDamiao.Executar();
        }
    }
}
