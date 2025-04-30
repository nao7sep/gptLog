using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Serilog;

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
        public static async Task SaveMessagesToFileAsync(IEnumerable<Message> messages, string filePath, string title)
        {
            if (string.IsNullOrEmpty(filePath))
                throw new ArgumentException("File path cannot be null or empty", nameof(filePath));

            if (messages == null)
                throw new ArgumentNullException(nameof(messages));

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
                catch (JsonException ex)
                {
                    Log.Warning(ex, "Invalid JSON format in file: {FilePath}. Creating a new conversation.", filePath);
                    conversationDto = new ConversationDto();
                    isNewFile = true;
                }
                catch (IOException ex)
                {
                    Log.Error(ex, "Error reading file: {FilePath}", filePath);
                    throw new IOException($"Could not read the file: {filePath}. The file might be in use by another application.", ex);
                }
                catch (Exception ex)
                {
                    Log.Error(ex, "Unexpected error reading file: {FilePath}", filePath);
                    // Create a new DTO if there's an error reading the file
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
            try
            {
                conversationDto.Messages = messages.Select(message => new MessageDto
                {
                    Role = message.Role.ToString().ToLowerInvariant(),
                    Lines = SplitTextIntoLines(message.Text)
                }).ToList();
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Error converting messages to DTO format");
                throw new InvalidOperationException("Failed to prepare messages for saving.", ex);
            }

            // Save to file with UTF-8 BOM
            try
            {
                // First save to a temporary file
                string tempFilePath = filePath + ".temp";

                using var fileStream = new FileStream(tempFilePath, FileMode.Create, FileAccess.Write);
                using var streamWriter = new StreamWriter(fileStream, new UTF8Encoding(true)); // true = include BOM
                var jsonString = JsonSerializer.Serialize(conversationDto, _options);
                await streamWriter.WriteAsync(jsonString);
                await streamWriter.FlushAsync();
                streamWriter.Close();
                fileStream.Close();

                // If that succeeds, replace the original file safely
                string backupPath = filePath + ".bak";

                if (File.Exists(filePath))
                {
                    // Keep a backup of the original file if it exists
                    if (File.Exists(backupPath))
                        File.Delete(backupPath);

                    File.Move(filePath, backupPath);
                }

                File.Move(tempFilePath, filePath);

                // Delete the backup only if everything succeeded
                if (File.Exists(backupPath))
                    File.Delete(backupPath);

                Log.Debug("Successfully saved file: {FilePath}", filePath);
            }
            catch (UnauthorizedAccessException ex)
            {
                Log.Error(ex, "No permission to save file: {FilePath}", filePath);
                throw new UnauthorizedAccessException($"You don't have permission to save to {filePath}. Try saving to a different location.", ex);
            }
            catch (IOException ex)
            {
                Log.Error(ex, "Error writing to file: {FilePath}", filePath);
                throw new IOException($"Could not write to {filePath}. The file might be read-only or in use by another application.", ex);
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Unexpected error saving file: {FilePath}", filePath);
                throw new Exception($"Failed to save file: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// Loads messages from a JSON file
        /// </summary>
        /// <returns>A tuple containing the list of messages and the conversation title</returns>
        public static async Task<(List<Message> Messages, string? Title)> LoadMessagesFromFileAsync(string filePath)
        {
            if (string.IsNullOrEmpty(filePath))
                throw new ArgumentException("File path cannot be null or empty", nameof(filePath));

            if (!File.Exists(filePath))
                throw new FileNotFoundException($"The file {filePath} does not exist.", filePath);

            try
            {
                using var stream = new FileStream(filePath, FileMode.Open, FileAccess.Read);
                try
                {
                    var conversationDto = await JsonSerializer.DeserializeAsync<ConversationDto>(stream);

                    if (conversationDto == null || conversationDto.Messages == null)
                    {
                        Log.Warning("File {FilePath} contained null content or messages", filePath);
                        return (new List<Message>(), null);
                    }

                    var messages = conversationDto.Messages.Select(dto => new Message
                    {
                        Role = Enum.Parse<Role>(dto.Role, ignoreCase: true),
                        Text = string.Join(Environment.NewLine, dto.Lines)
                    }).ToList();

                    // Return the title directly, which may be null
                    return (messages, conversationDto.Metadata.Title);
                }
                catch (JsonException)
                {
                    // If main format fails, try legacy format
                    stream.Position = 0;

                    try
                    {
                        var legacyDtos = await JsonSerializer.DeserializeAsync<List<MessageDto>>(stream);

                        if (legacyDtos == null)
                        {
                            Log.Warning("File {FilePath} appears to be in an invalid format", filePath);
                            return (new List<Message>(), null);
                        }

                        var messages = legacyDtos.Select(dto => new Message
                        {
                            Role = Enum.Parse<Role>(dto.Role, ignoreCase: true),
                            Text = string.Join(Environment.NewLine, dto.Lines)
                        }).ToList();

                        Log.Information("Loaded file {FilePath} using legacy format", filePath);
                        return (messages, null);
                    }
                    catch (JsonException ex)
                    {
                        Log.Error(ex, "Invalid JSON format in file: {FilePath}", filePath);
                        throw new FormatException($"The file {filePath} contains invalid JSON and cannot be loaded.", ex);
                    }
                }
            }
            catch (IOException ex)
            {
                Log.Error(ex, "Error accessing file: {FilePath}", filePath);
                throw new IOException($"Could not access the file: {filePath}. It might be in use by another application.", ex);
            }
            catch (UnauthorizedAccessException ex)
            {
                Log.Error(ex, "No permission to access file: {FilePath}", filePath);
                throw new UnauthorizedAccessException($"You don't have permission to open {filePath}.", ex);
            }
            catch (Exception ex) when (ex is not FormatException) // We want to let FormatException propagate
            {
                Log.Error(ex, "Unexpected error loading file: {FilePath}", filePath);
                throw new Exception($"Failed to load file: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// Splits text into lines using StringReader instead of regex.
        /// Omits whitespace-only lines at the beginning and end of the text,
        /// but preserves whitespace-only lines in the middle.
        /// </summary>
        private static List<string> SplitTextIntoLines(string text)
        {
            if (text == null)
                return new List<string>();

            var lines = new List<string>();
            using (var reader = new StringReader(text))
            {
                string? line;
                while ((line = reader.ReadLine()) != null)
                {
                    lines.Add(line);
                }
            }

            // If no lines, return empty list
            if (lines.Count == 0)
                return lines;

            // Find first non-whitespace line
            int firstVisibleIndex = 0;
            while (firstVisibleIndex < lines.Count && string.IsNullOrWhiteSpace(lines[firstVisibleIndex]))
            {
                firstVisibleIndex++;
            }

            // If all lines are whitespace, return empty list
            if (firstVisibleIndex >= lines.Count)
                return new List<string>();

            // Find last non-whitespace line
            int lastVisibleIndex = lines.Count - 1;
            while (lastVisibleIndex >= 0 && string.IsNullOrWhiteSpace(lines[lastVisibleIndex]))
            {
                lastVisibleIndex--;
            }

            // Return the range from first visible to last visible (inclusive)
            return lines.GetRange(firstVisibleIndex, lastVisibleIndex - firstVisibleIndex + 1);
        }
    }
}