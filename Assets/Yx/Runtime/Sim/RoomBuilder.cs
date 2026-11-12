using System;
using System.Collections.Generic;
using System.Text;

namespace Yx.Sim
{
    public static class RoomBuilder
    {
        public static Room BuildSurvey(RunState run, int roomIndex, bool tutorial)
        {
            int chambers = 2 + run.Rng.NextInt(2);
            if (tutorial) chambers = 2;
            var parts = new List<Segment>();
            var names = new StringBuilder();
            var used = new HashSet<int>();
            for (int i = 0; i < chambers; i++)
            {
                if (i > 0)
                {
                    parts.Add(SegmentLibrary.Corridor);
                    if (names.Length > 0) names.Append('+');
                    names.Append(SegmentLibrary.Corridor.Id);
                }
                int pick = run.Rng.NextInt(SegmentLibrary.Chambers.Length);
                int guard = 0;
                while (used.Contains(pick) && guard < 8)
                {
                    pick = run.Rng.NextInt(SegmentLibrary.Chambers.Length);
                    guard++;
                }
                used.Add(pick);
                parts.Add(SegmentLibrary.Chambers[pick]);
                if (names.Length > 0) names.Append('+');
                names.Append(SegmentLibrary.Chambers[pick].Id);
            }
            Room room = Stamp(run, parts, false);
            room.ChamberCount = chambers;
            room.BuiltFrom = names.ToString();
            room.Boss = false;
            FinalizeSurvey(run, room, tutorial && roomIndex == 0);
            return room;
        }

        public static Room BuildBoss(RunState run)
        {
            var parts = new List<Segment> { SegmentLibrary.Boss };
            Room room = Stamp(run, parts, true);
            room.Boss = true;
            room.ChamberCount = 1;
            room.BuiltFrom = SegmentLibrary.Boss.Id;
            PlaceMarkers(run, room);
            EnsureSpawn(room);
            return room;
        }

        static Room Stamp(RunState run, List<Segment> parts, bool boss)
        {
            int maxW = 0;
            int totalH = 0;
            for (int i = 0; i < parts.Count; i++)
            {
                if (parts[i].Width > maxW) maxW = parts[i].Width;
                totalH += parts[i].Height;
            }
            var room = new Room
            {
                W = maxW,
                H = totalH,
                Cells = new Cell[maxW * totalH],
                Marks = new char[maxW * totalH],
                Glow = new float[maxW * totalH]
            };
            for (int i = 0; i < room.Cells.Length; i++)
            {
                room.Cells[i].Kind = CellKind.Void;
                room.Marks[i] = ' ';
            }

            int cursor = 0;
            for (int p = 0; p < parts.Count; p++)
            {
                Segment seg = parts[p];
                int x0 = (maxW - seg.Width) / 2;
                for (int row = 0; row < seg.Height; row++)
                {
                    int y = cursor + (seg.Height - 1 - row);
                    string line = seg.Rows[row];
                    for (int x = 0; x < seg.Width; x++)
                    {
                        int idx = room.Index(x0 + x, y);
                        room.Cells[idx] = CellFrom(line[x]);
                        room.Marks[idx] = line[x];
                    }
                }
                cursor += seg.Height;
            }
            if (!boss) SealBorder(room);
            return room;
        }

        static Cell CellFrom(char ch)
        {
            var cell = new Cell();
            switch (ch)
            {
                case '~': cell.Kind = CellKind.Water; break;
                case '^': cell.Kind = CellKind.Spike; break;
                case '#':
                case 'M': cell.Kind = CellKind.Wall; break;
                case 'G': cell.Kind = CellKind.Gate; break;
                case 'S':
                case 'F':
                case 'C':
                case '.':
                default:
                    cell.Kind = CellKind.Floor;
                    break;
            }
            return cell;
        }

        static void SealBorder(Room room)
        {
            for (int x = 0; x < room.W; x++)
            {
                Seal(room, x, 0);
                Seal(room, x, room.H - 1);
            }
            for (int y = 0; y < room.H; y++)
            {
                Seal(room, 0, y);
                Seal(room, room.W - 1, y);
            }
        }

        static void Seal(Room room, int x, int y)
        {
            int i = room.Index(x, y);
            CellKind k = room.Cells[i].Kind;
            if (k == CellKind.Floor || k == CellKind.Water || k == CellKind.Gate)
                room.Cells[i].Kind = CellKind.Wall;
        }

        static void FinalizeSurvey(RunState run, Room room, bool tutorial)
        {
            PlaceMarkers(run, room);
            EnsureSpawn(room);
            PlaceGate(room);
            if (tutorial) EnsureTutorialMoss(run, room);
            if (CountKind(room, ActorKind.Fish) == 0) SpawnFishNear(run, room, room.SpawnX, room.SpawnY + 3f);
            if (CountKind(room, ActorKind.Moss) == 0) EnsureTutorialMoss(run, room);
        }

