#if UNITY_EDITOR

using Assets.Scripts.Utils;
using Quiz.Gameplay;
using UnityEditor;

public class GameplayPlanAsset
{
    [MenuItem("Assets/Create/Gameplay Plan")]
    public static void CreateAsset()
    {
        ScriptableObjectUtility.CreateAsset<Plan>();
    }
}

#endif