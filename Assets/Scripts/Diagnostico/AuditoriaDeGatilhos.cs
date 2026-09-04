using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
using UnityEngine.Tilemaps;

namespace FavelaAmarela.Runtime.Diagnostico
{
    /// <summary>
    /// Responde duas perguntas que a auditoria de geometria não responde: <b>para que serve
    /// cada gatilho</b>, e <b>o callback casa com o colisor</b>.
    ///
    /// <para><b>As regras vêm da doc da 6000.4</b>, não de memória — e elas contrariam o
    /// palpite óbvio:</para>
    /// <list type="number">
    ///   <item>A mensagem vai <i>"para o Collider2D de trigger … <b>e para o Rigidbody2D (ou o
    ///   Collider2D, se não houver Rigidbody2D) que TOCA o trigger</b>"</i>. Ou seja, um script
    ///   num objeto <b>sólido</b> receber <c>OnTriggerEnter2D</c> é legítimo: ele recebe ao
    ///   entrar no gatilho de outro. Marcar isso como defeito seria acusar o padrão certo.</item>
    ///   <item><i>"<b>Trigger events are only sent if one of the Colliders also has a
    ///   Rigidbody2D attached.</b>"</i> Duas coisas sem corpo que se sobrepõem não disparam
    ///   nada. Neste projeto quem entra nas zonas é o Damião, que tem corpo — então zona sem
    ///   corpo funciona, e acusá-la seria falso positivo.</item>
    ///   <item>A mensagem vai para o <b>GameObject do colisor</b>. Script no pai com o colisor
    ///   num filho <b>nunca recebe</b> — este é defeito de verdade, e silencioso.</item>
    /// </list>
    ///
    /// <para><b>O que NÃO existe neste projeto:</b> nenhum script declara
    /// <c>OnCollisionEnter2D</c>, <c>Stay</c> ou <c>Exit</c>. Todas as declarações de callback
    /// são de trigger. Não há nada que dependa de evento de colisão sólida — o colisor sólido
    /// aqui só barra movimento, e a física resolve isso sozinha.</para>
    ///
    /// <para>Essa contagem <b>não mora aqui</b>: ela é feita no <c>Rigidbody2DAuditor</c> com
    /// <c>TypeCache</c>, que enxerga todo tipo do projeto. Uma versão runtime existiu por
    /// alguns minutos e foi apagada — ela só veria os scripts <b>instanciados nas cenas</b>, e
    /// afirmaria sobre a amostra achando que afirma sobre o projeto.</para>
    /// </summary>
    public static class AuditoriaDeGatilhos
    {
        /// <summary>Para que serve o colisor, deduzido dos componentes que o acompanham.</summary>
        public enum Proposito
        {
            Desconhecido,
            HitboxDeGolpe,
            Hurtbox,
            Coletavel,
            Interacao,
            PortalDeCena,
            ZonaDeAmbiente,

            /// <summary>
            /// Gatilho que <b>se move e encosta</b>: contato de inimigo, projétil. Distingue-se
            /// da zona por ter <see cref="Rigidbody2D"/> próprio.
            /// </summary>
            VolumeDeContato,

            Cenario,
        }

        /// <summary>O que há de errado, quando há.</summary>
        public enum Veredito
        {
            Ok,

            /// <summary>Script com callback de trigger num objeto <b>sem colisor nenhum</b>.</summary>
            CallbackSemColisor,

            /// <summary>Colisor de trigger no filho e o script com o callback no pai.</summary>
            CallbackNoPaiColisorNoFilho,

            /// <summary>Gatilho sem script de callback e sem propósito reconhecido.</summary>
            GatilhoSemDono,

            /// <summary>Zona de ambiente marcada como sólida — ela barra o jogador.</summary>
            ZonaSolida,
        }

        /// <summary>Uma linha do relatório de gatilhos.</summary>
        public struct Linha
        {
            public string Caminho;
            public string Tipo;
            public bool EhTrigger;
            public bool TemCorpoNaHierarquia;
            public Proposito Funcao;
            public string Callbacks;      // nomes dos callbacks achados, ou "—"
            public Veredito Diagnostico;
            public string Explicacao;
        }

