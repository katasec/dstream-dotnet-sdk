using Katasec.DStream.Proto;

namespace Katasec.DStream.Plugin.Interfaces;

/// <summary>
/// Generic interface for dstream plugins with strongly-typed configuration
/// </summary>
/// <typeparam name="TConfig">The type of configuration object for the plugin</typeparam>
public interface IDStreamPlugin<TConfig> where TConfig : class, new()
{
    /// <summary>
    /// The main processing method for the plugin using input and output providers with strongly-typed configuration
    /// </summary>
    /// <param name="input">The configured input provider</param>
    /// <param name="output">The configured output provider</param>
    /// <param name="config">Strongly-typed global plugin configuration</param>
    /// <param name="cancellationToken">Cancellation token for graceful shutdown</param>
    /// <returns>A task representing the asynchronous operation</returns>
    Task ProcessAsync(IInput input, IOutput output, TConfig config, CancellationToken cancellationToken);
    
    /// <summary>
    /// Gets the schema fields for the plugin
    /// </summary>
    /// <returns>A collection of field schemas</returns>
    IEnumerable<FieldSchema> GetSchemaFields();

    /// <summary>
    /// Gets the name of the plugin module for logging
    /// </summary>
    string ModuleName { get; }
}
