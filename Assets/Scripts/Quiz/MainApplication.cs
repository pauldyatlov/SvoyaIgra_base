using System.Collections.Generic;
using System.Linq;
using Quiz.Gameplay.UI;
using Quiz.Network;
using UnityEngine;

namespace Quiz.Gameplay
{
    public class MainApplication : MonoBehaviour
    {
        [SerializeField] private Plan _plan = default;
        [SerializeField] private UIController _uiController = default;

        private readonly Dictionary<string, Player> _registeredPlayers = new Dictionary<string, Player>();

        private void Awake()
        {
            _uiController.Show(roomName =>
            {
                SocketServer.Init(roomName);

                SocketServer.OnPlayerConnected += PlayerConnectedHandler;
                SocketServer.OnPlayerDisconnected += PlayerDisconnectedHandler;

                _uiController.StartGame(_plan, OnPlayerKicked);
            });
        }

        private void PlayerConnectedHandler(Player player)
        {
            if (_registeredPlayers.ContainsKey(player.Name))
            {
                var connectedPlayer = _registeredPlayers[player.Name];

                player.Points = connectedPlayer.Points;
                player.OnPointsUpdateAction = connectedPlayer.OnPointsUpdateAction;

                _registeredPlayers[player.Name] = player;
            }
            else
            {
                _uiController.NewPlayerConnected(player);

                _registeredPlayers.Add(player.Name, player);
            }

            player.OnPointsUpdateAction?.Invoke(player);
        }

        private void PlayerDisconnectedHandler(Matchmaker.Stream stream)
        {
            var connectedPlayer = _registeredPlayers.FirstOrDefault(x => x.Value.Stream == stream);

            if (connectedPlayer.Value == null)
            {
                Debug.LogError("Player to delete is null");
                return;
            }

            _uiController.PlayerDisconnected(connectedPlayer);

            _registeredPlayers.Remove(connectedPlayer.Key);
        }

        private void OnPlayerKicked(Player player)
        {
            player.Stream.Kick();
        }

        private void OnApplicationQuit()
        {
            SocketServer.OnPlayerConnected -= PlayerConnectedHandler;
            SocketServer.OnPlayerDisconnected -= PlayerDisconnectedHandler;
        }
    }
}