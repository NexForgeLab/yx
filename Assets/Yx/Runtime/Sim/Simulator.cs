using System;
using System.Collections.Generic;

namespace Yx.Sim
{
    public static class Simulator
    {
        const float FanCos = 0.8660254f;
        static int castDepth;

        public static void Tick(RunState run, float dt, SimInput input, SimEvents ev)
        {
            ev.Clear();
            if (dt <= 0f || run.Finished || run.DraftOpen || run.MenuPause) return;
            if (dt <= 0.05f)
            {
                TickOnce(run, dt, input, ev);
                return;
            }
            int n = (int)Math.Ceiling(dt / 0.05f);
            if (n > 8) n = 8;
            float step = dt / n;
            var part = new SimEvents();
            for (int i = 0; i < n; i++)
            {
                part.Clear();
                bool ping = input.Ping && i == 0;
                bool breath = input.Breath && i == 0;
                var slice = new SimInput { X = input.X, Y = input.Y, Ping = ping, Breath = breath };
                TickOnce(run, step, slice, part);
                ev.Absorb(part);
                if (run.Finished || run.DraftOpen) break;
            }
        }

        static void TickOnce(RunState run, float dt, SimInput input, SimEvents ev)
        {
            if (run.Tutorial) run.TutorialTime += dt;
            if (run.Chaos > 0f) run.Chaos -= dt;
            if (run.Invuln > 0f) run.Invuln -= dt;
            if (run.PingCd > 0f) run.PingCd -= dt;
            if (run.BreathCd > 0f) run.BreathCd -= dt;
            if (run.Breath > 0f) run.Breath -= dt;
            if (run.Anchor > 0f) run.Anchor -= dt;
            if (run.SonarTime > 0f) run.SonarTime -= dt;

            if (input.Breath && run.BreathCd <= 0f && run.Breath <= 0f)
            {
                run.Breath = ModuleBook.BreathDuration(run);
                run.BreathCd = ModuleBook.BreathCooldown(run);
                ev.BreathStarted = true;
                NoteUse(run, ModuleId.YaBu);
            }

            float mag = (float)Math.Sqrt(input.X * input.X + input.Y * input.Y);
            if (mag > 0.18f)
            {
                run.FaceX = input.X / mag;
                run.FaceY = input.Y / mag;
            }

            if (run.Anchor <= 0f) MovePlayer(run, input.X, input.Y, dt, ev);
            if (run.Finished) return;
            Footstep(run, mag, dt);
            DecayDisturbance(run, dt);
            DrainAir(run, dt, ev);
            if (run.Finished) return;
            DecayGlow(run, dt);
            UpdateBlasts(run, dt, ev);
            if (run.Finished) return;
            UpdateDrops(run, dt, ev);
            if (run.Finished) return;
            UpdateAuto(run, dt, ev);
            if (run.Finished) return;

            if (input.Ping && run.PingCd <= 0f)
            {
                run.PingCd = Rules.PingCooldown;
                if (run.Breath > 0f) run.Breath = 0f;
                ManualPing(run, ev);
                ev.Pinged = true;
            }
            if (run.Finished) return;

            TickActors(run, dt);
            Contacts(run, ev);
            if (run.Finished) return;
            UpdateGate(run, ev);
            TryChangeRoom(run, ev);
            if (!run.DraftOpen && run.DraftQueue > 0 && !run.Finished) OpenDraft(run, ev);
        }

        public static void Choose(RunState run, int index)
        {
            if (run == null || !run.DraftOpen || run.Offer == null) return;
            if (index < 0 || index >= run.Offer.Length) return;
            int id = run.Offer[index];
            if (id < 0 || id > 11) return;
            if (run.Levels[id] < 3) run.Levels[id]++;
            run.Picks++;
            run.UseScore[id] += 3;
            if ((ModuleId)id == ModuleId.QunZao)
                run.AutoTimer = ModuleBook.AutoInterval(run);
            run.DraftOpen = false;
            run.Offer = new int[0];
            var ev = new SimEvents();
            if (run.DraftQueue > 0 && !run.Finished) OpenDraft(run, ev);
        }

        public static bool CanRetry(RunState run)
        {
            return run != null && run.Finished && !run.Victory && !run.RetryUsed && run.S < Rules.DisturbQuiet;
        }

        public static void Retry(RunState run)
        {
            if (!CanRetry(run)) return;
            run.RetryUsed = true;
            run.Finished = false;
            run.Victory = false;
            run.Cause = "";
            run.Life = 3f;
            run.Air = run.MaxAir * 0.5f;
            run.Invuln = 1.2f;
            run.Chaos = 0f;
            run.MenuPause = false;
            run.DraftOpen = false;
            run.PlayerX = run.Current.SpawnX;
            run.PlayerY = run.Current.SpawnY;
        }

