using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace gptLog.App.Model
{
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