using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace gptLog.App.Model
{
    /// <summary>
    /// Metadata for the conversation log
    /// </summary>
    public class ConversationMetadata
    {
        [JsonPropertyName("title")]
        public string? Title { get; set; }

        [JsonPropertyName("created_at")]
        public DateTime CreatedAt { get; set; }

        [JsonPropertyName("last_modified_at")]
        public DateTime LastModifiedAt { get; set; }
    }

    /// <summary>
    /// Root object for JSON serialization/deserialization
    /// </summary>
    public class ConversationDto
    {
        [JsonPropertyName("metadata")]
        public ConversationMetadata Metadata { get; set; } = new ConversationMetadata();

        [JsonPropertyName("messages")]
        public List<MessageDto> Messages { get; set; } = new List<MessageDto>();
    }

    /// <summary>
    /// Data Transfer Object for Message serialization/deserialization
    /// </summary>
    public class MessageDto
    {
        [JsonPropertyName("role")]
        public string Role { get; set; } = string.Empty;

        [JsonPropertyName("lines")]
        public List<string> Lines { get; set; } = new List<string>();
    }
}