using System;
using System.IO;
using System.Net.Sockets;
using UnityEngine;

namespace Quiz.Network
{
    public class SocketServer
    {
        public const string CorrectAnswer = "CorrectAnswer";
        public const string WrongAnswer = "WrongAnswer";
        public const string SetScore = "SetPoints";
        public const string KickPlayer = "KickPlayer";

        public static event Action<Player> OnPlayerConnected;
        public static event Action<Matchmaker.Stream> OnPlayerDisconnected;
        public static event Action<string> OnPlayerAnswered;

        private const string Host = "partyquiz.club";
        private const int Port = 3013;

        private static TcpListener _listener;
        private static Matchmaker _matchmaker;

        public static async void Init(string roomName)
        {
            try
            {
                _matchmaker = await Matchmaker.Create(new Uri("ws://" + Host + ":" + Port), roomName);

                _matchmaker.PlayerAdded += OnPlayerAdded;
                _matchmaker.PlayerRemoved += OnPlayerRemoved;
            }
            catch (Exception e)
            {
                Debug.LogException(e);
            }
        }

        private static void OnPlayerAdded(Matchmaker.Stream stream)
        {
            _ = new Player(stream);
        }

        private static void OnPlayerRemoved(Matchmaker.Stream stream)
        {
            OnPlayerDisconnected?.Invoke(stream);
        }

        public static void PlayerAnswer(string playerName)
        {
            OnPlayerAnswered?.Invoke(playerName);
        }

        public static void PlayerConnect(Player player)
        {
            OnPlayerConnected?.Invoke(player);
        }

        private void OnApplicationQuit()
        {
            _matchmaker.PlayerAdded -= OnPlayerAdded;
            _matchmaker.PlayerRemoved -= OnPlayerRemoved;
        }
    }
}