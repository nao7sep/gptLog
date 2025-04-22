using System;
using System.Text.RegularExpressions;

namespace gptLog.App.Model
{
    public sealed class Message
    {
        public Role Role { get; init; }
        public string Text { get; init; } = string.Empty;

        public string PreviewText =>
            $"{(Role == Role.User ? "User" : "Assistant")}: {Trimmed}";

        private string Trimmed
        {
            get
            {
                var normalised = Regex.Replace(Text, @"\s+", " ");
                return normalised.Length <= 50
                    ? normalised
                    : normalised[..50].TrimEnd() + "...";
            }
        }
    }
}