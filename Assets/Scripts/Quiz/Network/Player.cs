using System;
using System.Collections.Generic;
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

        private readonly string[] _consonants =
        {
            "b", "c", "d", "f", "g", "h", "j", "k", "l", "m", "n", "p", "q", "r", "s", "t", "v", "w", "x", "y", "z"
        };

        private readonly string[] _vowels = {"a", "e", "i", "o", "u"};

        public event Action<bool> OnlineStatusChanged
        {
            add => Stream.OnlineStatusChanged += value;
            remove => Stream.OnlineStatusChanged -= value;
        }

        public Player(Matchmaker.Stream stream)
        {
            Stream = stream;

            void OnOnNameChanged(string newName)
                => SendMessage(new QuizCommand {Command = "NameChanged", Parameter = newName});

            void OnPointsChanged(Player player)
                => SendMessage(new QuizCommand {Command = "PointsChanged", Parameter = player.Points.ToString()});

            OnNameChanged += OnOnNameChanged;
            OnPointsUpdateAction += OnPointsChanged;

            Points = 0;
            OnPointsUpdateAction?.Invoke(this);
            Name = GenerateRandomName();
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
                        SocketServer.PlayerAnswer(Name);

                    OnButtonPressed?.Invoke();
                }
            };

            stream.OnlineStatusChanged += online =>
            {
                if (!online)
                    return;

                OnOnNameChanged(Name);
                OnPointsChanged(this);
            };

            SocketServer.PlayerConnect(this);
        }

        private string GenerateRandomName()
        {
            var requestedLength = UnityEngine.Random.Range(2, 7);

            var word = "";

            if (requestedLength == 1)
            {
                word = GetRandomLetter(_vowels);
            }
            else
            {
                for (var i = 0; i < requestedLength; i += 2)
                    word += GetRandomLetter(_consonants) + GetRandomLetter(_vowels);

                word = word.Replace("q", "qu").Substring(0, requestedLength);
            }

            return word;
        }

        private string GetRandomLetter(IReadOnlyList<string> letters)
        {
            return letters[UnityEngine.Random.Range(0, letters.Count - 1)];
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