        static void ManualPing(RunState run, SimEvents ev)
        {
            if (run.HasPing)
            {
                run.PrevX = run.PingX;
                run.PrevY = run.PingY;
                run.HasPrev = true;
            }
            run.PingX = run.PlayerX;
            run.PingY = run.PlayerY;
            run.HasPing = true;
            run.ManualPings++;
            for (int i = 0; i < 12; i++)
                if (run.Levels[i] > 0) run.UseScore[i]++;

            bool refundCell = PlayerCellSurveyed(run);
            if (ModuleBook.Lv(run, ModuleId.ChenDi) > 0) run.Anchor = 0.25f;
            run.BossHitThisCast = false;

            float dmg = ModuleBook.ManualDamage(run);
            Cast(run, run.Pos, ModuleBook.Radius(run), ModuleBook.UseFan(run), dmg, ModuleBook.Afterglow(run), true, true, ev);

            if (refundCell)
            {
                float back = ModuleBook.AirRefund(run);
                if (back > 0f)
                {
                    run.Air += back;
                    if (run.Air > run.MaxAir) run.Air = run.MaxAir;
                }
            }
            AddDisturbance(run, ModuleBook.ManualDisturbance(run), true, ev);
        }

        static void Cast(RunState run, Vec2 origin, float radius, bool fan, float damage, float glow, bool manual, bool primary, SimEvents ev)
        {
            Room room = run.Current;
            castDepth++;
            if (castDepth == 1) run.BossHitThisCast = false;
            for (int i = 0; i < room.Actors.Count; i++) room.Actors[i].HitThisCast = false;

            var face = new Vec2(run.FaceX, run.FaceY);
            bool newWall = false;
            bool touched = false;
            var walls = new List<Vec2>();

            int x0 = (int)Math.Floor(origin.X - radius - 1f);
            int x1 = (int)Math.Floor(origin.X + radius + 1f);
            int y0 = (int)Math.Floor(origin.Y - radius - 1f);
            int y1 = (int)Math.Floor(origin.Y + radius + 1f);
            for (int y = y0; y <= y1; y++)
            {
                for (int x = x0; x <= x1; x++)
                {
                    if (!room.In(x, y)) continue;
                    var center = new Vec2(x + 0.5f, y + 0.5f);
                    if (!InShape(center, origin, radius, fan, face)) continue;
                    Cell cell = room.Get(x, y);
                    if (cell.Kind == CellKind.Void) continue;
                    if ((cell.Kind == CellKind.Wall || cell.Kind == CellKind.Spike) && !cell.Surveyed)
                        newWall = true;
                    if (cell.Kind == CellKind.Wall || cell.Kind == CellKind.Spike)
                        walls.Add(center);
                }
            }

            for (int i = 0; i < room.Actors.Count; i++)
            {
                Actor actor = room.Actors[i];
                if (!actor.Alive) continue;
                var pos = new Vec2(actor.X, actor.Y);
                if (!InShape(pos, origin, radius, fan, face)) continue;
                touched = true;
                actor.Marked = true;
                float dmg = damage;
                if (ModuleBook.Lv(run, ModuleId.FanXiang) > 0 && ActorSurveyed(room, actor))
                    dmg *= 0.5f;
                Pulse(run, actor, dmg, primary, origin, radius, fan, face, ev);
            }

            for (int y = y0; y <= y1; y++)
            {
                for (int x = x0; x <= x1; x++)
                {
                    if (!room.In(x, y)) continue;
                    var center = new Vec2(x + 0.5f, y + 0.5f);
                    if (!InShape(center, origin, radius, fan, face)) continue;
                    Light(room, x, y, glow, true);
                }
            }

            if (manual && primary)
            {
                if (run.TutorialMossPending) ForceTutorialMoss(run, glow, ev, ref touched);
                ExtendCorridor(run, origin, radius, glow);
                Bounce(run, origin, walls, glow, damage, ev);
                ScheduleDrops(run, origin, radius, fan, face);
                int echo = ModuleBook.Lv(run, ModuleId.CanXiang);
                if (echo > 0)
                {
                    run.Blasts.Add(new Blast
                    {
                        X = origin.X,
                        Y = origin.Y,
                        Time = 2f,
                        Radius = ModuleBook.BlastRadius(run)
                    });
                    NoteUse(run, ModuleId.CanXiang);
                }
                if (newWall || touched)
                {
                    run.Effective++;
                    ev.Hit = true;
                    if (run.Effective >= Rules.EffectivePerDraft)
                    {
                        run.Effective = 0;
                        QueueDraft(run);
                    }
                }
            }

            if (glow > run.SonarTime) run.SonarTime = glow;
            if (castDepth == 1) run.BossHitThisCast = false;
            castDepth--;
        }

        static void ForceTutorialMoss(RunState run, float glow, SimEvents ev, ref bool touched)
        {
            Room room = run.Current;
            Actor moss = null;
            for (int i = 0; i < room.Actors.Count; i++)
            {
                if (room.Actors[i].TutorialMoss && room.Actors[i].Alive)
                {
                    moss = room.Actors[i];
                    break;
                }
            }
            run.TutorialMossPending = false;
            ev.TutorialHit = true;
            if (moss == null) return;
            Light(room, moss.WallX, moss.WallY, glow, true);
            Light(room, (int)Math.Floor(moss.BumpX), (int)Math.Floor(moss.BumpY), glow, true);
            moss.Marked = true;
            touched = true;
            if (moss.HitThisCast) return;
            moss.HitThisCast = true;
            moss.Hp -= 1f;
            if (moss.Hp <= 0f) moss.Alive = false;
        }

