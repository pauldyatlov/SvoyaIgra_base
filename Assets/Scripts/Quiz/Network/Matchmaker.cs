using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.WebSockets;
using System.Text;
using System.Threading;
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

            public void Kick() => _matchmaker.KickPlayer(this);
            public void SendMessage(string data) => _matchmaker.SendMessage(Enumerable.Repeat(this, 1), data);
        }

        public readonly List<Stream> Players = new List<Stream>();
        public event Action<Stream> PlayerAdded;
        public event Action<Stream> PlayerRemoved;

        private readonly ClientWebSocket _webSocket;
        private readonly string _gameName;

        private Matchmaker(ClientWebSocket webSocket, string gameName)
        {
            _webSocket = webSocket;
            _gameName = gameName;
        }

        public static async Task<Matchmaker> Create(Uri uri, string gameName)
        {
            var webSocket = new ClientWebSocket();
            await webSocket.ConnectAsync(uri, CancellationToken.None);

            var matchmaker = new Matchmaker(webSocket, gameName);
            _ = matchmaker.MainLoop();

            return matchmaker;
        }

        private void SendToIds(ICollection<string> ids, string message) =>
            _ = SendObjectAsync(new
            {
                Message = new
                {
                    target = ids.Count == 1
                        ? (object) new { Id = ids.Single() }
                        : (object) new { Ids = ids },
                    message
                }
            });

        public void SendMessage(IEnumerable<Stream> players, string message)
        {
            var onlinePlayers = players.Where(x => x.Online).ToArray();

            if (onlinePlayers.Any())
                SendToIds(onlinePlayers.Select(x => x.Id).ToArray(), message);
        }

        public void Broadcast(string message) =>
            _ = SendObjectAsync(new
            {
                Message = new
                {
                    target = "Broadcast",
                    message
                }
            });

        public void KickPlayer(Stream stream) => _ = SendObjectAsync(new { KickPlayer = stream.Id });

        private Task SendStringAsync(string text)
            => _webSocket.SendAsync(
                buffer: new ArraySegment<byte>(Encoding.UTF8.GetBytes(text)),
                messageType: WebSocketMessageType.Text,
                endOfMessage: true,
                CancellationToken.None);

        private Task SendObjectAsync(object obj) => SendStringAsync(JsonConvert.SerializeObject(obj));
        private Task Login(string gameName) => SendObjectAsync(new { Login = gameName });

        private async Task<string> ReadStringAsync()
        {
            var buffer = new ArraySegment<byte>(new byte[8192]);

            using (var ms = new MemoryStream())
            {
                WebSocketReceiveResult result;

                do
                {
                    result = await _webSocket.ReceiveAsync(buffer, CancellationToken.None);
                    ms.Write(buffer.Array, buffer.Offset, result.Count);
                }
                while (!result.EndOfMessage);

                ms.Seek(0, SeekOrigin.Begin);

                using (var reader = new StreamReader(ms, Encoding.UTF8))
                    return reader.ReadToEnd();
            }
        }

        private async Task MainLoop()
        {
            using (_webSocket) {
                await Login(_gameName);

                while (true)
                {
                    try
                    {
                        var matchmakerMessage = await ReadStringAsync();

                        Debug.LogWarning("Message: " + matchmakerMessage);

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
                    catch (WebSocketException socketException)
                    {
                        Debug.LogError("Matchmaker has been disconnected: " + socketException.Message);
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