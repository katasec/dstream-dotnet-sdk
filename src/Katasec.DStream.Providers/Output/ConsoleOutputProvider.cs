using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Katasec.DStream.Plugin.Models;

namespace Katasec.DStream.Providers.Output
{
    /// <summary>
    /// An output provider that writes to the console
    /// </summary>
    public class ConsoleOutputProvider : OutputProviderBase
    {
        private string _format = "json"; // Default format
        
        /// <summary>
        /// Gets the name of this output provider
        /// </summary>
        public override string Name => "console";
        
        /// <summary>
        /// Initializes the output provider with the given configuration
        /// </summary>
        /// <param name="config">Provider-specific configuration</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>A task representing the asynchronous operation</returns>
        public override Task InitializeAsync(Dictionary<string, object> config, CancellationToken cancellationToken)
        {
            // Get format from config if available
            if (config.TryGetValue("format", out var formatObj) && formatObj is string format)
            {
                _format = format;
            }
            
            return Task.CompletedTask;
        }
        
        /// <summary>
        /// Writes data to the console
        /// </summary>
        /// <param name="items">The items to write</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>A task representing the asynchronous operation</returns>
        public override Task WriteAsync(IEnumerable<StreamItem> items, CancellationToken cancellationToken)
        {
            foreach (var item in items)
            {
                if (_format.Equals("json", StringComparison.OrdinalIgnoreCase))
                {
                    // Output as JSON
                    var json = JsonSerializer.Serialize(item, new JsonSerializerOptions { WriteIndented = true });
                    Console.WriteLine(json);
                }
                else
                {
                    // Output as text
                    Console.WriteLine($"Source: {item.Source}");
                    Console.WriteLine($"Operation: {item.Operation}");
                    Console.WriteLine($"Data: {item.Data}");
                    
                    // Output metadata
                    Console.WriteLine("Metadata:");
                    foreach (var kvp in item.Metadata)
                    {
                        Console.WriteLine($"  {kvp.Key}: {kvp.Value}");
                    }
                    Console.WriteLine();
                }
            }
            
            return Task.CompletedTask;
        }
    }
}