        static void Bounce(RunState run, Vec2 origin, List<Vec2> walls, float glow, float damage, SimEvents ev)
        {
            int lv = ModuleBook.Lv(run, ModuleId.ErCi);
            if (lv <= 0 || walls.Count == 0) return;
            var first = Farthest(origin, walls, 4);
            var secondWalls = new List<Vec2>();
            for (int i = 0; i < first.Count; i++)
            {
                Vec2 center = PullIntoRoom(origin, first[i]);
                Cast(run, center, 2.5f, false, damage, glow, false, false, ev);
                if (lv >= 2) CollectWalls(run.Current, center, 2.5f, secondWalls);
            }
            if (lv >= 2)
            {
                var second = Farthest(origin, secondWalls, 3);
                for (int i = 0; i < second.Count; i++)
                {
                    Vec2 center = PullIntoRoom(origin, second[i]);
                    Cast(run, center, 2.5f, false, damage, glow, false, false, ev);
                }
            }
            if (lv >= 3)
            {
                Room room = run.Current;
                int extra = 0;
                for (int i = 0; i < room.Actors.Count && extra < 3; i++)
                {
                    Actor actor = room.Actors[i];
                    if (!actor.Alive || actor.Kind == ActorKind.Boss) continue;
                    if (!actor.HitThisCast && !actor.Marked) continue;
                    Cast(run, new Vec2(actor.X, actor.Y), 2.5f, false, damage, glow, false, false, ev);
                    extra++;
                }
            }
        }

        static void CollectWalls(Room room, Vec2 origin, float radius, List<Vec2> into)
        {
            int x0 = (int)Math.Floor(origin.X - radius);
            int x1 = (int)Math.Floor(origin.X + radius);
            int y0 = (int)Math.Floor(origin.Y - radius);
            int y1 = (int)Math.Floor(origin.Y + radius);
            for (int y = y0; y <= y1; y++)
            {
                for (int x = x0; x <= x1; x++)
                {
                    if (!room.In(x, y)) continue;
                    CellKind k = room.Get(x, y).Kind;
                    if (k != CellKind.Wall && k != CellKind.Spike) continue;
                    var c = new Vec2(x + 0.5f, y + 0.5f);
                    if (Vec2.Dist(c, origin) <= radius) into.Add(c);
                }
            }
        }

        static List<Vec2> Farthest(Vec2 origin, List<Vec2> walls, int count)
        {
            var picked = new List<Vec2>();
            var used = new bool[walls.Count];
            for (int n = 0; n < count; n++)
            {
                int best = -1;
                float bestD = -1f;
                for (int i = 0; i < walls.Count; i++)
                {
                    if (used[i]) continue;
                    float d = Vec2.Dist(walls[i], origin);
                    bool near = false;
                    for (int p = 0; p < picked.Count; p++)
                    {
                        if (Vec2.Dist(picked[p], walls[i]) < 1.2f) near = true;
                    }
                    if (near) continue;
                    if (d > bestD)
                    {
                        bestD = d;
                        best = i;
                    }
                }
                if (best < 0) break;
                used[best] = true;
                picked.Add(walls[best]);
            }
            return picked;
        }

        static Vec2 PullIntoRoom(Vec2 origin, Vec2 wall)
        {
            Vec2 back = (origin - wall).Normalized * 0.85f;
            return wall + back;
        }

        static void ScheduleDrops(RunState run, Vec2 origin, float radius, bool fan, Vec2 face)
        {
            int lv = ModuleBook.Lv(run, ModuleId.DuiKong);
            if (lv <= 0) return;
            float pad = lv <= 1 ? 0f : (lv == 2 ? 1f : 2f);
            Room room = run.Current;
            for (int i = 0; i < room.Actors.Count; i++)
            {
                Actor actor = room.Actors[i];
                if (!actor.Alive) continue;
                var pos = new Vec2(actor.X, actor.Y);
                if (!InShape(pos, origin, radius + pad, fan, face)) continue;
                bool exists = false;
                for (int d = 0; d < run.Drops.Count; d++)
                    if (run.Drops[d].ActorId == actor.Id) exists = true;
                if (exists) continue;
                run.Drops.Add(new Droplet { ActorId = actor.Id, Time = 0.28f, X = actor.X, Y = actor.Y + 1.4f });
            }
        }

