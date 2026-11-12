using System;

namespace Yx.Sim
{
    public static class ModuleBook
    {
        public static readonly ModuleId[] UnlockOrder = new[]
        {
            ModuleId.ChangMing,
            ModuleId.XiXian,
            ModuleId.YaBu,
            ModuleId.CanXiang,
            ModuleId.FanXiang,
            ModuleId.JiPu,
            ModuleId.ErCi,
            ModuleId.CeShen,
            ModuleId.QunZao,
            ModuleId.ChenDi,
            ModuleId.HuiLang,
            ModuleId.DuiKong
        };

        public static int UnlockedCount(int successes)
        {
            int n = Rules.StartModules + successes / 3;
            if (n > 12) n = 12;
            if (n < Rules.StartModules) n = Rules.StartModules;
            return n;
        }

        public static bool IsUnlocked(int unlockedCount, ModuleId id)
        {
            int idx = Array.IndexOf(UnlockOrder, id);
            return idx >= 0 && idx < unlockedCount;
        }

        public static string Name(ModuleId id)
        {
            switch (id)
            {
                case ModuleId.ChangMing: return "长鸣";
                case ModuleId.XiXian: return "细线";
                case ModuleId.ErCi: return "二次回声";
                case ModuleId.YaBu: return "哑步";
                case ModuleId.CanXiang: return "残响";
                case ModuleId.CeShen: return "测深";
                case ModuleId.FanXiang: return "反相";
                case ModuleId.QunZao: return "群噪";
                case ModuleId.ChenDi: return "沉底";
                case ModuleId.HuiLang: return "回廊";
                case ModuleId.JiPu: return "记谱";
                case ModuleId.DuiKong: return "对空";
                default: return "模组";
            }
        }

        public static bool WarnsDisturbance(ModuleId id)
        {
            return id == ModuleId.QunZao;
        }

        public static string Warning()
        {
            return "扰动会涨得很快";
        }

        public static string Describe(ModuleId id, int nextLevel)
        {
            int lv = nextLevel < 1 ? 1 : (nextLevel > 3 ? 3 : nextLevel);
            switch (id)
            {
                case ModuleId.ChangMing:
                    return "余辉 +" + (0.5f * lv).ToString("0.0") + " 秒。每次拍声扰动额外 +" + (3 * lv) + "。";
                case ModuleId.XiXian:
                    return "改成 60° 窄扇，半径 +" + (1.5f * lv).ToString("0.0") + " 格。扰动改为 +6。";
                case ModuleId.ErCi:
                    if (lv == 1) return "命中墙面后折一次，折线半径 2.5 格。";
                    if (lv == 2) return "命中墙面后折两次，折线半径 2.5 格。";
                    return "折两次。折线打到生物时，再在它身上扩一圈。";
                case ModuleId.YaBu:
                    return "憋声延长到 " + (2 + lv) + " 秒，冷却减 " + lv + " 秒。";
                case ModuleId.CanXiang:
                    return "拍声处留下余波，2 秒后引爆，伤害一段。扰动 +4。" + (lv == 1 ? "" : lv == 2 ? " 余波半径 5.2 格。" : " 余波半径 6 格。");
                case ModuleId.CeShen:
                    int pct = lv == 1 ? 30 : (lv == 2 ? 50 : 70);
                    return "余辉期内，挨着已测绘格的暗格以 " + pct + "% 显出轮廓。轮廓不算完成度。";
                case ModuleId.FanXiang:
                    int air = lv == 1 ? 4 : (lv == 2 ? 6 : 8);
                    return "在已测绘格里拍声，回复空气 " + air + " 秒。这一拍对已测绘格上的生物伤害减半。";
                case ModuleId.QunZao:
                    string gap = lv == 1 ? "2.0" : (lv == 2 ? "1.6" : "1.2");
                    return "每 " + gap + " 秒自动弱响一次，半径 2 格，伤害一半，扰动 +5。这是唯一会自己拍的模组。";
                case ModuleId.ChenDi:
                    return "拍声时自己被锚定 0.25 秒。伤害 +" + lv + " 段，扰动 +2。";
                case ModuleId.HuiLang:
                    return "窄走廊里，余辉沿水道再延伸 " + (lv + 1) + " 格。";
                case ModuleId.JiPu:
                    return "本局每选一张模组，开闸门槛 -3%，最低 55%。扰动的基础消耗不变。";
                case ModuleId.DuiKong:
                    string extra = lv == 1 ? "声呐范围内" : (lv == 2 ? "声呐外再 1 格" : "声呐外再 2 格");
                    return "拍声震落顶上的水滴，" + extra + " 的生物受 1 段伤害。水滴不增加扰动。";
                default:
                    return "";
            }
        }

        public static int Lv(RunState run, ModuleId id)
        {
            return run.Levels[(int)id];
        }

        public static float Afterglow(RunState run)
        {
            return Rules.BaseAfterglow + 0.5f * Lv(run, ModuleId.ChangMing);
        }

