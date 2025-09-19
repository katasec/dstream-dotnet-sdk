using Katasec.DStream.Abstractions;

namespace Katasec.DStream.SDK.Core;

/// <summary>
/// Base class for providers that need infrastructure lifecycle management
/// Provides default implementations for infrastructure operations and Pulumi integration
/// </summary>
public abstract class InfrastructureProviderBase<TConfig> : ProviderBase<TConfig>, IInfrastructureProvider
    where TConfig : class
{
    /// <summary>
    /// Initialize infrastructure resources (create queues, topics, etc.)
    /// </summary>
    public virtual async Task<InfrastructureResult> InitializeAsync(CancellationToken ct)
    {
        await Console.Error.WriteLineAsync($"[{GetType().Name}] Initializing infrastructure...");
        
        try
        {
            // Call the provider-specific initialization
            var resources = await OnInitializeInfrastructureAsync(ct);
            
            await Console.Error.WriteLineAsync($"[{GetType().Name}] Infrastructure initialization completed");
            
            return new InfrastructureResult
            {
                Status = "Success",
                Resources = resources,
                Message = "Infrastructure initialized successfully"
            };
        }
        catch (Exception ex)
        {
            await Console.Error.WriteLineAsync($"[{GetType().Name}] Infrastructure initialization failed: {ex.Message}");
            
            return new InfrastructureResult
            {
                Status = "Failed",
                Error = ex.Message,
                Message = "Infrastructure initialization failed"
            };
        }
    }

    /// <summary>
    /// Destroy infrastructure resources (delete queues, topics, etc.)
    /// </summary>
    public virtual async Task<InfrastructureResult> DestroyAsync(CancellationToken ct)
    {
        await Console.Error.WriteLineAsync($"[{GetType().Name}] Destroying infrastructure...");
        
        try
        {
            // Call the provider-specific destruction
            var resources = await OnDestroyInfrastructureAsync(ct);
            
            await Console.Error.WriteLineAsync($"[{GetType().Name}] Infrastructure destruction completed");
            
            return new InfrastructureResult
            {
                Status = "Success",
                Resources = resources,
                Message = "Infrastructure destroyed successfully"
            };
        }
        catch (Exception ex)
        {
            await Console.Error.WriteLineAsync($"[{GetType().Name}] Infrastructure destruction failed: {ex.Message}");
            
            return new InfrastructureResult
            {
                Status = "Failed",
                Error = ex.Message,
                Message = "Infrastructure destruction failed"
            };
        }
    }

    /// <summary>
    /// Get current status of infrastructure resources
    /// </summary>
    public virtual async Task<InfrastructureResult> GetStatusAsync(CancellationToken ct)
    {
        await Console.Error.WriteLineAsync($"[{GetType().Name}] Checking infrastructure status...");
        
        try
        {
            // Call the provider-specific status check
            var (resources, metadata) = await OnGetInfrastructureStatusAsync(ct);
            
            await Console.Error.WriteLineAsync($"[{GetType().Name}] Infrastructure status check completed");
            
            return new InfrastructureResult
            {
                Status = "Success",
                Resources = resources,
                Metadata = metadata,
                Message = "Infrastructure status retrieved successfully"
            };
        }
        catch (Exception ex)
        {
            await Console.Error.WriteLineAsync($"[{GetType().Name}] Infrastructure status check failed: {ex.Message}");
            
            return new InfrastructureResult
            {
                Status = "Failed",
                Error = ex.Message,
                Message = "Infrastructure status check failed"
            };
        }
    }

    /// <summary>
    /// Preview infrastructure changes (similar to terraform plan)
    /// </summary>
    public virtual async Task<InfrastructureResult> PlanAsync(CancellationToken ct)
    {
        await Console.Error.WriteLineAsync($"[{GetType().Name}] Planning infrastructure changes...");
        
        try
        {
            // Call the provider-specific planning
            var (resources, changes) = await OnPlanInfrastructureChangesAsync(ct);
            
            await Console.Error.WriteLineAsync($"[{GetType().Name}] Infrastructure planning completed");
            
            return new InfrastructureResult
            {
                Status = "Success",
                Resources = resources,
                Metadata = changes,
                Message = "Infrastructure plan generated successfully"
            };
        }
        catch (Exception ex)
        {
            await Console.Error.WriteLineAsync($"[{GetType().Name}] Infrastructure planning failed: {ex.Message}");
            
            return new InfrastructureResult
            {
                Status = "Failed",
                Error = ex.Message,
                Message = "Infrastructure planning failed"
            };
        }
    }

    // ---------- Provider-specific implementation methods ----------
    
    /// <summary>
    /// Override this method to implement provider-specific infrastructure initialization
    /// </summary>
    protected virtual Task<string[]> OnInitializeInfrastructureAsync(CancellationToken ct)
    {
        // Default implementation - no infrastructure to initialize
        return Task.FromResult(Array.Empty<string>());
    }

    /// <summary>
    /// Override this method to implement provider-specific infrastructure destruction
    /// </summary>
    protected virtual Task<string[]> OnDestroyInfrastructureAsync(CancellationToken ct)
    {
        // Default implementation - no infrastructure to destroy
        return Task.FromResult(Array.Empty<string>());
    }

    /// <summary>
    /// Override this method to implement provider-specific status checking
    /// </summary>
    protected virtual Task<(string[] resources, Dictionary<string, object?>? metadata)> OnGetInfrastructureStatusAsync(CancellationToken ct)
    {
        // Default implementation - no infrastructure to check
        return Task.FromResult((Array.Empty<string>(), (Dictionary<string, object?>?)null));
    }

    /// <summary>
    /// Override this method to implement provider-specific planning
    /// </summary>
    protected virtual Task<(string[] resources, Dictionary<string, object?>? changes)> OnPlanInfrastructureChangesAsync(CancellationToken ct)
    {
        // Default implementation - no changes to plan
        return Task.FromResult((Array.Empty<string>(), (Dictionary<string, object?>?)null));
    }
}