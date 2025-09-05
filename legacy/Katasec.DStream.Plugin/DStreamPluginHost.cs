using HCLog.Net;
using Katasec.DStream.Proto;
using Google.Protobuf.WellKnownTypes;
using Grpc.Core;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Server.Kestrel.Core;
using Microsoft.Extensions.Logging;
using static Katasec.DStream.Proto.Plugin;

namespace Katasec.DStream.Plugin;

/// <summary>
/// Base class for hosting dstream plugins
/// Handles all the HashiCorp go-plugin protocol details
/// </summary>
/// <typeparam name="TPlugin">The type of plugin to host</typeparam>
public class DStreamPluginHost<TPlugin> where TPlugin : class, IDStreamPlugin, new()
{
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

        // Create the plugin instance
        Plugin = new TPlugin();

        // Create a HashiCorp compatible logger
        Logger = new HCLogger("dotnet-plugin");

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
    /// Implementation of the Plugin service
    /// </summary>
    private class PluginServiceImpl : PluginBase
    {
        private readonly IDStreamPlugin _plugin;
        private readonly HCLogger _logger;
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
            try
            {
                // Link the cancellation token from the context
                using var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(
                    context.CancellationToken, _cts.Token);
                
                // Log the raw request structure received from dstream CLI
                _logger.Info("Raw request structure from dstream CLI:");
                _logger.Info($"Input Provider: {request.Input?.Provider}");
                _logger.Info($"Output Provider: {request.Output?.Provider}");
                if (request.Config != null)
                {
                    _logger.Info("Global Config:");
                    foreach (var field in request.Config.Fields)
                    {
                        _logger.Info($"  {field.Key}: {field.Value}");
                    }
                }
                
                // Check if we have input and output configurations
                if (request.Input != null && request.Output != null)
                {
                    _logger.Info("Starting plugin with input provider '{0}' and output provider '{1}'", 
                        request.Input.Provider, request.Output.Provider);
                    
                    try
                    {
                        // Create and configure input provider
                        var input = ProviderRegistry.CreateInputProvider(request.Input.Provider);
                        var inputConfig = request.Input.Config?.Fields.ToDictionary(
                            kvp => kvp.Key, 
                            kvp => (object)kvp.Value) ?? new Dictionary<string, object>();
                        
                        // Log input configuration
                        _logger.Info("Input Configuration:");
                        foreach (var kvp in inputConfig)
                        {
                            _logger.Info($"  {kvp.Key}: {kvp.Value}");
                        }
                        
                        await input.InitializeAsync(inputConfig, linkedCts.Token);
                        
                        // Create and configure output provider
                        var output = ProviderRegistry.CreateOutputProvider(request.Output.Provider);
                        var outputConfig = request.Output.Config?.Fields.ToDictionary(
                            kvp => kvp.Key, 
                            kvp => (object)kvp.Value) ?? new Dictionary<string, object>();
                        
                        // Log output configuration
                        _logger.Info("Output Configuration:");
                        foreach (var kvp in outputConfig)
                        {
                            _logger.Info($"  {kvp.Key}: {kvp.Value}");
                        }
                        
                        await output.InitializeAsync(outputConfig, linkedCts.Token);
                        
                        // Get global config
                        var globalConfig = request.Config?.Fields.ToDictionary(
                            kvp => kvp.Key, 
                            kvp => (object)kvp.Value) ?? new Dictionary<string, object>();
                        
                        // Process using input/output providers
                        await _plugin.ProcessAsync(input, output, globalConfig, linkedCts.Token);
                        
                        // Clean up providers
                        await input.CloseAsync(linkedCts.Token);
                        await output.CloseAsync(linkedCts.Token);
                    }
                    catch (KeyNotFoundException ex)
                    {
                        _logger.Error("Provider not found: {0}", ex.Message);
                        throw;
                    }
                }
                else
                {
                    _logger.Info("No input/output providers)");
                    // Fall back to legacy execution mode
                    // await _plugin.ExecuteAsync(linkedCts.Token);
                }
            }
            catch (OperationCanceledException)
            {
                // Expected when cancellation occurs
                _logger.Info("Plugin stopped by cancellation");
            }
            catch (Exception ex)
            {
                // Log any unexpected exceptions
                _logger.Error("Error: {0}", ex.Message);
            }

            // Return an empty response when done
            return new Empty();
        }
    }
}
