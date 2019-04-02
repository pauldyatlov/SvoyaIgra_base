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

        public static Action<Player> OnPlayerConnected;
        public static Action<Matchmaker.Stream> OnPlayerDisconnected;
        public static Action<string> OnPlayerAnswered;

        private const string Host = "139.162.199.78";
        private const int Port = 3013;

        private static TcpListener _listener;

        public static async void Init()
        {
            try {
                var matchmaker = await Matchmaker.Create(new Uri("ws://" + Host + ":" + Port), "SVOYAIGRA");
                matchmaker.PlayerAdded += player => _ = new Player(player);
                matchmaker.PlayerRemoved += stream => OnPlayerDisconnected?.Invoke(stream);
            } catch (Exception e) {
                Debug.LogException(e);
            }
        }
    }
}