using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;
using FavelaAmarela.Core.Loot;
using FavelaAmarela.Core.Artefatos;
using FavelaAmarela.Core.Combat;
using FavelaAmarela.Inventario;
using FavelaAmarela.Player;
using FavelaAmarela.Runtime.Enemies;

namespace FavelaAmarela.EditorTools
{
    /// <summary>
    /// Ferramenta de Editor, só funciona em Play Mode (precisa dos singletons vivos).
    /// Concede artefatos e armas sob demanda e invoca os dois chefes do Vertical Slice
    /// (Byakhee, Rei em Amarelo) numa cena qualquer — pensada para a
    /// <c>Cena_ArenaDeTestes</c>, mas funciona em qualquer cena com um Player e um
    /// <c>GameManager</c>.
    ///
    /// <para><b>Por que existe:</b> o ritual de selamento do Rei exige a Coroa de Ossos, e
    /// ela não tem fonte jogável ainda (Templo da Serpente sem cena) — sem este atalho não
    /// haveria como testar a luta final de ponta a ponta. Nasceu também da calibração do
    /// Byakhee, feita inteira por simulação Python fora do jogo por falta de visibilidade ao
    /// vivo do estado da FSM; a seção "Estado ao vivo" abaixo é a resposta a isso.</para>
    ///
    /// <para><b>Nunca entra em build de jogador</b> — vive em <c>Assets/FavelaAmarela/Editor/</c>,
    /// que já é editor-only por convenção do projeto (ver `RodarTodoOWiring.cs` e vizinhos).</para>
    /// </summary>
    public sealed class CarcosaDebuggerWindow : EditorWindow
    {
        private const string FichaByakheePath = "Assets/FavelaAmarela/Config/Ficha_Byakhee.asset";

        private static readonly (string Id, string Nome)[] Artefatos =
        {
            ("necronomicon", "Necronomicon"),
            ("patua_luas_gemeas", "Patuá das Luas Gêmeas"),
            ("anel_sinal_amarelo", "Anel do Sinal Amarelo"),
            ("coroa_de_ossos", "Coroa de Ossos"),
        };

        /// <summary>
        /// As armas do catálogo, <b>lidas do disco</b>.
        ///
        /// <para>Era um array de GUIDs escritos à mão — a <b>quarta</b> lista de armas do
        /// projeto, depois do enum <c>TipoArmaFisica</c>, do dicionário da fábrica e do
        /// <c>ArmaDeTeste</c> da bridge (as três outras saíram na Fase 4). Uma arma nova criada
        /// pela forja não aparecia aqui até alguém editar este array.</para>
        /// </summary>
        private static (string Id, string Nome)[] Armas()
        {
            var itens = Resources.LoadAll<ItemDef>("");
            if (itens == null) return new (string, string)[0];

            return itens.Where(d => d != null && d.Tipo == ItemType.Arma)
                        .OrderBy(d => d.Nome)
                        .Select(d => (d.Id, string.IsNullOrWhiteSpace(d.Nome) ? d.name : d.Nome))
                        .ToArray();
        }

        private Vector2 _scroll;

        [MenuItem("Tools/FavelaAmarela/Carcosa Debugger")]
        private static void AbrirJanela()
        {
            GetWindow<CarcosaDebuggerWindow>("Carcosa Debugger");
        }

        private void OnEnable()
        {
            EditorApplication.update += Repaint;
        }

        private void OnDisable()
        {
            EditorApplication.update -= Repaint;
        }

        /// <summary>As duas metades da janela. Só uma delas exige Play Mode.</summary>
        private enum Aba { Partida, Forja }

        private Aba _aba = Aba.Partida;

        private void OnGUI()
        {
            // A FORJA funciona fora do Play Mode, e isso não é detalhe: criar item é AUTORIA,
            // e autoria acontece com o jogo parado. Antes desta separação a janela inteira
            // recusava trabalhar fora do Play Mode, porque toda ela dependia dos singletons.
            _aba = (Aba)GUILayout.Toolbar((int)_aba,
                new[] { "Partida (Play Mode)", "Forja de Itens" });

            GUILayout.Space(6);

            _scroll = EditorGUILayout.BeginScrollView(_scroll);

            if (_aba == Aba.Forja) DesenharForja();
            else DesenharPartida();

            EditorGUILayout.EndScrollView();
        }

