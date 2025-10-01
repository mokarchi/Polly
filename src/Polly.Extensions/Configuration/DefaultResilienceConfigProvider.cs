using Microsoft.Extensions.Configuration;
using Polly.Utils;

namespace Polly.Configuration;

/// <summary>
/// Default implementation of <see cref="IResilienceConfigProvider"/> that reads configuration from <see cref="IConfiguration"/>.
/// </summary>
public sealed class DefaultResilienceConfigProvider : IResilienceConfigProvider
{
    /// <summary>
    /// The default configuration section name for resilience pipelines.
    /// </summary>
    public const string DefaultSectionName = "ResiliencePipelines";

    private readonly IConfiguration _configuration;
    private readonly string _sectionName;

    /// <summary>
    /// Initializes a new instance of the <see cref="DefaultResilienceConfigProvider"/> class.
    /// </summary>
    /// <param name="configuration">The configuration instance.</param>
    /// <param name="sectionName">The section name to read resilience pipeline configurations from. Defaults to "ResiliencePipelines".</param>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="configuration"/> is <see langword="null"/>.</exception>
    public DefaultResilienceConfigProvider(IConfiguration configuration, string? sectionName = null)
    {
        _configuration = Guard.NotNull(configuration);
        _sectionName = sectionName ?? DefaultSectionName;
    }

    /// <inheritdoc/>
    public IConfigurationSection? GetPipelineConfiguration(string pipelineName)
    {
        Guard.NotNull(pipelineName);

        var section = _configuration.GetSection(_sectionName);
        if (!section.Exists())
        {
            return null;
        }

        // Look for a child section with matching name
        foreach (var child in section.GetChildren())
        {
            var name = child.GetValue<string>("Name");
            if (string.Equals(name, pipelineName, StringComparison.OrdinalIgnoreCase))
            {
                return child;
            }
        }

        return null;
    }

    /// <inheritdoc/>
    public IEnumerable<string> PipelineNames
    {
        get
        {
            var section = _configuration.GetSection(_sectionName);
            if (!section.Exists())
            {
                yield break;
            }

            foreach (var child in section.GetChildren())
            {
                var name = child.GetValue<string>("Name");
                if (!string.IsNullOrEmpty(name))
                {
                    yield return name;
                }
            }
        }
    }
}