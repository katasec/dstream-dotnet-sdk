using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Katasec.DStream.Plugin.Models;

namespace Katasec.DStream.Plugin.Interfaces
{
    /// <summary>
    /// Represents an output provider that can write StreamItems to a destination.
    /// </summary>
    public interface IOutput
    {
        /// <summary>
        /// Gets the name of this output provider
        /// </summary>
        string Name { get; }

        /// <summary>
        /// Initializes the output provider with the given configuration
        /// </summary>
        /// <param name="config">Provider-specific configuration</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>A task representing the asynchronous operation</returns>
        Task InitializeAsync(Dictionary<string, object> config, CancellationToken cancellationToken);

        /// <summary>
        /// Writes a collection of stream items to the output destination
        /// </summary>
        /// <param name="items">The items to write</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>A task representing the asynchronous operation</returns>
        Task WriteAsync(IEnumerable<StreamItem> items, CancellationToken cancellationToken);

        /// <summary>
        /// Flushes any buffered data to the output destination
        /// </summary>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>A task representing the asynchronous operation</returns>
        Task FlushAsync(CancellationToken cancellationToken);

        /// <summary>
        /// Performs cleanup operations when the output provider is no longer needed
        /// </summary>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>A task representing the asynchronous operation</returns>
        Task CloseAsync(CancellationToken cancellationToken);
    }
}
