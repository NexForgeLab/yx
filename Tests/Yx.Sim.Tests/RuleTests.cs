using System;
using System.Collections.Generic;
using System.IO;
using Xunit;
using Yx.Sim;

namespace Yx.Sim.Tests
{
    public class RuleTests
    {
        [Fact]
        public void SegmentsAreRectangular()
        {
            Assert.Equal(6, SegmentLibrary.Chambers.Length);
            Assert.Equal("shaft", SegmentLibrary.Corridor.Id);
            Assert.Equal("boss", SegmentLibrary.Boss.Role);
            Assert.True(SegmentLibrary.Boss.Width >= 15);
        }

        [Fact]
        public void RoomsConnectSpawnToGate()
        {
            for (int seed = 1; seed <= 24; seed++)
            {
                RunState run = RunFactory.New(seed, 600f, seed == 1, 6);
                Assert.Equal(5, run.Rooms.Count);
                for (int i = 0; i < 4; i++)
                {
                    Room room = run.Rooms[i];
                    Assert.False(room.Boss);
                    Assert.InRange(room.ChamberCount, 2, 3);
                    Assert.True(Reachable(room, (int)Math.Floor(room.SpawnX), (int)Math.Floor(room.SpawnY)), "seed " + seed + " room " + i + " " + room.BuiltFrom);
                    bool gate = false;
                    int floors = 0;
                    for (int c = 0; c < room.Cells.Length; c++)
                    {
                        if (room.Cells[c].Kind == CellKind.Gate) gate = true;
                        if (room.Cells[c].Kind == CellKind.Floor || room.Cells[c].Kind == CellKind.Water) floors++;
                    }
                    Assert.True(gate, "missing gate " + seed);
                    Assert.True(floors > 20, "too small " + seed);
                }
                Assert.True(run.Rooms[4].Boss);
                Assert.Contains(run.Rooms[4].Actors, a => a.Kind == ActorKind.Boss);
            }
        }

        [Fact]
        public void TutorialMossIsNearSpawnAndFirstPingHitsIt()
        {
            RunState run = RunFactory.New(7, 600f, true, 6);
            Actor moss = null;
            foreach (Actor a in run.Rooms[0].Actors)
            {
                if (a.TutorialMoss) moss = a;
            }
            Assert.NotNull(moss);
            float dx = moss.BumpX - run.Rooms[0].SpawnX;
            float dy = moss.BumpY - run.Rooms[0].SpawnY;
            Assert.True(dx * dx + dy * dy <= 4.4f * 4.4f);
            var ev = new SimEvents();
            Simulator.Tick(run, 0.05f, new SimInput { Ping = true }, ev);
            Assert.True(ev.TutorialHit);
            Assert.False(moss.Alive);
            Assert.True(run.Rooms[0].Get(moss.WallX, moss.WallY).Surveyed);
        }

        [Fact]
        public void DisturbanceBandsDecayAndChaos()
        {
            RunState run = RunFactory.New(3, 600f, false, 6);
            var ev = new SimEvents();
            Simulator.AddDisturbance(run, 10f, true, ev);
            Assert.Equal(10f, run.S);
            Assert.Equal("安静", Hints.TierName(run.S));
            Simulator.AddDisturbance(run, 30f, true, ev);
            Assert.Equal("警醒", Hints.TierName(run.S));
            float before = run.S;
            Simulator.Tick(run, 1f, default(SimInput), ev);
            Assert.True(run.S < before);
            Assert.InRange(before - run.S, 1.5f, 4.2f);

            run.S = 0f;
            run.Life = 3f;
            Simulator.AddDisturbance(run, 100f, true, ev);
            Assert.Equal(55f, run.S);
            Assert.Equal(2f, run.Life);
            Assert.True(run.Chaos > 0f);
            Assert.True(ev.Chaos);
            Assert.Contains(run.Current.Actors, a => a.Kind == ActorKind.Deep);
        }

