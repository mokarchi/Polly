using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using NSubstitute;
using Polly.Configuration;

namespace Polly.Extensions.Tests.Configuration;

public class ResilienceConfigProviderServiceCollectionExtensionsTests
{
    [Fact]
    public void AddResilienceConfigProvider_WithNullServices_ThrowsArgumentNullException()
    {
        // Arrange
        IServiceCollection services = null!;

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => services.AddResilienceConfigProvider());
        Assert.Throws<ArgumentNullException>(() => services.AddResilienceConfigProvider("CustomSection"));
        Assert.Throws<ArgumentNullException>(() => services.AddResilienceConfigProvider(_ => Substitute.For<IResilienceConfigProvider>()));
        Assert.Throws<ArgumentNullException>(() => services.AddResilienceConfigProvider<DefaultResilienceConfigProvider>());
    }

    [Fact]
    public void AddResilienceConfigProvider_WithDefaultSection_RegistersCorrectly()
    {
        // Arrange
        var services = new ServiceCollection();
        var configData = new Dictionary<string, string?>
        {
            { "ResiliencePipelines:0:Name", "test-pipeline" }
        };
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(configData)
            .Build();
        services.AddSingleton<IConfiguration>(configuration);

        // Act
        services.AddResilienceConfigProvider();

        // Assert
        var serviceProvider = services.BuildServiceProvider();
        var provider = serviceProvider.GetRequiredService<IResilienceConfigProvider>();

        provider.ShouldNotBeNull();
        provider.ShouldBeOfType<DefaultResilienceConfigProvider>();
        provider.PipelineNames.ShouldContain("test-pipeline");
    }

    [Fact]
    public void AddResilienceConfigProvider_WithCustomSection_RegistersCorrectly()
    {
        // Arrange
        var services = new ServiceCollection();
        var configData = new Dictionary<string, string?>
        {
            { "CustomSection:0:Name", "test-pipeline" }
        };
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(configData)
            .Build();
        services.AddSingleton<IConfiguration>(configuration);

        // Act
        services.AddResilienceConfigProvider("CustomSection");

        // Assert
        var serviceProvider = services.BuildServiceProvider();
        var provider = serviceProvider.GetRequiredService<IResilienceConfigProvider>();

        provider.ShouldNotBeNull();
        provider.ShouldBeOfType<DefaultResilienceConfigProvider>();
        provider.PipelineNames.ShouldContain("test-pipeline");
    }

    [Fact]
    public void AddResilienceConfigProvider_WithFactory_RegistersCorrectly()
    {
        // Arrange
        var services = new ServiceCollection();
        var mockProvider = Substitute.For<IResilienceConfigProvider>();
        mockProvider.PipelineNames.Returns(new[] { "factory-pipeline" });

        // Act
        services.AddResilienceConfigProvider(_ => mockProvider);

        // Assert
        var serviceProvider = services.BuildServiceProvider();
        var provider = serviceProvider.GetRequiredService<IResilienceConfigProvider>();

        provider.ShouldBe(mockProvider);
        provider.PipelineNames.ShouldContain("factory-pipeline");
    }

    [Fact]
    public void AddResilienceConfigProvider_WithFactoryNull_ThrowsArgumentNullException()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => services.AddResilienceConfigProvider((Func<IServiceProvider, IResilienceConfigProvider>)null!));
    }

    [Fact]
    public void AddResilienceConfigProvider_WithGenericType_RegistersCorrectly()
    {
        // Arrange
        var services = new ServiceCollection();
        var configData = new Dictionary<string, string?>
        {
            { "ResiliencePipelines:0:Name", "generic-pipeline" }
        };
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(configData)
            .Build();
        services.AddSingleton<IConfiguration>(configuration);

        // Act
        services.AddResilienceConfigProvider<DefaultResilienceConfigProvider>();

        // Assert
        var serviceProvider = services.BuildServiceProvider();
        var provider = serviceProvider.GetRequiredService<IResilienceConfigProvider>();

        provider.ShouldNotBeNull();
        provider.ShouldBeOfType<DefaultResilienceConfigProvider>();
        provider.PipelineNames.ShouldContain("generic-pipeline");
    }

    [Fact]
    public void AddResilienceConfigProvider_MultipleRegistrations_OnlyRegistersOnce()
    {
        // Arrange
        var services = new ServiceCollection();
        var configuration = new ConfigurationBuilder().Build();
        services.AddSingleton<IConfiguration>(configuration);

        // Act
        services.AddResilienceConfigProvider();
        services.AddResilienceConfigProvider("AnotherSection"); // Should be ignored

        // Assert
        var serviceDescriptors = services.Where(s => s.ServiceType == typeof(IResilienceConfigProvider)).ToList();
        serviceDescriptors.Count.ShouldBe(1);
    }

    [Fact]
    public void AddResilienceConfigProvider_RegistersAsSingleton()
    {
        // Arrange
        var services = new ServiceCollection();
        var configuration = new ConfigurationBuilder().Build();
        services.AddSingleton<IConfiguration>(configuration);

        // Act
        services.AddResilienceConfigProvider();

        // Assert
        var serviceDescriptor = services.First(s => s.ServiceType == typeof(IResilienceConfigProvider));
        serviceDescriptor.Lifetime.ShouldBe(ServiceLifetime.Singleton);
    }

    [Fact]
    public void AddResilienceConfigProvider_WithFactory_ReceivesServiceProvider()
    {
        // Arrange
        var services = new ServiceCollection();
        var configuration = new ConfigurationBuilder().Build();
        services.AddSingleton<IConfiguration>(configuration);

        IServiceProvider? receivedServiceProvider = null;
        services.AddResilienceConfigProvider(sp =>
        {
            receivedServiceProvider = sp;
            return new DefaultResilienceConfigProvider(sp.GetRequiredService<IConfiguration>());
        });

        // Act
        var serviceProvider = services.BuildServiceProvider();

        // This triggers the factory
        _ = serviceProvider.GetRequiredService<IResilienceConfigProvider>();

        // Assert
        receivedServiceProvider.ShouldNotBeNull();
    }
}