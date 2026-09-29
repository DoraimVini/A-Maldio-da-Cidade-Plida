using System.Collections.Generic;
using System.Globalization;

namespace FavelaAmarela.Inventario
{
    /// <summary>Uma linha da comparação entre o item escolhido e o que está vestido.</summary>
    public readonly struct LinhaDeComparacao
    {
        /// <summary>O texto, já no vocabulário do jogador ("+4 Defesa Física").</summary>
        public readonly string Texto;

        /// <summary>+1 se trocar melhora, −1 se piora, 0 se é só informação.</summary>
        public readonly int Sinal;

        /// <summary>Cria a linha.</summary>
        public LinhaDeComparacao(string texto, int sinal)
        {
            Texto = texto;
            Sinal = sinal;
        }
    }

    /// <summary>
    /// O que a tela de inventário escreve sobre um item: o subtítulo, os atributos, a
    /// comparação com o que está vestido e o porquê de uma recusa.
    ///
    /// <para><b>Por que existe (2026-09-28).</b> Escolher um item na mochila acendia a casa e
    /// não dizia mais nada: o jogador equipava para descobrir o que o item fazia, e com afixos
    /// rolados dois exemplares do mesmo nome podiam ser coisas bem diferentes. O
    /// <c>ItemInstance.LinhasDeAfixo</c> existia desde agosto "para tooltip e ficha" — e nenhum
    /// tooltip o chamava.</para>
    ///
    /// <para>Sem Unity: só lê <see cref="ItemInstance"/> e <see cref="ItemDef"/>, e devolve texto.
    /// Aloca listas — é chamado quando a seleção muda, não por quadro.</para>
    /// </summary>
    public static class DescricaoDeItem
    {
        private static readonly CultureInfo PtBr = CultureInfo.GetCultureInfo("pt-BR");

        /// <summary>
        /// "Arma · Duas Mãos · Nível 3 · Marcado", "Consumível · ×2" — o que o item é, numa linha.
        /// </summary>
        public static string Subtitulo(ItemInstance item)
        {
            var def = item?.Def;
            if (def == null) return "";

            var partes = new List<string> { Tipo(def) };

            if (def.Tipo == ItemType.Arma)
                partes.Add(def.Empunhadura == Empunhadura.DuasMaos ? "Duas Mãos" : "Uma Mão");
            if (def.SlotEquipamento == EquipmentSlot.MaoSecundaria && def.Funcao != FuncaoDeMaoSecundaria.Nenhuma)
                partes.Add(def.Funcao == FuncaoDeMaoSecundaria.Escudo ? "Escudo" : "Foco");

            if (SeVeste(def))
            {
                partes.Add($"Nível {item.NivelDoItem}");
                partes.Add(NomesDeAtributo.De(item.Grau));
            }

            if (item.Quantidade > 1) partes.Add($"×{item.Quantidade}");
            return string.Join(" · ", partes);
        }

