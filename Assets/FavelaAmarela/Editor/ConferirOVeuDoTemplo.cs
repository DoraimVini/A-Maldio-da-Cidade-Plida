using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using FavelaAmarela.Runtime.Environment;
using FavelaAmarela.Runtime.Itens;

namespace FavelaAmarela.EditorTools
{
    /// <summary>
    /// Mede se o véu da tempestade <b>realmente veda</b> o Templo do Povo Serpente.
    ///
    /// <para><b>Por que isto existe (2026-09-01).</b> A primeira versão do véu era uma caixa
    /// 14×26 centrada a oeste da entrada. Parecia certo no Inspector, e a ferramenta relatou
    /// sucesso. Medido: sobravam <b>29 unidades</b> de corredor livre por baixo e <b>7</b> por
    /// cima — o jogador contornava sem nunca tocar o gatilho. É a mesma falha do chão do
    /// Deserto: um número autorado à mão sobre um mapa que mudou de tamanho.</para>
    ///
    /// <para>Um portão que se pode contornar é <b>pior</b> que nenhum: promete uma regra ao
    /// jogador e não a cumpre, e o único a descobrir é quem estiver jogando.</para>
    ///
    /// <para>Confere quatro coisas: (1) a entrada do Templo está dentro do véu; (2) não sobra
    /// passagem por cima nem por baixo; (3) nada de conteúdo colecionável ficou preso atrás
    /// dele sem necessidade; (4) a Carta das Areias está FORA — a chave dentro da fechadura
    /// seria inalcançável.</para>
    /// </summary>
    public static class ConferirOVeuDoTemplo
    {
        private const string Marcador = "[ConfereVeu]";
        private const string Cena = "Assets/Scenes/Deserto_Hali.unity";

        [MenuItem("Tools/FavelaAmarela/Deserto: conferir o véu do Templo")]
        public static void Executar()
        {
            var cena = EditorSceneManager.OpenScene(Cena, OpenSceneMode.Single);
            var raizes = cena.GetRootGameObjects();
            var tudo = raizes.SelectMany(r => r.GetComponentsInChildren<Transform>(true)).ToArray();

            var veu = raizes.SelectMany(r => r.GetComponentsInChildren<VeuDaTempestade>(true))
                            .FirstOrDefault();

            if (veu == null) { Debug.LogError($"{Marcador} Nenhum VeuDaTempestade na cena."); return; }

            var col = veu.GetComponent<BoxCollider2D>();
            if (col == null) { Debug.LogError($"{Marcador} Véu sem BoxCollider2D."); return; }

            var caixa = new Bounds(col.bounds.center, col.bounds.size);

            var entrada = tudo.FirstOrDefault(t => t.name.Contains("TemploSerpente") &&
                                                   t.name.StartsWith("Entrada"));
            if (entrada == null) { Debug.LogError($"{Marcador} Entrada do Templo ausente."); return; }

            var limites = tudo.Where(t => t.name.StartsWith("Limite_")).ToArray();
            float mapaX = limites.Max(t => Mathf.Abs(t.position.x));
            float mapaY = limites.Max(t => Mathf.Abs(t.position.y));

            // ── (1) a entrada está dentro? ────────────────────────────────────
            bool entradaCoberta = entrada.position.x >= caixa.min.x && entrada.position.x <= caixa.max.x;

            // ── (2) sobra passagem? ───────────────────────────────────────────
            float folgaPorBaixo = caixa.min.y - (-mapaY);
            float folgaPorCima = mapaY - caixa.max.y;
            float folgaPorLeste = mapaX - caixa.max.x;

            // ── (3) o que ficou preso atrás? ──────────────────────────────────
            var presos = raizes.SelectMany(r => r.GetComponentsInChildren<ColetavelDeItem>(true))
                               .Where(c => c.transform.position.x >= caixa.min.x)
                               .Select(c => $"{c.name} em ({c.transform.position.x:0.0}, " +
                                            $"{c.transform.position.y:0.0})")
                               .ToArray();

            // ── (4) a carta está fora? ────────────────────────────────────────
            var carta = raizes.SelectMany(r => r.GetComponentsInChildren<ColetavelDeItem>(true))
                              .FirstOrDefault(c => c.name.Contains("CartaDasAreias"));

            bool cartaForaDoVeu = carta != null && carta.transform.position.x < caixa.min.x;

            string quebra = System.Environment.NewLine + "  ";
            var texto = $"{Marcador} Véu '{veu.name}'" + quebra +
                        $"faixa vedada : x {caixa.min.x:0.0}..{caixa.max.x:0.0}  " +
                        $"y {caixa.min.y:0.0}..{caixa.max.y:0.0}" + quebra +
                        $"mapa         : x ±{mapaX:0}  y ±{mapaY:0}" + quebra +
                        $"entrada      : ({entrada.position.x:0.0}, {entrada.position.y:0.0}) " +
                        $"-> {(entradaCoberta ? "DENTRO do véu" : "FORA — não protegida!")}" + quebra +
                        $"folga baixo  : {folgaPorBaixo:0.0}" + quebra +
                        $"folga cima   : {folgaPorCima:0.0}" + quebra +
                        $"folga leste  : {folgaPorLeste:0.0}" + quebra +
                        $"carta        : {(carta == null ? "AUSENTE" : cartaForaDoVeu ? "fora do véu, alcançável" : "DENTRO do véu — inalcançável!")}" + quebra +
                        $"presos atrás : {(presos.Length == 0 ? "nenhum" : string.Join("; ", presos))}";

            bool vedado = entradaCoberta && folgaPorBaixo <= 0f && folgaPorCima <= 0f &&
                          folgaPorLeste <= 0f && cartaForaDoVeu;

            if (vedado) Debug.Log(texto + quebra + "VEREDITO: VEDADO — não há como contornar.");
            else Debug.LogError(texto + quebra + "VEREDITO: CONTORNÁVEL — o véu não cumpre a regra.");
        }
    }
}
