// Assets/Scripts/Inventario/ItemInstance.cs
using System;
using System.Collections.Generic;
using UnityEngine;
using FavelaAmarela.Core.Loot;

namespace FavelaAmarela.Inventario
{
    /// <summary>
    /// Representa uma instância concreta de um item no inventário.
    /// Guarda apenas a referência ao ItemDef (via GUID) e a quantidade atual.
    /// Todos os modificadores são obtidos do ItemDef, não há aleatoriedade.
    /// </summary>
    [Serializable]
    public class ItemInstance
    {
        public string ItemDefId;
        public int Quantidade;

        /// <summary>
        /// Quanto de Carcosa entrou NESTE exemplar. Decide quantos afixos ele carrega
        /// (ver <see cref="RegrasDeGrau"/>).
        /// </summary>
        public GrauDeImpregnacao Grau = GrauDeImpregnacao.Inerte;

        /// <summary>
        /// Nível <b>do item</b>, derivado da fonte do drop. Governa que afixos podiam cair.
        /// Não confundir com o nível do jogador: comparar com ele faria uma zona inicial
        /// dropar tier máximo assim que o jogador subisse.
        /// </summary>
        public int NivelDoItem = 1;

        /// <summary>
        /// Os afixos que ESTE exemplar recebeu, com os valores já rolados. É o que faz duas
        /// cópias da mesma base deixarem de ser idênticas — e o que dá sentido a olhar um
        /// drop e perguntar "essa é melhor que a minha?".
        /// </summary>
        public List<AfixoRolado> Afixos = new List<AfixoRolado>();

        public ItemInstance(string itemDefId, int quantidade = 1)
        {
            ItemDefId = itemDefId;
            Quantidade = Math.Max(1, quantidade);
        }

        /// <summary>
        /// Acesso conveniente à definição do item via singleton.
        /// </summary>
        public ItemDef Def
        {
            get
            {
                if (ItemDatabase.Instance == null)
                {
                    Debug.LogError("[ItemInstance] ItemDatabase.Instance é null. Certifique-se de que o prefab está na cena.");
                    return null;
                }
                return ItemDatabase.Instance.Get(ItemDefId);
            }
        }

        /// <summary>
        /// Resolve o ItemDef usando um database específico (para injeção de dependência).
        /// </summary>
        public ItemDef GetDef(ItemDatabase database)
        {
            return database != null ? database.Get(ItemDefId) : null;
        }

        /// <summary>
        /// Cria uma cópia profunda (útil para transferências entre inventários).
        ///
        /// <para>Os afixos são <b>copiados</b>, não compartilhados: dois exemplares que
        /// dividissem a mesma lista mudariam juntos ao primeiro que fosse alterado.</para>
        /// </summary>
        public ItemInstance Clone()
        {
            var copia = new ItemInstance(ItemDefId, Quantidade)
            {
                Grau = Grau,
                NivelDoItem = NivelDoItem,
            };

            if (Afixos != null)
                foreach (var a in Afixos)
                    if (a != null) copia.Afixos.Add(new AfixoRolado(a.AfixoId, a.Stat, a.Valor));

            return copia;
        }

        /// <summary>
        /// O nome que o jogador lê: <b>prefixos + nome da base + sufixos</b>.
        ///
        /// <para>Ex.: <i>"Cravado Estilete de Irem do Sinal"</i>. É a convenção de D2 e PoE, e
        /// existe por um motivo funcional, não estético: o nome é a <b>primeira</b> informação
        /// sobre o item, e muitas vezes a única que o jogador lê antes de decidir se pega. Sem
        /// isso, dois exemplares com rolagens completamente diferentes aparecem idênticos.</para>
        ///
        /// <para>Um afixo cujo <c>AfixoDef</c> sumiu do projeto perde o rótulo mas <b>mantém o
        /// efeito</b> — o valor está gravado no save, e tirá-lo puniria o jogador por uma
        /// decisão de autoria.</para>
        /// </summary>
        public string NomeExibido()
        {
            string nomeBase = Def != null ? Def.Nome : ItemDefId;

            if (Afixos == null || Afixos.Count == 0) return nomeBase;

            string prefixos = "";
            string sufixos = "";

            foreach (var a in Afixos)
            {
                if (a == null) continue;

                var def = CatalogoDeAfixos.PorId(a.AfixoId);
                if (def == null || string.IsNullOrWhiteSpace(def.Rotulo)) continue;

                if (def.Tipo == TipoDeAfixo.Prefixo) prefixos += def.Rotulo + " ";
                else sufixos += " " + def.Rotulo;
            }

            return (prefixos + nomeBase + sufixos).Trim();
        }

        /// <summary>
        /// As linhas de modificador, para tooltip e ficha — <c>"+13 Vitalidade"</c>.
        ///
        /// <para>Sem elas, um sistema de afixos <b>piora</b> o jogo: o jogador acumula itens
        /// que não consegue comparar, e a mochila de 12 posições vira um problema sem virar uma
        /// decisão.</para>
        /// </summary>
        public IReadOnlyList<string> LinhasDeAfixo()
        {
            var linhas = new List<string>();
            if (Afixos == null) return linhas;

            foreach (var a in Afixos)
            {
                if (a == null) continue;

                string sinal = a.Valor >= 0f ? "+" : "";
                linhas.Add($"{sinal}{a.Valor:0.##} {NomesDeAtributo.De(a.Stat)}");
            }

            return linhas;
        }


        /// <summary>
        /// Todos os modificadores que este exemplar concede: os <b>implícitos</b> da base
        /// (autorados no <c>ItemDef</c>) mais os <b>afixos rolados</b> desta instância.
        ///
        /// <para><b>É por aqui que o sistema de afixos entra no jogo.</b> O
        /// <c>GerenciadorEfeitosPassivos</c> lia <c>slot.Def.Modificadores</c>, que é só a
        /// camada da base — ler isso agora perderia tudo que a instância rolou, e todo o
        /// sistema seria invisível em jogo.</para>
        ///
        /// <para>Aloca uma lista por chamada, então <b>não deve ser usada em hot path</b>: o
        /// agregador de bônus tem cache próprio, invalidado por evento (Regra de Ouro 1).</para>
        /// </summary>
        public IReadOnlyList<ModificadorFixo> ModificadoresEfetivos()
        {
            var todos = new List<ModificadorFixo>();

            var def = Def;
            if (def?.Modificadores != null) todos.AddRange(def.Modificadores);

            if (Afixos != null)
                foreach (var a in Afixos)
                    if (a != null) todos.Add(a.ComoModificador());

            return todos;
        }
    }
}