        static void PlaceMarkers(RunState run, Room room)
        {
            float spawnX = -1f;
            float spawnY = 9999f;
            bool hasSpawn = false;
            for (int y = 0; y < room.H; y++)
            {
                for (int x = 0; x < room.W; x++)
                {
                    char mark = MarkerAt(room, x, y);
                    if (mark == 'S')
                    {
                        if (!hasSpawn || y < spawnY)
                        {
                            hasSpawn = true;
                            spawnX = x + 0.5f;
                            spawnY = y + 0.5f;
                        }
                    }
                    else if (mark == 'F' && room.Walkable(x, y))
                    {
                        AddFish(run, room, x + 0.5f, y + 0.5f);
                    }
                    else if (mark == 'C' && room.Walkable(x, y))
                    {
                        AddCavity(run, room, x + 0.5f, y + 0.5f);
                    }
                    else if (mark == 'M')
                    {
                        TryAddMoss(run, room, x, y);
                    }
                }
            }
            if (hasSpawn)
            {
                room.SpawnX = spawnX;
                room.SpawnY = spawnY;
            }
        }

        static char MarkerAt(Room room, int x, int y)
        {
            if (!room.In(x, y) || room.Marks == null) return '\0';
            return room.Marks[room.Index(x, y)];
        }

        static void EnsureSpawn(Room room)
        {
            if (room.SpawnX > 0.1f || room.SpawnY > 0.1f)
            {
                int sx = (int)Math.Floor(room.SpawnX);
                int sy = (int)Math.Floor(room.SpawnY);
                if (room.Walkable(sx, sy)) return;
            }
            for (int y = 0; y < room.H; y++)
            {
                for (int x = 0; x < room.W; x++)
                {
                    if (!room.Walkable(x, y)) continue;
                    room.SpawnX = x + 0.5f;
                    room.SpawnY = y + 0.5f;
                    return;
                }
            }
        }

        static void PlaceGate(Room room)
        {
            int bestX = -1;
            int bestY = -1;
            float bestScore = -999f;
            for (int y = 1; y < room.H - 1; y++)
            {
                for (int x = 1; x < room.W - 1; x++)
                {
                    if (!room.Walkable(x, y)) continue;
                    float dx = (x + 0.5f) - room.SpawnX;
                    float dy = (y + 0.5f) - room.SpawnY;
                    float score = dy * 3f - Math.Abs((x + 0.5f) - room.W * 0.5f) * 0.15f;
                    if (dx * dx + dy * dy < 9f) continue;
                    if (score > bestScore)
                    {
                        bestScore = score;
                        bestX = x;
                        bestY = y;
                    }
                }
            }
            if (bestX < 0) return;
            int i = room.Index(bestX, bestY);
            room.Cells[i].Kind = CellKind.Gate;
        }

        public static void EnsureTutorialMoss(RunState run, Room room)
        {
            Actor nearest = null;
            float best = 999f;
            for (int i = 0; i < room.Actors.Count; i++)
            {
                if (room.Actors[i].Kind != ActorKind.Moss || !room.Actors[i].Alive) continue;
                float d = Dist(room.Actors[i].BumpX, room.Actors[i].BumpY, room.SpawnX, room.SpawnY);
                if (d < best)
                {
                    best = d;
                    nearest = room.Actors[i];
                }
            }
            if (nearest != null && best <= 4.2f)
            {
                nearest.TutorialMoss = true;
                run.TutorialMossPending = true;
                return;
            }
            int sx = (int)Math.Floor(room.SpawnX);
            int sy = (int)Math.Floor(room.SpawnY);
            int foundX = -1;
            int foundY = -1;
            float foundD = 999f;
            for (int y = 0; y < room.H; y++)
            {
                for (int x = 0; x < room.W; x++)
                {
                    if (room.Get(x, y).Kind != CellKind.Wall && room.Get(x, y).Kind != CellKind.Spike) continue;
                    if (!HasFloorNeighbor(room, x, y)) continue;
                    float d = Dist(x + 0.5f, y + 0.5f, room.SpawnX, room.SpawnY);
                    if (d < 1.4f || d > 4.3f) continue;
                    if (d < foundD)
                    {
                        foundD = d;
                        foundX = x;
                        foundY = y;
                    }
                }
            }
            if (foundX >= 0)
            {
                Actor moss = TryAddMoss(run, room, foundX, foundY);
                if (moss != null)
                {
                    moss.TutorialMoss = true;
                    run.TutorialMossPending = true;
                }
            }
        }

