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

            public string PlayerJoined = null;
            public string PlayerLeft = null;
            public string PlayerConnected = null;
            public string PlayerDisconnected = null;
            public Message PlayerMessage = null;
            public PlayerState[] CurrentState = null;
        }

        public class Stream
        {
            public readonly string Id;

            private bool _online;

            public bool Online
            {
                get => _online;
                internal set
                {
                    if (_online != value)
                    {
                        _online = value;
                        _onlineStatusChanged?.Invoke(value);
                    }
                }
            }

            private readonly Matchmaker _matchmaker;
            public Action<string> MessageReceived;

            private Action<bool> _onlineStatusChanged;
            public event Action<bool> OnlineStatusChanged
            {
                add
                {
                    _onlineStatusChanged += value;
                    value(_online);
                }
                remove => _onlineStatusChanged -= value;
            }

            internal Stream(string id, bool online, Matchmaker matchmaker)
            {
                Id = id;
                _online = online;
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
        private readonly SemaphoreSlim _sendLock = new SemaphoreSlim(1, 1);

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

        private async Task SendStringAsync(string text)
        {
            await _sendLock.WaitAsync();

            try {
                await _webSocket.SendAsync(
                    buffer: new ArraySegment<byte>(Encoding.UTF8.GetBytes(text)),
                    messageType: WebSocketMessageType.Text,
                    endOfMessage: true,
                    CancellationToken.None);
            } finally {
                _sendLock.Release();
            }
        }

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
            using (_webSocket)
            {
                await Login(_gameName);

                while (true)
                {
                    try
                    {
                        var matchmakerMessage = await ReadStringAsync();

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
                        else if (!string.IsNullOrEmpty(response.PlayerJoined))
                        {
                            var player = new Stream(response.PlayerJoined, true, this);

                            Players.Add(player);
                            PlayerAdded?.Invoke(player);
                        }
                        else if (!string.IsNullOrEmpty(response.PlayerLeft))
                        {
                            var player = Players.FirstOrDefault(x => x.Id == response.PlayerLeft);

                            Players.Remove(player);
                            PlayerRemoved?.Invoke(player);
                        }
                        else if (!string.IsNullOrEmpty(response.PlayerConnected)) {
                            var player = Players.FirstOrDefault(x => x.Id == response.PlayerConnected);

                            if (player != null)
                                player.Online = true;
                        }
                        else if (!string.IsNullOrEmpty(response.PlayerDisconnected))
                        {
                            var player = Players.FirstOrDefault(x => x.Id == response.PlayerDisconnected);

                            if (player != null)
                                player.Online = false;
                        }
                        else if (response.PlayerMessage != null)
                        {
                            var innerMessage = response.PlayerMessage.text.Replace("\\\"", "\"");
                            Debug.Log("[Inner] message: " + innerMessage);
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