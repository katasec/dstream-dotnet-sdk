using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Katasec.DStream.Plugin.Interfaces;
using Katasec.DStream.Plugin.Models;

namespace Katasec.DStream.Providers.Output
{
    /// <summary>
    /// Base class for output providers that implements common functionality
    /// </summary>
    public abstract class OutputProviderBase : IOutput
    {
        /// <summary>
        /// Gets the name of this output provider
        /// </summary>
        public abstract string Name { get; }

        /// <summary>
        /// Initializes the output provider with the given configuration
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
        /// Writes a collection of stream items to the output destination
        /// </summary>
        /// <param name="items">The items to write</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>A task representing the asynchronous operation</returns>
        public abstract Task WriteAsync(IEnumerable<StreamItem> items, CancellationToken cancellationToken);

        /// <summary>
        /// Flushes any buffered data to the output destination
        /// </summary>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>A task representing the asynchronous operation</returns>
        public virtual Task FlushAsync(CancellationToken cancellationToken)
        {
            // Default implementation does nothing
            return Task.CompletedTask;
        }

        /// <summary>
        /// Performs cleanup operations when the output provider is no longer needed
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
