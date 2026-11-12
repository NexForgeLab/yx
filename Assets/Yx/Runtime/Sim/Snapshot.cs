using System;

namespace Yx.Sim
{
    [Serializable]
    public class RunSnapshot
    {
        public int version = 1;
        public int seed;
        public uint rng;
        public int unlocked;
        public int roomIndex;
        public float px, py, faceX, faceY;
        public float s, maxS, air, maxAir, life, invuln;
        public float pingCd, breath, breathCd, anchor, chaos, sonar, autoTimer, tutorialTime, footTimer;
        public int picks, effective, draftQueue, manualPings, nextActor, maxRoom;
        public int tutorial, tutorialMoss, retryUsed, finished, victory, menuPause, draftOpen;
        public int awake, hasPing, hasPrev, hasFoot, chaosEver;
        public float pingX, pingY, prevX, prevY, footX, footY;
        public string cause;
        public int[] levels;
        public int[] useScore;
        public int[] offer;
        public RoomSnap[] rooms;
        public BlastSnap[] blasts;
        public DropSnap[] drops;
    }

    [Serializable]
    public class RoomSnap
    {
        public int w, h, chambers;
        public int boss, cleared, drafted, gateFound;
        public float spawnX, spawnY;
        public string built;
        public int[] kind;
        public int[] surveyed;
        public float[] glow;
        public ActorSnap[] actors;
    }

    [Serializable]
    public class ActorSnap
    {
        public int id, kind, wallX, wallY, weak, alive, marked, covered, prox, tutorial, charging;
        public float x, y, homeX, homeY, hp, bumpX, bumpY, expose, wanderT, wanderX, wanderY, accel, chargeT;
        public float fromX, fromY, toX, toY;
    }

    [Serializable]
    public class BlastSnap
    {
        public float x, y, time, radius;
    }

    [Serializable]
    public class DropSnap
    {
        public float time, x, y;
        public int actorId;
    }

    public static class Snapshots
    {
        public static RunSnapshot Capture(RunState run)
        {
            var snap = new RunSnapshot();
            snap.seed = run.Seed;
            snap.rng = run.Rng.State;
            snap.unlocked = run.UnlockedCount;
            snap.roomIndex = run.RoomIndex;
            snap.px = run.PlayerX;
            snap.py = run.PlayerY;
            snap.faceX = run.FaceX;
            snap.faceY = run.FaceY;
            snap.s = run.S;
            snap.maxS = run.MaxS;
            snap.air = run.Air;
            snap.maxAir = run.MaxAir;
            snap.life = run.Life;
            snap.invuln = run.Invuln;
            snap.pingCd = run.PingCd;
            snap.breath = run.Breath;
            snap.breathCd = run.BreathCd;
            snap.anchor = run.Anchor;
            snap.chaos = run.Chaos;
            snap.sonar = run.SonarTime;
            snap.autoTimer = run.AutoTimer;
            snap.tutorialTime = run.TutorialTime;
            snap.footTimer = run.FootTimer;
            snap.picks = run.Picks;
            snap.effective = run.Effective;
            snap.draftQueue = run.DraftQueue;
            snap.manualPings = run.ManualPings;
            snap.nextActor = run.NextActorId;
            snap.maxRoom = run.MaxRoomReached;
            snap.tutorial = run.Tutorial ? 1 : 0;
            snap.tutorialMoss = run.TutorialMossPending ? 1 : 0;
            snap.retryUsed = run.RetryUsed ? 1 : 0;
            snap.finished = run.Finished ? 1 : 0;
            snap.victory = run.Victory ? 1 : 0;
            snap.menuPause = run.MenuPause ? 1 : 0;
            snap.draftOpen = run.DraftOpen ? 1 : 0;
            snap.awake = run.AwakeLatched ? 1 : 0;
            snap.hasPing = run.HasPing ? 1 : 0;
            snap.hasPrev = run.HasPrev ? 1 : 0;
            snap.hasFoot = run.HasFoot ? 1 : 0;
            snap.chaosEver = run.ChaosHappened ? 1 : 0;
            snap.pingX = run.PingX;
            snap.pingY = run.PingY;
            snap.prevX = run.PrevX;
            snap.prevY = run.PrevY;
            snap.footX = run.FootX;
            snap.footY = run.FootY;
            snap.cause = run.Cause ?? "";
            snap.levels = Copy(run.Levels);
            snap.useScore = Copy(run.UseScore);
            snap.offer = run.Offer == null ? new int[0] : Copy(run.Offer);
            snap.rooms = new RoomSnap[run.Rooms.Count];
            for (int i = 0; i < run.Rooms.Count; i++) snap.rooms[i] = CaptureRoom(run.Rooms[i]);
            snap.blasts = new BlastSnap[run.Blasts.Count];
            for (int i = 0; i < run.Blasts.Count; i++)
            {
                snap.blasts[i] = new BlastSnap
                {
                    x = run.Blasts[i].X,
                    y = run.Blasts[i].Y,
                    time = run.Blasts[i].Time,
                    radius = run.Blasts[i].Radius
                };
            }
            snap.drops = new DropSnap[run.Drops.Count];
            for (int i = 0; i < run.Drops.Count; i++)
            {
                snap.drops[i] = new DropSnap
                {
                    time = run.Drops[i].Time,
                    x = run.Drops[i].X,
                    y = run.Drops[i].Y,
                    actorId = run.Drops[i].ActorId
                };
            }
            return snap;
        }

