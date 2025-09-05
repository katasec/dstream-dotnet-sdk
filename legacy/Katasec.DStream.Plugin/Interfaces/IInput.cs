using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Katasec.DStream.Plugin.Models;

namespace Katasec.DStream.Plugin.Interfaces
{
    /// <summary>
    /// Represents an input provider that can read data from a source
    /// and produce StreamItems for processing.
    /// </summary>
    public interface IInput
    {
        /// <summary>
        /// Gets the name of this input provider
        /// </summary>
        string Name { get; }

        /// <summary>
        /// Initializes the input provider with the given configuration
        /// </summary>
        /// <param name="config">Provider-specific configuration</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>A task representing the asynchronous operation</returns>
        Task InitializeAsync(Dictionary<string, object> config, CancellationToken cancellationToken);

        /// <summary>
        /// Reads the next batch of items from the input source
        /// </summary>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>A collection of stream items</returns>
        Task<IEnumerable<StreamItem>> ReadAsync(CancellationToken cancellationToken);

        /// <summary>
        /// Determines if there is more data available to read
        /// </summary>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>True if more data is available, false otherwise</returns>
        Task<bool> HasMoreDataAsync(CancellationToken cancellationToken);

        /// <summary>
        /// Performs cleanup operations when the input provider is no longer needed
        /// </summary>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>A task representing the asynchronous operation</returns>
        Task CloseAsync(CancellationToken cancellationToken);
    }
}
