using DStreamDotnetTest;
using Katasec.DStream.Providers;
using PluginHostFactory = Katasec.DStream.Plugin.PluginHostFactory;

// Create and run the plugin host with strongly-typed configuration
var plugin = new GenericCounterPlugin();
var inputProvider = ProviderRegistry.GetInputProvider("null");
var outputProvider = ProviderRegistry.GetOutputProvider("console");

var host = PluginHostFactory.CreatePluginHost<GenericCounterPlugin, GenericCounterConfig>(plugin, inputProvider, outputProvider);
    
await host.RunAsync(args);
