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

            // Aqui havia uma chamada a MontarTelaDeInventario, que CONSTRUÍA casas novas de
            // inventário. Era duplicação: a Janela já tinha Mochila/Slot_0..11 e Corpo/Corpo_0..6
            // diagramados — faltava só preencher os arrays do PainelDeInventario. A ferramenta
            // foi apagada e substituída por LigarSlotsDoInventarioExistentes, que NÃO entra
            // nesta rodada: é correção pontual, não passo recorrente de montagem de arte.
        }
    }
}
