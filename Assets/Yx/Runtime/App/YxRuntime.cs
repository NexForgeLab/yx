using UnityEngine;

namespace Yx.Game
{
    public static class YxRuntime
    {
        public static void Apply(SaveEnvelope save)
        {
            Application.runInBackground = false;
            Screen.sleepTimeout = SleepTimeout.NeverSleep;
            Screen.autorotateToPortrait = true;
            Screen.autorotateToPortraitUpsideDown = false;
            Screen.autorotateToLandscapeLeft = false;
            Screen.autorotateToLandscapeRight = false;
            Screen.orientation = ScreenOrientation.Portrait;
            bool low = save != null && save.lowQuality != 0;
            string[] names = QualitySettings.names;
            int count = names != null ? names.Length : 1;
            int level = low ? 0 : (count > 2 ? 2 : 0);
            if (names != null)
            {
                string want = low ? "省电" : "标准";
                for (int i = 0; i < names.Length; i++)
                {
                    if (names[i] == want)
                    {
                        level = i;
                        break;
                    }
                }
            }
            if (level >= count) level = count - 1;
            if (level < 0) level = 0;
            QualitySettings.SetQualityLevel(level, true);
            QualitySettings.vSyncCount = 0;
            Application.targetFrameRate = 30;
            if (low)
            {
                QualitySettings.pixelLightCount = 0;
                QualitySettings.antiAliasing = 0;
                QualitySettings.shadows = ShadowQuality.Disable;
                QualitySettings.anisotropicFiltering = AnisotropicFiltering.Disable;
            }
        }
    }
}