        /// <summary>O que o item faz, uma linha por efeito.</summary>
        public static List<string> Atributos(ItemInstance item)
        {
            var linhas = new List<string>();
            var def = item?.Def;
            if (def == null) return linhas;

            if (def.Tipo == ItemType.Consumivel)
            {
                LinhasDeConsumivel(def, linhas);
                return linhas;
            }

            if (def.Tipo == ItemType.Arma && def.Base != null)
            {
                var p = def.Base.PerfilNoNivel(item.NivelDoItem);
                linhas.Add($"Dano {p.DanoMin.ToString("0", PtBr)}–{p.DanoMax.ToString("0", PtBr)}");
                linhas.Add($"Crítico {(p.ChanceCritica * 100f).ToString("0", PtBr)}% · ×{p.MultiplicadorCritico.ToString("0.##", PtBr)}");
                linhas.Add($"Precisão {(p.Precisao * 100f).ToString("0", PtBr)}%");
            }

            if (def.SlotEquipamento == EquipmentSlot.MaoSecundaria && def.Funcao == FuncaoDeMaoSecundaria.Escudo)
                linhas.Add($"Bloquear reduz o golpe em {(def.ReducaoAoBloquear * 100f).ToString("0", PtBr)}%");

            foreach (var par in SomaPorAtributo(item))
            {
                string linha = NomesDeAtributo.Linha(par.Key, par.Value);
                // Honesto sem jargão: um bônus que nenhum sistema lê não pode aparecer como se
                // valesse. "Adormecido" é a palavra do jogo para isso.
                if (NomesDeAtributo.NaoTemEfeito(par.Key)) linha += " (adormecido)";
                linhas.Add(linha);
            }

            return linhas;
        }

        /// <summary>
        /// Os efeitos de um consumível no vocabulário da cura: o jogo aplica a Vitalidade como
        /// cura da carne e a Resiliência como Ancoragem (<c>VitalidadeBridge.AplicarEfeitoConsumivel</c>).
        /// </summary>
        private static void LinhasDeConsumivel(ItemDef def, List<string> linhas)
        {
            if (def.Modificadores == null) return;
            foreach (var mod in def.Modificadores)
            {
                string valor = mod.Valor.ToString("0.#", PtBr);
                if (mod.Stat == StatType.VitMaxima) linhas.Add($"Restaura {valor} de Vitalidade Corpórea");
                else if (mod.Stat == StatType.RMMaxima) linhas.Add($"Ancora {valor} de Resiliência Mental");
            }
        }

        /// <summary>
        /// Soma os modificadores do item por atributo (implícitos da base + afixos rolados), na
        /// ordem em que aparecem — dois afixos no mesmo atributo viram uma linha só.
        /// </summary>
        public static List<KeyValuePair<StatType, float>> SomaPorAtributo(ItemInstance item)
        {
            var ordem = new List<StatType>();
            var soma = new Dictionary<StatType, float>();
            if (item?.Def == null) return new List<KeyValuePair<StatType, float>>();

            foreach (var mod in item.ModificadoresEfetivos())
            {
                if (!soma.ContainsKey(mod.Stat)) { soma[mod.Stat] = 0f; ordem.Add(mod.Stat); }
                soma[mod.Stat] += mod.Valor;
            }

            var saida = new List<KeyValuePair<StatType, float>>();
            foreach (var s in ordem)
                if (soma[s] != 0f) saida.Add(new KeyValuePair<StatType, float>(s, soma[s]));
            return saida;
        }

        /// <summary>
        /// O que muda se o <paramref name="escolhido"/> tomar o lugar do <paramref name="vestido"/>.
        /// Vazio quando não se compara (o escolhido não se veste, ou é o próprio vestido).
        /// </summary>
        public static List<LinhaDeComparacao> Comparar(ItemInstance escolhido, ItemInstance vestido)
        {
            var linhas = new List<LinhaDeComparacao>();
            var def = escolhido?.Def;
            if (def == null || !SeVeste(def) || ReferenceEquals(escolhido, vestido)) return linhas;

            if (vestido?.Def == null)
            {
                linhas.Add(new LinhaDeComparacao(
                    $"Nada vestido como {NomesDeAtributo.De(def.SlotEquipamento)}: tudo é ganho.", 1));
                return linhas;
            }

            if (def.Tipo == ItemType.Arma && def.Base != null && vestido.Def.Base != null)
            {
                float delta = DanoMedio(escolhido) - DanoMedio(vestido);
                if (System.Math.Abs(delta) >= 0.05f)
                    linhas.Add(new LinhaDeComparacao(
                        $"{(delta > 0f ? "+" : "−")}{System.Math.Abs(delta).ToString("0.#", PtBr)} de dano médio",
                        delta > 0f ? 1 : -1));
            }

            var a = Dicionario(escolhido);
            var b = Dicionario(vestido);
            var todos = new List<StatType>(a.Keys);
            foreach (var k in b.Keys) if (!a.ContainsKey(k)) todos.Add(k);

            foreach (var stat in todos)
            {
                a.TryGetValue(stat, out float va);
                b.TryGetValue(stat, out float vb);
                float d = va - vb;
                if (System.Math.Abs(d) < 0.001f) continue;

                int sinal = NomesDeAtributo.NaoTemEfeito(stat) ? 0 : (d > 0f ? 1 : -1);
                if (MenorEMelhor(stat)) sinal = -sinal;
                linhas.Add(new LinhaDeComparacao(NomesDeAtributo.Linha(stat, d), sinal));
            }

            if (linhas.Count == 0) linhas.Add(new LinhaDeComparacao("Igual ao que está vestido.", 0));
            return linhas;
        }

        /// <summary>A frase de recusa ao vestir, no vocabulário do jogo.</summary>
        /// <param name="armaEmpunhada">A arma de duas mãos vestida (para a recusa das mãos).</param>
        /// <param name="maoSecundaria">O item da Mão Secundária (para a recusa da mochila cheia).</param>
        public static string TextoDaRecusa(RecusaAoEquipar recusa, ItemInstance item,
                                           ItemInstance armaEmpunhada, ItemInstance maoSecundaria)
        {
            string nome = Nome(item);
            return recusa switch
            {
                RecusaAoEquipar.NaoSeVeste when item?.Def?.Tipo == ItemType.Consumivel =>
                    $"{nome} não se veste — consome-se pela barra, nas teclas 1 a 8.",
                RecusaAoEquipar.NaoSeVeste => $"{nome} não se veste nem se empunha.",
                RecusaAoEquipar.SemLugarNoCorpo => $"O corpo de Damião não tem onde levar {nome}.",
                RecusaAoEquipar.MaosTomadasPorArmaDeDuasMaos =>
                    $"{Nome(armaEmpunhada)} ocupa as duas mãos de Damião. Não há onde segurar {nome}.",
                RecusaAoEquipar.SemEspacoParaGuardarAMaoSecundaria =>
                    $"Para empunhar {nome} com as duas mãos, {Nome(maoSecundaria)} tem de voltar à " +
                    "mochila — e ela está cheia.",
                _ => "",
            };
        }

        /// <summary>A frase de recusa ao tirar do corpo.</summary>
        public static string TextoDaRecusa(RecusaAoDesequipar recusa, ItemInstance item) => recusa switch
        {
            RecusaAoDesequipar.MochilaCheia => $"A mochila está cheia. {Nome(item)} continua com Damião.",
            _ => "",
        };

        // ── Apoio ────────────────────────────────────────────────────────────

        /// <summary>Se o item vai para o corpo.</summary>
        public static bool SeVeste(ItemDef def) =>
            def != null && (def.Tipo == ItemType.Arma || def.Tipo == ItemType.Armadura || def.Tipo == ItemType.Amuleto)
            && def.SlotEquipamento != EquipmentSlot.Nenhum;

        private static string Tipo(ItemDef def) => def.Tipo switch
        {
            ItemType.Arma => "Arma",
            ItemType.Armadura or ItemType.Amuleto => NomesDeAtributo.De(def.SlotEquipamento),
            ItemType.Consumivel => "Consumível",
            ItemType.Chave => "Achado",
            ItemType.Artefato => "Artefato",
            _ => def.Tipo.ToString(),
        };

        private static string Nome(ItemInstance item) => item == null ? "isto" : item.NomeExibido();

        private static float DanoMedio(ItemInstance item)
        {
            var p = item.Def.Base.PerfilNoNivel(item.NivelDoItem);
            return (p.DanoMin + p.DanoMax) * 0.5f;
        }

        private static Dictionary<StatType, float> Dicionario(ItemInstance item)
        {
            var d = new Dictionary<StatType, float>();
            foreach (var par in SomaPorAtributo(item)) d[par.Key] = par.Value;
            return d;
        }

        /// <summary>Atributos de custo: menos é melhor.</summary>
        private static bool MenorEMelhor(StatType stat) =>
            stat == StatType.CustoEsquivaVigor || stat == StatType.CustoCorridaVigor || stat == StatType.DrenoRM;
    }
}
