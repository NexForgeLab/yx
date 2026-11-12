using UnityEngine;
using UnityEngine.Rendering;

namespace Yx.Game
{
    public static class SpriteBank
    {
        static Sprite pixel;
        static Sprite player;
        static Sprite fish;
        static Sprite moss;
        static Sprite cavity;
        static Sprite deep;
        static Sprite boss;
        static Sprite ring;
        static Material material;

        public static Material WorldMaterial
        {
            get
            {
                if (material != null) return material;
                Shader shader = PickShader();
                material = new Material(shader);
                material.color = Color.white;
                return material;
            }
        }

        public static Shader PickShader()
        {
            bool urp = GraphicsSettings.defaultRenderPipeline != null;
            string[] names = urp
                ? new[]
                {
                    "Universal Render Pipeline/2D/Sprite-Unlit-Default",
                    "Universal Render Pipeline/2D/Sprite-Lit-Default",
                    "Sprites/Default",
                    "Yx/Block"
                }
                : new[]
                {
                    "Yx/Block",
                    "Sprites/Default",
                    "Unlit/Transparent",
                    "UI/Default"
                };
            for (int i = 0; i < names.Length; i++)
            {
                Shader shader = Shader.Find(names[i]);
                if (shader != null && shader.isSupported) return shader;
            }
            return Shader.Find("Hidden/InternalErrorShader");
        }

        public static Sprite Pixel()
        {
            if (pixel != null) return pixel;
            var tex = new Texture2D(4, 4, TextureFormat.RGBA32, false);
            tex.filterMode = FilterMode.Point;
            var fill = new Color32(255, 255, 255, 255);
            var px = new Color32[16];
            for (int i = 0; i < px.Length; i++) px[i] = fill;
            tex.SetPixels32(px);
            tex.Apply(false, true);
            pixel = Sprite.Create(tex, new Rect(0, 0, 4, 4), new Vector2(0.5f, 0.5f), 4f);
            return pixel;
        }

        public static Sprite PlayerBody()
        {
            if (player != null) return player;
            player = Make("surveyor", 24, 24, PaintPlayer);
            return player;
        }

        public static Sprite FishSprite()
        {
            if (fish != null) return fish;
            fish = Make("fish", 16, 16, (p, x, y, w, h) =>
            {
                int dx = x - 8;
                int dy = y - 8;
                if (dx * dx + dy * dy * 2 < 36) p[y * w + x] = new Color32(61, 150, 156, 255);
            });
            return fish;
        }

        public static Sprite MossSprite()
        {
            if (moss != null) return moss;
            moss = Make("moss", 16, 16, (p, x, y, w, h) =>
            {
                if (y < 5 || x < 2 || x > 13) return;
                p[y * w + x] = new Color32(92, 122, 64, 255);
            });
            return moss;
        }

        public static Sprite CavitySprite()
        {
            if (cavity != null) return cavity;
            cavity = Make("cavity", 16, 16, (p, x, y, w, h) =>
            {
                int dx = x - 8;
                int dy = y - 8;
                if (dx * dx + dy * dy < 30) p[y * w + x] = new Color32(28, 40, 52, 255);
                if (dx * dx + dy * dy < 6) p[y * w + x] = new Color32(227, 155, 74, 255);
            });
            return cavity;
        }

        public static Sprite DeepSprite()
        {
            if (deep != null) return deep;
            deep = Make("deep", 20, 20, (p, x, y, w, h) =>
            {
                int dx = x - 10;
                int dy = y - 10;
                if (dx * dx + dy * dy < 70) p[y * w + x] = new Color32(18, 42, 58, 255);
                if (y > 8 && y < 12 && x > 6 && x < 14) p[y * w + x] = new Color32(227, 140, 64, 255);
            });
            return deep;
        }

        public static Sprite BossSprite()
        {
            if (boss != null) return boss;
            boss = Make("boss", 28, 28, (p, x, y, w, h) =>
            {
                bool edge = x < 2 || y < 2 || x > 25 || y > 25;
                bool cross = (x > 12 && x < 16) || (y > 12 && y < 16);
                if (edge || cross) p[y * w + x] = new Color32(232, 196, 140, 255);
            });
            return boss;
        }

        public static Sprite Ring()
        {
            if (ring != null) return ring;
            ring = Make("ring", 32, 32, (p, x, y, w, h) =>
            {
                float dx = x - 15.5f;
                float dy = y - 15.5f;
                float d = Mathf.Sqrt(dx * dx + dy * dy);
                if (d > 12f && d < 15.5f) p[y * w + x] = new Color32(232, 186, 122, 220);
            });
            return ring;
        }

        static Sprite Make(string name, int w, int h, System.Action<Color32[], int, int, int, int> paint)
        {
            var tex = new Texture2D(w, h, TextureFormat.RGBA32, false);
            tex.filterMode = FilterMode.Point;
            tex.wrapMode = TextureWrapMode.Clamp;
            var px = new Color32[w * h];
            for (int y = 0; y < h; y++)
                for (int x = 0; x < w; x++)
                    paint(px, x, y, w, h);
            tex.SetPixels32(px);
            tex.Apply(false, true);
            return Sprite.Create(tex, new Rect(0, 0, w, h), new Vector2(0.5f, 0.5f), w);
        }

        static void PaintPlayer(Color32[] p, int x, int y, int w, int h)
        {
            bool body = x > 6 && x < 16 && y > 3 && y < 16;
            bool head = (x - 11) * (x - 11) + (y - 18) * (y - 18) < 12;
            bool drum = (x - 18) * (x - 18) + (y - 8) * (y - 8) < 10;
            bool scroll = x > 2 && x < 6 && y > 6 && y < 18;
            if (body || head) p[y * w + x] = Palette.Player;
            if (drum) p[y * w + x] = Palette.Drum;
            if (scroll) p[y * w + x] = Palette.WallLine;
        }
    }
}
