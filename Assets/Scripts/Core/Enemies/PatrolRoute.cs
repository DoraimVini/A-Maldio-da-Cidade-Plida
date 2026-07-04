using System;
using UnityEngine;

namespace FavelaAmarela.Core.Enemies
{
    public sealed class PatrolRoute
    {
        private readonly Vector2[] _waypoints;
        private int _currentIndex;
        private readonly bool _loop; // true = volta ao início, false = ping-pong
        private int _direction = 1;  // usado só no modo ping-pong

        public PatrolRoute(Vector2[] waypoints, bool loop = true)
        {
            if (waypoints == null || waypoints.Length == 0)
            {
                throw new ArgumentException("Waypoints cannot be null or empty.", nameof(waypoints));
            }
            
            _waypoints = waypoints;
            _loop = loop;
            _currentIndex = 0;
        }

        public Vector2 AlvoAtual => _waypoints[_currentIndex];

        /// <summary>
        /// Chamado a cada frame de movimento com a posição atual do Cultista.
        /// Se chegou perto o suficiente do alvo (threshold), avança o índice.
        /// Retorna true se o índice mudou neste chamado.
        /// </summary>
        public bool AtualizarChegada(Vector2 posicaoAtual, float raioDeChegada)
        {
            if (Vector2.Distance(posicaoAtual, AlvoAtual) > raioDeChegada)
                return false;

            AvancarIndice();
            return true;
        }

        private void AvancarIndice()
        {
            if (_waypoints.Length <= 1) return; // Se tem só 1 waypoint, não avança

            if (_loop)
            {
                _currentIndex = (_currentIndex + 1) % _waypoints.Length;
            }
            else
            {
                _currentIndex += _direction;

                if (_currentIndex >= _waypoints.Length)
                {
                    _currentIndex = _waypoints.Length - 2; // Volta para o penúltimo
                    _direction = -1;
                }
                else if (_currentIndex < 0)
                {
                    _currentIndex = 1; // Volta para o segundo
                    _direction = 1;
                }
            }
        }
    }
}
