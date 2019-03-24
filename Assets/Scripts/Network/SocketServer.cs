using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Debug = UnityEngine.Debug;

public class QuizCommand
{
    public string Command;
    public string Parameter;
}

public interface IMatchmakerServer
{
}

public class Matchmaker
{
    private class Response
    {
        public class Message
        {
            public string id;
            public string text;
        }

        public class PlayerState
        {
            public string id;
            public bool online;
        }

        public string PlayerConnected;
        public string PlayerDisconnected;
        public Message PlayerMessage;
        public PlayerState[] CurrentState;
    }

    public class Player
    {
        public readonly string Id;
        public bool Online { get; private set; }

        private readonly Matchmaker _matchmaker;

        internal Player(string id, bool online, Matchmaker matchmaker)
        {
            Id = id;
            Online = online;
            _matchmaker = matchmaker;
        }

        public void Kick() =>
            _matchmaker.KickPlayer(this);

        public Action<string> MessageReceived;

        public void SendMessage(string data)
        {
            _matchmaker.SendMessage(Enumerable.Repeat(this, 1), data);
        }
    }

    public readonly List<Player> Players = new List<Player>();
    public event Action<Player> PlayerAdded;
    public event Action<Player> PlayerRemoved;

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
//        tcpClient.Connect(host, port);

        var matchmaker = new Matchmaker(tcpClient);
        _ = matchmaker.MainLoop();
        return matchmaker;
    }

    private void SendToIds(ICollection<string> ids, string message)
    {
        Debug.LogWarning("Send to ids: " + string.Join(", ", ids) + ": " + message);

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

    public void SendMessage(IEnumerable<Player> players, string message)
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

    public void KickPlayer(Player player) =>
        _ = _networkStream.WriteString(JsonConvert.SerializeObject(new {KickPlayer = player.Id}));

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

                    Debug.LogWarning("Message: " + matchmakerMessage);

                    var response = JsonConvert.DeserializeObject<Response>(matchmakerMessage);

                    if (response == null)
                        throw new IOException("Oh shit");

                    if (response.CurrentState != null)
                    {
                        foreach (var playerState in response.CurrentState)
                        {
                            var player = new Player(playerState.id, playerState.online, this);

                            Players.Add(player);
                            PlayerAdded?.Invoke(player);
                        }
                    }
                    else if (!string.IsNullOrEmpty(response.PlayerConnected))
                    {
                        var player = new Player(response.PlayerConnected, true, this);

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
                        Players.Single(x => x.Id == response.PlayerMessage.id).MessageReceived?.Invoke(innerMessage);
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

public static class SocketServer
{
    public const string CorrectAnswer = "CorrectAnswer";
    public const string WrongAnswer = "WrongAnswer";
    public const string SetScore = "SetPoints";
    public const string KickPlayer = "KickPlayer";

    public static Action<Player> OnPlayerConnected;
    public static Action<Matchmaker.Player> OnPlayerDisconnected;
    public static Action<string> OnPlayerAnswered;

    private static TcpListener _listener;

    public static async void Init()
    {
        var matchmaker = await Matchmaker.Create("139.162.199.78", 3013);

        matchmaker.PlayerAdded += player => _ = new Player(player);
        matchmaker.PlayerRemoved += stream => OnPlayerDisconnected?.Invoke(stream);
    }
}