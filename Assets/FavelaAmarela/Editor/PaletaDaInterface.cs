using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace FavelaAmarela.EditorTools
{
    /// <summary>
    /// Fonte única de verdade da aparência da interface: tipografia, cores e as molduras.
    ///
    /// <para><b>Por que existe (2026-08-19):</b> a fonte estava chumbada em <b>12 ferramentas</b>
    /// de montagem, cada uma chamando <c>Resources.GetBuiltinResource&lt;Font&gt;("LegacyRuntime.ttf")</c>
    /// por conta própria. Trocar a tipografia exigia 12 edições — e bastava esquecer uma para uma
    /// tela nascer diferente das outras, em silêncio.</para>
    ///
    /// <para><b>Por que Dark Ages UI e não o pacote da Kenney.</b> Cheguei a montar isto sobre o
    /// <c>ui-pack-rpg-expansion</c>, mas ele é cartoon RPG — bege-madeira, cantos arredondados,
    /// sombra suave — e precisaria ser tingido para não parecer outro jogo. O pacote da Hypnobius
    /// já <b>é</b> Carcosa: ouro ornamentado sobre quase-preto, aristocracia decadente. A paleta
    /// declarada por ele (<i>Honey Gold</i> <c>#DCC47C</c>, <i>Charcoal Brown</i> <c>#2E322A</c>)
    /// é o amarelo doentio sobre pedra que o jogo pede. Aqui a arte entra <b>na cor original</b>,
    /// sem tingimento.</para>
    ///
    /// <para><b>Licença:</b> Dark Ages UI v1.0, por Hypnobius (hypnobius.itch.io) — uso comercial
    /// e modificação permitidos, crédito opcional; proibido revender ou redistribuir o pacote.
    /// Os termos completos estão em <c>Assets/ThirdParty/DarkAgesUI/DarkAgesUi_v1.0/LICENSE.txt</c>.</para>
    /// </summary>
    public static class PaletaDaInterface
    {
        // ── Tipografia ───────────────────────────────────────────────────────

        private const string CaminhoDaFonte =
            "Assets/ThirdParty/Kenney/kenney_kenney-fonts/Fonts/Kenney Pixel.ttf";

        private const string CaminhoDaFonteMono =
            "Assets/ThirdParty/Kenney/kenney_kenney-fonts/Fonts/Kenney Mini Square.ttf";

        private static Font _fonte;
        private static Font _fonteMono;

        /// <summary>
        /// Fonte de texto corrido. <c>Kenney Pixel</c> combina com PPU 32; a built-in da Unity é
        /// vetorial e borra em corpo pequeno.
        /// </summary>
        public static Font Fonte => _fonte != null ? _fonte : (_fonte = Carregar(CaminhoDaFonte));

        /// <summary>Fonte para números e rótulos curtos, onde legibilidade importa mais que caráter.</summary>
        public static Font FonteMono =>
            _fonteMono != null ? _fonteMono : (_fonteMono = Carregar(CaminhoDaFonteMono));

        // ── Cores (as do próprio pacote, medidas na arte) ────────────────────

        /// <summary>Honey Gold — texto de destaque e o dourado do Sinal.</summary>
        public static readonly Color Ouro = Hex(0xDC, 0xC4, 0x7C);

        /// <summary>Camel — dourado mais fechado, para borda e rótulo.</summary>
        public static readonly Color OuroFechado = Hex(0xC1, 0x91, 0x49);

        /// <summary>Tan — texto secundário.</summary>
        public static readonly Color TintaFraca = Hex(0x9F, 0x88, 0x73);

        /// <summary>Charcoal Brown — o interior das molduras. É a cor do miolo do painel.</summary>
        public static readonly Color Pedra = Hex(0x2E, 0x32, 0x2A);

        /// <summary>Dark Blue — usado nas tramas da moldura.</summary>
        public static readonly Color AzulEscuro = Hex(0x15, 0x20, 0x2F);

        /// <summary>Texto corrido: pergaminho envelhecido, nunca branco puro.</summary>
        public static readonly Color Tinta = Hex(0xED, 0xE4, 0xC8);

        // ── Molduras (recortes do tilesheet 384×352) ─────────────────────────

        /// <summary>Caminho do tilesheet. Um PNG só, fatiado em sprites nomeados.</summary>
        public const string CaminhoDoTilesheet =
            "Assets/ThirdParty/DarkAgesUI/DarkAgesUi_v1.0/32x32-Tilesheet.png";

        /// <summary>Nome do sprite da moldura ornamentada, dentro do tilesheet.</summary>
        public const string SpritePainel = "painel_ornado";

        /// <summary>Nome do sprite do painel de pergaminho.</summary>
        public const string SpritePergaminho = "painel_pergaminho";

        /// <summary>Moldura pequena e discreta, para slot de item e de Artefato.</summary>
        public const string SpriteSlot = "moldura_slot";

        /// <summary>
        /// Borda 9-slice da moldura ornamentada, em pixels. <b>Medida na arte</b>, não estimada:
        /// a trama lateral tem 11px, mas o ornamento de canto vai até 23px — fatiar em 11
        /// esticaria a espiral do canto.
        /// </summary>
        public const int BordaDoPainel = 23;

        /// <summary>Retângulo do painel ornamentado no tilesheet (origem no canto inferior-esquerdo, como a Unity espera).</summary>
        public static readonly Rect RectPainel = new Rect(0, 256, 96, 96);

        /// <summary>Retângulo do painel de pergaminho.</summary>
        public static readonly Rect RectPergaminho = new Rect(96, 256, 96, 96);

        /// <summary>
        /// Retângulo da moldura de slot. No tilesheet ela está em (208, 16) contando de cima;
        /// a Unity conta de baixo, daí <c>352 − 16 − 64 = 272</c>.
        /// </summary>
        public static readonly Rect RectSlot = new Rect(208, 272, 64, 64);

        /// <summary>
        /// Borda 9-slice da moldura de slot. Menor que a do painel: a trama é fina e não tem
        /// espiral de canto.
        /// </summary>
        public const int BordaDoSlot = 10;

        private static Sprite _painel;
        private static Sprite _pergaminho;
        private static Sprite _slot;

        /// <summary>Moldura ornamentada, ou <c>null</c> se o tilesheet não estiver fatiado ainda.</summary>
        public static Sprite Painel =>
            _painel != null ? _painel : (_painel = CarregarSprite(SpritePainel));

        /// <summary>Painel de pergaminho, para texto longo (diálogo, recital).</summary>
        public static Sprite Pergaminho =>
            _pergaminho != null ? _pergaminho : (_pergaminho = CarregarSprite(SpritePergaminho));

        /// <summary>Moldura de slot, para as casas do inventário e da barra de Artefatos.</summary>
        public static Sprite Slot =>
            _slot != null ? _slot : (_slot = CarregarSprite(SpriteSlot));

        /// <summary>Aplica a moldura de slot. O ícone do item fica num filho, por cima.</summary>
        public static void AplicarSlot(Image alvo)
        {
            if (alvo == null) return;

            if (Slot == null) { alvo.color = Pedra; return; }

            alvo.sprite = Slot;
            alvo.type = Image.Type.Sliced;
            alvo.color = Color.white;
        }

        /// <summary>
        /// Aplica a moldura a um <see cref="Image"/>, em <c>Sliced</c>.
        ///
        /// <para><b>Sem tingimento</b>, ao contrário da primeira versão desta classe: a arte já
        /// vem na paleta certa. Tingir por cima só sujaria o dourado.</para>
        ///
        /// <para>Se o sprite não existir, deixa o retângulo com a cor <see cref="Pedra"/> — degrada
        /// para "mais simples", nunca para "invisível".</para>
        /// </summary>
        public static void AplicarPainel(Image alvo, bool pergaminho = false)
        {
            if (alvo == null) return;

            var sprite = pergaminho ? Pergaminho : Painel;

            if (sprite == null)
            {
                alvo.color = Pedra;
                return;
            }

            alvo.sprite = sprite;
            alvo.type = Image.Type.Sliced;
            alvo.color = Color.white; // a arte manda na cor
        }

        // ── Apoio ────────────────────────────────────────────────────────────

        private static Color Hex(int r, int g, int b) => new Color(r / 255f, g / 255f, b / 255f);

        private static Font Carregar(string caminho)
        {
            var f = AssetDatabase.LoadAssetAtPath<Font>(caminho);
            if (f != null) return f;

            Debug.LogWarning($"[PaletaDaInterface] Fonte não encontrada em '{caminho}'. " +
                             "Usando a built-in — a interface vai destoar.");

            try { return Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf"); }
            catch { return null; }
        }

        /// <summary>Busca um sprite nomeado dentro do tilesheet fatiado.</summary>
        private static Sprite CarregarSprite(string nome)
        {
            foreach (var asset in AssetDatabase.LoadAllAssetsAtPath(CaminhoDoTilesheet))
            {
                if (asset is Sprite s && s.name == nome) return s;
            }

            Debug.LogWarning($"[PaletaDaInterface] Sprite '{nome}' não achado no tilesheet. " +
                             "Rode 'Tools/FavelaAmarela/Aplicar cara da interface' — ele fatia o " +
                             "PNG antes de usar.");
            return null;
        }
    }
}