        private void DesenharPartida()
        {
            if (!EditorApplication.isPlaying)
            {
                EditorGUILayout.HelpBox(
                    "Entre em Play Mode para usar esta aba — ela depende dos singletons " +
                    "de cena (GameManager, InventoryManager) e do Player. " +
                    "A aba Forja funciona com o jogo parado.",
                    MessageType.Info);
                return;
            }

            DesenharSecaoArtefatos();
            GUILayout.Space(8);
            DesenharSecaoSet();
            GUILayout.Space(8);
            DesenharSecaoArmas();
            GUILayout.Space(8);
            DesenharSecaoChefes();
            GUILayout.Space(8);
            DesenharEstadoAoVivo();
        }

        // -- Forja de Itens ---------------------------------------------------
        //
        // A referencia que o Vini deu foi o Pokesav: nao um cheat de "god mode", mas um EDITOR
        // que cria o item com os atributos que voce quiser e o injeta no jogo. A diferenca
        // importante e que o Pokesav escrevia no arquivo de save, e esta forja escreve ASSET.
        //
        // Escrever asset nao e detalhe de implementacao -- e a diferenca entre funcionar e
        // corromper. Um ItemDef criado so em memoria (via ItemDatabase.Registrar) some no
        // reload, porque o banco reconstroi o cache so de Resources.LoadAll. E como o save
        // guarda IDs, um encostao num RefugioDeLuz -- que grava em disco sozinho -- deixaria a
        // partida referenciando um item que nao existe mais. Pior: BaseInventory.CanAdd recusa
        // Def nulo SEM LOG NENHUM, entao o item sumiria da mochila em silencio.

        /// <summary>Onde o item precisa nascer para existir em runtime.</summary>
        private const string PastaDosItens = "Assets/FavelaAmarela/Config/Resources/Itens";

        private readonly ReceitaDeItem _receita = new ReceitaDeItem();
        private Sprite _icone;
        private bool _idManual;

