// GenericCounterPlugin.cs
// Sample plugin implementation that uses the generic interface with strongly-typed configuration

using HCLog.Net;
using Katasec.DStream.Plugin;
using Katasec.DStream.Plugin.Interfaces;
using Katasec.DStream.Proto;

namespace DStreamDotnetTest;

/// <summary>
/// A simple counter plugin that uses the generic interface with strongly-typed configuration
/// </summary>
public class GenericCounterPlugin : IDStreamPlugin<GenericCounterConfig>
{
    /// <summary>
    /// Gets the name of the plugin module for logging
    /// </summary>
    public string ModuleName => "generic-counter";
    
    /// <summary>
    /// Gets the schema fields for the plugin
    /// </summary>
    public IEnumerable<FieldSchema> GetSchemaFields()
    {
        yield return new FieldSchema
        {
            Name = "interval",
            Type = "number",
            Description = "Interval between counter increments in milliseconds"
        };
    }
    
    /// <summary>
    /// Process data using input and output providers with strongly-typed configuration
    /// </summary>
    public async Task ProcessAsync(IInput input, IOutput output, GenericCounterConfig config, CancellationToken cancellationToken)
    {
        // Create a logger and log the configuration
        var logger = new HCLogger(ModuleName);
        logger.Info($"Hello World plugin started with interval: {config.Interval}ms");
        
        // Simple counter loop
        int counter = 0;
        while (!cancellationToken.IsCancellationRequested)
        {
            // Increment counter and create a simple message
            counter++;
            string message = $"Hello from .NET plugin! Count: {counter}";
            logger.Info(message);
            
            // Write counter with proper source/operation
            await output.WriteJsonAsync(new { counter },cancellationToken);
            
            try { 
                await Task.Delay(config.Interval, cancellationToken); 
            }
            catch (TaskCanceledException) { 
                break; 
            }
        }
    }
}