        private static readonly string[] CallbacksDeTrigger =
        {
            "OnTriggerEnter2D", "OnTriggerStay2D", "OnTriggerExit2D",
        };

        /// <summary>Varre a hierarquia inteira, inclusive objetos desativados.</summary>
        public static void Medir(GameObject raiz, List<Linha> saida)
        {
            if (raiz == null || saida == null) return;

            foreach (var col in raiz.GetComponentsInChildren<Collider2D>(includeInactive: true))
                saida.Add(Medir(col));

            // Um script com callback de trigger num objeto SEM colisor nenhum nunca dispara:
            // ele não é o gatilho, e também não tem com o que tocar um. É o caso mais mudo de
            // todos, e a varredura de colisores acima não o encontraria por definição.
            foreach (var comp in raiz.GetComponentsInChildren<MonoBehaviour>(includeInactive: true))
            {
                if (comp == null) continue;
                if (comp.GetComponent<Collider2D>() != null) continue;

                string achados = CallbacksDe(comp, CallbacksDeTrigger);
                if (achados == null) continue;

                saida.Add(new Linha
                {
                    Caminho = Hierarquia(comp.transform),
                    Tipo = "—",
                    Funcao = Proposito.Desconhecido,
                    Callbacks = achados,
                    Diagnostico = Veredito.CallbackSemColisor,
                    Explicacao = $"'{comp.GetType().Name}' declara {achados} e o GameObject não " +
                                 "tem Collider2D. Sem colisor ele não é gatilho nem toca um: o " +
                                 "método nunca roda, e nada no console avisa.",
                });
            }
        }

        private static Linha Medir(Collider2D col)
        {
            var l = new Linha
            {
                Caminho = Hierarquia(col.transform),
                Tipo = col.GetType().Name.Replace("Collider2D", ""),
                EhTrigger = col.isTrigger,
                TemCorpoNaHierarquia = col.attachedRigidbody != null,
                Funcao = PropositoDe(col),
            };

            l.Callbacks = CallbacksNoObjeto(col.gameObject, CallbacksDeTrigger) ?? "—";
            Diagnosticar(col, ref l);
            return l;
        }

        private static void Diagnosticar(Collider2D col, ref Linha l)
        {
            l.Diagnostico = Veredito.Ok;

            // Zona de ambiente sólida barra o jogador — o oposto do que ela existe para fazer.
            if (!l.EhTrigger &&
                (l.Funcao == Proposito.ZonaDeAmbiente || l.Funcao == Proposito.PortalDeCena ||
                 l.Funcao == Proposito.Coletavel || l.Funcao == Proposito.Hurtbox))
            {
                l.Diagnostico = Veredito.ZonaSolida;
                l.Explicacao = $"É {l.Funcao} e o colisor está SÓLIDO. Além de disparar o " +
                               "callback errado, ele empurra o Damião: uma zona que deveria ser " +
                               "atravessada vira parede.";
                return;
            }

            if (!l.EhTrigger) return;

            // Gatilho cujo callback mora no PAI: a mensagem vai para o GameObject do colisor,
            // então o script do pai nunca recebe. Silencioso.
            //
            // MAS só é defeito se o pai NÃO TIVER COLISOR PRÓPRIO. A primeira versão desta
            // regra não checava isso e acusou os três Pontos Focais de relíquia do Castelo: o
            // pai deles (Z5_TronoDeAldebaran) tem BoxCollider2D e o callback dele dispara no
            // colisor dele -- o trigger do filho é outra coisa (interação, achada por
            // OverlapCircle e não por callback). Pai com colisor próprio é composição normal,
            // não defeito.
            if (l.Callbacks == "—" && col.transform.parent != null
                && col.transform.parent.GetComponent<Collider2D>() == null)
            {
                string noPai = CallbacksNoObjeto(col.transform.parent.gameObject,
                                                 CallbacksDeTrigger);
                if (noPai != null)
                {
                    l.Diagnostico = Veredito.CallbackNoPaiColisorNoFilho;
                    l.Explicacao = $"O trigger está aqui e o {noPai} está no PAI " +
                                   $"('{col.transform.parent.name}'), que NÃO tem colisor " +
                                   "próprio. A doc da 6000.4 diz que a mensagem vai para o " +
                                   "GameObject do colisor — o script do pai nunca recebe.";
                    return;
                }
            }

            // Gatilho que não faz nada: sem callback e sem papel conhecido.
            if (l.Callbacks == "—" && l.Funcao == Proposito.Desconhecido)
            {
                l.Diagnostico = Veredito.GatilhoSemDono;
                l.Explicacao = "Trigger sem callback e sem componente que explique o que ele é. " +
                               "Ou sobrou de algo removido, ou alguém depende dele por consulta " +
                               "de física (OverlapCircle), que não aparece aqui.";
            }
        }

