using System;
using UnityEngine;
using System.Collections.Generic;
using UnityEngine.Video;

namespace Quiz.Gameplay
{
    public class Plan : ScriptableObject
    {
        public List<RoundPlan> RoundsList;
        public List<FinalQuestion> FinalQuestions;
    }

    [Serializable]
    public class RoundPlan
    {
        public List<ThemePlan> ThemesList;
    }

    [Serializable]
    public class ThemePlan
    {
        public string ThemeName;
        public List<QuestionPlan> QuestionsList;
    }

    [Serializable]
    public class Answer
    {
        public string Text;
        public Sprite Picture;
        public VideoClip Video;
    }

    [Serializable]
    public class QuestionPlan
    {
        public int Price;
        public string Question;
        public Sprite Picture;
        public AudioClip Audio;
        public VideoClip Video;

        [HideInInspector] public bool IsCatInPoke;
        [HideInInspector] public CatInPoke CatInPoke;

        public Answer Answer;
    }

    [Serializable]
    public class CatInPoke
    {
        public string Theme;
        public int Price;

        public CatInPoke(string theme, int price)
        {
            Theme = theme;
            Price = price;
        }
    }

    [Serializable]
    public class FinalQuestion
    {
        public string Theme;
        public string Question;
        public string Answer;
    }
}