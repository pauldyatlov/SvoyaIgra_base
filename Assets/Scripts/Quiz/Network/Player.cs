using System;
using Newtonsoft.Json;
using Quiz.Gameplay;
using UnityEngine;

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
        public Action<bool> OnSetAsDecisionMaker;

        public class PlayerMessage
        {
            public string SetName;
            public string Answer;
        }

        public bool Online => Stream.Online;
        public event Action<bool> OnlineStatusChanged
        {
            add => Stream.OnlineStatusChanged += value;
            remove => Stream.OnlineStatusChanged -= value;
        }

        public Player(Matchmaker.Stream stream)
        {
            Stream = stream;

            void OnOnNameChanged(string newName)
                => SendMessage(new QuizCommand { Command = "NameChanged", Parameter = newName });
            void OnPointsChanged(Player player)
                => SendMessage(new QuizCommand { Command = "PointsChanged", Parameter = player.Points.ToString() });

            OnNameChanged += OnOnNameChanged;
            OnPointsUpdateAction += OnPointsChanged;

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

            stream.OnlineStatusChanged += online => {
                if (!online)
                    return;

                OnOnNameChanged(Name);
                OnPointsChanged(this);
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