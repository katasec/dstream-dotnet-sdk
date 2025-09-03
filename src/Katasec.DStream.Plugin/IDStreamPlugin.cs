using Katasec.DStream.Proto;
using Katasec.DStream.Plugin.Interfaces;

namespace Katasec.DStream.Plugin;

/// <summary>
/// Core interface for dstream plugins
/// Plugin developers only need to implement this interface
/// </summary>
public interface IDStreamPlugin
{
    /// <summary>
    /// The main processing method for the plugin using input and output providers
    /// </summary>
    /// <param name="input">The configured input provider</param>
    /// <param name="output">The configured output provider</param>
    /// <param name="config">Global plugin configuration</param>
    /// <param name="cancellationToken">Cancellation token for graceful shutdown</param>
    /// <returns>A task representing the asynchronous operation</returns>
    Task ProcessAsync(IInput input, IOutput output, Dictionary<string, object> config, CancellationToken cancellationToken);
    
    /// <summary>
    /// Legacy execution method for backward compatibility
    /// </summary>
    /// <param name="cancellationToken">Cancellation token for graceful shutdown</param>
    /// <returns>A task representing the asynchronous operation</returns>
    // Task ExecuteAsync(CancellationToken cancellationToken);

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
