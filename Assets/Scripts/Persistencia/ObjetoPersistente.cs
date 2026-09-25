using System;
using UnityEngine;

namespace FavelaAmarela.Runtime.Persistencia
{
    /// <summary>
    /// Camada Runtime (MonoBehaviour). Dá a um objeto de cena uma <b>chave de persistência
    /// imutável</b> — o "RG" que costura o objeto da cena recarregada ao seu estado no save.
    ///
    /// <para><b>O GUID é gerado uma única vez</b> e fica serializado na cena. A partir daí
    /// nunca muda: renomear o objeto, movê-lo de pai ou mudá-lo de posição não afeta a
    /// chave.</para>
    ///
    /// <para><b>Por que não usar o nome ou o caminho da hierarquia:</b> é o erro clássico
    /// que quebra saves. Se o baú <c>Level1/Floresta/Bau_Magico</c> virar
    /// <c>Bau_Encantado</c>, a chave muda, o sistema não acha o estado salvo, conclui que é
    /// um baú novo — e o jogador reencontra fechado um baú que já tinha aberto, sem nenhuma
    /// mensagem de erro. Progresso perdido em silêncio é o pior tipo de bug.</para>
    ///
    /// <para><b>Nota de arquitetura:</b> a geração usa só <c>Reset</c>/<c>OnValidate</c>
    /// (mensagens comuns de <c>MonoBehaviour</c>, disparadas pelo Editor) e nenhuma API do
    /// namespace <c>UnityEditor</c> — o assembly <c>FavelaAmarela.Runtime</c> não o
    /// referencia. Para carimbar chaves em massa numa cena existente, use a ferramenta
    /// <c>Tools/FavelaAmarela/Gerar chaves de persistência</c>, que vive no assembly de
    /// Editor e pode marcar a cena como suja com segurança.</para>
    /// </summary>
    [DisallowMultipleComponent]
    [AddComponentMenu("Favela Amarela/Persistência/Objeto Persistente")]
    public sealed class ObjetoPersistente : MonoBehaviour
    {
        [Tooltip("Chave imutável deste objeto. Gerada automaticamente — NÃO edite à mão " +
                 "nem copie entre objetos: duas chaves iguais fazem um sobrescrever o outro.")]
        [SerializeField] private string chaveDePersistencia;

        /// <summary>Chave imutável deste objeto, usada como índice no save.</summary>
        public string Chave => chaveDePersistencia;

        /// <summary>Se a chave já foi gerada.</summary>
        public bool TemChave => !string.IsNullOrWhiteSpace(chaveDePersistencia);

        /// <summary>
        /// Gera a chave se ainda não houver uma. Idempotente e <b>nunca</b> sobrescreve uma
        /// existente — regenerar uma chave é exatamente o que faria o save órfão.
        /// Retorna se algo mudou (a ferramenta de Editor usa isso para marcar a cena suja).
        /// </summary>
        public bool GarantirChave()
        {
            if (TemChave) return false;

            // Objetos em modo Prefab (fora de cena) não ganham chave: todas as instâncias
            // herdariam a mesma e uma sobrescreveria o estado da outra no save. A chave
            // nasce quando a instância existe numa cena de verdade.
            if (!gameObject.scene.IsValid()) return false;

            chaveDePersistencia = Guid.NewGuid().ToString("N");
            return true;
        }

        private void Reset() => GarantirChave();

        private void OnValidate()
        {
            if (!Application.isPlaying) GarantirChave();
        }

        private void Awake()
        {
            if (!TemChave)
            {
                Debug.LogError($"[ObjetoPersistente] '{name}' está sem chave de persistência — " +
                               "o estado dele não será salvo nem restaurado. Rode " +
                               "'Tools/FavelaAmarela/Gerar chaves de persistência'.", this);
            }
        }
    }
}
