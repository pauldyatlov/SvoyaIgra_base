using System;
using System.Net.Sockets;
using UnityEngine;

namespace Quiz.Network
{
    public static class SocketServer
    {
        public const string CorrectAnswer = "CorrectAnswer";
        public const string WrongAnswer = "WrongAnswer";
        public const string SetScore = "SetPoints";
        public const string KickPlayer = "KickPlayer";

        public static event Action<Player> OnPlayerConnected;
        public static event Action<Matchmaker.Stream> OnPlayerDisconnected;
        public static event Action<string> OnPlayerAnswered;

        private const string Host = "139.162.199.78";
        private const int Port = 3013;

        private static TcpListener _listener;

        public static async void Init(string roomName)
        {
            try
            {
                var matchmaker = await Matchmaker.Create(new Uri("ws://" + Host + ":" + Port), roomName);

                matchmaker.PlayerAdded += player => _ = new Player(player);
                matchmaker.PlayerRemoved += stream => OnPlayerDisconnected?.Invoke(stream);
            }
            catch (Exception e)
            {
                Debug.LogException(e);
            }
        }

        public static void PlayerAnswer(string playerName)
        {
            OnPlayerAnswered?.Invoke(playerName);
        }

        public static void PlayerConnect(Player player)
        {
            OnPlayerConnected?.Invoke(player);
        }
    }
}