        static void ExtendCorridor(RunState run, Vec2 origin, float radius, float glow)
        {
            int extra = ModuleBook.CorridorExtra(run);
            if (extra <= 0) return;
            int stepX = Math.Abs(run.FaceX) >= Math.Abs(run.FaceY) ? (run.FaceX >= 0f ? 1 : -1) : 0;
            int stepY = stepX == 0 ? (run.FaceY >= 0f ? 1 : -1) : 0;
            Room room = run.Current;
            int cx = (int)Math.Floor(origin.X);
            int cy = (int)Math.Floor(origin.Y);
            if (!IsNarrow(room, cx, cy, stepX, stepY)) return;
            int lit = 0;
            for (int i = 1; i <= (int)Math.Ceiling(radius) + extra + 1 && lit < extra; i++)
            {
                int x = cx + stepX * i;
                int y = cy + stepY * i;
                if (!room.Walkable(x, y)) break;
                if (!IsNarrow(room, x, y, stepX, stepY)) break;
                float dist = i;
                if (dist <= radius) continue;
                Light(room, x, y, glow, true);
                Light(room, x + 1, y, glow, true);
                Light(room, x - 1, y, glow, true);
                Light(room, x, y + 1, glow, true);
                Light(room, x, y - 1, glow, true);
                lit++;
            }
        }

        static bool IsNarrow(Room room, int x, int y, int stepX, int stepY)
        {
            int px = -stepY;
            int py = stepX;
            if (px == 0 && py == 0) { px = 1; py = 0; }
            int width = 1;
            for (int s = -1; s <= 1; s += 2)
            {
                for (int i = 1; i <= 4; i++)
                {
                    int nx = x + px * i * s;
                    int ny = y + py * i * s;
                    if (!room.Walkable(nx, ny)) break;
                    width++;
                }
            }
            return width <= 3;
        }

        static void Pulse(RunState run, Actor actor, float damage, bool primary, Vec2 origin, float radius, bool fan, Vec2 face, SimEvents ev)
        {
            if (!actor.Alive || damage <= 0f || actor.HitThisCast) return;
            Room room = run.Current;
            if (actor.Kind == ActorKind.Moss)
            {
                var wall = new Vec2(actor.WallX + 0.5f, actor.WallY + 0.5f);
                bool lit = InShape(wall, origin, radius, fan, face);
                if (!lit && room.In(actor.WallX, actor.WallY) && room.Glow[room.Index(actor.WallX, actor.WallY)] > 0f)
                    lit = true;
                if (!lit) return;
            }
            else if (actor.Kind == ActorKind.Cavity)
            {
                if (actor.Expose <= 0f) return;
            }
            else if (actor.Kind == ActorKind.Boss)
            {
                if (run.BossHitThisCast) return;
                bool window = actor.Expose > 0f;
                if (!primary && !window) return;
                actor.Expose = 1.2f;
                actor.Charging = true;
                actor.ChargeT = 0f;
                actor.ChargeFromX = actor.X;
                actor.ChargeFromY = actor.Y;
                actor.ChargeToX = origin.X;
                actor.ChargeToY = origin.Y;
                actor.WeakPoints++;
                actor.HitThisCast = true;
                run.BossHitThisCast = true;
                if (actor.WeakPoints >= 3)
                {
                    actor.Alive = false;
                    Win(run, ev);
                }
                return;
            }

            actor.HitThisCast = true;
            actor.Hp -= damage;
            if (actor.Hp <= 0f)
            {
                actor.Hp = 0f;
                actor.Alive = false;
            }
        }

        static void UpdateBlasts(RunState run, float dt, SimEvents ev)
        {
            for (int i = run.Blasts.Count - 1; i >= 0; i--)
            {
                run.Blasts[i].Time -= dt;
                if (run.Blasts[i].Time > 0f) continue;
                var b = run.Blasts[i];
                run.Blasts.RemoveAt(i);
                Cast(run, new Vec2(b.X, b.Y), b.Radius, false, 1f, ModuleBook.Afterglow(run), false, false, ev);
                if (run.Finished) return;
            }
        }

        static void UpdateDrops(RunState run, float dt, SimEvents ev)
        {
            for (int i = run.Drops.Count - 1; i >= 0; i--)
            {
                run.Drops[i].Time -= dt;
                if (run.Drops[i].Time > 0f) continue;
                Droplet drop = run.Drops[i];
                run.Drops.RemoveAt(i);
                Actor actor = FindActor(run.Current, drop.ActorId);
                if (actor == null || !actor.Alive) continue;
                var face = new Vec2(run.FaceX, run.FaceY);
                Pulse(run, actor, 1f, false, new Vec2(actor.X, actor.Y), 0.6f, false, face, ev);
                if (run.Finished) return;
            }
        }

        static void UpdateAuto(RunState run, float dt, SimEvents ev)
        {
            if (ModuleBook.Lv(run, ModuleId.QunZao) <= 0) return;
            run.AutoTimer -= dt;
            if (run.AutoTimer > 0f) return;
            run.AutoTimer = ModuleBook.AutoInterval(run);
            run.BossHitThisCast = false;
            Cast(run, run.Pos, 2f, false, 0.5f, 0.4f + 0.5f * ModuleBook.Lv(run, ModuleId.ChangMing), false, false, ev);
            NoteUse(run, ModuleId.QunZao);
            AddDisturbance(run, 5f, true, ev);
        }

