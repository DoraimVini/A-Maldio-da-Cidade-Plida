using System;
using System.IO;
using UnityEngine;
using FavelaAmarela.Core.Persistence;

namespace FavelaAmarela.Runtime.GameLoop
{
    /// <summary>
    /// Adaptador Runtime de persistência: serializa/deserializa <see cref="SaveData"/>
    /// em JSON no disco, sob <see cref="Application.persistentDataPath"/>. Nunca usa
    /// <c>PlayerPrefs</c> para dados de progresso (regra §9 do CLAUDE.md).
    ///
    /// Slot único por enquanto (<c>save.json</c>) — múltiplos slots é uma decisão de
    /// design de um slice futuro. Toda IO é defensiva: falha logga <c>Debug.LogError</c>
    /// e devolve um resultado seguro em vez de estourar exceção (regra §7).
    /// </summary>
    public static class SaveSystem
    {
        private const string NomeArquivo = "save.json";

        private static string Caminho => Path.Combine(Application.persistentDataPath, NomeArquivo);

        /// <summary>Verdadeiro se existe um arquivo de save no disco.</summary>
        public static bool ExisteSave() => File.Exists(Caminho);

        /// <summary>Serializa e grava o estado no disco (sobrescreve o slot único).</summary>
        public static void Salvar(SaveData dados)
        {
            if (dados == null)
            {
                Debug.LogError("[SaveSystem] SaveData nulo; nada foi salvo.");
                return;
            }

            try
            {
                string json = JsonUtility.ToJson(dados, prettyPrint: true);
                File.WriteAllText(Caminho, json);
            }
            catch (Exception e)
            {
                Debug.LogError($"[SaveSystem] Falha ao salvar em '{Caminho}': {e.Message}");
            }
        }

        /// <summary>
        /// Lê e deserializa o save. Devolve <c>null</c> se não existir arquivo ou se a
        /// leitura/parse falhar (nunca estoura para o chamador).
        /// </summary>
        public static SaveData Carregar()
        {
            if (!ExisteSave()) return null;

            try
            {
                string json = File.ReadAllText(Caminho);
                return JsonUtility.FromJson<SaveData>(json);
            }
            catch (Exception e)
            {
                Debug.LogError($"[SaveSystem] Falha ao carregar de '{Caminho}': {e.Message}");
                return null;
            }
        }

        /// <summary>Apaga o save do disco, se existir (ex.: novo jogo, colapso final).</summary>
        public static void Apagar()
        {
            try
            {
                if (ExisteSave()) File.Delete(Caminho);
            }
            catch (Exception e)
            {
                Debug.LogError($"[SaveSystem] Falha ao apagar '{Caminho}': {e.Message}");
            }
        }
    }
}
