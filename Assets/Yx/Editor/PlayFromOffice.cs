using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace Yx.EditorTools
{
    [InitializeOnLoad]
    public static class PlayFromOffice
    {
        const string Key = "Yx.PlayFromOffice";
        const string Menu = "余响测绘/从测稿室开局";

        static PlayFromOffice()
        {
            EditorApplication.delayCall += Apply;
        }

        [MenuItem(Menu)]
        static void Toggle()
        {
            EditorPrefs.SetBool(Key, !EditorPrefs.GetBool(Key, true));
            Apply();
        }

        [MenuItem(Menu, true)]
        static bool Validate()
        {
            Menu.SetChecked(Menu, EditorPrefs.GetBool(Key, true));
            return true;
        }

        static void Apply()
        {
            if (!EditorPrefs.GetBool(Key, true))
            {
                EditorSceneManager.playModeStartScene = null;
                return;
            }
            var scene = AssetDatabase.LoadAssetAtPath<SceneAsset>("Assets/Scenes/SurveyOffice.unity");
            if (scene != null) EditorSceneManager.playModeStartScene = scene;
        }
    }
}
