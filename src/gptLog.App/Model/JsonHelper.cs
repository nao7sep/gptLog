using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace gptLog.App.Model
{
    public static class JsonHelper
    {
        private static readonly JsonSerializerOptions _options = new JsonSerializerOptions
        {
            WriteIndented = true
        };

        private static readonly Regex _newlineRegex = new Regex(@"\r\n?|\n");

        /// <summary>
        /// Saves a collection of messages to a JSON file
        /// </summary>
        public static async Task SaveMessagesToFileAsync(IEnumerable<Message> messages, string filePath)
        {
            var dtos = messages.Select(message => new MessageDto
            {
                Role = message.Role.ToString().ToLowerInvariant(),
                Lines = _newlineRegex.Split(message.Text).ToList()
            }).ToList();

            using var stream = new FileStream(filePath, FileMode.Create, FileAccess.Write);
            await JsonSerializer.SerializeAsync(stream, dtos, _options);
        }

        /// <summary>
        /// Loads messages from a JSON file
        /// </summary>
        public static async Task<List<Message>> LoadMessagesFromFileAsync(string filePath)
        {
            using var stream = new FileStream(filePath, FileMode.Open, FileAccess.Read);
            var dtos = await JsonSerializer.DeserializeAsync<List<MessageDto>>(stream);

            if (dtos == null)
                return new List<Message>();

            return dtos.Select(dto => new Message
            {
                Role = Enum.Parse<Role>(dto.Role, ignoreCase: true),
                Text = string.Join(Environment.NewLine, dto.Lines)
            }).ToList();
        }
    }
}