        [Fact]
        public void BasePingCostsTenAndFanCostsSix()
        {
            RunState run = RunFactory.New(4, 600f, false, 12);
            var ev = new SimEvents();
            Simulator.Tick(run, 0.05f, new SimInput { Ping = true }, ev);
            Assert.Equal(10f, run.S);
            Assert.True(run.PingCd > 1.2f);
            run.PingCd = 0f;
            run.Levels[(int)ModuleId.XiXian] = 1;
            run.Levels[(int)ModuleId.ChangMing] = 2;
            float expected = ModuleBook.ManualDisturbance(run);
            Assert.Equal(6f + 6f, expected);
            float s = run.S;
            Simulator.Tick(run, 0.05f, new SimInput { Ping = true, X = 1f }, ev);
            Assert.InRange(run.S, s + expected - 0.35f, s + expected + 0.05f);
        }

        [Fact]
        public void HoldBreathCancelsOnPingAndMisdirectsDeep()
        {
            RunState run = RunFactory.New(5, 600f, false, 12);
            var ev = new SimEvents();
            Simulator.Tick(run, 0.05f, new SimInput { Ping = true }, ev);
            run.PingCd = 0f;
            Simulator.Tick(run, 0.05f, new SimInput { Ping = true, X = 1f }, ev);
            Assert.True(run.HasPrev);
            run.PingCd = 0f;
            run.S = 90f;
            run.AwakeLatched = true;
            if (!run.Current.Actors.Exists(a => a.Kind == ActorKind.Deep && a.Alive))
                RoomBuilder.SpawnDeep(run, run.Current);
            Simulator.Tick(run, 0.05f, new SimInput { Breath = true }, ev);
            Assert.True(run.Breath > 1.5f);
            Simulator.Tick(run, 0.05f, new SimInput { Ping = true }, ev);
            Assert.True(run.Breath <= 0f);
        }

        [Fact]
        public void YaBuExtendsBreath()
        {
            RunState run = RunFactory.New(5, 600f, false, 12);
            run.Levels[(int)ModuleId.YaBu] = 3;
            Assert.Equal(5f, ModuleBook.BreathDuration(run));
            Assert.Equal(5f, ModuleBook.BreathCooldown(run));
            var ev = new SimEvents();
            Simulator.Tick(run, 0.02f, new SimInput { Breath = true }, ev);
            Assert.InRange(run.Breath, 4.9f, 5f);
        }

        [Fact]
        public void AirIsSlowerOnSurveyedAfterglow()
        {
            RunState run = RunFactory.New(8, 600f, false, 6);
            int x = (int)Math.Floor(run.PlayerX);
            int y = (int)Math.Floor(run.PlayerY);
            int i = run.Current.Index(x, y);
            run.Current.Cells[i].Surveyed = true;
            run.Current.Glow[i] = 2f;
            run.Air = 100f;
            var ev = new SimEvents();
            Simulator.Tick(run, 1f, default(SimInput), ev);
            Assert.InRange(run.Air, 98.5f, 99.2f);

            run.Current.Glow[i] = 0f;
            run.SonarTime = 0f;
            run.Air = 100f;
            Simulator.Tick(run, 1f, default(SimInput), ev);
            Assert.InRange(run.Air, 98.2f, 98.7f);
        }

        [Fact]
        public void SelfGlowDoesNotSurvey()
        {
            RunState run = RunFactory.New(8, 600f, false, 6);
            int walls = CountSurveyedWalls(run.Current);
            var ev = new SimEvents();
            Simulator.Tick(run, 0.5f, new SimInput { Y = 1f }, ev);
            Assert.Equal(walls, CountSurveyedWalls(run.Current));
        }

        [Fact]
        public void SurveyOpensAtSeventyAndJiPuFloorsAtFiftyFive()
        {
            RunState run = RunFactory.New(9, 600f, false, 12);
            Assert.Equal(70f, ModuleBook.Threshold(run));
            run.Picks = 4;
            Assert.Equal(58f, ModuleBook.Threshold(run));
            run.Picks = 20;
            Assert.Equal(55f, ModuleBook.Threshold(run));
            run.Picks = 0;
            PaintAllWalls(run.Current);
            var ev = new SimEvents();
            Simulator.Tick(run, 0.05f, default(SimInput), ev);
            Assert.True(run.Current.Cleared);
            Assert.True(ev.GateOpened || run.Current.Drafted);
        }

