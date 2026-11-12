using System.Collections.Generic;
using UnityEngine;
using Yx.Sim;

namespace Yx.Game
{
    public class WorldView : MonoBehaviour
    {
        public int PixelsPerTile = 4;
        Room room;
        Texture2D tex;
        SpriteRenderer mapRenderer;
        Sprite mapSprite;
        Transform actorRoot;
        readonly Dictionary<int, SpriteRenderer> actors = new Dictionary<int, SpriteRenderer>();
        Transform player;
        SpriteRenderer facingTick;
        readonly List<SpriteRenderer> rings = new List<SpriteRenderer>();
        readonly List<SpriteRenderer> drops = new List<SpriteRenderer>();
        Camera cam;

        public void Bind(Camera camera, int pixelsPerTile)
        {
            cam = camera;
            PixelsPerTile = pixelsPerTile < 1 ? 1 : pixelsPerTile;
            var mapGo = new GameObject("Map");
            mapGo.transform.SetParent(transform, false);
            mapRenderer = mapGo.AddComponent<SpriteRenderer>();
            mapRenderer.sharedMaterial = SpriteBank.WorldMaterial;
            mapRenderer.sortingOrder = 0;
            actorRoot = new GameObject("Actors").transform;
            actorRoot.SetParent(transform, false);
            player = new GameObject("Surveyor").transform;
            player.SetParent(transform, false);
            var body = player.gameObject.AddComponent<SpriteRenderer>();
            body.sprite = SpriteBank.PlayerBody();
            body.sharedMaterial = SpriteBank.WorldMaterial;
            body.sortingOrder = 20;
            body.color = Color.white;
            var tickGo = new GameObject("Facing");
            tickGo.transform.SetParent(player, false);
            facingTick = tickGo.AddComponent<SpriteRenderer>();
            facingTick.sprite = SpriteBank.Pixel();
            facingTick.sharedMaterial = SpriteBank.WorldMaterial;
            facingTick.color = Palette.Drum;
            facingTick.sortingOrder = 21;
            facingTick.transform.localScale = new Vector3(0.35f, 0.12f, 1f);
        }

        public void Sync(RunState run, bool particles, SimEvents ev)
        {
            if (run == null || run.Rooms.Count == 0) return;
            if (room != run.Current) Rebuild(run.Current);
            Paint(run);
            SyncPlayer(run);
            SyncActors(run);
            if (particles && ev != null && ev.Pinged) Burst(run.PlayerX, run.PlayerY);
            FadeRings();
            if (cam != null)
            {
                float aspect = Screen.width / Mathf.Max(1f, (float)Screen.height);
                cam.orthographicSize = 5.6f / Mathf.Max(0.2f, aspect);
                cam.transform.position = new Vector3(run.PlayerX, run.PlayerY, -12f);
                cam.backgroundColor = ev != null && run.Chaos > 0.05f
                    ? new Color(0.35f, 0.22f, 0.12f, 1f)
                    : new Color(0.02f, 0.024f, 0.028f, 1f);
            }
        }

        void Rebuild(Room next)
        {
            room = next;
            if (tex != null) Destroy(tex);
            if (mapSprite != null) Destroy(mapSprite);
            int w = room.W * PixelsPerTile;
            int h = room.H * PixelsPerTile;
            tex = new Texture2D(w, h, TextureFormat.RGBA32, false);
            tex.filterMode = FilterMode.Point;
            tex.wrapMode = TextureWrapMode.Clamp;
            mapSprite = Sprite.Create(tex, new Rect(0, 0, w, h), new Vector2(0f, 0f), PixelsPerTile);
            mapRenderer.sprite = mapSprite;
            foreach (var pair in actors) if (pair.Value != null) Destroy(pair.Value.gameObject);
            actors.Clear();
        }

