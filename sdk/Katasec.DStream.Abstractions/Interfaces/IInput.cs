using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Katasec.DStream.Abstractions.Interfaces
{
    /// <summary>
    /// Represents an input provider that can read data from a source
    /// and produce StreamItems for processing.
    /// </summary>
    public interface IInput
    {
        string Name { get; }
        Task InitializeAsync(Dictionary<string, object> config, CancellationToken cancellationToken);
        Task<IEnumerable<object>> ReadAsync(CancellationToken cancellationToken);
        Task<bool> HasMoreDataAsync(CancellationToken cancellationToken);
        Task CloseAsync(CancellationToken cancellationToken);
    }
}
