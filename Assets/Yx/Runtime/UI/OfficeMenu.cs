using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using Yx.Sim;

namespace Yx.Game
{
    public class OfficeMenu : MonoBehaviour
    {
        SaveEnvelope save;
        RectTransform root;
        RectTransform mainPanel;
        RectTransform settingsPanel;
        RectTransform residualPanel;
        Text residualBody;
        Text airLabel;
        Text vibeLabel;
        Text handLabel;
        Text particleLabel;
        Text qualityLabel;

        void Start()
        {
            save = SaveGame.Load();
            YxRuntime.Apply(save);
            Canvas canvas = UiKit.CreateCanvas("OfficeCanvas");
            root = UiKit.Panel(canvas.transform, "Root", new Color(0.025f, 0.03f, 0.034f, 1f));
            UiKit.ApplySafeArea(root);
            BuildMain();
            BuildSettings();
            BuildResidual();
            ShowMain();
        }

        void BuildMain()
        {
            mainPanel = UiKit.Panel(root, "Main", new Color(0f, 0f, 0f, 0f));
            var title = UiKit.Label(mainPanel, "余响测绘", 92, TextAnchor.MiddleCenter, Palette.Drum);
            title.rectTransform.anchorMin = new Vector2(0.08f, 0.78f);
            title.rectTransform.anchorMax = new Vector2(0.92f, 0.9f);
            title.rectTransform.offsetMin = Vector2.zero;
            title.rectTransform.offsetMax = Vector2.zero;
            var hook = UiKit.Label(mainPanel, "每拍一次声呐，路就亮一截，沉睡的东西也会醒一分。", 36, TextAnchor.MiddleCenter, Palette.Ink);
            hook.rectTransform.anchorMin = new Vector2(0.1f, 0.66f);
            hook.rectTransform.anchorMax = new Vector2(0.9f, 0.78f);
            hook.rectTransform.offsetMin = Vector2.zero;
            hook.rectTransform.offsetMax = Vector2.zero;
            float y = 80f;
            if (save.hasActive != 0)
            {
                UiKit.MakeButton(mainPanel, "Resume", "继续测绘", new Vector2(0.5f, 0.5f), new Vector2(0f, y + 150f), new Vector2(640f, 140f), Resume);
                var note = UiKit.Label(mainPanel, "开始新的一条，会丢掉还没画完的这一张。", 28, TextAnchor.MiddleCenter, new Color(0.7f, 0.75f, 0.74f, 1f));
                note.rectTransform.anchorMin = new Vector2(0.12f, 0.5f);
                note.rectTransform.anchorMax = new Vector2(0.88f, 0.5f);
                note.rectTransform.anchoredPosition = new Vector2(0f, y + 40f);
                note.rectTransform.sizeDelta = new Vector2(0f, 80f);
                note.rectTransform.offsetMin = new Vector2(40f, y);
                note.rectTransform.offsetMax = new Vector2(-40f, y + 70f);
            }
            UiKit.MakeButton(mainPanel, "Start", "开始测绘", new Vector2(0.5f, 0.5f), new Vector2(0f, y), new Vector2(640f, 140f), StartNew);
            UiKit.MakeButton(mainPanel, "Residual", "上次残页", new Vector2(0.5f, 0.5f), new Vector2(0f, y - 170f), new Vector2(640f, 120f), ShowResidual);
            UiKit.MakeButton(mainPanel, "Settings", "设置", new Vector2(0.5f, 0.5f), new Vector2(0f, y - 320f), new Vector2(640f, 120f), ShowSettings);
            var foot = UiKit.Label(mainPanel, "测试期免费 · 无广告 · 无内购 · 不联网", 28, TextAnchor.MiddleCenter, new Color(0.65f, 0.75f, 0.74f, 1f));
            foot.rectTransform.anchorMin = new Vector2(0.08f, 0.04f);
            foot.rectTransform.anchorMax = new Vector2(0.92f, 0.1f);
            foot.rectTransform.offsetMin = Vector2.zero;
            foot.rectTransform.offsetMax = Vector2.zero;
            int pool = ModuleBook.UnlockedCount(save.successes);
            var poolLabel = UiKit.Label(mainPanel, "模组池 " + pool + " / 12", 28, TextAnchor.MiddleCenter, Palette.WallLine);
            poolLabel.rectTransform.anchorMin = new Vector2(0.08f, 0.1f);
            poolLabel.rectTransform.anchorMax = new Vector2(0.92f, 0.15f);
            poolLabel.rectTransform.offsetMin = Vector2.zero;
            poolLabel.rectTransform.offsetMax = Vector2.zero;
        }

