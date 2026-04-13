using FluentAssertions;
using Kontent.Ai.Delivery.Abstractions;
using Microsoft.Extensions.DependencyInjection;

namespace Kontent.Ai.Delivery.Tests.Factories;

public class DeliveryClientFactoryTests
{
    [Fact]
    public void GetNamedClient_WithValidName_ReturnsClient()
    {
        var services = new ServiceCollection();
        services.AddDeliveryClient("test", options =>
        {
            options.EnvironmentId = Guid.NewGuid().ToString();
        });

        using var sp = services.BuildServiceProvider();
        var factory = sp.GetRequiredService<IDeliveryClientFactory>();

        var client = factory.Get("test");

        client.Should().NotBeNull();
    }

    [Fact]
    public void GetMultipleNamedClients_ReturnsDistinctInstances()
    {
        var services = new ServiceCollection();
        var env1 = Guid.NewGuid().ToString();
        var env2 = Guid.NewGuid().ToString();

        services.AddDeliveryClient("client1", o => o.EnvironmentId = env1);
        services.AddDeliveryClient("client2", o => o.EnvironmentId = env2);

        using var sp = services.BuildServiceProvider();
        var factory = sp.GetRequiredService<IDeliveryClientFactory>();

        var client1 = factory.Get("client1");
        var client2 = factory.Get("client2");

        client1.Should().NotBeNull();
        client2.Should().NotBeNull();
        client1.Should().NotBeSameAs(client2);
    }

    [Fact]
    public void GetSameNamedClient_ReturnsSameInstance()
    {
        var services = new ServiceCollection();
        services.AddDeliveryClient("test", options =>
        {
            options.EnvironmentId = Guid.NewGuid().ToString();
        });

        using var sp = services.BuildServiceProvider();
        var factory = sp.GetRequiredService<IDeliveryClientFactory>();

        var client1 = factory.Get("test");
        var client2 = factory.Get("test");

        client1.Should().BeSameAs(client2);
    }

    [Fact]
    public void AddDeliveryClient_WithDuplicateName_Throws()
    {
        var services = new ServiceCollection();
        services.AddDeliveryClient("test", o => o.EnvironmentId = Guid.NewGuid().ToString());

        var act = () => services.AddDeliveryClient("test", o => o.EnvironmentId = Guid.NewGuid().ToString());

        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*'test'*already been registered*");
    }

    [Fact]
    public void GetNamedClient_WithNonExistentName_Throws()
    {
        var services = new ServiceCollection();
        services.AddDeliveryClient("test", o => o.EnvironmentId = Guid.NewGuid().ToString());

        using var sp = services.BuildServiceProvider();
        var factory = sp.GetRequiredService<IDeliveryClientFactory>();

        var act = () => factory.Get("nonexistent");

        act.Should().Throw<InvalidOperationException>();
    }

    [Fact]
    public void GetDefaultClient_WithImplicitRegistration_Works()
    {
        var services = new ServiceCollection();
        services.AddDeliveryClient(o => o.EnvironmentId = Guid.NewGuid().ToString());

        using var sp = services.BuildServiceProvider();
        var client = sp.GetRequiredService<IDeliveryClient>();

        client.Should().NotBeNull();
    }

    [Fact]
    public void GetDefaultClient_ViaFactory_Works()
    {
        var services = new ServiceCollection();
        services.AddDeliveryClient(o => o.EnvironmentId = Guid.NewGuid().ToString());

        using var sp = services.BuildServiceProvider();
        var factory = sp.GetRequiredService<IDeliveryClientFactory>();

        var client = factory.Get();

        client.Should().NotBeNull();
    }

    [Fact]
    public void KeyedServiceInjection_WithFromKeyedServices_Works()
    {
        var services = new ServiceCollection();
        services.AddDeliveryClient("keyed", o => o.EnvironmentId = Guid.NewGuid().ToString());

        using var sp = services.BuildServiceProvider();
        var client = sp.GetRequiredKeyedService<IDeliveryClient>("keyed");

        client.Should().NotBeNull();
    }

    [Fact]
    public void Get_WithNullName_Throws()
    {
        var services = new ServiceCollection();
        services.AddDeliveryClient(o => o.EnvironmentId = Guid.NewGuid().ToString());

        using var sp = services.BuildServiceProvider();
        var factory = sp.GetRequiredService<IDeliveryClientFactory>();

        var act = () => factory.Get(null!);

        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void Get_WithEmptyName_Throws()
    {
        var services = new ServiceCollection();
        services.AddDeliveryClient(o => o.EnvironmentId = Guid.NewGuid().ToString());

        using var sp = services.BuildServiceProvider();
        var factory = sp.GetRequiredService<IDeliveryClientFactory>();

        var act = () => factory.Get("");

        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void Get_WithWhitespaceName_Throws()
    {
        var services = new ServiceCollection();
        services.AddDeliveryClient(o => o.EnvironmentId = Guid.NewGuid().ToString());

        using var sp = services.BuildServiceProvider();
        var factory = sp.GetRequiredService<IDeliveryClientFactory>();

        var act = () => factory.Get("   ");

        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void Factory_IsRegisteredAsSingleton()
    {
        var services = new ServiceCollection();
        services.AddDeliveryClient(o => o.EnvironmentId = Guid.NewGuid().ToString());

        using var sp = services.BuildServiceProvider();

        var factory1 = sp.GetRequiredService<IDeliveryClientFactory>();
        var factory2 = sp.GetRequiredService<IDeliveryClientFactory>();

        factory1.Should().BeSameAs(factory2);
    }

    [Fact]
    public void TryGetNamedClient_WithValidName_ReturnsClient()
    {
        var services = new ServiceCollection();
        services.AddDeliveryClient("test", o => o.EnvironmentId = Guid.NewGuid().ToString());

        using var sp = services.BuildServiceProvider();
        var factory = sp.GetRequiredService<IDeliveryClientFactory>();

        var client = factory.TryGet("test");

        client.Should().NotBeNull();
    }

    [Fact]
    public void TryGetNamedClient_WithNonExistentName_ReturnsNull()
    {
        var services = new ServiceCollection();
        services.AddDeliveryClient("test", o => o.EnvironmentId = Guid.NewGuid().ToString());

        using var sp = services.BuildServiceProvider();
        var factory = sp.GetRequiredService<IDeliveryClientFactory>();

        var client = factory.TryGet("nonexistent");

        client.Should().BeNull();
    }

    [Fact]
    public void TryGet_WithNullName_Throws()
    {
        var services = new ServiceCollection();
        services.AddDeliveryClient(o => o.EnvironmentId = Guid.NewGuid().ToString());

        using var sp = services.BuildServiceProvider();
        var factory = sp.GetRequiredService<IDeliveryClientFactory>();

        var act = () => factory.TryGet(null!);

        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void TryGet_WithEmptyName_Throws()
    {
        var services = new ServiceCollection();
        services.AddDeliveryClient(o => o.EnvironmentId = Guid.NewGuid().ToString());

        using var sp = services.BuildServiceProvider();
        var factory = sp.GetRequiredService<IDeliveryClientFactory>();

        var act = () => factory.TryGet("");

        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void TryGet_WithWhitespaceName_Throws()
    {
        var services = new ServiceCollection();
        services.AddDeliveryClient(o => o.EnvironmentId = Guid.NewGuid().ToString());

        using var sp = services.BuildServiceProvider();
        var factory = sp.GetRequiredService<IDeliveryClientFactory>();

        var act = () => factory.TryGet("   ");

        act.Should().Throw<ArgumentException>();
    }
}
