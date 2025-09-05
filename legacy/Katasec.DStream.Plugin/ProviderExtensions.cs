using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Katasec.DStream.Plugin.Interfaces;

namespace Katasec.DStream.Plugin
{
    /// <summary>
    /// Extension methods for working with input and output providers
    /// </summary>
    public static class ProviderExtensions
    {
        /// <summary>
        /// Creates and initializes an input provider from a provider factory
        /// </summary>
        /// <param name="providerFactory">Factory function that creates the provider</param>
        /// <param name="config">Configuration for the provider</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Initialized input provider</returns>
        public static async Task<IInput> CreateAndInitializeInputProviderAsync(
            Func<IInput> providerFactory,
            Dictionary<string, object> config,
            CancellationToken cancellationToken)
        {
            var provider = providerFactory();
            await provider.InitializeAsync(config, cancellationToken);
            return provider;
        }

        /// <summary>
        /// Creates and initializes an output provider from a provider factory
        /// </summary>
        /// <param name="providerFactory">Factory function that creates the provider</param>
        /// <param name="config">Configuration for the provider</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Initialized output provider</returns>
        public static async Task<IOutput> CreateAndInitializeOutputProviderAsync(
            Func<IOutput> providerFactory,
            Dictionary<string, object> config,
            CancellationToken cancellationToken)
        {
            var provider = providerFactory();
            await provider.InitializeAsync(config, cancellationToken);
            return provider;
        }
    }
}
