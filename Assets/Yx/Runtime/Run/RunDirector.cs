using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using Yx.Sim;

namespace Yx.Game
{
    public class RunDirector : MonoBehaviour
    {
        SaveEnvelope save;
        RunState run;
        WorldView world;
        AudioSynth audio;
        Canvas canvas;
        RectTransform hudRoot;
        TouchStick stick;
        PressCatch pingCatch;
        PressCatch breathCatch;
        Image pingFill;
        Image breathFill;
        Image surveyFill;
        Image[] lifePips = new Image[3];
        Image[] ripples = new Image[3];
        Text airText;
        Text roomText;
        Text tierText;
        Text surveyText;
        Text moduleText;
        Text toastText;
        Text tutorialText;
        float toastTimer;
        RectTransform draftRoot;
        RectTransform pauseRoot;
        RectTransform endRoot;
        Text endLines;
        Button retryButton;
        bool endShown;
        float saveTimer;
        bool booted;
        int unlockAtStart;

        void Start()
        {
            save = SaveGame.Load();
            YxRuntime.Apply(save);
            SegmentBinder.LoadAuthored();
            unlockAtStart = ModuleBook.UnlockedCount(save.successes);
            if (YxSession.Next == YxSession.Kind.Resume)
            {
                run = SaveGame.ReadActive(save);
                YxSession.Next = YxSession.Kind.New;
            }
            if (run == null)
            {
                bool tutorial = save.runsStarted == 0;
                int seed = Random.Range(1, int.MaxValue);
                run = RunFactory.New(seed, save.airSeconds, tutorial, unlockAtStart);
                save.runsStarted++;
                SaveGame.WriteActive(save, run);
            }
            Camera cam = Camera.main;
            if (cam == null)
            {
                var camGo = new GameObject("Main Camera");
                cam = camGo.AddComponent<Camera>();
                cam.tag = "MainCamera";
                camGo.AddComponent<AudioListener>();
            }
            cam.orthographic = true;
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = new Color(0.02f, 0.024f, 0.028f, 1f);
            cam.nearClipPlane = 0.1f;
            cam.farClipPlane = 40f;
            if (cam.GetComponent<AudioListener>() == null) cam.gameObject.AddComponent<AudioListener>();
            audio = cam.gameObject.AddComponent<AudioSynth>();
            audio.Init();
            var worldGo = new GameObject("World");
            world = worldGo.AddComponent<WorldView>();
            world.Bind(cam, save.lowQuality == 0 ? 4 : 1);
            BuildHud();
            if (run.MenuPause) ShowPause();
            if (run.DraftOpen) ShowDraft();
            booted = true;
            var ev = new SimEvents();
            world.Sync(run, ParticlesOn(), ev);
        }

        void Update()
        {
            if (run == null) return;
            UiKit.ApplySafeArea(hudRoot);
            if (Input.GetKeyDown(KeyCode.Escape))
            {
                if (endShown) ReturnOffice();
                else if (draftRoot != null && draftRoot.gameObject.activeSelf) { }
                else TogglePause();
            }
            var input = ReadInput();
            var ev = new SimEvents();
            bool suspended = run.MenuPause || run.DraftOpen || run.Finished || endShown;
            if (!suspended)
                Simulator.Tick(run, Time.deltaTime, input, ev);
            ConsumePresses();
            React(ev);
            if (world != null) world.Sync(run, ParticlesOn(), ev);
            RefreshHud();
            TickToast();
            Autosave();
        }

        void OnApplicationPause(bool paused)
        {
            if (!booted || run == null || !paused) return;
            Background();
        }

        void OnApplicationFocus(bool focus)
        {
            if (!booted || run == null || focus) return;
            if (Time.realtimeSinceStartup < 0.6f) return;
            Background();
        }

        void Background()
        {
            if (run.Finished) return;
            run.MenuPause = true;
            Time.timeScale = 0f;
            SaveGame.WriteActive(save, run);
            ShowPause();
        }

