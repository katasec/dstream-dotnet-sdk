using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Katasec.DStream.Abstractions;

/// <summary>
/// Core interface for dstream plugins
/// Plugin developers only need to implement this interface
/// </summary>
public interface IDStreamPlugin
{
    Task ProcessAsync(Interfaces.IInput input, Interfaces.IOutput output, Dictionary<string, object> config, CancellationToken cancellationToken);
    IEnumerable<object> GetSchemaFields();
    string ModuleName { get; }
}
