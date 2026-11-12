using UnityEngine;

namespace Yx.Game
{
    public static class Palette
    {
        public static readonly Color32 Void = new Color32(5, 6, 8, 255);
        public static readonly Color32 SelfFloor = new Color32(16, 40, 44, 255);
        public static readonly Color32 SelfWall = new Color32(46, 38, 32, 255);
        public static readonly Color32 SelfLine = new Color32(92, 74, 52, 255);
        public static readonly Color32 Floor = new Color32(31, 112, 116, 255);
        public static readonly Color32 Water = new Color32(18, 92, 104, 255);
        public static readonly Color32 Wall = new Color32(196, 120, 58, 255);
        public static readonly Color32 WallLine = new Color32(232, 186, 122, 255);
        public static readonly Color32 Spike = new Color32(164, 74, 52, 255);
        public static readonly Color32 Gate = new Color32(224, 156, 72, 255);
        public static readonly Color32 Outline = new Color32(58, 86, 82, 255);
        public static readonly Color32 Player = new Color32(127, 191, 178, 255);
        public static readonly Color32 Drum = new Color32(227, 155, 74, 255);
        public static readonly Color32 Ink = new Color32(232, 236, 232, 255);
        public static readonly Color32 Warn = new Color32(214, 92, 64, 255);
        public static readonly Color32 Panel = new Color32(12, 22, 26, 230);
        public static readonly Color32 Teal = new Color32(36, 110, 114, 255);

        public static Color Lerp(Color a, Color b, float t)
        {
            return new Color(a.r + (b.r - a.r) * t, a.g + (b.g - a.g) * t, a.b + (b.b - a.b) * t, 1f);
        }
    }
}