        public static float Radius(RunState run)
        {
            float r = Rules.BaseRadius;
            int fan = Lv(run, ModuleId.XiXian);
            if (fan > 0) r += 1.5f * fan;
            return r;
        }

        public static bool UseFan(RunState run)
        {
            return Lv(run, ModuleId.XiXian) > 0;
        }

        public static float ManualDisturbance(RunState run)
        {
            float s = Lv(run, ModuleId.XiXian) > 0 ? 6f : 10f;
            s += 3f * Lv(run, ModuleId.ChangMing);
            if (Lv(run, ModuleId.ChenDi) > 0) s += 2f;
            if (Lv(run, ModuleId.CanXiang) > 0) s += 4f;
            return s;
        }

        public static float Threshold(RunState run)
        {
            float t = Rules.SurveyOpen - 3f * run.Picks;
            if (t < Rules.SurveyMin) t = Rules.SurveyMin;
            return t;
        }

        public static float BreathDuration(RunState run)
        {
            int lv = Lv(run, ModuleId.YaBu);
            if (lv <= 0) return Rules.BaseBreath;
            return 2f + lv;
        }

        public static float BreathCooldown(RunState run)
        {
            int lv = Lv(run, ModuleId.YaBu);
            float cd = Rules.BaseBreathCd - lv;
            if (cd < 1f) cd = 1f;
            return cd;
        }

        public static float ManualDamage(RunState run)
        {
            float d = 1f;
            int sink = Lv(run, ModuleId.ChenDi);
            if (sink > 0) d += sink;
            return d;
        }

        public static float BlastRadius(RunState run)
        {
            int lv = Lv(run, ModuleId.CanXiang);
            if (lv >= 3) return 6f;
            if (lv == 2) return 5.2f;
            return Rules.BaseRadius;
        }

        public static float AutoInterval(RunState run)
        {
            int lv = Lv(run, ModuleId.QunZao);
            if (lv >= 3) return 1.2f;
            if (lv == 2) return 1.6f;
            return 2f;
        }

        public static int CorridorExtra(RunState run)
        {
            int lv = Lv(run, ModuleId.HuiLang);
            if (lv <= 0) return 0;
            return lv + 1;
        }

        public static float DepthAlpha(RunState run)
        {
            int lv = Lv(run, ModuleId.CeShen);
            if (lv >= 3) return 0.7f;
            if (lv == 2) return 0.5f;
            if (lv == 1) return 0.3f;
            return 0f;
        }

        public static float AirRefund(RunState run)
        {
            int lv = Lv(run, ModuleId.FanXiang);
            if (lv >= 3) return 8f;
            if (lv == 2) return 6f;
            if (lv == 1) return 4f;
            return 0f;
        }
    }

    public static class Hints
    {
        public static string Combo(RunState run)
        {
            int bestA = -1;
            int bestB = -1;
            int scoreA = -1;
            int scoreB = -1;
            for (int i = 0; i < 12; i++)
            {
                if (run.Levels[i] <= 0 && run.UseScore[i] <= 0) continue;
                int score = run.UseScore[i] * 10 + run.Levels[i];
                if (score > scoreA)
                {
                    scoreB = scoreA;
                    bestB = bestA;
                    scoreA = score;
                    bestA = i;
                }
                else if (score > scoreB)
                {
                    scoreB = score;
                    bestB = i;
                }
            }
            if (bestA < 0) return "这一局没有装模组";
            if (bestB < 0) return ModuleBook.Name((ModuleId)bestA);
            return ModuleBook.Name((ModuleId)bestA) + " · " + ModuleBook.Name((ModuleId)bestB);
        }

        public static string Next(RunState run)
        {
            if (run.Victory) return "下一局可以少拍。闸门到七成就开。";
            if (run.Cause == Causes.Air) return "少在全黑里停留。余辉里的格子，空气走得慢。";
            if (run.Cause == Causes.Deep) return "试一张哑步。苏醒之后，先别继续拍。";
            if (run.Cause == Causes.Spike) return "尖壁没被拍亮之前，不要靠上去。";
            if (run.Cause == Causes.Fish) return "警醒之后它们追声。拍完就换个位置。";
            if (run.Cause == Causes.Boss) return "对图者冲的是声源。拍完立刻侧移，或先憋声。";
            if (run.Cause == Causes.Chaos) return "扰动溢出会伤到自己。进了已测绘的格子，让它自己落下去。";
            if (run.Cause == Causes.Moss) return "壁苔要两下才扣一格，别贴着它蹭。";
            if (run.Levels[(int)ModuleId.YaBu] == 0 && run.MaxS >= Rules.DisturbQuiet)
                return "试一张哑步，少拍也能过闸。";
            return "顺着路拍一条就够。不必见影就打。";
        }

        public static string TierName(float s)
        {
            if (s >= 100f) return "乱响";
            if (s >= Rules.DisturbAwake) return "苏醒";
            if (s >= Rules.DisturbQuiet) return "警醒";
            return "安静";
        }

        public static string ResidualTier(float maxS, bool chaos)
        {
            if (chaos || maxS >= 100f) return "曾经乱响";
            return TierName(maxS);
        }
    }
}