        public static RunState Restore(RunSnapshot snap)
        {
            var run = new RunState();
            run.Seed = snap.seed;
            run.Rng = new Rng(snap.rng == 0 ? 1u : snap.rng);
            run.Rng.State = snap.rng == 0 ? 1u : snap.rng;
            run.UnlockedCount = snap.unlocked;
            run.RoomIndex = snap.roomIndex;
            run.PlayerX = snap.px;
            run.PlayerY = snap.py;
            run.FaceX = snap.faceX;
            run.FaceY = snap.faceY == 0f && snap.faceX == 0f ? 1f : snap.faceY;
            run.S = snap.s;
            run.MaxS = snap.maxS;
            run.Air = snap.air;
            run.MaxAir = snap.maxAir;
            run.Life = snap.life;
            run.Invuln = snap.invuln;
            run.PingCd = snap.pingCd;
            run.Breath = snap.breath;
            run.BreathCd = snap.breathCd;
            run.Anchor = snap.anchor;
            run.Chaos = snap.chaos;
            run.SonarTime = snap.sonar;
            run.AutoTimer = snap.autoTimer;
            run.TutorialTime = snap.tutorialTime;
            run.FootTimer = snap.footTimer;
            run.Picks = snap.picks;
            run.Effective = snap.effective;
            run.DraftQueue = snap.draftQueue;
            run.ManualPings = snap.manualPings;
            run.NextActorId = snap.nextActor;
            run.MaxRoomReached = snap.maxRoom;
            run.Tutorial = snap.tutorial != 0;
            run.TutorialMossPending = snap.tutorialMoss != 0;
            run.RetryUsed = snap.retryUsed != 0;
            run.Finished = snap.finished != 0;
            run.Victory = snap.victory != 0;
            run.MenuPause = snap.menuPause != 0;
            run.DraftOpen = snap.draftOpen != 0;
            run.AwakeLatched = snap.awake != 0;
            run.HasPing = snap.hasPing != 0;
            run.HasPrev = snap.hasPrev != 0;
            run.HasFoot = snap.hasFoot != 0;
            run.ChaosHappened = snap.chaosEver != 0;
            run.PingX = snap.pingX;
            run.PingY = snap.pingY;
            run.PrevX = snap.prevX;
            run.PrevY = snap.prevY;
            run.FootX = snap.footX;
            run.FootY = snap.footY;
            run.Cause = snap.cause ?? "";
            run.Levels = snap.levels == null || snap.levels.Length != 12 ? new int[12] : Copy(snap.levels);
            run.UseScore = snap.useScore == null || snap.useScore.Length != 12 ? new int[12] : Copy(snap.useScore);
            run.Offer = snap.offer == null ? new int[0] : Copy(snap.offer);
            if (snap.rooms != null)
            {
                for (int i = 0; i < snap.rooms.Length; i++) run.Rooms.Add(RestoreRoom(snap.rooms[i]));
            }
            if (snap.blasts != null)
            {
                for (int i = 0; i < snap.blasts.Length; i++)
                {
                    run.Blasts.Add(new Blast
                    {
                        X = snap.blasts[i].x,
                        Y = snap.blasts[i].y,
                        Time = snap.blasts[i].time,
                        Radius = snap.blasts[i].radius
                    });
                }
            }
            if (snap.drops != null)
            {
                for (int i = 0; i < snap.drops.Length; i++)
                {
                    run.Drops.Add(new Droplet
                    {
                        Time = snap.drops[i].time,
                        X = snap.drops[i].x,
                        Y = snap.drops[i].y,
                        ActorId = snap.drops[i].actorId
                    });
                }
            }
            return run;
        }

