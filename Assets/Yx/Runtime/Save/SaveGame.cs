using System;
using System.IO;
using UnityEngine;
using Yx.Sim;

namespace Yx.Game
{
    [Serializable]
    public class SaveEnvelope
    {
        public int version = 1;
        public float airSeconds = 600f;
        public int vibration = 1;
        public int leftHand = 0;
        public int particles = 1;
        public int lowQuality = 0;
        public int successes;
        public int runsStarted;
        public int hasResidual;
        public int resRoom;
        public float resMaxS;
        public int resChaos;
        public int resSuccess;
        public string resCause = "";
        public int hasActive;
        public string active = "";
    }

    public static class SaveGame
    {
        public const string FileName = "yuxiang_save.json";

        public static string PathFor()
        {
            return Path.Combine(Application.persistentDataPath, FileName);
        }

        public static SaveEnvelope Load()
        {
            try
            {
                string path = PathFor();
                if (!File.Exists(path)) return new SaveEnvelope();
                string json = File.ReadAllText(path);
                SaveEnvelope data = JsonUtility.FromJson<SaveEnvelope>(json);
                if (data == null) return new SaveEnvelope();
                if (data.airSeconds != 480f && data.airSeconds != 600f && data.airSeconds != 720f)
                    data.airSeconds = 600f;
                return data;
            }
            catch (Exception e)
            {
                Debug.LogWarning("余响测绘存档读取失败，使用新档。" + e.Message);
                return new SaveEnvelope();
            }
        }

        public static void Store(SaveEnvelope data)
        {
            try
            {
                string path = PathFor();
                string dir = Path.GetDirectoryName(path);
                if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir)) Directory.CreateDirectory(dir);
                File.WriteAllText(path, JsonUtility.ToJson(data));
            }
            catch (Exception e)
            {
                Debug.LogWarning("余响测绘存档写入失败。" + e.Message);
            }
        }

        public static void WriteActive(SaveEnvelope data, RunState run)
        {
            data.hasActive = 1;
            RunSnapshot snap = Snapshots.Capture(run);
            data.active = JsonUtility.ToJson(snap);
            Store(data);
        }

        public static RunState ReadActive(SaveEnvelope data)
        {
            if (data == null || data.hasActive == 0 || string.IsNullOrEmpty(data.active)) return null;
            RunSnapshot snap = JsonUtility.FromJson<RunSnapshot>(data.active);
            if (snap == null || snap.rooms == null || snap.rooms.Length == 0) return null;
            return Snapshots.Restore(snap);
        }

        public static void ClearActive(SaveEnvelope data)
        {
            data.hasActive = 0;
            data.active = "";
            Store(data);
        }

        public static void WriteResidual(SaveEnvelope data, RunState run)
        {
            data.hasResidual = 1;
            data.resRoom = run.RoomIndex;
            data.resMaxS = run.MaxS;
            data.resChaos = run.ChaosHappened ? 1 : 0;
            data.resSuccess = run.Victory ? 1 : 0;
            data.resCause = string.IsNullOrEmpty(run.Cause) ? (run.Victory ? Causes.Clear : "") : run.Cause;
            data.hasActive = 0;
            data.active = "";
            Store(data);
        }

        public static string RoomLabel(int roomIndex)
        {
            if (roomIndex >= 4) return "未命名厅";
            return "第 " + (roomIndex + 1) + " 室";
        }
    }
}