        SimInput ReadInput()
        {
            float x = stick != null ? stick.Value.x : 0f;
            float y = stick != null ? stick.Value.y : 0f;
            if (Input.GetKey(KeyCode.A) || Input.GetKey(KeyCode.LeftArrow)) x -= 1f;
            if (Input.GetKey(KeyCode.D) || Input.GetKey(KeyCode.RightArrow)) x += 1f;
            if (Input.GetKey(KeyCode.S) || Input.GetKey(KeyCode.DownArrow)) y -= 1f;
            if (Input.GetKey(KeyCode.W) || Input.GetKey(KeyCode.UpArrow)) y += 1f;
            bool ping = (pingCatch != null && pingCatch.Pressed) || Input.GetKeyDown(KeyCode.Space) || Input.GetKeyDown(KeyCode.J);
            bool breath = (breathCatch != null && breathCatch.Pressed) || Input.GetKeyDown(KeyCode.LeftShift) || Input.GetKeyDown(KeyCode.K);
            return new SimInput { X = x, Y = y, Ping = ping, Breath = breath };
        }

        void ConsumePresses()
        {
            if (pingCatch != null) pingCatch.Pressed = false;
            if (breathCatch != null) breathCatch.Pressed = false;
        }

        void React(SimEvents ev)
        {
            if (ev.Pinged)
            {
                audio.Drum();
                if (run.ManualPings == 1 && save.vibration != 0) Vibrate();
            }
            if (ev.BreathStarted) audio.Breath();
            if (ev.Damaged) audio.Hurt();
            if (ev.GateOpened)
            {
                audio.Gate();
                Toast("闸门开了");
            }
            if (ev.Awakened) Toast("深处醒了");
            if (ev.Chaos) Toast("乱响");
            if (ev.RoomChanged)
            {
                Toast(run.RoomIndex >= 4 ? "未命名厅" : "下一间水道");
                SaveGame.WriteActive(save, run);
            }
            if (ev.DraftOpened) ShowDraft();
            if ((ev.Died || ev.Won) && !endShown) ShowEnd();
        }

        bool ParticlesOn()
        {
            return save.particles != 0 && save.lowQuality == 0;
        }

        void Vibrate()
        {
#if UNITY_ANDROID || UNITY_IOS
            if (SystemInfo.supportsVibration) Handheld.Vibrate();
#endif
        }