        private void DesenharForja()
        {
            EditorGUILayout.LabelField("Forja de Itens", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox(
                "Cria um ItemDef de verdade em " + PastaDosItens + ".\n" +
                "Fora de Resources o item nao existe em runtime; em memoria, ele some no " +
                "reload e leva o save junto.", MessageType.None);

            GUILayout.Space(4);

            EditorGUI.BeginChangeCheck();
            _receita.Nome = EditorGUILayout.TextField("Nome (visivel)", _receita.Nome);
            if (EditorGUI.EndChangeCheck() && !_idManual)
                _receita.Id = ReceitaDeItem.SugerirId(_receita.Nome);

            using (new EditorGUILayout.HorizontalScope())
            {
                EditorGUI.BeginChangeCheck();
                _receita.Id = EditorGUILayout.TextField("Id (catalogo/save)", _receita.Id);
                if (EditorGUI.EndChangeCheck()) _idManual = true;

                if (GUILayout.Button("auto", GUILayout.Width(48)))
                {
                    _receita.Id = ReceitaDeItem.SugerirId(_receita.Nome);
                    _idManual = false;
                    GUI.FocusControl(null);
                }
            }

            _icone = (Sprite)EditorGUILayout.ObjectField("Icone", _icone, typeof(Sprite), false);
            _receita.TemIcone = _icone != null;

            GUILayout.Space(4);

            _receita.Tipo = (ItemType)EditorGUILayout.EnumPopup("Tipo", _receita.Tipo);
            _receita.Slot = (EquipmentSlot)EditorGUILayout.EnumPopup("Slot", _receita.Slot);
            _receita.EmpilhamentoMaximo =
                EditorGUILayout.IntField("Empilhamento max.", _receita.EmpilhamentoMaximo);

            if (_receita.Tipo == ItemType.Arma)
                _receita.Empunhadura =
                    (Empunhadura)EditorGUILayout.EnumPopup("Empunhadura", _receita.Empunhadura);

            GUILayout.Space(6);
            DesenharBlocoDeCombate();
            GUILayout.Space(6);
            DesenharModificadores();
            GUILayout.Space(6);
            DesenharValidacaoECriacao();
        }

        /// <summary>
        /// A <b>matemática construída</b>: família, nível, grau — e a conta que sai deles.
        ///
        /// <para><b>Por que a prévia existe (2026-08-28).</b> O Vini pediu que o Debugger criasse
        /// itens "com a matemática construída, para conseguirmos melhorar e expandir o arsenal
        /// do jogo". Expandir arsenal sem ver a conta é chutar: a única forma de saber se uma
        /// arma nova estava forte ou fraca era criá-la, equipar, entrar em Play e bater em
        /// alguém. A prévia responde antes de o asset existir.</para>
        /// </summary>
        private void DesenharBlocoDeCombate()
        {
            EditorGUILayout.LabelField("Matematica do item", EditorStyles.boldLabel);

            _receita.NivelDoItem = Mathf.Max(1,
                EditorGUILayout.IntField("Nivel do item", _receita.NivelDoItem));

            _receita.Grau = (GrauDeImpregnacao)EditorGUILayout.EnumPopup(
                "Grau (previa)", _receita.Grau);

            if (_receita.Tipo != ItemType.Arma)
            {
                EditorGUILayout.HelpBox(
                    "Nivel abre o pool de afixos. Dano branco so vale para arma.",
                    MessageType.None);

                DesenharPreviaDeAfixos();
                return;
            }

            _receita.Base = (BaseDeArma)EditorGUILayout.ObjectField(
                "Familia (BaseDeArma)", _receita.Base, typeof(BaseDeArma), false);

            if (_receita.Base == null)
            {
                // Este era o buraco silencioso da Forja: ela criava a arma sem familia, e o
                // jogador equipava e continuava desarmado.
                EditorGUILayout.HelpBox(
                    "SEM FAMILIA a arma sai INERTE: equipar nao causa dano nenhum. A familia " +
                    "carrega o dano branco, a geometria do golpe e a habilidade.",
                    MessageType.Error);
                return;
            }

            DesenharContaDaArma(_receita.Base.PerfilNoNivel(_receita.NivelDoItem));
            DesenharPreviaDeAfixos();
        }

        /// <summary>
        /// A conta de uma arma no nível escolhido, e o que ela significa contra o elenco.
        /// "Golpes para abater" é a única linha que um designer lê sem traduzir.
        /// </summary>
        private void DesenharContaDaArma(PerfilDeArma perfil)
        {
            float media = (perfil.DanoMin + perfil.DanoMax) * 0.5f;
            float esperado = media * perfil.Precisao
                             * (1f + perfil.ChanceCritica * (perfil.MultiplicadorCritico - 1f));

            using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
            {
                EditorGUILayout.LabelField(
                    "Dano branco: " + perfil.DanoMin.ToString("0.#") + " - " +
                    perfil.DanoMax.ToString("0.#") + "  (media " + media.ToString("0.#") + ")");

                EditorGUILayout.LabelField(
                    "Critico: " + perfil.ChanceCritica.ToString("P0") + " x" +
                    perfil.MultiplicadorCritico.ToString("0.##") + "   Precisao: " +
                    perfil.Precisao.ToString("P0"));

                EditorGUILayout.LabelField("Esperado por golpe: " + esperado.ToString("0.#"),
                                           EditorStyles.boldLabel);

                GUILayout.Space(2);
                EditorGUILayout.LabelField("Golpes para abater:", EditorStyles.miniBoldLabel);

                foreach (var ficha in FichasDoElenco())
                {
                    var alvo = ficha.CriarFicha(1);
                    if (alvo.VitalidadeMax <= 0f) continue;

                    float porGolpe = MitigacaoDeDano.Aplicar(esperado, alvo.Defesa);
                    if (porGolpe <= 0f) continue;

                    int golpes = Mathf.CeilToInt(alvo.VitalidadeMax / porGolpe);

                    EditorGUILayout.LabelField(
                        "   " + ficha.name + ": " + golpes + "  (" + porGolpe.ToString("0.#") +
                        " por golpe, " + alvo.VitalidadeMax.ToString("0") + " de Vitalidade, " +
                        alvo.Defesa.ToString("0") + " de Defesa)",
                        EditorStyles.miniLabel);
                }
            }
        }

        /// <summary>
        /// Quantos afixos o grau concede, e o que o pool tem a oferecer neste nível. Mostrar a
        /// <b>chance de cada grau</b> junto responde "quão raro é isto?" sem sortear mil vezes.
        /// </summary>
        private void DesenharPreviaDeAfixos()
        {
            int prefixos = RegrasDeGrau.Prefixos(_receita.Grau);
            int sufixos = RegrasDeGrau.Sufixos(_receita.Grau);

            var elegiveis = CatalogoDeAfixos.Todos
                .Where(a => a != null && a.EhLegalPara(_receita.Slot, _receita.NivelDoItem))
                .ToList();

            using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
            {
                EditorGUILayout.LabelField(
                    "Grau " + _receita.Grau + ": " + prefixos + " prefixo(s) + " +
                    sufixos + " sufixo(s)");

                EditorGUILayout.LabelField(
                    "Pool elegivel no nivel " + _receita.NivelDoItem + ": " +
                    elegiveis.Count + " afixo(s)", EditorStyles.miniLabel);

                if (elegiveis.Count == 0 && (prefixos + sufixos) > 0)
                    EditorGUILayout.HelpBox(
                        "O grau concede afixos e NENHUM afixo do pool e elegivel para este " +
                        "slot/nivel. O item sairia com grau alto e nenhum modificador.",
                        MessageType.Warning);

                GUILayout.Space(2);
                EditorGUILayout.LabelField("Chance de cair, por nivel de jogador:",
                                           EditorStyles.miniBoldLabel);

                foreach (int nivel in new[] { 1, 6, 12 })
                {
                    EditorGUILayout.LabelField(
                        "   nivel " + nivel + ": Inerte " +
                        CurvaDeGrau.Chance(GrauDeImpregnacao.Inerte, nivel).ToString("P1") +
                        "   Marcado " +
                        CurvaDeGrau.Chance(GrauDeImpregnacao.Marcado, nivel).ToString("P1") +
                        "   Impregnado " +
                        CurvaDeGrau.Chance(GrauDeImpregnacao.Impregnado, nivel).ToString("P1"),
                        EditorStyles.miniLabel);
                }
            }
        }

        /// <summary>As fichas do elenco, para a linha de "golpes para abater".</summary>
        private static FichaAtributosConfig[] FichasDoElenco()
        {
            return AssetDatabase.FindAssets("t:FichaAtributosConfig")
                .Select(AssetDatabase.GUIDToAssetPath)
                .Select(AssetDatabase.LoadAssetAtPath<FichaAtributosConfig>)
                .Where(f => f != null && f.Ataque > 0f)   // so quem luta de volta
                .OrderBy(f => f.name)
                .ToArray();
        }

        private void DesenharModificadores()
        {
            EditorGUILayout.LabelField("Modificadores implicitos", EditorStyles.boldLabel);

            for (int i = 0; i < _receita.Modificadores.Count; i++)
            {
                var mod = _receita.Modificadores[i];

                using (new EditorGUILayout.HorizontalScope())
                {
                    mod.Stat = (StatType)EditorGUILayout.EnumPopup(mod.Stat);
                    mod.Valor = EditorGUILayout.FloatField(mod.Valor, GUILayout.Width(70));

                    if (GUILayout.Button("-", GUILayout.Width(24)))
                    {
                        _receita.Modificadores.RemoveAt(i);
                        return;
                    }
                }

                _receita.Modificadores[i] = mod;

                // O aviso vem POR LINHA, na hora, e nao so na validacao final: quem esta
                // autorando precisa saber que aquele atributo nao faz nada ANTES de terminar
                // de montar o item em cima dele.
                if (ReceitaDeItem.AtributosSemEfeito.Contains(mod.Stat))
                    EditorGUILayout.HelpBox(
                        "'" + NomesDeAtributo.De(mod.Stat) + "' NAO TEM EFEITO no jogo -- " +
                        "nenhum sistema le este atributo. O jogador leria o numero e nao " +
                        "receberia nada.", MessageType.Error);
            }

            if (GUILayout.Button("+ Modificador"))
                _receita.Modificadores.Add(new ModificadorFixo(StatType.VitMaxima, 1f));
        }

        private void DesenharValidacaoECriacao()
        {
            var existentes = Resources.LoadAll<ItemDef>("")
                                      .Where(d => d != null)
                                      .Select(d => d.Id)
                                      .ToList();

            var problemas = _receita.Problemas(existentes);

            foreach (var p in problemas) EditorGUILayout.HelpBox(p, MessageType.Error);
            foreach (var a in _receita.Avisos()) EditorGUILayout.HelpBox(a, MessageType.Warning);

            using (new EditorGUI.DisabledScope(problemas.Count > 0))
            {
                if (GUILayout.Button("Forjar item", GUILayout.Height(28)))
                    Forjar();
            }

            if (problemas.Count > 0)
                EditorGUILayout.LabelField(problemas.Count + " problema(s) impedem a criacao.",
                                           EditorStyles.miniLabel);
        }

        /// <summary>
        /// Escreve o asset. Segue o padrao de <c>GeradorDeReliquias</c>: garante a pasta,
        /// carrega antes de criar (para nao perder o GUID de um asset ja existente), e fecha
        /// com <c>SetDirty</c> + <c>SaveAssetIfDirty</c>.
        /// </summary>
        private void Forjar()
        {
            if (!AssetDatabase.IsValidFolder(PastaDosItens))
            {
                Debug.LogError("[Forja] Pasta '" + PastaDosItens + "' nao existe.");
                return;
            }

            string arquivo = "Item_" + _receita.Id;
            string caminho = PastaDosItens + "/" + arquivo + ".asset";

            if (System.IO.File.Exists(caminho) &&
                !EditorUtility.DisplayDialog("Item ja existe",
                    "'" + arquivo + "' ja existe em " + PastaDosItens + ".\n\nSobrescrever?",
                    "Sim, sobrescrever", "Cancelar"))
                return;

            var def = AssetDatabase.LoadAssetAtPath<ItemDef>(caminho);
            bool existia = def != null;

            if (!existia)
            {
                def = ScriptableObject.CreateInstance<ItemDef>();
                AssetDatabase.CreateAsset(def, caminho);
            }

            def.Id = _receita.Id;
            def.Nome = _receita.Nome;
            def.Icone = _icone;
            def.Tipo = _receita.Tipo;
            def.SlotEquipamento = _receita.Slot;
            def.EmpilhamentoMaximo = _receita.EmpilhamentoMaximo;
            def.Empunhadura = _receita.Empunhadura;
            def.Modificadores = new List<ModificadorFixo>(_receita.Modificadores);

            // A FAMILIA. Sem esta linha a Forja criava arma inerte: o ItemDef existia, o
            // jogador equipava, e o golpe nao causava dano nenhum -- o MaoFisicaBridge grita,
            // mas gritar depois de o asset existir e tarde.
            def.Base = _receita.Base;

            EditorUtility.SetDirty(def);
            AssetDatabase.SaveAssetIfDirty(def);
            AssetDatabase.Refresh();

            // O catalogo em runtime e um cache: sem recarregar, o item novo so apareceria no
            // proximo Play Mode.
            CatalogoDeAfixos.Recarregar();

            Debug.Log("[Forja] '" + _receita.Nome + "' " + (existia ? "atualizado" : "criado") +
                      " em " + caminho + ". Id: " + _receita.Id, def);

            EditorGUIUtility.PingObject(def);
        }

        // ── Artefatos ─────────────────────────────────────────────────────────

        private void DesenharSecaoArtefatos()
        {
            EditorGUILayout.LabelField("Artefatos", EditorStyles.boldLabel);

            var artefatos = ObterArtefatosBridge();
            using (new EditorGUI.DisabledScope(artefatos == null))
            {
                foreach (var (id, nome) in Artefatos)
                {
                    if (GUILayout.Button($"Conceder: {nome}"))
                        ConcederArtefato(artefatos, id, nome);
                }
            }

            if (artefatos == null)
                EditorGUILayout.HelpBox("Nenhum Player com ArtefatosBridge encontrado na cena.", MessageType.Warning);
        }

        private static ArtefatosBridge ObterArtefatosBridge()
        {
            var jogador = GameObject.FindGameObjectWithTag("Player");
            return jogador != null ? jogador.GetComponent<ArtefatosBridge>() : null;
        }

        private static void ConcederArtefato(ArtefatosBridge artefatos, string id, string nome)
        {
            if (artefatos.Inventario.Contem(id))
            {
                Debug.Log($"[CarcosaDebugger] '{nome}' já está equipado.");
                return;
            }

            int slot = artefatos.EquiparNoPrimeiroSlotLivre(id);
            Debug.Log(slot >= 0
                ? $"[CarcosaDebugger] '{nome}' concedido no slot {slot}."
                : $"[CarcosaDebugger] Falha ao conceder '{nome}' — sem slot livre ou id inválido.");
        }

        // ── Set de relíquias ──────────────────────────────────────────────────

        /// <summary>
        /// Concede um <b>set inteiro</b> de relíquias num clique — o que faz o Rei em Amarelo
        /// ser derrotável sem catar relíquia por relíquia.
        ///
        /// <para><b>Por que o botão do rito lê o Rei da cena:</b> a lista <see cref="Artefatos"/>
        /// aqui é conveniência de UI e pode divergir do que o rito realmente exige — o campo
        /// <c>idsDasReliquiasExigidas</c> é serializado por instância e pode ser editado no
        /// Inspector. Uma segunda cópia da lista que saísse de sincronia concederia o conjunto
        /// errado, e o rito simplesmente nunca fecharia, <b>sem erro nenhum aparecendo</b>. O
        /// Rei é a fonte da verdade; sem Rei em cena o botão se desabilita em vez de adivinhar.</para>
        ///
        /// <para><b>O que isto NÃO conserta:</b> o Anel do Sinal Amarelo é espólio garantido do
        /// Byakhee (<c>Drop_Byakhee</c>), mas o Byakhee não está em cena nenhuma — falta a arena
        /// dos Portões (roadmap, item 9). Enquanto ela não existir, este atalho é o único
        /// caminho até o rito completo. Ele destrava o teste; não substitui a arena.</para>
        /// </summary>
        private void DesenharSecaoSet()
        {
            EditorGUILayout.LabelField("Set de relíquias", EditorStyles.boldLabel);

            var artefatos = ObterArtefatosBridge();
            var rei = FindAnyObjectByType<ReiEmAmareloAI>();

            using (new EditorGUI.DisabledScope(artefatos == null || rei == null))
            {
                if (GUILayout.Button("Conceder o set do rito (lido do Rei em cena)"))
                    ConcederSet(artefatos, rei.ReliquiasExigidas, "rito do Rei");
            }

            using (new EditorGUI.DisabledScope(artefatos == null))
            {
                if (GUILayout.Button($"Conceder o Set Lendário ({Artefatos.Length}/{Artefatos.Length})"))
                    ConcederSet(artefatos, System.Array.ConvertAll(Artefatos, a => a.Id), "Set Lendário");
            }

            if (rei == null)
                EditorGUILayout.HelpBox(
                    "Nenhum Rei em Amarelo nesta cena — o botão do rito fica desabilitado em vez " +
                    "de adivinhar a lista. Use o Set Lendário, ou abra Castelo_Carcosa.",
                    MessageType.Info);

            if (artefatos != null && rei != null)
                EditorGUILayout.LabelField("Rito exigido",
                    DescreverProgresso(artefatos, rei.ReliquiasExigidas));
        }

        /// <summary>
        /// Concede cada id da lista, pulando o que já está portado. Relata em uma linha só —
        /// e avisa quando alguma relíquia <b>não coube</b>: os slots de porte são
        /// <see cref="InventarioDeArtefatos.TotalDeSlots"/>, e um set maior que isso deixaria
        /// relíquia dormente, com o ponto focal recusando a interação sem dizer por quê.
        /// </summary>
        private static void ConcederSet(ArtefatosBridge artefatos, IReadOnlyList<string> ids,
                                         string rotuloDoSet)
        {
            if (artefatos == null || ids == null || ids.Count == 0)
            {
                Debug.LogWarning($"[CarcosaDebugger] Set '{rotuloDoSet}' vazio — nada concedido.");
                return;
            }

            var concedidos = new List<string>();
            var jaTinha = new List<string>();
            var recusados = new List<string>();

            foreach (var id in ids)
            {
                if (artefatos.Inventario.Contem(id)) { jaTinha.Add(id); continue; }

                if (artefatos.EquiparNoPrimeiroSlotLivre(id) >= 0) concedidos.Add(id);
                else recusados.Add(id);
            }

            Debug.Log($"[CarcosaDebugger] Set '{rotuloDoSet}': " +
                      $"{concedidos.Count} concedida(s), {jaTinha.Count} já portada(s), " +
                      $"{recusados.Count} recusada(s).");

            if (recusados.Count > 0)
                Debug.LogWarning(
                    $"[CarcosaDebugger] Sem slot livre (ou id inválido) para: " +
                    $"{string.Join(", ", recusados)}. São {InventarioDeArtefatos.TotalDeSlots} " +
                    "slots de porte, e o ponto focal só aceita relíquia PORTADA — uma dormente " +
                    "faz o rito travar em silêncio.");
        }

        /// <summary>"2/3 — falta: anel_sinal_amarelo", para o rito ser lido de relance.</summary>
        private static string DescreverProgresso(ArtefatosBridge artefatos, IReadOnlyList<string> ids)
        {
            var faltando = new List<string>();
            foreach (var id in ids)
                if (!artefatos.Inventario.Contem(id)) faltando.Add(id);

            return faltando.Count == 0
                ? $"{ids.Count}/{ids.Count} — completo"
                : $"{ids.Count - faltando.Count}/{ids.Count} — falta: {string.Join(", ", faltando)}";
        }

        // ── Armas ────────────────────────────────────────────────────────────

        private void DesenharSecaoArmas()
        {
            EditorGUILayout.LabelField("Armas da Tumba", EditorStyles.boldLabel);

            var inv = InventoryManager.Instance;
            using (new EditorGUI.DisabledScope(inv == null))
            {
                foreach (var (id, nome) in Armas())
                {
                    if (GUILayout.Button($"Conceder e equipar: {nome}"))
                        ConcederArma(inv, id, nome);
                }
            }

            if (inv == null)
                EditorGUILayout.HelpBox("InventoryManager.Instance está nulo.", MessageType.Warning);
        }

        /// <summary>
        /// Mesma sequência do <c>BauDaTumba</c>: guarda na mochila, acha o slot onde caiu e
        /// equipa nele. Não existe atalho "equipar por id" no <c>InventoryManager</c>.
        /// </summary>
        private static void ConcederArma(InventoryManager inv, string id, string nome)
        {
            bool coube = inv.Main.Add(new ItemInstance(id, 1));
            if (!coube)
            {
                Debug.LogWarning($"[CarcosaDebugger] Mochila cheia — '{nome}' não coube.");
                return;
            }

            for (int i = 0; i < inv.Main.Capacidade; i++)
            {
                var slot = inv.Main.GetSlot(i);
                if (slot != null && slot.Def != null && slot.Def.Id == id)
                {
                    inv.Equipar(i);
                    Debug.Log($"[CarcosaDebugger] '{nome}' concedido e equipado.");
                    return;
                }
            }

            Debug.LogWarning($"[CarcosaDebugger] '{nome}' foi para a mochila mas não achei o slot para equipar.");
        }

        // ── Invocar chefes ───────────────────────────────────────────────────

        private void DesenharSecaoChefes()
        {
            EditorGUILayout.LabelField("Invocar chefe", EditorStyles.boldLabel);

            var jogador = GameObject.FindGameObjectWithTag("Player");
            using (new EditorGUI.DisabledScope(jogador == null))
            {
                if (GUILayout.Button("Invocar Byakhee"))
                    InvocarByakhee(jogador);

                if (GUILayout.Button("Invocar Rei em Amarelo"))
                    InvocarReiEmAmarelo(jogador);

                var rei = FindAnyObjectByType<ReiEmAmareloAI>();
                using (new EditorGUI.DisabledScope(rei == null))
                {
                    if (GUILayout.Button("Rei: iniciar ritual (libera os pontos focais)"))
                        rei.IniciarRitual();
                }
            }

            if (jogador == null)
                EditorGUILayout.HelpBox("Nenhum objeto com a tag Player na cena.", MessageType.Warning);
        }

        private const string PrefabByakheePath = "Assets/FavelaAmarela/Art/Enemies/Byakhee.prefab";

        private static void InvocarByakhee(GameObject jogador)
        {
            var posicao = jogador.transform.position + new Vector3(3f, 2f, 0f);

            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabByakheePath);
            if (prefab != null)
            {
                var instancia = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
                instancia.name = "Byakhee (Debug)";
                instancia.transform.position = posicao;

                // Sem isto a FSM fica em Espreita para sempre: o Byakhee não se move (os
                // estados é que dirigem o voo) e é INTOCÁVEL, porque PodeReceberDano só vale
                // em Pousado/Frenesi. O playtest de 2026-08-12 leu isso como dois bugs
                // separados ("não leva dano" e "não se move"); é um só. O método existe e está
                // documentado como "chamado pelo gatilho da arena" — mas a arena não tem
                // gatilho, e no jogo real quem chamaria são os Portões, que ainda não existem.
                instancia.GetComponent<ByakheeAI>()?.IniciarLuta();

                Debug.Log("[CarcosaDebugger] Byakhee invocado (prefab) e luta iniciada.");
                return;
            }

            // Sem o prefab (rode 'Tools/FavelaAmarela/Montar Prefab do Byakhee'): cai no
            // corpo mínimo construído em runtime, quadrado colorido no lugar da arte.
            var ficha = AssetDatabase.LoadAssetAtPath<FichaAtributosConfig>(FichaByakheePath);
            if (ficha == null)
            {
                Debug.LogError($"[CarcosaDebugger] Não achei a Ficha em '{FichaByakheePath}'.");
                return;
            }

            var go = CriarCorpoDoChefe("Byakhee (Debug)", posicao);

            // EnemyBase.Awake lê o campo privado `ficha` — precisa ser preenchido com o
            // objeto ainda inativo, senão Awake já rodou com ficha nula.
            var enemyBase = go.AddComponent<EnemyBase>();
            var so = new SerializedObject(enemyBase);
            so.FindProperty("ficha").objectReferenceValue = ficha;
            so.ApplyModifiedPropertiesWithoutUndo();

            go.AddComponent<ByakheeAI>();

            go.SetActive(true);
            Debug.Log("[CarcosaDebugger] Byakhee invocado (placeholder — sem prefab).");
        }