        void Paint(RunState run)
        {
            if (tex == null || room == null) return;
            int ppt = PixelsPerTile;
            var px = new Color32[tex.width * tex.height];
            float depth = ModuleBook.DepthAlpha(run);
            bool sonar = run.SonarTime > 0f || run.Chaos > 0f;
            for (int y = 0; y < room.H; y++)
            {
                for (int x = 0; x < room.W; x++)
                {
                    Cell cell = room.Get(x, y);
                    float glow = room.Glow[room.Index(x, y)];
                    float dx = (x + 0.5f) - run.PlayerX;
                    float dy = (y + 0.5f) - run.PlayerY;
                    bool self = dx * dx + dy * dy <= Rules.SelfGlow * Rules.SelfGlow;
                    bool chaos = run.Chaos > 0f;
                    bool outline = depth > 0f && sonar && !cell.Surveyed && cell.Kind != CellKind.Void && glow <= 0f && NeighborSurveyed(room, x, y);
                    Color32 color = Palette.Void;
                    if (chaos || glow > 0f) color = Lit(cell);
                    else if (self && cell.Kind != CellKind.Void) color = Self(cell, room.Cleared);
                    else if (outline) color = Palette.Outline;
                    else if (room.Cleared && cell.Kind == CellKind.Gate && self) color = Palette.Gate;
                    Stamp(px, tex.width, x, y, ppt, color, glow > 0f || chaos);
                }
            }
            tex.SetPixels32(px);
            tex.Apply(false);
        }

        static bool NeighborSurveyed(Room room, int x, int y)
        {
            return Surveyed(room, x + 1, y) || Surveyed(room, x - 1, y) || Surveyed(room, x, y + 1) || Surveyed(room, x, y - 1);
        }

        static bool Surveyed(Room room, int x, int y)
        {
            return room.In(x, y) && room.Get(x, y).Surveyed;
        }

        static Color32 Lit(Cell cell)
        {
            switch (cell.Kind)
            {
                case CellKind.Water: return Palette.Water;
                case CellKind.Wall: return Palette.Wall;
                case CellKind.Spike: return Palette.Spike;
                case CellKind.Gate: return Palette.Gate;
                case CellKind.Floor: return Palette.Floor;
                default: return Palette.Void;
            }
        }

        static Color32 Self(Cell cell, bool cleared)
        {
            if (cell.Kind == CellKind.Gate && cleared) return Palette.Gate;
            if (cell.Kind == CellKind.Wall || cell.Kind == CellKind.Spike || cell.Kind == CellKind.Gate) return Palette.SelfWall;
            if (cell.Kind == CellKind.Void) return Palette.Void;
            return Palette.SelfFloor;
        }

        static void Stamp(Color32[] px, int width, int x, int y, int ppt, Color32 color, bool line)
        {
            for (int dy = 0; dy < ppt; dy++)
            {
                for (int dx = 0; dx < ppt; dx++)
                {
                    Color32 c = color;
                    if (line && ppt > 1 && dy == ppt - 1 && (color.r > 40 || color.g > 40))
                        c = Palette.WallLine;
                    px[(y * ppt + dy) * width + (x * ppt + dx)] = c;
                }
            }
        }

        void SyncPlayer(RunState run)
        {
            player.position = new Vector3(run.PlayerX, run.PlayerY, 0f);
            float len = Mathf.Sqrt(run.FaceX * run.FaceX + run.FaceY * run.FaceY);
            float fx = len < 0.01f ? 0f : run.FaceX / len;
            float fy = len < 0.01f ? 1f : run.FaceY / len;
            facingTick.transform.localPosition = new Vector3(fx * 0.55f, fy * 0.55f, 0f);
            float angle = Mathf.Atan2(fy, fx) * Mathf.Rad2Deg;
            facingTick.transform.localRotation = Quaternion.Euler(0f, 0f, angle);
            var body = player.GetComponent<SpriteRenderer>();
            body.color = run.Breath > 0f ? new Color(0.75f, 0.9f, 0.88f, 0.85f) : Color.white;
            if (run.Anchor > 0f) body.color = Palette.Drum;
        }

        void SyncActors(RunState run)
        {
            var alive = new HashSet<int>();
            Room current = run.Current;
            for (int i = 0; i < current.Actors.Count; i++)
            {
                Actor actor = current.Actors[i];
                if (!actor.Alive) continue;
                if (!Visible(run, current, actor)) continue;
                alive.Add(actor.Id);
                SpriteRenderer view = ViewFor(actor);
                float x = actor.Kind == ActorKind.Moss ? actor.WallX + 0.5f : actor.X;
                float y = actor.Kind == ActorKind.Moss ? actor.WallY + 0.5f : actor.Y;
                view.transform.position = new Vector3(x, y, 0f);
                view.color = ColorFor(actor);
            }
            var drop = new List<int>();
            foreach (var pair in actors)
                if (!alive.Contains(pair.Key)) drop.Add(pair.Key);
            for (int i = 0; i < drop.Count; i++)
            {
                if (actors[drop[i]] != null) Destroy(actors[drop[i]].gameObject);
                actors.Remove(drop[i]);
            }
            while (drops.Count < run.Drops.Count)
            {
                var dot = new GameObject("Drop");
                dot.transform.SetParent(actorRoot, false);
                var sr = dot.AddComponent<SpriteRenderer>();
                sr.sprite = SpriteBank.Pixel();
                sr.sharedMaterial = SpriteBank.WorldMaterial;
                sr.color = Palette.WallLine;
                sr.sortingOrder = 14;
                sr.transform.localScale = new Vector3(0.18f, 0.45f, 1f);
                drops.Add(sr);
            }
            for (int i = 0; i < drops.Count; i++)
            {
                bool on = i < run.Drops.Count;
                drops[i].gameObject.SetActive(on);
                if (!on) continue;
                Droplet drop = run.Drops[i];
                drops[i].transform.position = new Vector3(drop.X, drop.Y - (0.28f - Mathf.Clamp(drop.Time, 0f, 0.28f)) * 4f, 0f);
            }
        }