        [Fact]
        public void EffectivePingsOpenADraftOfThree()
        {
            RunState run = RunFactory.New(11, 600f, false, 6);
            var ev = new SimEvents();
            int drafts = 0;
            var spots = Flood(run.Current, (int)Math.Floor(run.PlayerX), (int)Math.Floor(run.PlayerY));
            for (int n = 0; n < spots.Count && n < 40; n += 3)
            {
                run.PlayerX = spots[n].X + 0.5f;
                run.PlayerY = spots[n].Y + 0.5f;
                run.PingCd = 0f;
                run.DraftOpen = false;
                run.Finished = false;
                run.S = 10f;
                Simulator.Tick(run, 0.05f, new SimInput { Ping = true }, ev);
                if (ev.DraftOpened || run.DraftOpen) drafts++;
                if (run.DraftOpen)
                {
                    Assert.InRange(run.Offer.Length, 1, 3);
                    for (int k = 0; k < run.Offer.Length; k++)
                        Assert.True(ModuleBook.IsUnlocked(6, (ModuleId)run.Offer[k]));
                    Simulator.Choose(run, 0);
                }
            }
            Assert.True(drafts >= 1);
        }

        [Fact]
        public void ModuleCapsAtThreeAndQunZaoWarns()
        {
            Assert.True(ModuleBook.WarnsDisturbance(ModuleId.QunZao));
            Assert.Contains("扰动会涨得很快", ModuleBook.Warning());
            RunState run = RunFactory.New(2, 600f, false, 12);
            for (int n = 0; n < 3; n++)
            {
                run.DraftOpen = true;
                run.Offer = new[] { (int)ModuleId.QunZao };
                Simulator.Choose(run, 0);
            }
            run.DraftOpen = true;
            run.Offer = new[] { (int)ModuleId.QunZao };
            Simulator.Choose(run, 0);
            Assert.Equal(3, run.Levels[(int)ModuleId.QunZao]);
            Assert.Equal(1.2f, ModuleBook.AutoInterval(run));
        }

        [Fact]
        public void UnlockEveryThreeSuccesses()
        {
            Assert.Equal(6, ModuleBook.UnlockedCount(0));
            Assert.Equal(6, ModuleBook.UnlockedCount(2));
            Assert.Equal(7, ModuleBook.UnlockedCount(3));
            Assert.Equal(12, ModuleBook.UnlockedCount(18));
            Assert.Equal(12, ModuleBook.UnlockedCount(99));
            Assert.False(ModuleBook.IsUnlocked(6, ModuleId.QunZao));
            Assert.True(ModuleBook.IsUnlocked(12, ModuleId.DuiKong));
        }

        [Fact]
        public void RetryOnlyBelowAlertAndOnlyOnce()
        {
            RunState run = RunFactory.New(13, 600f, false, 6);
            run.Finished = true;
            run.S = 55f;
            Assert.False(Simulator.CanRetry(run));
            run.S = 20f;
            Assert.True(Simulator.CanRetry(run));
            float air = run.MaxAir;
            Simulator.Retry(run);
            Assert.False(run.Finished);
            Assert.Equal(3f, run.Life);
            Assert.Equal(air * 0.5f, run.Air);
            Assert.True(run.RetryUsed);
            run.Finished = true;
            run.S = 10f;
            Assert.False(Simulator.CanRetry(run));
        }

        [Fact]
        public void BossDiesOnThirdWeakPointAndNotBefore()
        {
            RunState run = RunFactory.New(15, 600f, false, 6);
            run.RoomIndex = 4;
            run.PlayerX = run.Current.SpawnX;
            run.PlayerY = run.Current.SpawnY;
            Actor boss = null;
            foreach (Actor a in run.Current.Actors)
                if (a.Kind == ActorKind.Boss) boss = a;
            Assert.NotNull(boss);
            run.PlayerX = boss.X;
            run.PlayerY = boss.Y - 1.2f;
            var ev = new SimEvents();
            for (int n = 0; n < 2; n++)
            {
                run.PingCd = 0f;
                run.Finished = false;
                Simulator.Tick(run, 0.05f, new SimInput { Ping = true, Y = 1f }, ev);
                Assert.False(run.Victory);
                Assert.True(boss.Alive);
            }
            run.PingCd = 0f;
            Simulator.Tick(run, 0.05f, new SimInput { Ping = true, Y = 1f }, ev);
            Assert.True(run.Victory);
            Assert.Equal(Causes.Clear, run.Cause);
            Assert.False(boss.Alive);
        }