        private const string PrefabReiEmAmareloPath = "Assets/FavelaAmarela/Art/Enemies/ReiEmAmarelo.prefab";

        private static void InvocarReiEmAmarelo(GameObject jogador)
        {
            var posicao = jogador.transform.position + new Vector3(-3f, 2f, 0f);

            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabReiEmAmareloPath);
            if (prefab != null)
            {
                var instancia = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
                instancia.name = "Rei em Amarelo (Debug)";
                instancia.transform.position = posicao;
                Debug.Log("[CarcosaDebugger] Rei em Amarelo invocado (prefab). Use 'iniciar ritual' para começar.");
                return;
            }

            // Sem o prefab (rode 'Tools/FavelaAmarela/Montar Prefab do Rei em Amarelo'):
            // cai no corpo mínimo construído em runtime, sem EnemyBase/Vitalidade de
            // propósito — o design não prevê barra de vida para este confronto.
            var go = CriarCorpoDoChefe("Rei em Amarelo (Debug)", posicao);
            go.AddComponent<ReiEmAmareloAI>();
            go.SetActive(true);
            Debug.Log("[CarcosaDebugger] Rei em Amarelo invocado (placeholder — sem prefab). " +
                      "Use 'iniciar ritual' para começar.");
        }

        /// <summary>
        /// Corpo mínimo compartilhado pelos dois chefes: um quadrado colorido no lugar da
        /// arte (ainda não existe), na camada Enemy para o `MaoFisicaBridge` conseguir mirar
        /// nele via `Physics2D.OverlapCircle`. Começa inativo — quem chama termina de montar
        /// os componentes específicos antes de <c>SetActive(true)</c>, para nenhum `Awake`
        /// disparar cedo demais com dependência ainda não configurada.
        /// </summary>
        private static GameObject CriarCorpoDoChefe(string nome, Vector3 posicao)
        {
            var go = new GameObject(nome);
            go.SetActive(false);
            go.transform.position = posicao;
            go.layer = LayerMask.NameToLayer("Enemy");

            var sprite = go.AddComponent<SpriteRenderer>();
            sprite.sprite = CriarSpritePlaceholder();

            var colisor = go.AddComponent<CircleCollider2D>();
            colisor.radius = 0.5f;

            return go;
        }

