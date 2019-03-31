using System;
using Newtonsoft.Json;
using Quiz.Gameplay;

namespace Quiz.Network
{
    public class Player
    {
        public string Name;
        public int Points;

        public Matchmaker.Stream Stream;

        public Action<Player> OnPointsUpdateAction;
        public Action<string> OnNameChanged;
        public Action OnButtonPressed;

        public class PlayerMessage
        {
            public string SetName;
            public string Answer;
        }

        public Player(Matchmaker.Stream stream)
        {
            Stream = stream;

            OnNameChanged += newName =>
                SendMessage(new QuizCommand {Command = "NameChanged", Parameter = newName});

            OnPointsUpdateAction += player =>
                SendMessage(new QuizCommand {Command = "PointsChanged", Parameter = player.Points.ToString()});

            Points = 0;
            OnPointsUpdateAction?.Invoke(this);
            Name = $"Player {stream.Id}";
            OnNameChanged?.Invoke(Name);

            stream.MessageReceived += text =>
            {
                var message = JsonConvert.DeserializeObject<PlayerMessage>(text);

                if (message.SetName != null)
                {
                    Name = message.SetName;
                    OnNameChanged?.Invoke(Name);
                }
                else
                {
                    if (message.Answer != null)
                        SocketServer.OnPlayerAnswered?.Invoke(Name);

                    OnButtonPressed?.Invoke();
                }
            };

            SocketServer.OnPlayerConnected?.Invoke(this);
        }

        public void UpdatePoints(int arg)
        {
            Points += arg;
            OnPointsUpdateAction?.Invoke(this);
        }

        public void SetPoints(int arg)
        {
            Points = arg;
            OnPointsUpdateAction?.Invoke(this);
        }

        public void SendMessage(object message)
        {
            Stream.SendMessage(JsonConvert.SerializeObject(message));
        }
    }
}