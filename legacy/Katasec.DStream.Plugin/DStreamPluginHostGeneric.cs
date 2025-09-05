using Katasec.DStream.Proto;
using Google.Protobuf.WellKnownTypes;
using Grpc.Core;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Server.Kestrel.Core;
using Microsoft.Extensions.Logging;
using Katasec.DStream.Plugin.Interfaces;
using static Katasec.DStream.Proto.Plugin;
using HCLog.Net;

namespace Katasec.DStream.Plugin;

/// <summary>
/// Generic host class for dstream plugins with strongly-typed configuration
/// Handles all the HashiCorp go-plugin protocol details
/// </summary>
/// <typeparam name="TPlugin">The type of plugin to host</typeparam>
/// <typeparam name="TConfig">The type of configuration object for the plugin</typeparam>
public class DStreamPluginHost<TPlugin, TConfig> 
    where TPlugin : class, IDStreamPlugin<TConfig>
    where TConfig : class, new()
{
    // Static instance for service access
    private static DStreamPluginHost<TPlugin, TConfig>? _instance;

    /// <summary>
    /// Creates a new instance of the plugin host with the specified plugin and providers
    /// </summary>
    /// <param name="plugin">The plugin instance</param>
    /// <param name="inputProvider">The input provider</param>
    /// <param name="outputProvider">The output provider</param>
    public DStreamPluginHost(TPlugin plugin, IInput inputProvider, IOutput outputProvider)
    {
        Plugin = plugin ?? throw new ArgumentNullException(nameof(plugin));
        InputProvider = inputProvider ?? throw new ArgumentNullException(nameof(inputProvider));
        OutputProvider = outputProvider ?? throw new ArgumentNullException(nameof(outputProvider));
        Logger = new HCLogger(plugin.ModuleName);
        
        // Set the static instance for service access
        _instance = this;
    }

    /// <summary>
    /// Whether the plugin is running in standalone mode
    /// </summary>
    public static bool IsStandalone { get; private set; }

    /// <summary>
    /// The logger for the plugin host
    /// </summary>
    protected HCLogger Logger { get; private set; } = null!;

    /// <summary>
    /// The plugin instance
    /// </summary>
    protected TPlugin Plugin { get; private set; } = null!;

    /// <summary>
    /// The input provider
    /// </summary>
    protected IInput InputProvider { get; private set; } = null!;

    /// <summary>
    /// The output provider
    /// </summary>
    protected IOutput OutputProvider { get; private set; } = null!;

    /// <summary>
    /// Runs the plugin host
    /// </summary>
    /// <param name="args">Command line arguments</param>
    public async Task RunAsync(string[] args)
    {
        // Check if running in standalone mode
        IsStandalone = HashiCorpPluginUtils.IsStandaloneMode(args);

        // Check if this is a direct execution without a host (like terraform or dstream)
        // HashiCorp sets PLUGIN_PROTOCOL_VERSIONS environment variable when launching plugins
        if (!IsStandalone && Environment.GetEnvironmentVariable("PLUGIN_PROTOCOL_VERSIONS") == null &&
            Environment.GetEnvironmentVariable("PLUGIN_MIN_PORT") == null)
        {
            // This matches the HashiCorp plugin warning message format
            Console.WriteLine("This binary is a plugin. These are not meant to be executed directly.");
            Console.WriteLine("Please execute the program that consumes these plugins, which will");
            Console.WriteLine("load any plugins automatically");
            return;
        }

        // Plugin instance is already set in the constructor

        // Create a HashiCorp compatible logger
        Logger = new HCLogger("dotnet-plugin");
        Logger.Info("Starting generic plugin host with typed configuration support");

        try
        {
            // In plugin mode, only redirect stdout, leaving stderr available for logging
            if (!IsStandalone)
            {
                // Only redirect stdout, not stderr (which is used for logging)
                Console.SetOut(TextWriter.Null);
            }

            // Find an available port
            int port = HashiCorpPluginUtils.FindAvailablePort();

            // Create and start the gRPC server
            var builder = WebApplication.CreateBuilder(args);

            // Add services to the container
            builder.Services.AddGrpc();
            builder.Services.AddSingleton(Plugin);

            // Configure Kestrel to use the specific port
            builder.WebHost.ConfigureKestrel(options =>
            {
                options.ListenLocalhost(port, listenOptions =>
                {
                    listenOptions.Protocols = HttpProtocols.Http2;
                });
            });

            // Suppress ASP.NET Core logging in plugin mode
            if (!IsStandalone)
            {
                builder.Logging.ClearProviders();
            }

            var app = builder.Build();

            // Configure the HTTP request pipeline
            app.MapGrpcService<PluginServiceImpl>();

            // Start the server
            await app.StartAsync();

            // Log server startup
            Logger.Info("gRPC server started on port {0}", port);

            // Output the handshake string after the server has started
            if (!IsStandalone)
            {
                // Send the HashiCorp go-plugin handshake string
                HashiCorpPluginUtils.SendHandshakeString(port, Logger);
            }
            else
            {
                Console.WriteLine($"Server started on port {port} in standalone mode");
                Console.WriteLine("Press Ctrl+C to stop the server");
            }

            // Create a reset event for graceful shutdown
            var exitEvent = new ManualResetEvent(false);

            // Register for SIGINT (Ctrl+C) and SIGTERM
            Console.CancelKeyPress += (sender, eventArgs) => {
                Logger.Info("Received shutdown signal");
                // Cancel the default behavior (termination)
                eventArgs.Cancel = true;
                // Signal the exit event
                exitEvent.Set();
            };

            // Wait for exit signal
            exitEvent.WaitOne();

            // Gracefully stop the server
            Logger.Info("Shutting down server");
            await app.StopAsync();
        }
        catch (Exception ex)
        {
            Logger.Error("Fatal error: {0}", ex.Message);
            if (IsStandalone)
            {
                Console.WriteLine($"Error: {ex.Message}");
            }
        }
    }

    /// <summary>
    /// Implementation of the Plugin service with generic configuration support
    /// </summary>
    private class PluginServiceImpl : PluginBase
    {
        private readonly IDStreamPlugin<TConfig> _plugin;
        private readonly HCLogger? _logger;
        private CancellationTokenSource _cts;

        public PluginServiceImpl(TPlugin plugin)
        {
            _plugin = plugin;
            _logger = new HCLogger(_plugin.ModuleName);
            _cts = new CancellationTokenSource();
        }

        public override Task<GetSchemaResponse> GetSchema(Empty request, ServerCallContext context)
        {
            // Create schema response from plugin's schema fields
            var response = new GetSchemaResponse();
            foreach (var field in _plugin.GetSchemaFields())
            {
                response.Fields.Add(field);
            }

            return Task.FromResult(response);
        }

        public override async Task<Empty> Start(StartRequest request, ServerCallContext context)
        {
            ArgumentNullException.ThrowIfNull(request);
            try
            {
                // Link the cancellation token from the context
                using var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(
                    context.CancellationToken, _cts.Token);
                
                // Log the raw request structure received from dstream CLI
                _logger?.Info("Raw request structure from dstream CLI:");
                _logger?.Info($"Input Provider: {request.Input?.Provider}");
                _logger?.Info($"Output Provider: {request.Output?.Provider}");
                if (request.Config != null)
                {
                    _logger?.Info("Global Config:");
                    foreach (var field in request.Config.Fields)
                    {
                        _logger?.Info($"  {field.Key}: {field.Value}");
                    }
                }
                
                // Check if we have input and output configurations
                if (request.Input != null && request.Output != null)
                {
                    _logger?.Info("Starting plugin with existing input and output providers");
                    
                    try
                    {
                        // Use the existing input and output providers
                        var input = _instance?.InputProvider;
                        var output = _instance?.OutputProvider;
                        
                        if (input == null || output == null)
                        {
                            _logger?.Error("Input or output provider is null");
                            throw new InvalidOperationException("Input or output provider is null");
                        }
                        
                        // Convert the protobuf struct to a strongly-typed configuration object
                        _logger?.Info("Converting protobuf configuration to strongly-typed configuration");
                        if (request.Config != null)
                        {
                            var typedConfig = ConfigurationUtils.ConvertToTypedConfig<TConfig>(request.Config, _logger);
                        
                            // Log the typed configuration
                            _logger?.Info("Using strongly-typed configuration of type: {0}", typeof(TConfig).Name);
                        
                            // Process using input/output providers with typed configuration
                            _logger?.Info("Calling plugin's ProcessAsync with strongly-typed configuration");
                            await _plugin.ProcessAsync(input, output, typedConfig, linkedCts.Token);
                        }

                        // Note: We don't close the providers here as they are managed by the host
                    }
                    catch (KeyNotFoundException ex)
                    {
                        _logger?.Error("Provider not found: {0}", ex.Message);
                        throw;
                    }
                }
                else
                {
                    _logger?.Error("Input or output configuration is missing");
                }
            }
            catch (OperationCanceledException)
            {
                // Expected when cancellation occurs
                _logger?.Info("Plugin stopped by cancellation");
            }
            catch (Exception ex)
            {
                // Log any unexpected exceptions
                _logger?.Error("Error: {0}", ex.Message);
            }

            // Return an empty response when done
            return new Empty();
        }
    }
}
