using UnityEditor;
using UnityEngine;

namespace Quiz.Gameplay
{
    [CustomEditor(typeof(Plan))]
    public sealed class PlanEditor : Editor
    {
        public override void OnInspectorGUI()
        {
            base.OnInspectorGUI();

            if (GUILayout.Button("Clear cats"))
            {
                var plan = (Plan)target;

                foreach (var round in plan.RoundsList)
                {
                    foreach (var themePlan in round.ThemesList)
                    {
                        foreach (var questionPlan in themePlan.QuestionsList)
                        {
                            questionPlan.IsCatInPoke = false;
                            questionPlan.CatInPoke = new CatInPoke(string.Empty, 0);
                        }
                    }
                }
            }

            if (GUILayout.Button("Clear answers"))
            {
                var plan = (Plan)target;

                foreach (var round in plan.RoundsList)
                {
                    foreach (var themePlan in round.ThemesList)
                    {
                        foreach (var questionPlan in themePlan.QuestionsList)
                            questionPlan.Answer = new Answer();
                    }
                }
            }

            if (GUILayout.Button("Generate random cats"))
            {
                var plan = (Plan)target;

                foreach (var round in plan.RoundsList)
                {
                    for (var i = 0; i < 2; i++)
                    {
                        var themeIndex = Random.Range(0, round.ThemesList.Count);
                        var theme = round.ThemesList[themeIndex];

                        var questionIndex = Random.Range(0, theme.QuestionsList.Count);
                        var question = theme.QuestionsList[questionIndex];

                        question.IsCatInPoke = true;
                        question.CatInPoke = new CatInPoke(theme.ThemeName, question.Price);

                        Debug.Log("Question " + theme.ThemeName + ", price: " + question.Price);
                    }
                }
            }

            if (GUILayout.Button("Order price"))
            {
                var plan = (Plan)target;
                for (var i = 0; i < plan.RoundsList.Count; i++)
                {
                    var round = plan.RoundsList[i];
                    for (var j = 0; j < round.ThemesList.Count; j++)
                    {
                        var theme = round.ThemesList[j];
                        for (var k = 0; k < theme.QuestionsList.Count; k++)
                        {
                            var question = theme.QuestionsList[k];
                            question.Price = 100 + (int)((i + 1) / 2f * (k + 1) * 50f + 25);
                        }
                    }
                }
            }
        }
    }
}