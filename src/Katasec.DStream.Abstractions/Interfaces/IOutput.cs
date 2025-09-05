using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Katasec.DStream.Abstractions.Interfaces
{
    /// <summary>
    /// Represents an output provider that can write StreamItems to a destination.
    /// </summary>
    public interface IOutput
    {
        string Name { get; }
        Task InitializeAsync(Dictionary<string, object> config, CancellationToken cancellationToken);
        Task WriteAsync(IEnumerable<object> items, CancellationToken cancellationToken);
        Task FlushAsync(CancellationToken cancellationToken);
        Task CloseAsync(CancellationToken cancellationToken);
    }
}
