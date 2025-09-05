using System;
using System.Collections.Generic;
using System.Text.Json;

namespace Katasec.DStream.Plugin.Models
{
    /// <summary>
    /// Represents a single data item flowing through the stream pipeline
    /// from input providers to output providers.
    /// </summary>
    public class StreamItem
    {
        /// <summary>
        /// Unique identifier for the stream item
        /// </summary>
        public string Id { get; set; } = Guid.NewGuid().ToString();

        /// <summary>
        /// Timestamp when the item was created
        /// </summary>
        public DateTime Timestamp { get; set; } = DateTime.UtcNow;

        /// <summary>
        /// Source information (e.g., table name, file path)
        /// </summary>
        public string Source { get; set; } = string.Empty;

        /// <summary>
        /// Type of operation (e.g., Insert, Update, Delete)
        /// </summary>
        public string Operation { get; set; } = string.Empty;

        /// <summary>
        /// The actual data payload
        /// </summary>
        public JsonElement Data { get; set; }

        /// <summary>
        /// Additional metadata about the stream item
        /// </summary>
        public Dictionary<string, string> Metadata { get; set; } = new Dictionary<string, string>();

        /// <summary>
        /// Creates a new stream item with the specified data
        /// </summary>
        /// <param name="data">The data payload</param>
        /// <param name="source">Optional source information</param>
        /// <param name="operation">Optional operation type</param>
        /// <returns>A new stream item</returns>
        public static StreamItem Create(JsonElement data, string source = "", string operation = "")
        {
            return new StreamItem
            {
                Data = data,
                Source = source,
                Operation = operation
            };
        }

        /// <summary>
        /// Creates a new stream item from a JSON string
        /// </summary>
        /// <param name="json">JSON string representation of the data</param>
        /// <param name="source">Optional source information</param>
        /// <param name="operation">Optional operation type</param>
        /// <returns>A new stream item</returns>
        public static StreamItem FromJson(string json, string source = "", string operation = "")
        {
            var data = JsonSerializer.Deserialize<JsonElement>(json);
            return Create(data, source, operation);
        }

        /// <summary>
        /// Serializes the data payload to a JSON string
        /// </summary>
        /// <returns>JSON string representation of the data</returns>
        public string ToJson()
        {
            return JsonSerializer.Serialize(Data);
        }
    }
}