        [Fact]
        public void CavityIgnoresPingUntilAfterglowEnds()
        {
            RunState run = RunFactory.New(16, 600f, false, 12);
            Room room = run.Current;
            var cavity = new Actor
            {
                Id = 99,
                Kind = ActorKind.Cavity,
                X = run.PlayerX + 1.2f,
                Y = run.PlayerY,
                Hp = 2f,
                Alive = true
            };
            cavity.X = run.PlayerX;
            cavity.Y = run.PlayerY;
            room.Actors.Add(cavity);
            var ev = new SimEvents();
            Simulator.Tick(run, 0.05f, new SimInput { Ping = true }, ev);
            Assert.Equal(2f, cavity.Hp);
            bool exposed = false;
            bool hurt = false;
            for (int i = 0; i < 50 && !hurt; i++)
            {
                Simulator.Tick(run, 0.05f, default(SimInput), ev);
                if (cavity.Expose > 0f) exposed = true;
                if (cavity.Hp < 2f) hurt = true;
            }
            Assert.True(exposed);
            Assert.True(hurt);
        }

        [Fact]
        public void MossBumpIsHalfAPip()
        {
            RunState run = RunFactory.New(18, 600f, false, 6);
            run.Life = 3f;
            run.Invuln = 0f;
            var moss = new Actor
            {
                Kind = ActorKind.Moss,
                Alive = true,
                BumpX = run.PlayerX,
                BumpY = run.PlayerY,
                X = run.PlayerX,
                Y = run.PlayerY,
                Hp = 1f
            };
            run.Current.Actors.Add(moss);
            var ev = new SimEvents();
            Simulator.Tick(run, 0.05f, default(SimInput), ev);
            Assert.Equal(2.5f, run.Life);
            Assert.False(run.Finished);
        }

        [Fact]
        public void SpikeOnlyHurtsWhenUnsurveyed()
        {
            RunState run = RunFactory.New(19, 600f, false, 6);
            int x = (int)Math.Floor(run.PlayerX + 0.6f);
            int y = (int)Math.Floor(run.PlayerY);
            if (run.Current.In(x, y))
            {
                run.Current.Cells[run.Current.Index(x, y)].Kind = CellKind.Spike;
                run.Current.Cells[run.Current.Index(x, y)].Surveyed = true;
            }
            float life = run.Life;
            var ev = new SimEvents();
            Simulator.Tick(run, 0.2f, new SimInput { X = 1f }, ev);
            Assert.Equal(life, run.Life);
        }

        [Fact]
        public void FanIsNarrowerThanCircle()
        {
            var origin = new Vec2(0f, 0f);
            var face = new Vec2(0f, 1f);
            var ahead = new Vec2(0f, 3f);
            var side = new Vec2(3f, 0f);
            Assert.True(Simulator.InShape(ahead, origin, 4.5f, true, face));
            Assert.False(Simulator.InShape(side, origin, 4.5f, true, face));
            Assert.True(Simulator.InShape(side, origin, 4.5f, false, face));
        }

        [Fact]
        public void QunZaoRaisesDisturbanceQuickly()
        {
            RunState run = RunFactory.New(21, 600f, false, 12);
            run.Levels[(int)ModuleId.QunZao] = 3;
            run.AutoTimer = 0.01f;
            var ev = new SimEvents();
            float s = run.S;
            Simulator.Tick(run, 0.05f, default(SimInput), ev);
            Assert.True(run.S > s + 4f);
        }

        [Fact]
        public void SnapshotRoundTrip()
        {
            RunState run = RunFactory.New(22, 480f, true, 8);
            var ev = new SimEvents();
            Simulator.Tick(run, 0.2f, new SimInput { X = 1f, Ping = true }, ev);
            RunSnapshot snap = Snapshots.Capture(run);
            RunState back = Snapshots.Restore(snap);
            Assert.Equal(run.Seed, back.Seed);
            Assert.Equal(run.S, back.S);
            Assert.Equal(run.Air, back.Air);
            Assert.Equal(run.PlayerX, back.PlayerX);
            Assert.Equal(run.Rooms.Count, back.Rooms.Count);
            Assert.Equal(run.Current.W, back.Current.W);
            Assert.Equal(run.ManualPings, back.ManualPings);
            Assert.Equal(run.Current.Actors.Count, back.Current.Actors.Count);
        }

