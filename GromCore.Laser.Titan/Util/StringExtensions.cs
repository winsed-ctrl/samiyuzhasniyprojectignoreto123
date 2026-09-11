public static class StringExtensions
{
    public static bool ContainsAny(this string text, List<string> blockedWords)
    {
        if (string.IsNullOrEmpty(text) || blockedWords == null || !blockedWords.Any())
            return false;

        foreach (var word in blockedWords)
        {
            if (text.IndexOf(word, StringComparison.OrdinalIgnoreCase) >= 0)
                return true;
        }

        return false;
    }
}