        public static void AddDisturbance(RunState run, float amount, bool fromSound, SimEvents ev)
        {
            if (amount <= 0f || run.Finished) return;
            float before = run.S;
            run.S += amount;
            if (run.S > run.MaxS) run.MaxS = run.S;
            bool entered = before < Rules.DisturbAwake && run.S >= Rules.DisturbAwake;
            if (run.S >= 100f)
            {
                if (run.MaxS < 100f) run.MaxS = 100f;
                run.S = 55f;
                run.Chaos = 0.3f;
                run.ChaosHappened = true;
                ev.Chaos = true;
                run.Life -= 1f;
                run.Invuln = 0.45f;
                ev.Damaged = true;
                if (run.Life <= 0f)
                {
                    run.Life = 0f;
                    Die(run, Causes.Chaos, ev);
                }
            }
            if (run.Finished) return;
            if (entered || run.S >= Rules.DisturbAwake)
            {
                Actor deep = FindAlive(run.Current, ActorKind.Deep);
                if (deep == null)
                {
                    if (entered || !run.AwakeLatched)
                    {
                        RoomBuilder.SpawnDeep(run, run.Current);
                        run.AwakeLatched = true;
                        ev.Awakened = true;
                    }
                }
                else if (fromSound)
                {
                    deep.Accel = 3.2f;
                    run.AwakeLatched = true;
                }
                else run.AwakeLatched = true;
            }
            else run.AwakeLatched = false;
        }

        static void DecayDisturbance(RunState run, float dt)
        {
            int x = (int)Math.Floor(run.PlayerX);
            int y = (int)Math.Floor(run.PlayerY);
            bool surveyed = run.Current.In(x, y) && run.Current.Get(x, y).Surveyed;
            float rate = surveyed ? 4f : 2f;
            run.S -= rate * dt;
            if (run.S < 0f) run.S = 0f;
            if (run.S < Rules.DisturbAwake) run.AwakeLatched = false;
        }

        static void DrainAir(RunState run, float dt, SimEvents ev)
        {
            int x = (int)Math.Floor(run.PlayerX);
            int y = (int)Math.Floor(run.PlayerY);
            bool surveyed = false;
            bool glow = run.Chaos > 0f;
            if (run.Current.In(x, y))
            {
                surveyed = run.Current.Get(x, y).Surveyed;
                if (run.Current.Glow[run.Current.Index(x, y)] > 0f) glow = true;
            }
            float rate = (surveyed && glow) ? 1f : 1.5f;
            run.Air -= rate * dt;
            if (run.Air <= 0f)
            {
                run.Air = 0f;
                Die(run, Causes.Air, ev);
            }
        }

        static void DecayGlow(RunState run, float dt)
        {
            Room room = run.Current;
            for (int i = 0; i < room.Glow.Length; i++)
            {
                if (room.Glow[i] > 0f) room.Glow[i] -= dt;
                if (room.Glow[i] < 0f) room.Glow[i] = 0f;
            }
            for (int a = 0; a < room.Actors.Count; a++)
            {
                Actor actor = room.Actors[a];
                if (actor.Kind != ActorKind.Cavity || !actor.Alive) continue;
                int x = (int)Math.Floor(actor.X);
                int y = (int)Math.Floor(actor.Y);
                bool cover = room.In(x, y) && room.Glow[room.Index(x, y)] > 0f;
                if (actor.Covered && !cover)
                {
                    actor.Expose = 1.15f;
                    actor.ProximityUsed = false;
                }
                actor.Covered = cover;
                if (actor.Expose > 0f) actor.Expose -= dt;
            }
        }

        static void TickActors(RunState run, float dt)
        {
            Room room = run.Current;
            for (int i = 0; i < room.Actors.Count; i++)
            {
                Actor actor = room.Actors[i];
                if (!actor.Alive) continue;
                if (actor.Kind == ActorKind.Fish) TickFish(run, room, actor, dt);
                else if (actor.Kind == ActorKind.Deep) TickDeep(run, room, actor, dt);
                else if (actor.Kind == ActorKind.Boss) TickBoss(room, actor, dt);
                else if (actor.Kind == ActorKind.Cavity && actor.Expose > 0f && !actor.ProximityUsed)
                {
                    float dx = actor.X - run.PlayerX;
                    float dy = actor.Y - run.PlayerY;
                    if (dx * dx + dy * dy <= 1.15f * 1.15f)
                    {
                        actor.ProximityUsed = true;
                        actor.Hp -= 1f;
                        actor.Marked = true;
                        if (actor.Hp <= 0f)
                        {
                            actor.Hp = 0f;
                            actor.Alive = false;
                        }
                    }
                }
            }
        }

