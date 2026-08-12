using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.Serialization;
using FavelaAmarela.Core.Combat;

namespace FavelaAmarela.Tests.EditMode
{
    /// <summary>
    /// Guarda uma <b>classe de bug</b>, não um caso: <c>ScriptableObject</c> cujo campo foi
    /// renomeado sem <see cref="FormerlySerializedAs"/> passa a ignorar em silêncio o valor do
    /// disco e cai no default da classe. Nada aparece no console, e o Inspector continua
    /// mostrando o número certo — porque o Inspector lê o objeto, não o arquivo.
    ///
    /// <para><b>O caso que motivou (2026-08-12):</b> os campos de
    /// <see cref="FichaAtributosConfig"/> viraram <c>PascalCase</c> enquanto todos os
    /// <c>.asset</c> gravavam em <c>camelCase</c>. Por um período indeterminado, <b>nenhuma
    /// ficha do projeto carregou seus valores</b>. O <c>Ficha_Byakhee</c> lutava com 100 de
    /// Vitalidade em vez dos 500 autorados e 0 de Resistência Anômala em vez de 12 — um quinto
    /// do chefe projetado. Todo balanceamento por playtest daquele período foi feito contra
    /// números que não eram os das fichas.</para>
    ///
    /// <para>A estratégia é a mesma resolução que a Unity faz: para cada chave escalar gravada
    /// no arquivo, exigir que exista um campo serializado que a aceite — pelo nome exato ou por
    /// um <see cref="FormerlySerializedAs"/> — e que o valor carregado bata com o do disco.
    /// Varre por tipo, então <b>ficha nova entra no teste sozinha</b>, sem editar este arquivo.</para>
    /// </summary>
    public sealed class FichaAtributosAssetsTests
    {
        // Chaves do próprio serializador da Unity, não do nosso tipo.
        private static readonly HashSet<string> ChavesDaEngine = new HashSet<string>
        {
            "serializedVersion"
        };

        private static readonly Regex Escalar =
            new Regex(@"^  ([A-Za-z_][A-Za-z0-9_]*): (-?(?:\d+\.?\d*|\.\d+))$");

        private static List<string> CaminhosDasFichas()
        {
            var caminhos = new List<string>();
            foreach (var guid in AssetDatabase.FindAssets("t:FichaAtributosConfig"))
                caminhos.Add(AssetDatabase.GUIDToAssetPath(guid));

            return caminhos;
        }

        [Test]
        public void ExistemFichasParaGuardar()
        {
            // Se a varredura voltar vazia, os outros testes passariam sem verificar nada —
            // um guarda que não guarda é pior que nenhum.
            Assert.IsNotEmpty(CaminhosDasFichas(),
                "Nenhum FichaAtributosConfig encontrado. O teste ficaria verde sem checar nada.");
        }

        [Test]
        public void TodaChaveGravadaNoAssetChegaAoObjetoCarregado()
        {
            var problemas = new StringBuilder();

            foreach (string caminho in CaminhosDasFichas())
            {
                var def = AssetDatabase.LoadAssetAtPath<FichaAtributosConfig>(caminho);
                if (def == null)
                {
                    problemas.AppendLine($"- {caminho}: não carregou como FichaAtributosConfig.");
                    continue;
                }

                foreach (var par in LerEscalares(caminho))
                {
                    var campo = ResolverCampo(par.Key);

                    if (campo == null)
                    {
                        problemas.AppendLine(
                            $"- {Path.GetFileName(caminho)}: a chave '{par.Key}' não casa com nenhum " +
                            "campo serializado. O valor do disco está sendo descartado em silêncio. " +
                            "Se o campo foi renomeado, acrescente [FormerlySerializedAs(\"" + par.Key + "\")].");
                        continue;
                    }

                    float carregado = System.Convert.ToSingle(campo.GetValue(def));
                    if (Proximo(carregado, par.Value)) continue;

                    problemas.AppendLine(
                        $"- {Path.GetFileName(caminho)}: '{par.Key}' vale {par.Value} no arquivo mas " +
                        $"{carregado} no objeto (campo '{campo.Name}').");
                }
            }

            Assert.IsEmpty(problemas.ToString(),
                "Ficha(s) perdendo valor autorado na desserialização:\n" + problemas);
        }