        private static Sprite _spritePlaceholder;

        /// <summary>Quadrado branco 32×32 (PPU 32, convenção do projeto) — tingido por
        /// cada AI conforme o estado da FSM. Gerado uma vez e reaproveitado.</summary>
        private static Sprite CriarSpritePlaceholder()
        {
            if (_spritePlaceholder != null) return _spritePlaceholder;

            var tex = new Texture2D(32, 32) { filterMode = FilterMode.Point };
            var pixels = new Color[32 * 32];
            for (int i = 0; i < pixels.Length; i++) pixels[i] = Color.white;
            tex.SetPixels(pixels);
            tex.Apply();

            _spritePlaceholder = Sprite.Create(tex, new Rect(0, 0, 32, 32), new Vector2(0.5f, 0.5f), 32f);
            return _spritePlaceholder;
        }

        // ── Estado ao vivo ───────────────────────────────────────────────────

        private void DesenharEstadoAoVivo()
        {
            EditorGUILayout.LabelField("Estado ao vivo", EditorStyles.boldLabel);

            var byakhee = FindAnyObjectByType<ByakheeAI>();
            if (byakhee != null)
            {
                EditorGUILayout.LabelField("Byakhee", $"Estado: {byakhee.Fsm.CurrentState}  |  " +
                    $"Pode receber dano: {byakhee.Fsm.PodeReceberDano}  |  " +
                    $"Dreno/s: {byakhee.Fsm.DrenoDeResilienciaPorSegundo:0.0}");
            }

            var rei = FindAnyObjectByType<ReiEmAmareloAI>();
            if (rei != null)
            {
                EditorGUILayout.LabelField("Rei em Amarelo",
                    $"Estado: {rei.Fsm.CurrentState}  |  " +
                    $"Relíquias: {rei.Fsm.ReliquiasAtivas}/{rei.Fsm.TotalDeReliquiasExigidas}  |  " +
                    $"Ciclos: {rei.Fsm.CiclosSobrevividos}/{rei.Fsm.TotalDeCiclos}");
            }

            if (byakhee == null && rei == null)
                EditorGUILayout.HelpBox("Nenhum chefe na cena ainda.", MessageType.None);
        }
    }
}
