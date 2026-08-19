using UnityEngine;
using FavelaAmarela.Player;
using FavelaAmarela.Runtime.GameLoop;

namespace FavelaAmarela.EditorTools
{
    /// <summary>
    /// Helper das ferramentas que montam cena: cria o GameObject de bootstrap com o conjunto
    /// completo de componentes focados.
    ///
    /// <para><b>Por que existe (2026-08-18):</b> seis ferramentas criavam um GameObject chamado
    /// "GameManager" com um componente só. Depois da refatoração, o papel dele virou <b>seis</b>
    /// componentes — e replicar essa lista em seis arquivos garantiria que uma cena nova nascesse
    /// faltando algum, em silêncio. Foi assim que o Vigor acabou existindo só na Arena e a
    /// persistência só na Tumba.</para>
    ///
    /// <para>Quem acrescentar um componente focado novo mexe <b>aqui</b>, e todas as ferramentas
    /// de montagem passam a criá-lo.</para>
    /// </summary>
    public static class MontarBootstrapDaCena
    {
        /// <summary>Nome do GameObject. Mantido de propósito para não quebrar buscas por nome.</summary>
        public const string NomeDoObjeto = "GameManager";

        /// <summary>
        /// Cria (ou completa) o objeto de bootstrap da cena aberta e devolve-o.
        ///
        /// <para>Idempotente: se o objeto já existir, apenas acrescenta os componentes que
        /// faltarem — o que também serve para atualizar cenas antigas.</para>
        /// </summary>
        public static GameObject Garantir()
        {
            var existente = Object.FindAnyObjectByType<GameLoopBootstrap>(
                FindObjectsInactive.Include);

            var go = existente != null
                ? existente.gameObject
                : new GameObject(NomeDoObjeto);

            Acrescentar<GameLoopBootstrap>(go);
            Acrescentar<GameStatePresenter>(go);
            Acrescentar<PlayerDeathController>(go);
            Acrescentar<CutsceneController>(go);
            Acrescentar<PausaInputHandler>(go);
            Acrescentar<CompanionManager>(go);

            return go;
        }

        private static void Acrescentar<T>(GameObject alvo) where T : Component
        {
            if (alvo.GetComponent<T>() == null) alvo.AddComponent<T>();
        }
    }
}
