using System;
using System.Collections.Generic;

namespace FavelaAmarela.Core.Persistencia
{
    /// <summary>
    /// Uma entrada do arquivo de save: a <b>chave de persistência</b> e o estado serializado
    /// do que ela identifica.
    /// </summary>
    [Serializable]
    public sealed class EntradaDeSave
    {
        /// <summary>Chave de persistência — o "RG" imutável do objeto ou da flag.</summary>
        public string Chave;

        /// <summary>Estado serializado (JSON ou valor simples), opaco para o registro.</summary>
        public string Valor;

        /// <summary>Construtor sem argumentos exigido pela serialização.</summary>
        public EntradaDeSave() { }

        /// <summary>Cria uma entrada já preenchida.</summary>
        public EntradaDeSave(string chave, string valor)
        {
            Chave = chave;
            Valor = valor;
        }
    }

    /// <summary>
    /// Formato <b>em disco</b> do save. É uma lista, e não um dicionário, porque os
    /// serializadores da Unity (<c>JsonUtility</c>) não sabem serializar
    /// <c>Dictionary</c> — a conversão de/para dicionário acontece no
    /// <see cref="RegistroDeSave"/>, que é quem o jogo consulta em memória.
    ///
    /// <para>Classe POCO <c>[Serializable]</c> conforme a Regra de Ouro 9 do
    /// <c>CLAUDE.md</c>: <b>JSON, nunca <c>PlayerPrefs</c></b> para dados de progresso.</para>
    /// </summary>
    [Serializable]
    public sealed class EstadoDeSave
    {
        /// <summary>Versão do formato — permite migrar saves antigos no futuro.</summary>
        public int Versao = 1;

        /// <summary>Todas as entradas salvas.</summary>
        public List<EntradaDeSave> Entradas = new List<EntradaDeSave>();
    }
}
