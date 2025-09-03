using Katasec.DStream.Plugin.Interfaces;
using Katasec.DStream.Proto;
using HCLog.Net;

namespace Katasec.DStream.Plugin;

/// <summary>
/// Base class for DStream plugins that provides compatibility between generic and non-generic interfaces
/// </summary>
/// <typeparam name="TConfig">The type of configuration object for the plugin</typeparam>
public abstract class DStreamPluginBase<TConfig> : IDStreamPlugin where TConfig : class, new()
{
    /// <summary>
    /// Logger for the plugin
    /// </summary>
    protected readonly HCLogger Logger;
    
    /// <summary>
    /// Initializes a new instance of the DStreamPluginBase class
    /// </summary>
    protected DStreamPluginBase()
    {
        Logger = new HCLogger(ModuleName);
    }
    
    /// <summary>
    /// Gets the name of the plugin module for logging
    /// </summary>
    public abstract string ModuleName { get; }
    
    /// <summary>
    /// Gets the schema fields for the plugin
    /// </summary>
    /// <returns>A collection of field schemas</returns>
    public abstract IEnumerable<FieldSchema> GetSchemaFields();
    
    /// <summary>
    /// The main processing method for the plugin using input and output providers
    /// This implementation converts the dictionary configuration to a strongly-typed object
    /// and delegates to the typed ProcessAsync method
    /// </summary>
    /// <param name="input">The configured input provider</param>
    /// <param name="output">The configured output provider</param>
    /// <param name="config">Global plugin configuration as a dictionary</param>
    /// <param name="cancellationToken">Cancellation token for graceful shutdown</param>
    /// <returns>A task representing the asynchronous operation</returns>
    public async Task ProcessAsync(IInput input, IOutput output, Dictionary<string, object> config, CancellationToken cancellationToken)
    {
        Logger.Debug("Converting dictionary configuration to typed configuration");
        
        // Log the raw configuration
        Logger.Debug("Raw configuration dictionary:");
        foreach (var kvp in config)
        {
            Logger.Debug($"  {kvp.Key}: {kvp.Value}");
        }
        
        try
        {
            // Convert the dictionary to a JSON string
            var jsonString = System.Text.Json.JsonSerializer.Serialize(config);
            Logger.Debug($"Serialized configuration JSON: {jsonString}");
            
            // Deserialize to the typed configuration
            var options = new System.Text.Json.JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            };
            
            var typedConfig = System.Text.Json.JsonSerializer.Deserialize<TConfig>(jsonString, options) ?? new TConfig();
            Logger.Debug("Successfully converted to typed configuration");
            
            // Call the typed ProcessAsync method
            Logger.Debug("Delegating to typed ProcessAsync method");
            await ProcessAsync(input, output, typedConfig, cancellationToken);
        }
        catch (Exception ex)
        {
            Logger.Error($"Error converting configuration: {ex.Message}");
            Logger.Error("Falling back to default configuration");
            await ProcessAsync(input, output, new TConfig(), cancellationToken);
        }
    }
    
    /// <summary>
    /// The main processing method for the plugin using input and output providers with strongly-typed configuration
    /// This method must be implemented by derived classes
    /// </summary>
    /// <param name="input">The configured input provider</param>
    /// <param name="output">The configured output provider</param>
    /// <param name="config">Strongly-typed global plugin configuration</param>
    /// <param name="cancellationToken">Cancellation token for graceful shutdown</param>
    /// <returns>A task representing the asynchronous operation</returns>
    public abstract Task ProcessAsync(IInput input, IOutput output, TConfig config, CancellationToken cancellationToken);
}
