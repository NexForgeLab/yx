using System;
using System.Collections.Generic;

namespace Yx.Sim
{
    public struct Cell
    {
        public CellKind Kind;
        public bool Surveyed;
    }

    public sealed class Actor
    {
        public int Id;
        public ActorKind Kind;
        public float X;
        public float Y;
        public float HomeX;
        public float HomeY;
        public float Hp;
        public int WallX;
        public int WallY;
        public float BumpX;
        public float BumpY;
        public bool Marked;
        public bool Alive = true;
        public float Expose;
        public bool Covered;
        public bool ProximityUsed;
        public float WanderT;
        public float WanderX;
        public float WanderY;
        public float Accel;
        public int WeakPoints;
        public bool Charging;
        public float ChargeT;
        public float ChargeFromX;
        public float ChargeFromY;
        public float ChargeToX;
        public float ChargeToY;
        public bool TutorialMoss;
        public bool HitThisCast;
    }

    public sealed class Blast
    {
        public float X;
        public float Y;
        public float Time;
        public float Radius;
    }

    public sealed class Droplet
    {
        public float Time;
        public int ActorId;
        public float X;
        public float Y;
    }

    public sealed class Room
    {
        public int W;
        public int H;
        public Cell[] Cells;
        public char[] Marks;
        public float[] Glow;
        public bool Boss;
        public bool Cleared;
        public bool Drafted;
        public bool GateFound;
        public float SpawnX;
        public float SpawnY;
        public int ChamberCount;
        public string BuiltFrom = "";
        public List<Actor> Actors = new List<Actor>();

        public bool In(int x, int y)
        {
            return x >= 0 && y >= 0 && x < W && y < H;
        }

        public int Index(int x, int y)
        {
            return y * W + x;
        }

        public Cell Get(int x, int y)
        {
            return Cells[Index(x, y)];
        }

        public bool Walkable(int x, int y)
        {
            if (!In(x, y)) return false;
            Cell c = Cells[Index(x, y)];
            if (c.Kind == CellKind.Floor || c.Kind == CellKind.Water) return true;
            if (c.Kind == CellKind.Gate && Cleared) return true;
            return false;
        }

        public bool Solid(int x, int y)
        {
            return !Walkable(x, y);
        }

        public float SurveyScore()
        {
            int walls = 0;
            int lit = 0;
            for (int i = 0; i < Cells.Length; i++)
            {
                CellKind k = Cells[i].Kind;
                if (k != CellKind.Wall && k != CellKind.Spike) continue;
                walls++;
                if (Cells[i].Surveyed) lit++;
            }
            int enemies = 0;
            int marked = 0;
            for (int i = 0; i < Actors.Count; i++)
            {
                Actor a = Actors[i];
                if (a.Kind == ActorKind.Deep || a.Kind == ActorKind.Boss) continue;
                enemies++;
                if (a.Marked || !a.Alive) marked++;
            }
            float wallW = 75f;
            float enemyW = 15f;
            float gateW = 10f;
            if (enemies == 0)
            {
                wallW = 90f;
                enemyW = 0f;
            }
            float score = 0f;
            if (walls > 0) score += wallW * lit / walls;
            if (enemies > 0) score += enemyW * marked / enemies;
            if (GateFound) score += gateW;
            return score;
        }
    }

    public sealed class RunState
    {
        public int Seed;
        public Rng Rng;
        public int UnlockedCount = Rules.StartModules;
        public List<Room> Rooms = new List<Room>();
        public int RoomIndex;
        public float PlayerX;
        public float PlayerY;
        public float FaceX;
        public float FaceY = 1f;
        public float S;
        public float MaxS;
        public bool ChaosHappened;
        public float Air;
        public float MaxAir = 600f;
        public float Life = 3f;
        public float Invuln;
        public float PingCd;
        public float Breath;
        public float BreathCd;
        public float Anchor;
        public float Chaos;
        public int Picks;
        public int Effective;
        public int DraftQueue;
        public bool DraftOpen;
        public int[] Offer = new int[0];
        public int[] Levels = new int[12];
        public int[] UseScore = new int[12];
        public bool Tutorial;
        public bool TutorialMossPending;
        public float TutorialTime;
        public bool RetryUsed;
        public bool Finished;
        public bool Victory;
        public bool MenuPause;
        public string Cause = "";
        public bool AwakeLatched;
        public bool HasPing;
        public bool HasPrev;
        public float PingX;
        public float PingY;
        public float PrevX;
        public float PrevY;
        public bool HasFoot;
        public float FootX;
        public float FootY;
        public float FootTimer;
        public float SonarTime;
        public float AutoTimer;
        public int ManualPings;
        public int NextActorId = 1;
        public bool BossHitThisCast;
        public List<Blast> Blasts = new List<Blast>();
        public List<Droplet> Drops = new List<Droplet>();
        public int MaxRoomReached = 1;

        public Room Current
        {
            get { return Rooms[RoomIndex]; }
        }

        public Vec2 Pos
        {
            get { return new Vec2(PlayerX, PlayerY); }
        }
    }
}