        private static Proposito PropositoDe(Collider2D col)
        {
            var go = col.gameObject;

            if (Tem(go, "Hitbox")) return Proposito.HitboxDeGolpe;
            if (Tem(go, "Hurtbox")) return Proposito.Hurtbox;
            if (Tem(go, "ColetavelDeItem")) return Proposito.Coletavel;
            if (Tem(go, "PortalDeCena")) return Proposito.PortalDeCena;
            if (col.GetComponent<Interaction.IInteragivel>() != null) return Proposito.Interacao;
            if (col is TilemapCollider2D || col is CompositeCollider2D) return Proposito.Cenario;

            foreach (var c in go.GetComponents<MonoBehaviour>())
            {
                if (c == null) continue;
                string n = c.GetType().Name;
                if (n.EndsWith("Trigger") || n.EndsWith("Zone") || n.EndsWith("Zona"))
                    return Proposito.ZonaDeAmbiente;
            }

            // Quem tem callback de trigger TEM propósito -- ele está escrito no script. A
            // primeira versão parava no nome da classe e devolvia "Desconhecido" para
            // RefugioDeLuz, VeuDaTempestade, ArenaDosPortoes, ConeDeGelo e CoisaDoCemiterioAI,
            // que são justamente os gatilhos mais claros do jogo. Listar esses nomes à mão
            // envelheceria; o que separa os dois grupos é derivável:
            //
            //   com corpo  -> se MOVE e encosta: projétil, contato de inimigo.
            //   sem corpo  -> fica parado e o jogador entra: refúgio, véu, arena.
            if (CallbacksNoObjeto(go, CallbacksDeTrigger) != null)
            {
                return col.attachedRigidbody != null
                    ? Proposito.VolumeDeContato
                    : Proposito.ZonaDeAmbiente;
            }

            return Proposito.Desconhecido;
        }

        private static bool Tem(GameObject go, string nomeDoTipo)
        {
            foreach (var c in go.GetComponents<Component>())
                if (c != null && c.GetType().Name == nomeDoTipo) return true;

            return false;
        }

        /// <summary>Callbacks declarados por algum script deste GameObject, ou null.</summary>
        private static string CallbacksNoObjeto(GameObject go, string[] nomes)
        {
            var todos = new List<string>();

            foreach (var c in go.GetComponents<MonoBehaviour>())
            {
                if (c == null) continue;
                string achados = CallbacksDe(c, nomes);
                if (achados != null) todos.Add($"{c.GetType().Name}.{achados}");
            }

            return todos.Count == 0 ? null : string.Join(", ", todos);
        }

        /// <summary>
        /// Callbacks que ESTE tipo declara. <c>DeclaredOnly</c> de propósito: herdar de uma base
        /// que declara o método não faz o filho ser o dono do comportamento, e contar herança
        /// encheria o relatório com a mesma linha repetida.
        /// </summary>
        private static string CallbacksDe(MonoBehaviour comp, string[] nomes)
        {
            var achados = new List<string>();

            for (var t = comp.GetType(); t != null && t != typeof(MonoBehaviour); t = t.BaseType)
            {
                foreach (var nome in nomes)
                {
                    if (achados.Contains(nome)) continue;
                    var m = t.GetMethod(nome, BindingFlags.Instance | BindingFlags.Public |
                                              BindingFlags.NonPublic | BindingFlags.DeclaredOnly);
                    if (m != null) achados.Add(nome);
                }
            }

            return achados.Count == 0 ? null : string.Join("+", achados);
        }

        private static string Hierarquia(Transform t)
        {
            var partes = new List<string>();
            for (var a = t; a != null; a = a.parent) partes.Add(a.name);
            partes.Reverse();
            return string.Join("/", partes);
        }
    }
}