        static void TickFish(RunState run, Room room, Actor actor, float dt)
        {
            if (run.S < Rules.DisturbQuiet)
            {
                actor.WanderT -= dt;
                if (actor.WanderT <= 0f)
                {
                    actor.WanderT = 1.5f + run.Rng.NextFloat();
                    PickWander(run, room, actor);
                }
                Step(room, actor, actor.WanderX, actor.WanderY, 0.62f, dt);
            }
            else
            {
                float tx = actor.HomeX;
                float ty = actor.HomeY;
                if (run.Breath > 0f && run.HasPing) { tx = run.PingX; ty = run.PingY; }
                else if (run.Breath <= 0f && run.HasFoot) { tx = run.FootX; ty = run.FootY; }
                else if (run.HasPing) { tx = run.PingX; ty = run.PingY; }
                Step(room, actor, tx, ty, 1.9f, dt);
            }
        }

        static void TickDeep(RunState run, Room room, Actor actor, float dt)
        {
            if (actor.Accel > 0f) actor.Accel -= dt;
            float speed = actor.Accel > 0f ? 4.15f : 2.75f;
            float tx = actor.HomeX;
            float ty = actor.HomeY;
            if (run.Breath > 0f && run.HasPrev) { tx = run.PrevX; ty = run.PrevY; }
            else if (run.HasPing) { tx = run.PingX; ty = run.PingY; }
            Step(room, actor, tx, ty, speed, dt);
        }

        static void TickBoss(Room room, Actor actor, float dt)
        {
            if (actor.Expose > 0f) actor.Expose -= dt;
            if (!actor.Charging) return;
            actor.ChargeT += dt;
            float u = actor.ChargeT / 0.48f;
            if (u > 1f) u = 1f;
            float nx = actor.ChargeFromX + (actor.ChargeToX - actor.ChargeFromX) * u;
            float ny = actor.ChargeFromY + (actor.ChargeToY - actor.ChargeFromY) * u;
            if (!CircleHitsSolid(room, nx, ny, 0.2f))
            {
                actor.X = nx;
                actor.Y = ny;
            }
            else actor.Charging = false;
            if (u >= 1f) actor.Charging = false;
        }

        static void PickWander(RunState run, Room room, Actor actor)
        {
            for (int n = 0; n < 8; n++)
            {
                int x = (int)Math.Floor(actor.HomeX) + run.Rng.NextInt(7) - 3;
                int y = (int)Math.Floor(actor.HomeY) + run.Rng.NextInt(7) - 3;
                if (!room.Walkable(x, y)) continue;
                actor.WanderX = x + 0.5f;
                actor.WanderY = y + 0.5f;
                return;
            }
        }

        static void Contacts(RunState run, SimEvents ev)
        {
            Room room = run.Current;
            for (int i = 0; i < room.Actors.Count; i++)
            {
                Actor actor = room.Actors[i];
                if (!actor.Alive) continue;
                if (run.Finished) return;
                if (actor.Kind == ActorKind.Moss)
                {
                    float dx = actor.BumpX - run.PlayerX;
                    float dy = actor.BumpY - run.PlayerY;
                    if (dx * dx + dy * dy < 0.42f * 0.42f) Hurt(run, 0.5f, Causes.Moss, ev);
                }
                else if (actor.Kind == ActorKind.Fish)
                {
                    float dx = actor.X - run.PlayerX;
                    float dy = actor.Y - run.PlayerY;
                    if (dx * dx + dy * dy < 0.46f * 0.46f) Hurt(run, 1f, Causes.Fish, ev);
                }
                else if (actor.Kind == ActorKind.Deep)
                {
                    float dx = actor.X - run.PlayerX;
                    float dy = actor.Y - run.PlayerY;
                    if (dx * dx + dy * dy < 0.52f * 0.52f) Hurt(run, 1f, Causes.Deep, ev);
                }
                else if (actor.Kind == ActorKind.Boss && actor.Charging)
                {
                    float dx = actor.X - run.PlayerX;
                    float dy = actor.Y - run.PlayerY;
                    if (dx * dx + dy * dy < 0.56f * 0.56f) Hurt(run, 1f, Causes.Boss, ev);
                }
            }
        }

        static void UpdateGate(RunState run, SimEvents ev)
        {
            Room room = run.Current;
            if (room.Boss || room.Cleared) return;
            if (room.SurveyScore() + 0.001f < ModuleBook.Threshold(run)) return;
            room.Cleared = true;
            if (!room.Drafted)
            {
                room.Drafted = true;
                ev.GateOpened = true;
                QueueDraft(run);
            }
        }

        static void TryChangeRoom(RunState run, SimEvents ev)
        {
            Room room = run.Current;
            if (room.Boss || !room.Cleared) return;
            if (run.RoomIndex >= run.Rooms.Count - 1) return;
            int x = (int)Math.Floor(run.PlayerX);
            int y = (int)Math.Floor(run.PlayerY);
            if (!room.In(x, y) || room.Get(x, y).Kind != CellKind.Gate) return;
            run.RoomIndex++;
            Room next = run.Current;
            run.PlayerX = next.SpawnX;
            run.PlayerY = next.SpawnY;
            run.MaxRoomReached = run.RoomIndex + 1;
            ev.RoomChanged = true;
        }

