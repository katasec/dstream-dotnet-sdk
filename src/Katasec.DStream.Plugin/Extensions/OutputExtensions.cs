using System.Text.Json;
using Katasec.DStream.Plugin.Interfaces;
using Katasec.DStream.Plugin.Models;

namespace Katasec.DStream.Plugin.Extensions;

public static class OutputExtensions
{
    public static Task WriteJsonAsync<T>(this IOutput output, T value, CancellationToken cancellationToken = default, string source = "plugin", string operation = "write")
    {
        // Serialize to JsonElement
        var json = JsonSerializer.Serialize(value);
        var data = JsonDocument.Parse(json).RootElement;
        
        // Create stream item and write
        var item = StreamItem.Create(data, source, operation);
        return output.WriteAsync(new[] { item }, cancellationToken);
    }
}
