using UnityEditor;
using UnityEngine;

namespace FavelaAmarela.EditorTools
{
    /// <summary>
    /// Põe o pivô das 26 fatias do Byakhee no rodapé.
    ///
    /// <para><b>O defeito:</b> a folha veio fatiada com pivô <c>Center</c>, mas o
    /// <c>BoxCollider2D</c> do prefab (<c>size 2.625×2.633</c>, <c>offset 0, 2.19</c>) só faz
    /// sentido com pivô no rodapé — com <c>Center</c> ele ficava de 0.87 a 3.50 num sprite que
    /// ia de −2.19 a +2.19, ou seja <b>1,3 unidade acima da arte</b>. E o <c>offsetPes = 0</c> do
    /// <c>DynamicYSort</c> ordenava pelo meio do sprite em vez dos pés.</para>
    ///
    /// <para><b>Esta ferramenta não anima nada.</b> A animação do Byakhee é do
    /// <see cref="FavelaAmarela.Runtime.Enemies.AnimadorDoByakhee"/>, que já existia, já está no
    /// prefab com os 26 quadros preenchidos, e cujo XML doc explica por que o projeto
    /// <b>não</b> usa <c>AnimatorController</c> aqui: um Animator seria uma segunda máquina de
    /// estados a manter em sincronia com a <c>ByakheeFSM</c> do Core — a duplicação de regra que
    /// <c>Assets/Scripts/CLAUDE.md</c> proíbe. Uma versão anterior desta ferramenta montava
    /// clipes e um controller por cima daquele componente; os dois escreviam no mesmo
    /// <c>SpriteRenderer</c> e brigavam. Foi revertido em 2026-08-19.</para>
    /// </summary>
    public static class CorrigirPivoDoByakhee
    {
        private const string Folha = "Assets/FavelaAmarela/Art/Enemies/Byakhee_Spritesheet.png";

        [MenuItem("Tools/FavelaAmarela/Corrigir pivo do Byakhee")]
        public static void Executar()
        {
            if (MontadorDeAnimacao.CorrigirPivoDasFatias(Folha, SpriteAlignment.BottomCenter))
                AssetDatabase.SaveAssets();
        }
    }
}