        bool Visible(RunState run, Room current, Actor actor)
        {
            if (run.Chaos > 0f) return true;
            int x = actor.Kind == ActorKind.Moss ? actor.WallX : (int)Mathf.Floor(actor.X);
            int y = actor.Kind == ActorKind.Moss ? actor.WallY : (int)Mathf.Floor(actor.Y);
            if (!current.In(x, y)) return false;
            if (current.Glow[current.Index(x, y)] > 0f) return true;
            if (actor.Kind == ActorKind.Boss && actor.Expose > 0f) return true;
            if (actor.Kind == ActorKind.Cavity && actor.Expose > 0f)
            {
                float dx = actor.X - run.PlayerX;
                float dy = actor.Y - run.PlayerY;
                return dx * dx + dy * dy < 16f;
            }
            return false;
        }

        SpriteRenderer ViewFor(Actor actor)
        {
            SpriteRenderer view;
            if (actors.TryGetValue(actor.Id, out view) && view != null) return view;
            var go = new GameObject(actor.Kind.ToString());
            go.transform.SetParent(actorRoot, false);
            view = go.AddComponent<SpriteRenderer>();
            view.sharedMaterial = SpriteBank.WorldMaterial;
            view.sortingOrder = 12;
            switch (actor.Kind)
            {
                case ActorKind.Fish: view.sprite = SpriteBank.FishSprite(); break;
                case ActorKind.Moss: view.sprite = SpriteBank.MossSprite(); break;
                case ActorKind.Cavity: view.sprite = SpriteBank.CavitySprite(); break;
                case ActorKind.Deep: view.sprite = SpriteBank.DeepSprite(); view.transform.localScale = new Vector3(1.35f, 1.35f, 1f); break;
                default: view.sprite = SpriteBank.BossSprite(); view.transform.localScale = new Vector3(1.6f, 1.6f, 1f); break;
            }
            actors[actor.Id] = view;
            return view;
        }

        static Color ColorFor(Actor actor)
        {
            if (actor.Kind == ActorKind.Boss)
            {
                if (actor.Expose > 0f) return Color.white;
                return new Color(1f, 1f, 1f, 0.08f);
            }
            if (actor.Kind == ActorKind.Cavity)
                return actor.Expose > 0f ? Color.white : new Color(1f, 1f, 1f, 0.45f);
            if (actor.Kind == ActorKind.Deep && actor.Accel > 0f) return new Color(1f, 0.8f, 0.55f, 1f);
            return Color.white;
        }

        void Burst(float x, float y)
        {
            var go = new GameObject("Ripple");
            go.transform.SetParent(transform, false);
            go.transform.position = new Vector3(x, y, 0f);
            go.transform.localScale = Vector3.one * 0.4f;
            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = SpriteBank.Ring();
            sr.sharedMaterial = SpriteBank.WorldMaterial;
            sr.sortingOrder = 8;
            sr.color = new Color(0.9f, 0.7f, 0.4f, 0.85f);
            rings.Add(sr);
        }

        void FadeRings()
        {
            for (int i = rings.Count - 1; i >= 0; i--)
            {
                SpriteRenderer sr = rings[i];
                if (sr == null) { rings.RemoveAt(i); continue; }
                sr.transform.localScale += Vector3.one * (8f * Time.deltaTime);
                Color c = sr.color;
                c.a -= Time.deltaTime * 1.8f;
                sr.color = c;
                if (c.a <= 0f)
                {
                    Destroy(sr.gameObject);
                    rings.RemoveAt(i);
                }
            }
        }
    }
}
