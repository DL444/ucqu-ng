namespace DL444.Ucqu.Models
{
    internal static class Utilities
    {
        public static string GetShortformName(string? name)
        {
            if (string.IsNullOrWhiteSpace(name))
            {
                return string.Empty;
            }
            string[] segments = name.Split(']');
            return segments.Length < 2 ? name : segments[1];
        }
    }
}