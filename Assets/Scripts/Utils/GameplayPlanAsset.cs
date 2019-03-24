#if UNITY_EDITOR

using Assets.Scripts.Utils;
using UnityEditor;

public class GameplayPlanAsset
{
    [MenuItem("Assets/Create/Gameplay Plan")]
    public static void CreateAsset()
    {
        ScriptableObjectUtility.CreateAsset<GameplayPlan>();
    }
}

#endif