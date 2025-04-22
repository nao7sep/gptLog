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
            WriteIndented = true,
            Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping // Ensure CJK characters are stored as-is
        };

        /// <summary>
        /// Saves a collection of messages to a JSON file
        /// </summary>
        public static async Task SaveMessagesToFileAsync(IEnumerable<Message> messages, string filePath, string title = "Conversation")
        {
            // Check if file exists to get existing metadata
            ConversationDto conversationDto;
            bool isNewFile = !File.Exists(filePath);

            if (!isNewFile)
            {
                try
                {
                    using var readStream = new FileStream(filePath, FileMode.Open, FileAccess.Read);
                    conversationDto = await JsonSerializer.DeserializeAsync<ConversationDto>(readStream)
                        ?? new ConversationDto();
                }
                catch
                {
                    // If there's an error reading the file, create a new DTO
                    conversationDto = new ConversationDto();
                    isNewFile = true;
                }
            }
            else
            {
                conversationDto = new ConversationDto();
            }

            // Update the metadata
            conversationDto.Metadata.Title = title;

            // Always update LastModifiedAt when saving
            conversationDto.Metadata.LastModifiedAt = DateTime.UtcNow;

            // Set CreatedAt only for new files
            if (isNewFile)
            {
                conversationDto.Metadata.CreatedAt = DateTime.UtcNow;
            }

            // Convert messages to DTOs
            conversationDto.Messages = messages.Select(message => new MessageDto
            {
                Role = message.Role.ToString().ToLowerInvariant(),
                Lines = SplitTextIntoLines(message.Text)
            }).ToList();

            // Save to file
            using var writeStream = new FileStream(filePath, FileMode.Create, FileAccess.Write);
            await JsonSerializer.SerializeAsync(writeStream, conversationDto, _options);
        }

        /// <summary>
        /// Loads messages from a JSON file
        /// </summary>
        /// <returns>A tuple containing the list of messages and the conversation title</returns>
        public static async Task<(List<Message> Messages, string Title)> LoadMessagesFromFileAsync(string filePath)
        {
            using var stream = new FileStream(filePath, FileMode.Open, FileAccess.Read);

            try
            {
                var conversationDto = await JsonSerializer.DeserializeAsync<ConversationDto>(stream);

                if (conversationDto == null || conversationDto.Messages == null)
                    return (new List<Message>(), "Conversation");

                var messages = conversationDto.Messages.Select(dto => new Message
                {
                    Role = Enum.Parse<Role>(dto.Role, ignoreCase: true),
                    Text = string.Join(Environment.NewLine, dto.Lines)
                }).ToList();

                return (messages, conversationDto.Metadata.Title);
            }
            catch
            {
                // Try to load legacy format (just an array of MessageDto)
                stream.Position = 0;
                var legacyDtos = await JsonSerializer.DeserializeAsync<List<MessageDto>>(stream);

                if (legacyDtos == null)
                    return (new List<Message>(), "Conversation");

                var messages = legacyDtos.Select(dto => new Message
                {
                    Role = Enum.Parse<Role>(dto.Role, ignoreCase: true),
                    Text = string.Join(Environment.NewLine, dto.Lines)
                }).ToList();

                return (messages, "Conversation");
            }
        }

        /// <summary>
        /// Splits text into lines using StringReader instead of regex
        /// </summary>
        private static List<string> SplitTextIntoLines(string text)
        {
            var lines = new List<string>();
            using (var reader = new StringReader(text))
            {
                string? line;
                while ((line = reader.ReadLine()) != null)
                {
                    lines.Add(line);
                }
            }
            return lines;
        }
    }
}