        void BuildHud()
        {
            canvas = UiKit.CreateCanvas("Hud");
            hudRoot = UiKit.Panel(canvas.transform, "HudRoot", new Color(0f, 0f, 0f, 0f));
            var bg = hudRoot.GetComponent<Image>();
            bg.raycastTarget = false;
            bool left = save.leftHand != 0;
            roomText = AnchoredLabel(hudRoot, new Vector2(0.5f, 1f), new Vector2(0f, -70f), new Vector2(700f, 70f), 40, TextAnchor.MiddleCenter, Palette.Ink);
            airText = AnchoredLabel(hudRoot, new Vector2(0.5f, 1f), new Vector2(0f, -130f), new Vector2(400f, 60f), 36, TextAnchor.MiddleCenter, Palette.WallLine);
            BuildLife();
            BuildRipple();
            surveyText = AnchoredLabel(hudRoot, new Vector2(0.5f, 1f), new Vector2(0f, -430f), new Vector2(700f, 50f), 30, TextAnchor.MiddleCenter, Palette.Ink);
            var bar = UiKit.Box(hudRoot, "SurveyBar", new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -480f), new Vector2(760f, 18f), new Color(0.08f, 0.1f, 0.1f, 0.8f), false);
            var fill = UiKit.Box(bar, "Fill", new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), Vector2.zero, new Vector2(760f, 18f), Palette.Teal, false);
            surveyFill = fill.GetComponent<Image>();
            surveyFill.type = Image.Type.Filled;
            surveyFill.fillMethod = Image.FillMethod.Horizontal;
            moduleText = AnchoredLabel(hudRoot, new Vector2(0f, 0.5f), new Vector2(180f, 80f), new Vector2(320f, 360f), 28, TextAnchor.LowerLeft, Palette.WallLine);
            toastText = AnchoredLabel(hudRoot, new Vector2(0.5f, 0.62f), Vector2.zero, new Vector2(800f, 80f), 36, TextAnchor.MiddleCenter, Palette.Drum);
            toastText.text = "";
            tutorialText = AnchoredLabel(hudRoot, new Vector2(0.5f, 0.58f), new Vector2(0f, -40f), new Vector2(860f, 160f), 32, TextAnchor.MiddleCenter, Palette.Ink);
            BuildStick(left);
            BuildPing(left);
            BuildBreath(left);
            UiKit.MakeButton(hudRoot, "Pause", "暂停", left ? new Vector2(1f, 1f) : new Vector2(0f, 1f), left ? new Vector2(-120f, -250f) : new Vector2(120f, -250f), new Vector2(180f, 90f), TogglePause);
            draftRoot = UiKit.Panel(hudRoot, "Draft", new Color(0.02f, 0.03f, 0.034f, 0.94f));
            draftRoot.gameObject.SetActive(false);
            pauseRoot = UiKit.Panel(hudRoot, "Pause", new Color(0.02f, 0.03f, 0.034f, 0.92f));
            pauseRoot.gameObject.SetActive(false);
            var pauseTitle = UiKit.Label(pauseRoot, "测绘已暂停\n进度写在这台设备上", 48, TextAnchor.MiddleCenter, Palette.Ink);
            pauseTitle.rectTransform.anchorMin = new Vector2(0.1f, 0.62f);
            pauseTitle.rectTransform.anchorMax = new Vector2(0.9f, 0.82f);
            pauseTitle.rectTransform.offsetMin = Vector2.zero;
            pauseTitle.rectTransform.offsetMax = Vector2.zero;
            UiKit.MakeButton(pauseRoot, "Continue", "继续测绘", new Vector2(0.5f, 0.5f), new Vector2(0f, 40f), new Vector2(560f, 130f), Continue);
            UiKit.MakeButton(pauseRoot, "Abort", "放弃并回测稿室", new Vector2(0.5f, 0.5f), new Vector2(0f, -140f), new Vector2(560f, 110f), Abort);
            endRoot = UiKit.Panel(hudRoot, "End", new Color(0.015f, 0.02f, 0.024f, 0.96f));
            endRoot.gameObject.SetActive(false);
            endLines = UiKit.Label(endRoot, "", 40, TextAnchor.MiddleCenter, Palette.Ink);
            endLines.rectTransform.anchorMin = new Vector2(0.08f, 0.48f);
            endLines.rectTransform.anchorMax = new Vector2(0.92f, 0.9f);
            endLines.rectTransform.offsetMin = Vector2.zero;
            endLines.rectTransform.offsetMax = Vector2.zero;
            retryButton = UiKit.MakeButton(endRoot, "Retry", "就地重测", new Vector2(0.5f, 0.5f), new Vector2(0f, -80f), new Vector2(640f, 120f), DoRetry);
            UiKit.MakeButton(endRoot, "Office", "回到测稿室", new Vector2(0.5f, 0.5f), new Vector2(0f, -240f), new Vector2(640f, 120f), ReturnOffice);
        }

        void BuildLife()
        {
            for (int i = 0; i < 3; i++)
            {
                var pip = UiKit.Box(hudRoot, "Life" + i, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(-80f + i * 80f, -190f), new Vector2(48f, 28f), Palette.Drum, false);
                lifePips[i] = pip.GetComponent<Image>();
                lifePips[i].type = Image.Type.Filled;
                lifePips[i].fillMethod = Image.FillMethod.Horizontal;
            }
        }

        void BuildRipple()
        {
            var anchor = UiKit.Box(hudRoot, "RippleAnchor", new Vector2(0.5f, 1f), new Vector2(0.5f, 0.5f), new Vector2(0f, -310f), new Vector2(180f, 180f), new Color(0f, 0f, 0f, 0f), false);
            for (int i = 0; i < 3; i++)
            {
                var ring = UiKit.Box(anchor, "Ring" + i, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(150f, 150f), Palette.Teal, false);
                var image = ring.GetComponent<Image>();
                image.sprite = SpriteBank.Ring();
                image.type = Image.Type.Simple;
                ripples[i] = image;
            }
            tierText = AnchoredLabel(hudRoot, new Vector2(0.5f, 1f), new Vector2(0f, -410f), new Vector2(240f, 40f), 28, TextAnchor.MiddleCenter, Palette.Ink);
        }

        void BuildStick(bool left)
        {
            Vector2 anchor = left ? new Vector2(1f, 0f) : new Vector2(0f, 0f);
            float x = left ? -260f : 260f;
            var back = UiKit.Box(hudRoot, "Stick", anchor, new Vector2(0.5f, 0.5f), new Vector2(x, 280f), new Vector2(460f, 460f), new Color(0.1f, 0.16f, 0.17f, 0.45f), true);
            var knob = UiKit.Box(back, "Knob", new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(160f, 160f), Palette.Teal, false);
            stick = back.gameObject.AddComponent<TouchStick>();
            stick.Bind(back, knob);
        }

        void BuildPing(bool left)
        {
            Vector2 anchor = left ? new Vector2(0f, 0f) : new Vector2(1f, 0f);
            float x = left ? 230f : -230f;
            var button = UiKit.Box(hudRoot, "Ping", anchor, new Vector2(0.5f, 0.5f), new Vector2(x, 250f), new Vector2(300f, 300f), Palette.Drum, true);
            pingFill = button.GetComponent<Image>();
            pingFill.type = Image.Type.Filled;
            pingFill.fillMethod = Image.FillMethod.Radial360;
            pingFill.fillOrigin = (int)Image.Origin360.Top;
            pingFill.fillAmount = 1f;
            var label = UiKit.Label(button, "拍声", 56, TextAnchor.MiddleCenter, new Color(0.12f, 0.08f, 0.05f, 1f));
            label.rectTransform.offsetMin = Vector2.zero;
            label.rectTransform.offsetMax = Vector2.zero;
            pingCatch = button.gameObject.AddComponent<PressCatch>();
        }

        void BuildBreath(bool left)
        {
            Vector2 anchor = left ? new Vector2(0f, 1f) : new Vector2(1f, 1f);
            float x = left ? 160f : -160f;
            var button = UiKit.Box(hudRoot, "Breath", anchor, new Vector2(0.5f, 0.5f), new Vector2(x, -200f), new Vector2(220f, 220f), new Color(0.16f, 0.28f, 0.3f, 0.95f), true);
            breathFill = button.GetComponent<Image>();
            breathFill.type = Image.Type.Filled;
            breathFill.fillMethod = Image.FillMethod.Radial360;
            breathFill.fillOrigin = (int)Image.Origin360.Top;
            var label = UiKit.Label(button, "憋声", 42, TextAnchor.MiddleCenter, Palette.Ink);
            label.rectTransform.offsetMin = Vector2.zero;
            label.rectTransform.offsetMax = Vector2.zero;
            breathCatch = button.gameObject.AddComponent<PressCatch>();
        }

        Text AnchoredLabel(Transform parent, Vector2 anchor, Vector2 pos, Vector2 size, int font, TextAnchor align, Color color)
        {
            var rect = UiKit.Box(parent, "T", anchor, new Vector2(0.5f, 0.5f), pos, size, new Color(0f, 0f, 0f, 0f), false);
            var label = UiKit.Label(rect, "", font, align, color);
            label.rectTransform.offsetMin = Vector2.zero;
            label.rectTransform.offsetMax = Vector2.zero;
            return label;
        }

        void RefreshHud()
        {
            if (airText == null || run == null) return;
            int sec = Mathf.CeilToInt(Mathf.Max(0f, run.Air));
            airText.text = (sec / 60).ToString("00") + ":" + (sec % 60).ToString("00");
            airText.color = sec < 60 ? (Color)Palette.Warn : (Color)Palette.WallLine;
            roomText.text = run.RoomIndex >= 4 ? "未命名厅" : "水道 · 第 " + (run.RoomIndex + 1) + " / 4 室";
            float life = Mathf.Clamp(run.Life, 0f, 3f);
            for (int i = 0; i < 3; i++)
            {
                float fill = Mathf.Clamp01(life - i);
                lifePips[i].fillAmount = fill;
                lifePips[i].color = fill > 0.01f ? Palette.Drum : new Color(0.25f, 0.2f, 0.16f, 0.8f);
            }
            float s = Mathf.Clamp01(run.S / 100f);
            Color ripple = s < 0.4f ? Palette.Teal : (s < 0.8f ? Palette.Drum : Palette.Warn);
            float spin = 20f + s * 140f;
            for (int i = 0; i < ripples.Length; i++)
            {
                float wave = 0.55f + 0.45f * Mathf.Sin(Time.unscaledTime * spin * 0.05f + i);
                ripples[i].color = new Color(ripple.r, ripple.g, ripple.b, 0.25f + s * 0.55f);
                ripples[i].rectTransform.localScale = Vector3.one * (0.45f + i * 0.22f + wave * s * 0.15f);
                ripples[i].rectTransform.localRotation = Quaternion.Euler(0f, 0f, Time.unscaledTime * (spin + i * 15f));
            }
            tierText.text = Hints.TierName(run.S);
            tierText.color = ripple;
            if (!run.Current.Boss)
            {
                float score = run.Current.SurveyScore();
                float threshold = ModuleBook.Threshold(run);
                surveyFill.fillAmount = Mathf.Clamp01(score / 100f);
                surveyText.text = run.Current.Cleared ? "闸门已开" : "测绘 " + Mathf.FloorToInt(score) + " / " + Mathf.CeilToInt(threshold);
            }
            else
            {
                int hits = 0;
                for (int i = 0; i < run.Current.Actors.Count; i++)
                    if (run.Current.Actors[i].Kind == ActorKind.Boss) hits = run.Current.Actors[i].WeakPoints;
                surveyFill.fillAmount = hits / 3f;
                surveyText.text = "对图者弱点 " + hits + " / 3";
            }
            pingFill.fillAmount = run.PingCd <= 0f ? 1f : 1f - Mathf.Clamp01(run.PingCd / Rules.PingCooldown);
            float breathMax = ModuleBook.BreathCooldown(run);
            breathFill.fillAmount = run.Breath > 0f ? 1f : (run.BreathCd <= 0f ? 1f : 1f - Mathf.Clamp01(run.BreathCd / breathMax));
            breathFill.color = run.Breath > 0f ? Palette.Drum : new Color(0.16f, 0.28f, 0.3f, 0.95f);
            moduleText.text = ModuleLine();
            if (run.Tutorial && run.TutorialTime < 30f && !endShown && !run.DraftOpen && !run.MenuPause)
            {
                tutorialText.gameObject.SetActive(true);
                if (run.ManualPings == 0) tutorialText.text = "点右下「拍声」。脚边只有一小圈余光，用来别撞墙，看不见东西。";
                else if (run.TutorialTime < 18f) tutorialText.text = "壁苔贴在没测绘的墙上。这一拍会打到它。亮了才受伤。";
                else tutorialText.text = "测绘到七成，闸门就开。少拍也可以过关。";
            }
            else tutorialText.gameObject.SetActive(false);
        }

        string ModuleLine()
        {
            string text = "";
            for (int i = 0; i < 12; i++)
            {
                if (run.Levels[i] <= 0) continue;
                if (text.Length > 0) text += "\n";
                text += ModuleBook.Name((ModuleId)i) + " " + run.Levels[i];
            }
            if (text.Length == 0) text = "还没有模组";
            return text;
        }

        void Toast(string text)
        {
            toastText.text = text;
            toastTimer = 1.5f;
        }

        void TickToast()
        {
            if (toastTimer <= 0f)
            {
                if (toastText != null) toastText.text = "";
                return;
            }
            toastTimer -= Time.unscaledDeltaTime;
        }

        void Autosave()
        {
            if (run.Finished || endShown) return;
            saveTimer += Time.unscaledDeltaTime;
            if (saveTimer < 2f) return;
            saveTimer = 0f;
            SaveGame.WriteActive(save, run);
        }

        void ShowDraft()
        {
            if (draftRoot == null) return;
            Time.timeScale = 0f;
            for (int i = draftRoot.childCount - 1; i >= 0; i--) Destroy(draftRoot.GetChild(i).gameObject);
            draftRoot.gameObject.SetActive(true);
            var title = UiKit.Label(draftRoot, "测稿节点　选一张", 48, TextAnchor.MiddleCenter, Palette.Drum);
            title.rectTransform.anchorMin = new Vector2(0.08f, 0.86f);
            title.rectTransform.anchorMax = new Vector2(0.92f, 0.96f);
            title.rectTransform.offsetMin = Vector2.zero;
            title.rectTransform.offsetMax = Vector2.zero;
            int n = run.Offer == null ? 0 : run.Offer.Length;
            for (int i = 0; i < n; i++)
            {
                int index = i;
                int id = run.Offer[i];
                int next = run.Levels[id] + 1;
                string caption = ModuleBook.Name((ModuleId)id) + "　" + next + " 级\n" + ModuleBook.Describe((ModuleId)id, next);
                if (ModuleBook.WarnsDisturbance((ModuleId)id)) caption += "\n警告：" + ModuleBook.Warning();
                float y = 280f - i * 280f;
                var button = UiKit.MakeButton(draftRoot, "Card" + i, caption, new Vector2(0.5f, 0.5f), new Vector2(0f, y), new Vector2(860f, 250f), () => Pick(index));
                var image = button.GetComponent<Image>();
                if (ModuleBook.WarnsDisturbance((ModuleId)id) && image != null) image.color = new Color(0.45f, 0.18f, 0.14f, 1f);
                Text label = button.GetComponentInChildren<Text>();
                if (label != null)
                {
                    label.fontSize = 32;
                    label.alignment = TextAnchor.MiddleCenter;
                }
            }
        }

        void Pick(int index)
        {
            audio.Tick();
            Simulator.Choose(run, index);
            if (run.DraftOpen) ShowDraft();
            else
            {
                draftRoot.gameObject.SetActive(false);
                if (!run.MenuPause && !run.Finished) Time.timeScale = 1f;
            }
            SaveGame.WriteActive(save, run);
        }

        void TogglePause()
        {
            if (endShown || run.Finished) return;
            if (run.MenuPause) Continue();
            else
            {
                run.MenuPause = true;
                Time.timeScale = 0f;
                SaveGame.WriteActive(save, run);
                ShowPause();
            }
        }

        void ShowPause()
        {
            if (pauseRoot == null || endShown) return;
            pauseRoot.gameObject.SetActive(true);
            Time.timeScale = 0f;
        }

        void Continue()
        {
            run.MenuPause = false;
            if (pauseRoot != null) pauseRoot.gameObject.SetActive(false);
            if (!run.DraftOpen && !run.Finished) Time.timeScale = 1f;
            SaveGame.WriteActive(save, run);
        }

        void Abort()
        {
            Time.timeScale = 1f;
            SaveGame.ClearActive(save);
            SceneManager.LoadScene("SurveyOffice");
        }

        void ShowEnd()
        {
            endShown = true;
            Time.timeScale = 0f;
            if (draftRoot != null) draftRoot.gameObject.SetActive(false);
            if (pauseRoot != null) pauseRoot.gameObject.SetActive(false);
            if (run.Victory)
            {
                save.successes++;
                int now = ModuleBook.UnlockedCount(save.successes);
                unlockAtStart = now;
            }
            SaveGame.WriteResidual(save, run);
            string head = run.Victory ? "结果：" + Causes.Clear : "死因：" + run.Cause;
            string extra = "";
            if (run.Victory)
            {
                int pool = ModuleBook.UnlockedCount(save.successes);
                extra = "\n\n模组池 " + pool + " / 12";
                if (save.successes > 0 && save.successes % 3 == 0 && pool < 12)
                    extra += "。新的一张已放进池里。";
                else if (pool >= 12) extra += "。池子已经满了。";
            }
            endLines.text = head + "\n\n模组：" + Hints.Combo(run) + "\n\n下一局：" + Hints.Next(run) + extra;
            bool retry = Simulator.CanRetry(run);
            retryButton.gameObject.SetActive(retry);
            if (retry)
            {
                Text label = retryButton.GetComponentInChildren<Text>();
                if (label != null) label.text = "就地重测\n空气回到一半，模组还在。本局仅此一次。";
            }
            endRoot.gameObject.SetActive(true);
        }

        void DoRetry()
        {
            if (!Simulator.CanRetry(run)) return;
            Simulator.Retry(run);
            endShown = false;
            endRoot.gameObject.SetActive(false);
            Time.timeScale = 1f;
            save.hasActive = 1;
            SaveGame.WriteActive(save, run);
            Toast("就地重测");
        }

        void ReturnOffice()
        {
            Time.timeScale = 1f;
            SceneManager.LoadScene("SurveyOffice");
        }
    }
}
