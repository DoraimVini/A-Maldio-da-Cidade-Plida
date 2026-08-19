using UnityEditor;

namespace FavelaAmarela.EditorTools
{
    /// <summary>
    /// Agrega o que ficou pendente desta rodada de arte, para rodar tudo numa única invocação de
    /// <c>-executeMethod</c> em batch mode — a Unity precisa estar fechada, então cada abertura
    /// custa tempo do Vini.
    /// </summary>
    public static class RodadaDeArte
    {
        [MenuItem("Tools/FavelaAmarela/Rodada de arte (animações + Canvas)")]
        public static void Executar()
        {
            MontarAnimacaoDoCultista.Executar();
            MontarAnimacaoDoEspectro.Executar();
            MontarAnimacaoDoDamiao.Executar();
            PadronizarCanvasDasCenas.Executar();
        }
    }
}