        static void MovePlayer(RunState run, float ix, float iy, float dt, SimEvents ev)
        {
            float mag = (float)Math.Sqrt(ix * ix + iy * iy);
            if (mag > 1f)
            {
                ix /= mag;
                iy /= mag;
            }
            float dx = ix * Rules.PlayerSpeed * dt;
            float dy = iy * Rules.PlayerSpeed * dt;
            TryAxis(run, dx, 0f, ev);
            if (!run.Finished) TryAxis(run, 0f, dy, ev);
        }

        static void TryAxis(RunState run, float dx, float dy, SimEvents ev)
        {
            float nx = run.PlayerX + dx;
            float ny = run.PlayerY + dy;
            int hx, hy;
            if (CircleHitsSolid(run.Current, nx, ny, Rules.PlayerRadius, out hx, out hy))
            {
                if (run.Current.In(hx, hy))
                {
                    Cell cell = run.Current.Get(hx, hy);
                    if (cell.Kind == CellKind.Spike && !cell.Surveyed)
                        Hurt(run, 1f, Causes.Spike, ev);
                }
                return;
            }
            run.PlayerX = nx;
            run.PlayerY = ny;
        }

        static void Footstep(RunState run, float mag, float dt)
        {
            if (mag < 0.2f || run.Breath > 0f) return;
            run.FootTimer -= dt;
            if (run.FootTimer > 0f) return;
            run.FootTimer = 0.38f;
            run.FootX = run.PlayerX;
            run.FootY = run.PlayerY;
            run.HasFoot = true;
        }

        static void Hurt(RunState run, float amount, string cause, SimEvents ev)
        {
            if (run.Finished || run.Invuln > 0f) return;
            run.Life -= amount;
            run.Invuln = 0.62f;
            ev.Damaged = true;
            if (run.Life <= 0.001f)
            {
                run.Life = 0f;
                Die(run, cause, ev);
            }
        }

        static void Die(RunState run, string cause, SimEvents ev)
        {
            if (run.Finished) return;
            run.Finished = true;
            run.Victory = false;
            run.Cause = cause;
            run.DraftOpen = false;
            run.DraftQueue = 0;
            ev.Died = true;
        }

        static void Win(RunState run, SimEvents ev)
        {
            if (run.Finished) return;
            run.Finished = true;
            run.Victory = true;
            run.Cause = Causes.Clear;
            run.DraftOpen = false;
            run.DraftQueue = 0;
            ev.Won = true;
        }

        static void QueueDraft(RunState run)
        {
            if (run.Finished) return;
            run.DraftQueue++;
        }

        static void OpenDraft(RunState run, SimEvents ev)
        {
            if (run.DraftQueue <= 0 || run.Finished) return;
            int[] offer = DrawOffer(run);
            if (offer.Length == 0)
            {
                run.DraftQueue = 0;
                return;
            }
            run.DraftQueue--;
            run.Offer = offer;
            run.DraftOpen = true;
            ev.DraftOpened = true;
        }

        static int[] DrawOffer(RunState run)
        {
            var pool = new List<int>();
            for (int i = 0; i < ModuleBook.UnlockOrder.Length; i++)
            {
                ModuleId id = ModuleBook.UnlockOrder[i];
                if (!ModuleBook.IsUnlocked(run.UnlockedCount, id)) continue;
                if (run.Levels[(int)id] >= 3) continue;
                pool.Add((int)id);
            }
            if (pool.Count == 0) return new int[0];
            int n = pool.Count < 3 ? pool.Count : 3;
            var offer = new int[n];
            for (int i = 0; i < n; i++)
            {
                int pick = run.Rng.NextInt(pool.Count);
                offer[i] = pool[pick];
                pool.RemoveAt(pick);
            }
            return offer;
        }

        public static bool InShape(Vec2 point, Vec2 origin, float radius, bool fan, Vec2 face)
        {
            float dx = point.X - origin.X;
            float dy = point.Y - origin.Y;
            float dist = (float)Math.Sqrt(dx * dx + dy * dy);
            if (dist > radius) return false;
            if (!fan || dist < 0.35f) return true;
            float inv = 1f / dist;
            float dot = dx * inv * face.X + dy * inv * face.Y;
            return dot >= FanCos;
        }

        static void Light(Room room, int x, int y, float dur, bool survey)
        {
            if (!room.In(x, y)) return;
            int i = room.Index(x, y);
            if (room.Cells[i].Kind == CellKind.Void) return;
            if (room.Glow[i] < dur) room.Glow[i] = dur;
            if (!survey) return;
            room.Cells[i].Surveyed = true;
            if (room.Cells[i].Kind == CellKind.Gate) room.GateFound = true;
        }

        static bool PlayerCellSurveyed(RunState run)
        {
            int x = (int)Math.Floor(run.PlayerX);
            int y = (int)Math.Floor(run.PlayerY);
            return run.Current.In(x, y) && run.Current.Get(x, y).Surveyed;
        }