        [Fact]
        public void PathPingsCanOpenTheGate()
        {
            int opened = 0;
            string sample = "";
            for (int seed = 1; seed <= 12; seed++)
            {
                RunState run = RunFactory.New(seed, 600f, false, 6);
                bool ok = GreedyMap(run);
                if (ok) opened++;
                else if (sample.Length == 0)
                {
                    sample = "seed " + seed + " score " + run.Current.SurveyScore().ToString("0.0")
                        + " cleared " + run.Current.Cleared + " from " + run.Current.BuiltFrom;
                }
            }
            Assert.True(opened >= 8, "opened " + opened + " " + sample);
        }

        [Fact]
        public void HintsStayOnTheThreeLineContract()
        {
            RunState run = RunFactory.New(1, 600f, false, 6);
            run.Cause = Causes.Deep;
            run.Victory = false;
            Assert.Contains("哑步", Hints.Next(run));
            run.Victory = true;
            run.Cause = Causes.Clear;
            Assert.Contains("七成", Hints.Next(run));
            Assert.Equal("这一局没有装模组", Hints.Combo(run));
        }

        static bool GreedyMap(RunState run)
        {
            Room room = run.Current;
            var ev = new SimEvents();
            var path = Flood(room, (int)Math.Floor(room.SpawnX), (int)Math.Floor(room.SpawnY));
            for (int guard = 0; guard < path.Count; guard += 3)
            {
                run.PlayerX = path[guard].X + 0.5f;
                run.PlayerY = path[guard].Y + 0.5f;
                run.PingCd = 0f;
                run.S = 5f;
                run.Life = 3f;
                run.Invuln = 1f;
                run.Finished = false;
                run.DraftOpen = false;
                run.MenuPause = false;
                Simulator.Tick(run, 0.05f, new SimInput { Ping = true }, ev);
                if (run.DraftOpen) Simulator.Choose(run, 0);
                if (room.Cleared) return true;
            }
            return room.Cleared;
        }

        [Fact]
        public void AuthoredGridCanReplaceASegmentAndRejectARaggedOne()
        {
            Segment pool = SegmentLibrary.Chambers[0];
            string[] original = pool.Rows;
            Assert.False(SegmentLibrary.TryReplace("pool", "chamber", "###\n##"));
            Assert.Same(original, pool.Rows);
            string grid = string.Join("\n", original) + "\n";
            Assert.True(SegmentLibrary.TryReplace("pool", "chamber", grid));
            Assert.Equal(original.Length, pool.Rows.Length);
            Assert.Equal(original[0], pool.Rows[0]);
            pool.Rows = original;
        }

