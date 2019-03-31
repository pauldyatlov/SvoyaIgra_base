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
    public class QuestionPlan
    {
        public int Price;
        public string Question;
        public string Answer;
        public Sprite Picture;
        public AudioClip Audio;
        public VideoClip Video;

        public bool IsCatInPoke;
        public CatInPoke CatInPoke;
    }

    [Serializable]
    public class CatInPoke
    {
        public string Theme;
        public int Price;
    }

    [Serializable]
    public class FinalQuestion
    {
        public string Theme;
        public string Question;
        public string Answer;
    }
}