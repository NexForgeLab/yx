using System;
using System.Collections.Generic;

namespace Yx.Sim
{
    public sealed class Segment
    {
        public string Id;
        public string Role;
        public string[] Rows;

        public int Width
        {
            get { return Rows.Length == 0 ? 0 : Rows[0].Length; }
        }

        public int Height
        {
            get { return Rows.Length; }
        }
    }

    public static class SegmentLibrary
    {
        public static readonly Segment[] Chambers;
        public static readonly Segment Corridor;
        public static readonly Segment Boss;

        static SegmentLibrary()
        {
            Chambers = new[]
            {
                Parse("pool", "chamber", new[]
                {
                    "######...######",
                    "#F...........C#",
                    "#.............#",
                    "#....~~~~~....#",
                    "#......~......#",
                    "#...##M##.....#",
                    "#...#...#.....#",
                    "#...#####..^..#",
                    "#......S......#",
                    "#.............#",
                    "######...######"
                }),
                Parse("fork", "chamber", new[]
                {
                    "######...######",
                    "#......F......#",
                    "#.............#",
                    "#..#.....#....#",
                    "#..#.....#..C.#",
                    "#..#.....#....#",
                    "#..#######....#",
                    "#.............#",
                    "#......S......#",
                    "#..^.......^..#",
                    "######...######"
                }),
                Parse("well", "chamber", new[]
                {
                    "######...######",
                    "#.............#",
                    "#..~~~........#",
                    "#..~F~...C....#",
                    "#..~~~........#",
                    "#......###....#",
                    "#......#M#....#",
                    "#......###....#",
                    "#......S......#",
                    "#.............#",
                    "######...######"
                }),
                Parse("pockets", "chamber", new[]
                {
                    "######...######",
                    "#F....#......C#",
                    "#.....#.......#",
                    "#.....#.......#",
                    "#.............#",
                    "#.............#",
                    "#.......##....#",
                    "#.......M#....#",
                    "#......S#.....#",
                    "#.............#",
                    "######...######"
                }),
                Parse("moss", "chamber", new[]
                {
                    "######...######",
                    "#.............#",
                    "#M...........M#",
                    "#.............#",
                    "#.~~~....~~~..#",
                    "#..F......C...#",
                    "#.~~~....~~~..#",
                    "#.............#",
                    "#......S......#",
                    "#M...........M#",
                    "######...######"
                }),
                Parse("cavity", "chamber", new[]
                {
                    "######...######",
                    "#......C......#",
                    "#.............#",
                    "#..####.#####.#",
                    "#..#.......#..#",
                    "#..#...F...#..#",
                    "#..#.......#..#",
                    "#..####.#####.#",
                    "#......S......#",
                    "#.............#",
                    "######...######"
                })
            };
            Corridor = Parse("shaft", "corridor", new[]
            {
                "######...######",
                "######...######",
                "######...######",
                "######...######"
            });
            Boss = BuildBoss();
            Validate(Corridor);
            Validate(Boss);
            for (int i = 0; i < Chambers.Length; i++) Validate(Chambers[i]);
        }

        public static Segment Parse(string id, string role, string[] rows)
        {
            var copy = new string[rows.Length];
            for (int i = 0; i < rows.Length; i++) copy[i] = rows[i];
            return new Segment { Id = id, Role = role, Rows = copy };
        }

        public static void Validate(Segment seg)
        {
            if (seg.Rows.Length == 0) throw new InvalidOperationException(seg.Id + " empty");
            int w = seg.Rows[0].Length;
            for (int i = 0; i < seg.Rows.Length; i++)
            {
                if (seg.Rows[i].Length != w)
                    throw new InvalidOperationException(seg.Id + " row " + i + " width " + seg.Rows[i].Length);
            }
        }

        public static string GridText(Segment seg)
        {
            return string.Join("\n", seg.Rows);
        }

        static Segment BuildBoss()
        {
            const int w = 17;
            const int h = 21;
            var rows = new char[h][];
            for (int y = 0; y < h; y++)
            {
                rows[y] = new char[w];
                for (int x = 0; x < w; x++)
                {
                    bool border = x == 0 || y == 0 || x == w - 1 || y == h - 1;
                    rows[y][x] = border ? '#' : '.';
                }
            }
            StampPillar(rows, 4, 6);
            StampPillar(rows, 11, 6);
            StampPillar(rows, 4, 12);
            StampPillar(rows, 11, 12);
            rows[2][4] = '^';
            rows[2][12] = '^';
            rows[h - 4][8] = 'S';
            var text = new string[h];
            for (int y = 0; y < h; y++) text[y] = new string(rows[y]);
            return Parse("unnamed-hall", "boss", text);
        }

        static void StampPillar(char[][] rows, int x, int y)
        {
            rows[y][x] = '#';
            rows[y][x + 1] = '#';
            rows[y + 1][x] = '#';
            rows[y + 1][x + 1] = '#';
        }

        public static List<Segment> All()
        {
            var list = new List<Segment>();
            for (int i = 0; i < Chambers.Length; i++) list.Add(Chambers[i]);
            list.Add(Corridor);
            list.Add(Boss);
            return list;
        }

        public static bool TryReplace(string id, string role, string grid)
        {
            if (string.IsNullOrEmpty(id) || string.IsNullOrEmpty(grid)) return false;
            Segment target = null;
            for (int i = 0; i < Chambers.Length; i++)
            {
                if (Chambers[i].Id == id) target = Chambers[i];
            }
            if (Corridor.Id == id) target = Corridor;
            if (Boss.Id == id) target = Boss;
            if (target == null) return false;
            string[] rows = grid.Replace("\r", "").Split('\n');
            int end = rows.Length;
            while (end > 0 && rows[end - 1].Length == 0) end--;
            if (end <= 0) return false;
            if (end != rows.Length)
            {
                var trimmed = new string[end];
                for (int i = 0; i < end; i++) trimmed[i] = rows[i];
                rows = trimmed;
            }
            try
            {
                Validate(Parse(id, string.IsNullOrEmpty(role) ? target.Role : role, rows));
            }
            catch (InvalidOperationException)
            {
                return false;
            }
            if (!string.IsNullOrEmpty(role)) target.Role = role;
            target.Rows = rows;
            return true;
        }
    }
}
