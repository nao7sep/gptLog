using System;
using System.Text.RegularExpressions;

namespace gptLog.App.Model
{
    public sealed class Message
    {
        public Role Role { get; init; }
        public string Text { get; init; } = string.Empty;

        public string PreviewText =>
            $"{(Role == Role.User ? "User" : "Assistant")}: {TrimMessageText(Text)}";

        private string Trimmed => TrimMessageText(Text);

        /// <summary>
        /// Takes a message text, normalizes whitespace, trims it and adds ellipsis if it's longer than the specified length
        /// </summary>
        /// <param name="text">The text to trim</param>
        /// <param name="maxLength">Maximum length (default: 256)</param>
        /// <returns>Trimmed and formatted text</returns>
        public static string TrimMessageText(string text, int maxLength = 256)
        {
            if (string.IsNullOrEmpty(text))
                return string.Empty;

            var normalised = Regex.Replace(text, @"\s+", " ").Trim();
            return normalised.Length <= maxLength
                ? normalised
                : normalised[..maxLength].TrimEnd() + "...";
        }
    }
}