        /// <summary>
        /// Trava o caso concreto que passou meses errado. Números de
        /// <c>systems/ficha_de_atributos.md</c>; se o design mudá-los, atualize os dois juntos.
        /// </summary>
        [Test]
        public void Byakhee_CarregaOsNumerosDoChefeProjetado()
        {
            var def = AssetDatabase.LoadAssetAtPath<FichaAtributosConfig>(
                "Assets/FavelaAmarela/Config/Ficha_Byakhee.asset");

            Assert.IsNotNull(def, "Ficha_Byakhee não encontrada.");

            Assert.AreEqual(500f, def.VitalidadeMax, 1e-4f,
                "O Byakhee já lutou com 100 de Vitalidade por causa do bug de serialização.");
            Assert.AreEqual(12f, def.ResistenciaAnomala, 1e-4f,
                "Sem Resistência Anômala, o canal de Trauma de Anomalia não tem o que mitigar.");
            Assert.AreEqual(120f, def.ResilienciaMax, 1e-4f,
                "Criatura de Carcosa precisa de mente a ferir — é o segundo vetor de derrota.");
        }

        /// <summary>
        /// Nenhuma ficha autorada deveria ser idêntica aos defaults da classe em todos os
        /// campos de combate. Se for, é o sintoma exato do bug: o arquivo existe, tem valores,
        /// e o objeto veio em branco.
        /// </summary>
        [Test]
        public void NenhumaFichaEhIdenticaAosDefaultsDaClasse()
        {
            var padrao = CriarFichaPadrao();
            var suspeitas = new List<string>();

            foreach (string caminho in CaminhosDasFichas())
            {
                var def = AssetDatabase.LoadAssetAtPath<FichaAtributosConfig>(caminho);
                if (def == null) continue;

                bool igualEmTudo =
                    Proximo(def.VitalidadeMax, padrao.VitalidadeMax) &&
                    Proximo(def.Ataque, padrao.Ataque) &&
                    Proximo(def.Defesa, padrao.Defesa) &&
                    Proximo(def.Conjuracao, padrao.Conjuracao) &&
                    Proximo(def.ResistenciaAnomala, padrao.ResistenciaAnomala);

                if (igualEmTudo) suspeitas.Add(Path.GetFileName(caminho));
            }

            Object.DestroyImmediate(padrao);

            Assert.IsEmpty(suspeitas,
                "Ficha(s) idênticas aos defaults da classe em todos os atributos de combate — " +
                "sintoma de desserialização silenciosamente descartada: " + string.Join(", ", suspeitas));
        }

        // ── Apoio ────────────────────────────────────────────────────────────

        /// <summary>Ficha em branco, só com os defaults escritos na classe.</summary>
        private static FichaAtributosConfig CriarFichaPadrao()
            => ScriptableObject.CreateInstance<FichaAtributosConfig>();

        private static bool Proximo(float a, float b) => System.Math.Abs(a - b) < 1e-4f;

        /// <summary>
        /// Pares chave→valor numérico gravados no YAML do asset, ignorando as chaves do
        /// serializador da Unity (<c>m_*</c> e <c>serializedVersion</c>).
        /// </summary>
        private static List<KeyValuePair<string, float>> LerEscalares(string caminho)
        {
            var pares = new List<KeyValuePair<string, float>>();

            foreach (string linha in File.ReadAllLines(caminho))
            {
                var m = Escalar.Match(linha);
                if (!m.Success) continue;

                string chave = m.Groups[1].Value;
                if (chave.StartsWith("m_") || ChavesDaEngine.Contains(chave)) continue;

                if (float.TryParse(m.Groups[2].Value, NumberStyles.Float,
                        CultureInfo.InvariantCulture, out float valor))
                {
                    pares.Add(new KeyValuePair<string, float>(chave, valor));
                }
            }

            return pares;
        }

        /// <summary>
        /// Mesma resolução que a Unity faz: nome exato do campo, ou qualquer campo cujo
        /// <see cref="FormerlySerializedAs"/> aponte para este nome antigo.
        /// </summary>
        private static FieldInfo ResolverCampo(string chaveDoArquivo)
        {
            var campos = typeof(FichaAtributosConfig)
                .GetFields(BindingFlags.Public | BindingFlags.Instance);

            foreach (var campo in campos)
                if (campo.Name == chaveDoArquivo) return campo;

            foreach (var campo in campos)
                foreach (var attr in campo.GetCustomAttributes<FormerlySerializedAsAttribute>())
                    if (attr.oldName == chaveDoArquivo) return campo;

            return null;
        }
    }
}
