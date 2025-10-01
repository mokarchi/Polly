using Microsoft.Extensions.Configuration;
using Polly.Configuration;

namespace Polly.Extensions.Tests.Configuration;

public class DefaultResilienceConfigProviderTests
{
    [Fact]
    public void Constructor_WithNullConfiguration_ThrowsArgumentNullException()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => new DefaultResilienceConfigProvider(null!));
    }

    [Fact]
    public void Constructor_WithValidConfiguration_InitializesCorrectly()
    {
        // Arrange
        var configuration = new ConfigurationBuilder().Build();

        // Act
        var provider = new DefaultResilienceConfigProvider(configuration);

        // Assert
        provider.ShouldNotBeNull();
    }

    [Fact]
    public void Constructor_WithCustomSectionName_InitializesCorrectly()
    {
        // Arrange
        var configuration = new ConfigurationBuilder().Build();
        const string customSectionName = "CustomSection";

        // Act
        var provider = new DefaultResilienceConfigProvider(configuration, customSectionName);

        // Assert
        provider.ShouldNotBeNull();
    }

    [Fact]
    public void GetPipelineConfiguration_WithNullPipelineName_ThrowsArgumentNullException()
    {
        // Arrange
        var configuration = new ConfigurationBuilder().Build();
        var provider = new DefaultResilienceConfigProvider(configuration);

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => provider.GetPipelineConfiguration(null!));
    }

    [Fact]
    public void GetPipelineConfiguration_WithEmptyConfiguration_ReturnsNull()
    {
        // Arrange
        var configuration = new ConfigurationBuilder().Build();
        var provider = new DefaultResilienceConfigProvider(configuration);

        // Act
        var result = provider.GetPipelineConfiguration("test-pipeline");

        // Assert
        result.ShouldBeNull();
    }

    [Fact]
    public void GetPipelineConfiguration_WithValidConfiguration_ReturnsCorrectSection()
    {
        // Arrange
        var configData = new Dictionary<string, string?>
        {
            { "ResiliencePipelines:0:Name", "pipeline1" },
            { "ResiliencePipelines:0:RetryOptions:MaxRetryAttempts", "3" },
            { "ResiliencePipelines:1:Name", "pipeline2" },
            { "ResiliencePipelines:1:TimeoutOptions:Timeout", "00:00:30" }
        };

        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(configData)
            .Build();

        var provider = new DefaultResilienceConfigProvider(configuration);

        // Act
        var result = provider.GetPipelineConfiguration("pipeline1");

        // Assert
        result.ShouldNotBeNull();
        result.GetValue<string>("Name").ShouldBe("pipeline1");
        result.GetValue<int>("RetryOptions:MaxRetryAttempts").ShouldBe(3);
    }

    [Fact]
    public void GetPipelineConfiguration_WithCaseInsensitiveMatch_ReturnsCorrectSection()
    {
        // Arrange
        var configData = new Dictionary<string, string?>
        {
            { "ResiliencePipelines:0:Name", "Pipeline1" },
            { "ResiliencePipelines:0:RetryOptions:MaxRetryAttempts", "3" }
        };

        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(configData)
            .Build();

        var provider = new DefaultResilienceConfigProvider(configuration);

        // Act
        var result = provider.GetPipelineConfiguration("pipeline1");

        // Assert
        result.ShouldNotBeNull();
        result.GetValue<string>("Name").ShouldBe("Pipeline1");
    }

    [Fact]
    public void GetPipelineConfiguration_WithCustomSectionName_ReturnsCorrectSection()
    {
        // Arrange
        var configData = new Dictionary<string, string?>
        {
            { "CustomPipelines:0:Name", "pipeline1" },
            { "CustomPipelines:0:RetryOptions:MaxRetryAttempts", "3" }
        };

        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(configData)
            .Build();

        var provider = new DefaultResilienceConfigProvider(configuration, "CustomPipelines");

        // Act
        var result = provider.GetPipelineConfiguration("pipeline1");

        // Assert
        result.ShouldNotBeNull();
        result.GetValue<string>("Name").ShouldBe("pipeline1");
    }

    [Fact]
    public void GetPipelineConfiguration_WithNonExistentPipeline_ReturnsNull()
    {
        // Arrange
        var configData = new Dictionary<string, string?>
        {
            { "ResiliencePipelines:0:Name", "pipeline1" }
        };

        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(configData)
            .Build();

        var provider = new DefaultResilienceConfigProvider(configuration);

        // Act
        var result = provider.GetPipelineConfiguration("nonexistent");

        // Assert
        result.ShouldBeNull();
    }

    [Fact]
    public void PipelineNames_WithEmptyConfiguration_ReturnsEmpty()
    {
        // Arrange
        var configuration = new ConfigurationBuilder().Build();
        var provider = new DefaultResilienceConfigProvider(configuration);

        // Act
        var result = provider.PipelineNames.ToList();

        // Assert
        result.ShouldBeEmpty();
    }

    [Fact]
    public void PipelineNames_WithValidConfiguration_ReturnsAllNames()
    {
        // Arrange
        var configData = new Dictionary<string, string?>
        {
            { "ResiliencePipelines:0:Name", "pipeline1" },
            { "ResiliencePipelines:1:Name", "pipeline2" },
            { "ResiliencePipelines:2:Name", "pipeline3" }
        };

        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(configData)
            .Build();

        var provider = new DefaultResilienceConfigProvider(configuration);

        // Act
        var result = provider.PipelineNames.ToList();

        // Assert
        result.ShouldContain("pipeline1");
        result.ShouldContain("pipeline2");
        result.ShouldContain("pipeline3");
        result.Count.ShouldBe(3);
    }

    [Fact]
    public void PipelineNames_WithEmptyNames_SkipsEmptyEntries()
    {
        // Arrange
        var configData = new Dictionary<string, string?>
        {
            { "ResiliencePipelines:0:Name", "pipeline1" },
            { "ResiliencePipelines:1:Name", "" },
            { "ResiliencePipelines:2:Name", "pipeline3" },
            { "ResiliencePipelines:3:OtherProperty", "value" } // No Name property
        };

        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(configData)
            .Build();

        var provider = new DefaultResilienceConfigProvider(configuration);

        // Act
        var result = provider.PipelineNames.ToList();

        // Assert
        result.ShouldContain("pipeline1");
        result.ShouldContain("pipeline3");
        result.Count.ShouldBe(2);
    }

    [Fact]
    public void PipelineNames_WithCustomSectionName_ReturnsNamesFromCustomSection()
    {
        // Arrange
        var configData = new Dictionary<string, string?>
        {
            { "CustomPipelines:0:Name", "custom1" },
            { "CustomPipelines:1:Name", "custom2" },
            { "ResiliencePipelines:0:Name", "default1" } // Should be ignored
        };

        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(configData)
            .Build();

        var provider = new DefaultResilienceConfigProvider(configuration, "CustomPipelines");

        // Act
        var result = provider.PipelineNames.ToList();

        // Assert
        result.ShouldContain("custom1");
        result.ShouldContain("custom2");
        result.ShouldNotContain("default1");
        result.Count.ShouldBe(2);
    }

    [Fact]
    public void DefaultSectionName_HasCorrectValue()
    {
        // Assert
        DefaultResilienceConfigProvider.DefaultSectionName.ShouldBe("ResiliencePipelines");
    }
}