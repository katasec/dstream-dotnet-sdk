using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Katasec.DStream.Plugin.Interfaces;

namespace Katasec.DStream.Plugin
{
    /// <summary>
    /// Factory for creating plugin hosts with configured providers
    /// </summary>
    public static class PluginHostFactory
    {
        /// <summary>
        /// Creates a plugin host with the specified plugin, input provider, and output provider
        /// </summary>
        /// <typeparam name="TPlugin">Type of plugin</typeparam>
        /// <typeparam name="TConfig">Type of plugin configuration</typeparam>
        /// <param name="plugin">The plugin instance</param>
        /// <param name="inputProvider">The input provider</param>
        /// <param name="outputProvider">The output provider</param>
        /// <returns>A configured plugin host</returns>
        public static DStreamPluginHost<TPlugin, TConfig> CreatePluginHost<TPlugin, TConfig>(
            TPlugin plugin,
            IInput inputProvider,
            IOutput outputProvider)
            where TPlugin : class, IDStreamPlugin<TConfig>
            where TConfig : class, new()
        {
            return new DStreamPluginHost<TPlugin, TConfig>(plugin, inputProvider, outputProvider);
        }

        /// <summary>
        /// Creates a plugin host with the specified plugin and provider factories
        /// </summary>
        /// <typeparam name="TPlugin">Type of plugin</typeparam>
        /// <typeparam name="TConfig">Type of plugin configuration</typeparam>
        /// <param name="pluginFactory">Factory function that creates the plugin</param>
        /// <param name="inputProviderFactory">Factory function that creates the input provider</param>
        /// <param name="outputProviderFactory">Factory function that creates the output provider</param>
        /// <param name="inputConfig">Configuration for the input provider</param>
        /// <param name="outputConfig">Configuration for the output provider</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>A configured plugin host</returns>
        public static async Task<DStreamPluginHost<TPlugin, TConfig>> CreatePluginHostAsync<TPlugin, TConfig>(
            Func<TPlugin> pluginFactory,
            Func<IInput> inputProviderFactory,
            Func<IOutput> outputProviderFactory,
            Dictionary<string, object> inputConfig,
            Dictionary<string, object> outputConfig,
            CancellationToken cancellationToken = default)
            where TPlugin : class, IDStreamPlugin<TConfig>
            where TConfig : class, new()
        {
            var plugin = pluginFactory();
            
            var inputProvider = await ProviderExtensions.CreateAndInitializeInputProviderAsync(
                inputProviderFactory, inputConfig, cancellationToken);
                
            var outputProvider = await ProviderExtensions.CreateAndInitializeOutputProviderAsync(
                outputProviderFactory, outputConfig, cancellationToken);
                
            return new DStreamPluginHost<TPlugin, TConfig>(plugin, inputProvider, outputProvider);
        }
    }
}