        static RoomSnap CaptureRoom(Room room)
        {
            var snap = new RoomSnap();
            snap.w = room.W;
            snap.h = room.H;
            snap.chambers = room.ChamberCount;
            snap.boss = room.Boss ? 1 : 0;
            snap.cleared = room.Cleared ? 1 : 0;
            snap.drafted = room.Drafted ? 1 : 0;
            snap.gateFound = room.GateFound ? 1 : 0;
            snap.spawnX = room.SpawnX;
            snap.spawnY = room.SpawnY;
            snap.built = room.BuiltFrom ?? "";
            snap.kind = new int[room.Cells.Length];
            snap.surveyed = new int[room.Cells.Length];
            snap.glow = new float[room.Glow.Length];
            for (int i = 0; i < room.Cells.Length; i++)
            {
                snap.kind[i] = (int)room.Cells[i].Kind;
                snap.surveyed[i] = room.Cells[i].Surveyed ? 1 : 0;
                snap.glow[i] = room.Glow[i];
            }
            snap.actors = new ActorSnap[room.Actors.Count];
            for (int i = 0; i < room.Actors.Count; i++) snap.actors[i] = CaptureActor(room.Actors[i]);
            return snap;
        }

        static ActorSnap CaptureActor(Actor a)
        {
            return new ActorSnap
            {
                id = a.Id,
                kind = (int)a.Kind,
                wallX = a.WallX,
                wallY = a.WallY,
                weak = a.WeakPoints,
                alive = a.Alive ? 1 : 0,
                marked = a.Marked ? 1 : 0,
                covered = a.Covered ? 1 : 0,
                prox = a.ProximityUsed ? 1 : 0,
                tutorial = a.TutorialMoss ? 1 : 0,
                charging = a.Charging ? 1 : 0,
                x = a.X,
                y = a.Y,
                homeX = a.HomeX,
                homeY = a.HomeY,
                hp = a.Hp,
                bumpX = a.BumpX,
                bumpY = a.BumpY,
                expose = a.Expose,
                wanderT = a.WanderT,
                wanderX = a.WanderX,
                wanderY = a.WanderY,
                accel = a.Accel,
                chargeT = a.ChargeT,
                fromX = a.ChargeFromX,
                fromY = a.ChargeFromY,
                toX = a.ChargeToX,
                toY = a.ChargeToY
            };
        }

        static Room RestoreRoom(RoomSnap snap)
        {
            var room = new Room();
            room.W = snap.w;
            room.H = snap.h;
            room.ChamberCount = snap.chambers;
            room.Boss = snap.boss != 0;
            room.Cleared = snap.cleared != 0;
            room.Drafted = snap.drafted != 0;
            room.GateFound = snap.gateFound != 0;
            room.SpawnX = snap.spawnX;
            room.SpawnY = snap.spawnY;
            room.BuiltFrom = snap.built ?? "";
            int n = snap.w * snap.h;
            room.Cells = new Cell[n];
            room.Glow = new float[n];
            room.Marks = new char[n];
            for (int i = 0; i < n; i++)
            {
                room.Cells[i].Kind = (CellKind)snap.kind[i];
                room.Cells[i].Surveyed = snap.surveyed != null && i < snap.surveyed.Length && snap.surveyed[i] != 0;
                room.Glow[i] = snap.glow != null && i < snap.glow.Length ? snap.glow[i] : 0f;
            }
            if (snap.actors != null)
            {
                for (int i = 0; i < snap.actors.Length; i++) room.Actors.Add(RestoreActor(snap.actors[i]));
            }
            return room;
        }

        static Actor RestoreActor(ActorSnap a)
        {
            return new Actor
            {
                Id = a.id,
                Kind = (ActorKind)a.kind,
                WallX = a.wallX,
                WallY = a.wallY,
                WeakPoints = a.weak,
                Alive = a.alive != 0,
                Marked = a.marked != 0,
                Covered = a.covered != 0,
                ProximityUsed = a.prox != 0,
                TutorialMoss = a.tutorial != 0,
                Charging = a.charging != 0,
                X = a.x,
                Y = a.y,
                HomeX = a.homeX,
                HomeY = a.homeY,
                Hp = a.hp,
                BumpX = a.bumpX,
                BumpY = a.bumpY,
                Expose = a.expose,
                WanderT = a.wanderT,
                WanderX = a.wanderX,
                WanderY = a.wanderY,
                Accel = a.accel,
                ChargeT = a.chargeT,
                ChargeFromX = a.fromX,
                ChargeFromY = a.fromY,
                ChargeToX = a.toX,
                ChargeToY = a.toY
            };
        }

        static int[] Copy(int[] src)
        {
            var d = new int[src.Length];
            for (int i = 0; i < src.Length; i++) d[i] = src[i];
            return d;
        }
    }
}
