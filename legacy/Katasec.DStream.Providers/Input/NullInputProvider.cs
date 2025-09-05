using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Katasec.DStream.Plugin.Models;

namespace Katasec.DStream.Providers.Input
{
    /// <summary>
    /// A null input provider that doesn't provide any input
    /// </summary>
    public class NullInputProvider : InputProviderBase
    {
        /// <summary>
        /// Gets the name of this input provider
        /// </summary>
        public override string Name => "null";

        /// <summary>
        /// Reads the next batch of items from the input source
        /// </summary>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>A collection of stream items</returns>
        public override Task<IEnumerable<StreamItem>> ReadAsync(CancellationToken cancellationToken)
        {
            // Return an empty collection as this is a null provider
            return Task.FromResult<IEnumerable<StreamItem>>(new StreamItem[0]);
        }

        /// <summary>
        /// Determines if there is more data available to read
        /// </summary>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>True if more data is available, false otherwise</returns>
        public override Task<bool> HasMoreDataAsync(CancellationToken cancellationToken)
        {
            // Always return false as this provider never has data
            return Task.FromResult(false);
        }
    }
}
