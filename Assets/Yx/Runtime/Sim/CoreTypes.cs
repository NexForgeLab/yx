using System;

namespace Yx.Sim
{
    public struct Vec2
    {
        public float X;
        public float Y;

        public Vec2(float x, float y)
        {
            X = x;
            Y = y;
        }

        public float Length
        {
            get { return (float)Math.Sqrt(X * X + Y * Y); }
        }

        public Vec2 Normalized
        {
            get
            {
                float l = Length;
                if (l < 0.0001f) return new Vec2(0f, 1f);
                return new Vec2(X / l, Y / l);
            }
        }

        public static float Dot(Vec2 a, Vec2 b)
        {
            return a.X * b.X + a.Y * b.Y;
        }

        public static float Dist(Vec2 a, Vec2 b)
        {
            float dx = a.X - b.X;
            float dy = a.Y - b.Y;
            return (float)Math.Sqrt(dx * dx + dy * dy);
        }

        public static Vec2 operator +(Vec2 a, Vec2 b) { return new Vec2(a.X + b.X, a.Y + b.Y); }
        public static Vec2 operator -(Vec2 a, Vec2 b) { return new Vec2(a.X - b.X, a.Y - b.Y); }
        public static Vec2 operator *(Vec2 a, float s) { return new Vec2(a.X * s, a.Y * s); }
    }

    public struct Rng
    {
        public uint State;

        public Rng(uint seed)
        {
            State = seed == 0 ? 1u : seed;
        }

        public uint NextU()
        {
            uint x = State;
            x ^= x << 13;
            x ^= x >> 17;
            x ^= x << 5;
            State = x == 0 ? 1u : x;
            return State;
        }

        public int NextInt(int count)
        {
            if (count <= 1) return 0;
            return (int)(NextU() % (uint)count);
        }

        public float NextFloat()
        {
            return (NextU() & 0xFFFFFF) / (float)0x1000000;
        }
    }

    public static class Rules
    {
        public const float PingCooldown = 1.4f;
        public const float BaseRadius = 4.5f;
        public const float BaseAfterglow = 0.9f;
        public const float SelfGlow = 1.2f;
        public const float BaseBreath = 2f;
        public const float BaseBreathCd = 8f;
        public const float PlayerSpeed = 3.55f;
        public const float PlayerRadius = 0.28f;
        public const float SurveyOpen = 70f;
        public const float SurveyMin = 55f;
        public const float DisturbQuiet = 40f;
        public const float DisturbAwake = 80f;
        public const int EffectivePerDraft = 8;
        public const int StartModules = 6;
    }

    public static class Causes
    {
        public const string Air = "空气耗尽";
        public const string Deep = "被深物追上";
        public const string Spike = "撞上未照亮的壁障";
        public const string Fish = "被寻声鱼咬到";
        public const string Boss = "被对图者撞上";
        public const string Chaos = "乱响反噬";
        public const string Moss = "壁苔蹭伤";
        public const string Clear = "测稿完成";
    }

    public enum ModuleId
    {
        ChangMing = 0,
        XiXian = 1,
        ErCi = 2,
        YaBu = 3,
        CanXiang = 4,
        CeShen = 5,
        FanXiang = 6,
        QunZao = 7,
        ChenDi = 8,
        HuiLang = 9,
        JiPu = 10,
        DuiKong = 11
    }

    public enum CellKind : byte
    {
        Void = 0,
        Floor = 1,
        Water = 2,
        Wall = 3,
        Spike = 4,
        Gate = 5
    }

    public enum ActorKind : byte
    {
        Moss = 0,
        Fish = 1,
        Cavity = 2,
        Deep = 3,
        Boss = 4
    }

    public struct SimInput
    {
        public float X;
        public float Y;
        public bool Ping;
        public bool Breath;
    }

    public sealed class SimEvents
    {
        public bool Pinged;
        public bool Hit;
        public bool Chaos;
        public bool GateOpened;
        public bool DraftOpened;
        public bool Died;
        public bool Won;
        public bool BreathStarted;
        public bool RoomChanged;
        public bool Damaged;
        public bool Awakened;
        public bool TutorialHit;

        public void Clear()
        {
            Pinged = false;
            Hit = false;
            Chaos = false;
            GateOpened = false;
            DraftOpened = false;
            Died = false;
            Won = false;
            BreathStarted = false;
            RoomChanged = false;
            Damaged = false;
            Awakened = false;
            TutorialHit = false;
        }

        public void Absorb(SimEvents o)
        {
            Pinged |= o.Pinged;
            Hit |= o.Hit;
            Chaos |= o.Chaos;
            GateOpened |= o.GateOpened;
            DraftOpened |= o.DraftOpened;
            Died |= o.Died;
            Won |= o.Won;
            BreathStarted |= o.BreathStarted;
            RoomChanged |= o.RoomChanged;
            Damaged |= o.Damaged;
            Awakened |= o.Awakened;
            TutorialHit |= o.TutorialHit;
        }
    }
}
