using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Katasec.DStream.Abstractions;

/// <summary>
/// Base class for DStream plugins that provides compatibility between generic and non-generic interfaces
/// </summary>
/// <typeparam name="TConfig">The type of configuration object for the plugin</typeparam>
public abstract class DStreamPluginBase<TConfig> : IDStreamPlugin where TConfig : class, new()
{
    public abstract string ModuleName { get; }
    public abstract IEnumerable<object> GetSchemaFields();
    public abstract Task ProcessAsync(Interfaces.IInput input, Interfaces.IOutput output, TConfig config, CancellationToken cancellationToken);
    public Task ProcessAsync(Interfaces.IInput input, Interfaces.IOutput output, Dictionary<string, object> config, CancellationToken cancellationToken)
    {
        // Implement config conversion logic here or in derived class
        throw new NotImplementedException();
    }
}