        void BuildSettings()
        {
            settingsPanel = UiKit.Panel(root, "Settings", new Color(0.02f, 0.025f, 0.03f, 1f));
            var title = UiKit.Label(settingsPanel, "设置", 72, TextAnchor.MiddleCenter, Palette.Drum);
            title.rectTransform.anchorMin = new Vector2(0.1f, 0.84f);
            title.rectTransform.anchorMax = new Vector2(0.9f, 0.94f);
            title.rectTransform.offsetMin = Vector2.zero;
            title.rectTransform.offsetMax = Vector2.zero;
            airLabel = Row(settingsPanel, "空气时限", 520f, () => CycleAir());
            vibeLabel = Row(settingsPanel, "震动", 360f, () => { save.vibration = save.vibration == 0 ? 1 : 0; RefreshSettings(); });
            handLabel = Row(settingsPanel, "左手镜像", 200f, () => { save.leftHand = save.leftHand == 0 ? 1 : 0; RefreshSettings(); });
            particleLabel = Row(settingsPanel, "余辉粒子", 40f, () => { save.particles = save.particles == 0 ? 1 : 0; RefreshSettings(); });
            qualityLabel = Row(settingsPanel, "画质", -120f, () => { save.lowQuality = save.lowQuality == 0 ? 1 : 0; YxRuntime.Apply(save); RefreshSettings(); });
            UiKit.MakeButton(settingsPanel, "Back", "返回", new Vector2(0.5f, 0f), new Vector2(0f, 180f), new Vector2(480f, 120f), () => { SaveGame.Store(save); ShowMain(); });
            RefreshSettings();
        }

        Text Row(Transform parent, string name, float y, UnityEngine.Events.UnityAction click)
        {
            UiKit.MakeButton(parent, name, name, new Vector2(0.5f, 0.5f), new Vector2(0f, y), new Vector2(760f, 120f), click);
            return null;
        }

        void CycleAir()
        {
            if (save.airSeconds < 500f) save.airSeconds = 600f;
            else if (save.airSeconds < 700f) save.airSeconds = 720f;
            else save.airSeconds = 480f;
            RefreshSettings();
        }

        void RefreshSettings()
        {
            SetCaption("空气时限", "空气时限　" + Minutes());
            SetCaption("震动", "震动　" + OnOff(save.vibration));
            SetCaption("左手镜像", "左手镜像　" + OnOff(save.leftHand));
            SetCaption("余辉粒子", "余辉粒子　" + OnOff(save.particles));
            SetCaption("画质", save.lowQuality == 0 ? "画质　标准（30 帧）" : "画质　省电（30 帧，关粒子）");
            SaveGame.Store(save);
        }

        void SetCaption(string objectName, string text)
        {
            Transform found = settingsPanel.Find(objectName);
            if (found == null) return;
            Text label = found.GetComponentInChildren<Text>();
            if (label != null) label.text = text;
        }

        string Minutes()
        {
            if (save.airSeconds < 500f) return "8 分钟";
            if (save.airSeconds < 700f) return "10 分钟";
            return "12 分钟";
        }

        static string OnOff(int value)
        {
            return value == 0 ? "关" : "开";
        }

        void BuildResidual()
        {
            residualPanel = UiKit.Panel(root, "Residual", new Color(0.02f, 0.025f, 0.03f, 1f));
            var title = UiKit.Label(residualPanel, "上次残页", 72, TextAnchor.MiddleCenter, Palette.Drum);
            title.rectTransform.anchorMin = new Vector2(0.1f, 0.8f);
            title.rectTransform.anchorMax = new Vector2(0.9f, 0.92f);
            title.rectTransform.offsetMin = Vector2.zero;
            title.rectTransform.offsetMax = Vector2.zero;
            residualBody = UiKit.Label(residualPanel, "", 40, TextAnchor.UpperCenter, Palette.Ink);
            residualBody.rectTransform.anchorMin = new Vector2(0.1f, 0.28f);
            residualBody.rectTransform.anchorMax = new Vector2(0.9f, 0.76f);
            residualBody.rectTransform.offsetMin = Vector2.zero;
            residualBody.rectTransform.offsetMax = Vector2.zero;
            UiKit.MakeButton(residualPanel, "BackResidual", "返回", new Vector2(0.5f, 0f), new Vector2(0f, 180f), new Vector2(480f, 120f), ShowMain);
        }

        void ShowMain()
        {
            mainPanel.gameObject.SetActive(true);
            settingsPanel.gameObject.SetActive(false);
            residualPanel.gameObject.SetActive(false);
        }

        void ShowSettings()
        {
            RefreshSettings();
            mainPanel.gameObject.SetActive(false);
            settingsPanel.gameObject.SetActive(true);
            residualPanel.gameObject.SetActive(false);
        }

        void ShowResidual()
        {
            if (save.hasResidual == 0)
                residualBody.text = "还没有残页。\n画完一条，或者倒在水道里，这里会留下三行。";
            else
            {
                string tier = Hints.ResidualTier(save.resMaxS, save.resChaos != 0);
                string result = save.resSuccess != 0 ? "测稿完成" : save.resCause;
                residualBody.text = "走到：" + SaveGame.RoomLabel(save.resRoom)
                    + "\n\n最高扰动：" + tier
                    + "\n\n记录：" + result
                    + "\n\n残页不加点，也不解锁数值。";
            }
            mainPanel.gameObject.SetActive(false);
            settingsPanel.gameObject.SetActive(false);
            residualPanel.gameObject.SetActive(true);
        }

        void StartNew()
        {
            SaveGame.ClearActive(save);
            YxSession.Next = YxSession.Kind.New;
            SceneManager.LoadScene("Waterway");
        }

        void Resume()
        {
            YxSession.Next = YxSession.Kind.Resume;
            SceneManager.LoadScene("Waterway");
        }
    }
}
