namespace GromCore.Laser.Server.Utils
{
    using System;
    using System.Diagnostics;
    using System.Security.Cryptography;
    using GromCore.Laser.Logic;
    using static System.Net.Mime.MediaTypeNames;

    public static class Helpers
    {
        private const string STRING_CHARACTERS = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789";

        public static string RandomString(int length)
        {
            return RandomNumberGenerator.GetString(STRING_CHARACTERS, length);
        }
        public static string GenerateTelegramLinkCode()
        {
            const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";
            var random = new Random();
            return new string(Enumerable.Repeat(chars, 8)
                .Select(s => s[random.Next(s.Length)]).ToArray());
        }
        public static string GenerateToken(long id)
        {
            return "#SC-" + "LASER" + 89 + "/" + RandomString(28);
        }

        public static bool IsAdequateString(string name)
        {
            if (!GeneralStaticLogic.ProfanityEnabled) return true;
            if (string.IsNullOrWhiteSpace(name)) return false;
            foreach (var word in Laser.Logic.GeneralStaticLogic.BlockedWords) { if (name.Contains(word, StringComparison.OrdinalIgnoreCase)) return false; }
            return !ProfanityManager.ProfanityContainCheck(name);
        }

        public static string Serialize(string text)
        {
            if (!GeneralStaticLogic.ProfanityEnabled) return text;
            if (string.IsNullOrWhiteSpace(text)) return text;
            string result = text;
            foreach (var _blockedWord in Laser.Logic.GeneralStaticLogic.BlockedWords)
            {
                if (text.Contains(_blockedWord, StringComparison.OrdinalIgnoreCase))
                {
                    return new string('*', text.Length);
                }
            }
            if (ProfanityManager.ProfanitySerialize(text) != text) return ProfanityManager.ProfanitySerialize(text);
           
            return text;
        }
    }
}
