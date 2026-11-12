namespace Yx.Game
{
    public static class YxSession
    {
        public enum Kind
        {
            New,
            Resume
        }

        public static Kind Next = Kind.New;
    }
}