        static bool HasFloorNeighbor(Room room, int x, int y)
        {
            return room.Walkable(x + 1, y) || room.Walkable(x - 1, y) || room.Walkable(x, y + 1) || room.Walkable(x, y - 1);
        }

        static Actor TryAddMoss(RunState run, Room room, int x, int y)
        {
            int fx = x;
            int fy = y;
            bool found = false;
            float best = 999f;
            int[] dx = { 1, -1, 0, 0 };
            int[] dy = { 0, 0, 1, -1 };
            for (int k = 0; k < 4; k++)
            {
                int nx = x + dx[k];
                int ny = y + dy[k];
                if (!room.Walkable(nx, ny)) continue;
                float d = Dist(nx + 0.5f, ny + 0.5f, room.W * 0.5f, room.H * 0.5f);
                if (!found || d < best)
                {
                    found = true;
                    best = d;
                    fx = nx;
                    fy = ny;
                }
            }
            if (!found) return null;
            var a = new Actor
            {
                Id = run.NextActorId++,
                Kind = ActorKind.Moss,
                X = fx + 0.5f,
                Y = fy + 0.5f,
                HomeX = fx + 0.5f,
                HomeY = fy + 0.5f,
                BumpX = fx + 0.5f,
                BumpY = fy + 0.5f,
                WallX = x,
                WallY = y,
                Hp = 1f,
                Alive = true
            };
            room.Actors.Add(a);
            return a;
        }

        static void AddFish(RunState run, Room room, float x, float y)
        {
            room.Actors.Add(new Actor
            {
                Id = run.NextActorId++,
                Kind = ActorKind.Fish,
                X = x,
                Y = y,
                HomeX = x,
                HomeY = y,
                WanderX = x,
                WanderY = y,
                Hp = 2f,
                WanderT = 0.4f
            });
        }

        static void AddCavity(RunState run, Room room, float x, float y)
        {
            room.Actors.Add(new Actor
            {
                Id = run.NextActorId++,
                Kind = ActorKind.Cavity,
                X = x,
                Y = y,
                HomeX = x,
                HomeY = y,
                Hp = 2f
            });
        }

        static void SpawnFishNear(RunState run, Room room, float x, float y)
        {
            for (int r = 2; r < 8; r++)
            {
                for (int iy = -r; iy <= r; iy++)
                {
                    for (int ix = -r; ix <= r; ix++)
                    {
                        int cx = (int)Math.Floor(x) + ix;
                        int cy = (int)Math.Floor(y) + iy;
                        if (!room.Walkable(cx, cy)) continue;
                        AddFish(run, room, cx + 0.5f, cy + 0.5f);
                        return;
                    }
                }
            }
        }

        public static void AddBoss(RunState run, Room room)
        {
            float cx = room.W * 0.5f;
            float cy = room.H * 0.55f;
            int ix = (int)Math.Floor(cx);
            int iy = (int)Math.Floor(cy);
            if (!room.Walkable(ix, iy))
            {
                cx = room.SpawnX;
                cy = room.SpawnY + 4f;
            }
            room.Actors.Add(new Actor
            {
                Id = run.NextActorId++,
                Kind = ActorKind.Boss,
                X = cx,
                Y = cy,
                HomeX = cx,
                HomeY = cy,
                Hp = 3f
            });
        }

        public static Actor SpawnDeep(RunState run, Room room)
        {
            int bestX = (int)Math.Floor(room.SpawnX);
            int bestY = (int)Math.Floor(room.SpawnY);
            float best = -1f;
            for (int y = 1; y < room.H - 1; y++)
            {
                for (int x = 1; x < room.W - 1; x++)
                {
                    if (!room.Walkable(x, y)) continue;
                    float dx = (x + 0.5f) - run.PlayerX;
                    float dy = (y + 0.5f) - run.PlayerY;
                    float d = dx * dx + dy * dy;
                    if (d > best)
                    {
                        best = d;
                        bestX = x;
                        bestY = y;
                    }
                }
            }
            var a = new Actor
            {
                Id = run.NextActorId++,
                Kind = ActorKind.Deep,
                X = bestX + 0.5f,
                Y = bestY + 0.5f,
                HomeX = bestX + 0.5f,
                HomeY = bestY + 0.5f,
                Hp = 5f
            };
            room.Actors.Add(a);
            return a;
        }

        static int CountKind(Room room, ActorKind kind)
        {
            int n = 0;
            for (int i = 0; i < room.Actors.Count; i++)
                if (room.Actors[i].Kind == kind && room.Actors[i].Alive) n++;
            return n;
        }

        static float Dist(float x, float y, float x2, float y2)
        {
            float dx = x - x2;
            float dy = y - y2;
            return (float)Math.Sqrt(dx * dx + dy * dy);
        }
    }
}
