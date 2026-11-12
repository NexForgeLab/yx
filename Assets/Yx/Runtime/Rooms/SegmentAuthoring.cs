using UnityEngine;

namespace Yx.Game
{
    public class SegmentAuthoring : MonoBehaviour
    {
        public string segmentId;
        public string role;
        [TextArea(4, 24)]
        public string grid;
    }
}
