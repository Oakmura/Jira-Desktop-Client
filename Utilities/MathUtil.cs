namespace JiraClient.Utilities
{
    public static class MathUtil
    {
        private static readonly float Epsilon = 0.00001f;

        public static bool IsTheSameAs(this float value, float other)
        {
            return Math.Abs(value - other) < Epsilon;
        }
    }
}
