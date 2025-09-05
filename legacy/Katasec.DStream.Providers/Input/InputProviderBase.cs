using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Katasec.DStream.Plugin.Interfaces;
using Katasec.DStream.Plugin.Models;

namespace Katasec.DStream.Providers.Input
{
    /// <summary>
    /// Base class for input providers that implements common functionality
    /// </summary>
    public abstract class InputProviderBase : IInput
    {
        /// <summary>
        /// Gets the name of this input provider
        /// </summary>
        public abstract string Name { get; }

        /// <summary>
        /// Initializes the input provider with the given configuration
        /// </summary>
        /// <param name="config">Provider-specific configuration</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>A task representing the asynchronous operation</returns>
        public virtual Task InitializeAsync(Dictionary<string, object> config, CancellationToken cancellationToken)
        {
            // Default implementation does nothing
            return Task.CompletedTask;
        }

        /// <summary>
        /// Reads the next batch of items from the input source
        /// </summary>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>A collection of stream items</returns>
        public abstract Task<IEnumerable<StreamItem>> ReadAsync(CancellationToken cancellationToken);

        /// <summary>
        /// Determines if there is more data available to read
        /// </summary>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>True if more data is available, false otherwise</returns>
        public abstract Task<bool> HasMoreDataAsync(CancellationToken cancellationToken);

        /// <summary>
        /// Performs cleanup operations when the input provider is no longer needed
        /// </summary>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>A task representing the asynchronous operation</returns>
        public virtual Task CloseAsync(CancellationToken cancellationToken)
        {
            // Default implementation does nothing
            return Task.CompletedTask;
        }
    }
}
