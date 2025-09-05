using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Katasec.DStream.Abstractions.Interfaces;

/// <summary>
/// Generic interface for dstream plugins with strongly-typed configuration
/// </summary>
/// <typeparam name="TConfig">The type of configuration object for the plugin</typeparam>
public interface IDStreamPlugin<TConfig> where TConfig : class, new()
{
    Task ProcessAsync(IInput input, IOutput output, TConfig config, CancellationToken cancellationToken);
    IEnumerable<object> GetSchemaFields();
    string ModuleName { get; }
}
