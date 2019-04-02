using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Sockets;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Debug = UnityEngine.Debug;
// ReSharper disable ClassNeverInstantiated.Local

namespace Quiz.Network
{
    public class Matchmaker
    {
        private class Response
        {
            public class Message
            {
                public string id = null;
                public string text = null;
            }

            public class PlayerState
            {
                public string id = null;
                public bool online = false;
            }

            public string PlayerConnected = null;
            public string PlayerDisconnected = null;
            public Message PlayerMessage = null;
            public PlayerState[] CurrentState = null;
        }

        public class Stream
        {
            public readonly string Id;
            public bool Online { get; private set; }

            private readonly Matchmaker _matchmaker;
            public Action<string> MessageReceived;

            internal Stream(string id, bool online, Matchmaker matchmaker)
            {
                Id = id;
                Online = online;
                _matchmaker = matchmaker;
            }

            public void Kick() =>
                _matchmaker.KickPlayer(this);

            public void SendMessage(string data)
            {
                _matchmaker.SendMessage(Enumerable.Repeat(this, 1), data);
            }
        }

        public readonly List<Stream> Players = new List<Stream>();
        public event Action<Stream> PlayerAdded;
        public event Action<Stream> PlayerRemoved;

        private readonly TcpClient _tcpClient;
        private NetworkStream _networkStream;

        private Matchmaker(TcpClient tcpClient)
        {
            _tcpClient = tcpClient;
        }

        public static async Task<Matchmaker> Create(string host, int port)
        {
            var tcpClient = new TcpClient();
            await tcpClient.ConnectAsync(host, port);

            var matchmaker = new Matchmaker(tcpClient);
            _ = matchmaker.MainLoop();

            return matchmaker;
        }

        private void SendToIds(ICollection<string> ids, string message)
        {
            async void Send()
            {
                try
                {
                    await _networkStream.WriteString(JsonConvert.SerializeObject(new
                    {
                        Message = new
                        {
                            target = ids.Count == 1
                                ? (object) new {Id = ids.Single()}
                                : (object) new {Ids = ids},
                            message
                        }
                    }));
                }
                catch (Exception e)
                {
                    Debug.LogException(e);
                }
            }

            Send();
        }

        public void SendMessage(IEnumerable<Stream> players, string message)
        {
            var onlinePlayers = players.Where(x => x.Online).ToArray();

            if (onlinePlayers.Any())
                SendToIds(onlinePlayers.Select(x => x.Id).ToArray(), message);
        }

        public void Broadcast(string message) =>
            _ = _networkStream.WriteString(JsonConvert.SerializeObject(new
            {
                Message = new
                {
                    target = "Broadcast",
                    message
                }
            }));

        public void KickPlayer(Stream stream) =>
            _ = _networkStream.WriteString(JsonConvert.SerializeObject(new {KickPlayer = stream.Id}));

        private async Task MainLoop()
        {
            using (_networkStream = _tcpClient.GetStream())
            {
                await _networkStream.WriteString(@"{ ""Login"": ""SVOYAIGRA"" }");

                while (true)
                {
                    try
                    {
                        var matchmakerMessage = await _networkStream.ReadString();

                        Debug.Log("Message: " + matchmakerMessage);

                        var response = JsonConvert.DeserializeObject<Response>(matchmakerMessage);

                        if (response == null)
                            throw new IOException("Oh shit");

                        if (response.CurrentState != null)
                        {
                            foreach (var playerState in response.CurrentState)
                            {
                                var player = new Stream(playerState.id, playerState.online, this);

                                Players.Add(player);
                                PlayerAdded?.Invoke(player);
                            }
                        }
                        else if (!string.IsNullOrEmpty(response.PlayerConnected))
                        {
                            var player = new Stream(response.PlayerConnected, true, this);

                            Players.Add(player);
                            PlayerAdded?.Invoke(player);
                        }
                        else if (!string.IsNullOrEmpty(response.PlayerDisconnected))
                        {
                            var player = Players.FirstOrDefault(x => x.Id == response.PlayerDisconnected);

                            Players.Remove(player);
                            PlayerRemoved?.Invoke(player);
                        }
                        else if (response.PlayerMessage != null)
                        {
                            var innerMessage = response.PlayerMessage.text.Replace("\\\"", "\"");
                            Debug.LogWarning("Inner message: " + innerMessage);
                            Players.Single(x => x.Id == response.PlayerMessage.id).MessageReceived
                                ?.Invoke(innerMessage);
                        }
                    }
                    catch (IOException ioException)
                    {
                        Debug.LogError("Matchmaker has been disconnected: " + ioException.Message);
                        throw;
                    }
                    catch (Exception e)
                    {
                        Debug.LogException(e);
                    }
                }
            }
        }
    }
}