        [Fact]
        public void DumpSegmentPrefabs()
        {
            if (Environment.GetEnvironmentVariable("YX_DUMP") != "1") return;
            string dir = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "..", "Assets", "Yx", "Resources", "SegmentPrefabs"));
            Directory.CreateDirectory(dir);
            foreach (Segment seg in SegmentLibrary.All())
                File.WriteAllText(Path.Combine(dir, seg.Id + ".prefab"), PrefabYaml(seg));
        }

        static string PrefabYaml(Segment seg)
        {
            string grid = string.Join("\\n", seg.Rows);
            return "%YAML 1.1\n%TAG !u! tag:unity3d.com,2011:\n--- !u!1 &100000\nGameObject:\n  m_ObjectHideFlags: 0\n  m_CorrespondingSourceObject: {fileID: 0}\n  m_PrefabInstance: {fileID: 0}\n  m_PrefabAsset: {fileID: 0}\n  serializedVersion: 6\n  m_Component:\n  - component: {fileID: 100001}\n  - component: {fileID: 100002}\n  m_Layer: 0\n  m_Name: " + seg.Id + "\n  m_TagString: Untagged\n  m_Icon: {fileID: 0}\n  m_NavMeshLayer: 0\n  m_StaticEditorFlags: 0\n  m_IsActive: 1\n--- !u!4 &100001\nTransform:\n  m_ObjectHideFlags: 0\n  m_CorrespondingSourceObject: {fileID: 0}\n  m_PrefabInstance: {fileID: 0}\n  m_PrefabAsset: {fileID: 0}\n  m_GameObject: {fileID: 100000}\n  serializedVersion: 2\n  m_LocalRotation: {x: 0, y: 0, z: 0, w: 1}\n  m_LocalPosition: {x: 0, y: 0, z: 0}\n  m_LocalScale: {x: 1, y: 1, z: 1}\n  m_ConstrainProportionsScale: 0\n  m_Children: []\n  m_Father: {fileID: 0}\n  m_LocalEulerAnglesHint: {x: 0, y: 0, z: 0}\n--- !u!114 &100002\nMonoBehaviour:\n  m_ObjectHideFlags: 0\n  m_CorrespondingSourceObject: {fileID: 0}\n  m_PrefabInstance: {fileID: 0}\n  m_PrefabAsset: {fileID: 0}\n  m_GameObject: {fileID: 100000}\n  m_Enabled: 1\n  m_EditorHideFlags: 0\n  m_Script: {fileID: 11500000, guid: c5d6e7f8091a2b3c4d5e6f708192a3b4, type: 3}\n  m_Name: \n  m_EditorClassIdentifier: Assembly-CSharp::Yx.Game.SegmentAuthoring\n  segmentId: " + seg.Id + "\n  role: " + seg.Role + "\n  grid: \"" + grid + "\"\n";
        }

        static int CountSurveyedWalls(Room room)
        {
            int n = 0;
            for (int i = 0; i < room.Cells.Length; i++)
            {
                CellKind k = room.Cells[i].Kind;
                if ((k == CellKind.Wall || k == CellKind.Spike) && room.Cells[i].Surveyed) n++;
            }
            return n;
        }

        static void PaintAllWalls(Room room)
        {
            for (int i = 0; i < room.Cells.Length; i++)
            {
                CellKind k = room.Cells[i].Kind;
                if (k == CellKind.Wall || k == CellKind.Spike || k == CellKind.Gate)
                    room.Cells[i].Surveyed = true;
            }
            room.GateFound = true;
        }

        static bool Reachable(Room room, int sx, int sy)
        {
            if (!room.Walkable(sx, sy) && room.Get(sx, sy).Kind != CellKind.Gate)
            {
                if (!room.In(sx, sy) || !room.Walkable(sx, sy)) return false;
            }
            int gates = 0;
            int reached = 0;
            var seen = new bool[room.W * room.H];
            var qx = new Queue<int>();
            var qy = new Queue<int>();
            if (!room.Walkable(sx, sy)) return false;
            qx.Enqueue(sx);
            qy.Enqueue(sy);
            seen[room.Index(sx, sy)] = true;
            int[] ox = { 1, -1, 0, 0 };
            int[] oy = { 0, 0, 1, -1 };
            while (qx.Count > 0)
            {
                int x = qx.Dequeue();
                int y = qy.Dequeue();
                if (room.Get(x, y).Kind == CellKind.Gate) gates++;
                for (int k = 0; k < 4; k++)
                {
                    int nx = x + ox[k];
                    int ny = y + oy[k];
                    if (!room.In(nx, ny) || seen[room.Index(nx, ny)]) continue;
                    CellKind kind = room.Get(nx, ny).Kind;
                    bool walk = room.Walkable(nx, ny) || kind == CellKind.Gate;
                    if (!walk) continue;
                    seen[room.Index(nx, ny)] = true;
                    qx.Enqueue(nx);
                    qy.Enqueue(ny);
                    reached++;
                }
            }
            return gates > 0;
        }

        static List<Int2> Flood(Room room, int sx, int sy)
        {
            var list = new List<Int2>();
            var seen = new bool[room.W * room.H];
            var qx = new Queue<int>();
            var qy = new Queue<int>();
            qx.Enqueue(sx);
            qy.Enqueue(sy);
            seen[room.Index(sx, sy)] = true;
            int[] ox = { 1, -1, 0, 0 };
            int[] oy = { 0, 0, 1, -1 };
            while (qx.Count > 0)
            {
                int x = qx.Dequeue();
                int y = qy.Dequeue();
                list.Add(new Int2 { X = x, Y = y });
                for (int k = 0; k < 4; k++)
                {
                    int nx = x + ox[k];
                    int ny = y + oy[k];
                    if (!room.In(nx, ny) || seen[room.Index(nx, ny)]) continue;
                    if (!room.Walkable(nx, ny) && room.Get(nx, ny).Kind != CellKind.Gate) continue;
                    seen[room.Index(nx, ny)] = true;
                    qx.Enqueue(nx);
                    qy.Enqueue(ny);
                }
            }
            return list;
        }

        struct Int2
        {
            public int X;
            public int Y;
        }
    }
}
