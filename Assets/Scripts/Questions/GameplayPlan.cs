using System;
using UnityEngine;
using System.Collections.Generic;
using UnityEngine.Video;

public class GameplayPlan : ScriptableObject
{
    public List<RoundGameplayPlan> RoundsList;
    public List<FinalQuestion> FinalQuestions;
}

[Serializable]
public class RoundGameplayPlan
{
    public List<ThemesGameplayPlan> ThemesList;
}

[Serializable]
public class ThemesGameplayPlan
{
    public string ThemeName;
    public List<QuestionsGameplayPlan> QuestionsList;
}

[Serializable]
public class QuestionsGameplayPlan
{
    public int Price;
    public string Question;
    public string Answer;
    public Sprite Picture;
    public AudioClip Audio;
    public VideoClip Video;

    public CatInPokeQuestion CatInPoke;

    public bool IsCatInPoke => !string.IsNullOrEmpty(CatInPoke.Theme) && CatInPoke.Price > 0;
}

[Serializable]
public class CatInPokeQuestion
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