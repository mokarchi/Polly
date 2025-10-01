using Microsoft.Extensions.Configuration;

namespace Polly.Configuration;

/// <summary>
/// Provides configuration for resilience pipelines.
/// </summary>
public interface IResilienceConfigProvider
{
    /// <summary>
    /// Gets the configuration section for the specified pipeline name.
    /// </summary>
    /// <param name="pipelineName">The name of the pipeline to get configuration for.</param>
    /// <returns>The configuration section for the pipeline, or null if not found.</returns>
    IConfigurationSection? GetPipelineConfiguration(string pipelineName);

    /// <summary>
    /// Gets all available pipeline names from the configuration.
    /// </summary>
    IEnumerable<string> PipelineNames { get; }
}