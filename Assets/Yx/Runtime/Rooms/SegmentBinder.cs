using UnityEngine;

namespace Yx.Game
{
    public static class SegmentBinder
    {
        public static void LoadAuthored()
        {
            SegmentAuthoring[] authored = Resources.LoadAll<SegmentAuthoring>("SegmentPrefabs");
            for (int i = 0; i < authored.Length; i++)
            {
                SegmentAuthoring piece = authored[i];
                if (piece == null) continue;
                Yx.Sim.SegmentLibrary.TryReplace(piece.segmentId, piece.role, piece.grid);
            }
        }
    }
}