        static bool ActorSurveyed(Room room, Actor actor)
        {
            int x = (int)Math.Floor(actor.X);
            int y = (int)Math.Floor(actor.Y);
            return room.In(x, y) && room.Get(x, y).Surveyed;
        }

        static Actor FindAlive(Room room, ActorKind kind)
        {
            for (int i = 0; i < room.Actors.Count; i++)
                if (room.Actors[i].Kind == kind && room.Actors[i].Alive) return room.Actors[i];
            return null;
        }

        static Actor FindActor(Room room, int id)
        {
            for (int i = 0; i < room.Actors.Count; i++)
                if (room.Actors[i].Id == id) return room.Actors[i];
            return null;
        }

        static void NoteUse(RunState run, ModuleId id)
        {
            if (ModuleBook.Lv(run, id) > 0) run.UseScore[(int)id]++;
        }

        static void Step(Room room, Actor actor, float tx, float ty, float speed, float dt)
        {
            int sx = (int)Math.Floor(actor.X);
            int sy = (int)Math.Floor(actor.Y);
            int gx = (int)Math.Floor(tx);
            int gy = (int)Math.Floor(ty);
            if (!room.In(sx, sy)) return;
            if (!room.Walkable(gx, gy))
            {
                gx = sx;
                gy = sy;
            }
            int nx = gx;
            int ny = gy;
            if ((sx != gx || sy != gy) && !NextStep(room, sx, sy, gx, gy, out nx, out ny))
            {
                nx = sx;
                ny = sy;
            }
            float cx = (sx == gx && sy == gy) ? tx : nx + 0.5f;
            float cy = (sx == gx && sy == gy) ? ty : ny + 0.5f;
            float dx = cx - actor.X;
            float dy = cy - actor.Y;
            float len = (float)Math.Sqrt(dx * dx + dy * dy);
            if (len < 0.001f) return;
            float step = speed * dt;
            if (step > len) step = len;
            float mx = actor.X + dx / len * step;
            float my = actor.Y + dy / len * step;
            if (!CircleHitsSolid(room, mx, actor.Y, 0.18f)) actor.X = mx;
            if (!CircleHitsSolid(room, actor.X, my, 0.18f)) actor.Y = my;
        }

        static bool NextStep(Room room, int sx, int sy, int gx, int gy, out int nx, out int ny)
        {
            nx = sx;
            ny = sy;
            int cap = room.W * room.H;
            var prev = new int[cap];
            var seen = new bool[cap];
            for (int i = 0; i < cap; i++) prev[i] = -1;
            var qx = new int[cap];
            var qy = new int[cap];
            int qh = 0;
            int qt = 0;
            qx[qt] = sx;
            qy[qt] = sy;
            qt++;
            seen[room.Index(sx, sy)] = true;
            bool found = false;
            int[] ox = { 1, -1, 0, 0 };
            int[] oy = { 0, 0, 1, -1 };
            while (qh < qt)
            {
                int x = qx[qh];
                int y = qy[qh];
                qh++;
                if (x == gx && y == gy) { found = true; break; }
                for (int k = 0; k < 4; k++)
                {
                    int ax = x + ox[k];
                    int ay = y + oy[k];
                    if (!room.Walkable(ax, ay)) continue;
                    int idx = room.Index(ax, ay);
                    if (seen[idx]) continue;
                    seen[idx] = true;
                    prev[idx] = room.Index(x, y);
                    qx[qt] = ax;
                    qy[qt] = ay;
                    qt++;
                }
            }
            if (!found) return false;
            int cur = room.Index(gx, gy);
            int start = room.Index(sx, sy);
            int guard = 0;
            while (prev[cur] != start && prev[cur] != -1 && guard < cap)
            {
                cur = prev[cur];
                guard++;
            }
            nx = cur % room.W;
            ny = cur / room.W;
            return true;
        }

        static bool CircleHitsSolid(Room room, float x, float y, float r)
        {
            int hx, hy;
            return CircleHitsSolid(room, x, y, r, out hx, out hy);
        }

        static bool CircleHitsSolid(Room room, float x, float y, float r, out int hx, out int hy)
        {
            hx = -1;
            hy = -1;
            int x0 = (int)Math.Floor(x - r);
            int x1 = (int)Math.Floor(x + r);
            int y0 = (int)Math.Floor(y - r);
            int y1 = (int)Math.Floor(y + r);
            float r2 = r * r;
            for (int cy = y0; cy <= y1; cy++)
            {
                for (int cx = x0; cx <= x1; cx++)
                {
                    if (!room.In(cx, cy) || !room.Solid(cx, cy)) continue;
                    float minX = cx;
                    float maxX = cx + 1f;
                    float minY = cy;
                    float maxY = cy + 1f;
                    float qx = x < minX ? minX : (x > maxX ? maxX : x);
                    float qy = y < minY ? minY : (y > maxY ? maxY : y);
                    float dx = x - qx;
                    float dy = y - qy;
                    if (dx * dx + dy * dy < r2)
                    {
                        hx = cx;
                        hy = cy;
                        return true;
                    }
                }
            }
            return false;
        }
    }
}
