using System.Reflection;
using FluentAssertions;
using NetArchTest.Rules;

namespace TicketSystem.Architecture.Tests;

public sealed class LayerArchitectureTests
{
    private static readonly Assembly DomainAssembly = typeof(Domain.Entities.Bus).Assembly;
    private static readonly Assembly ApplicationAssembly = typeof(Application.DependencyInjection).Assembly;
    private static readonly Assembly InfrastructureAssembly = typeof(Infrastructure.DependencyInjection).Assembly;
    private static readonly Assembly ApiAssembly = typeof(Api.Controllers.AuthController).Assembly;
    private static readonly Assembly ContractsAssembly = typeof(Contracts.Auth.LoginRequest).Assembly;

    [Fact]
    public void Domain_Should_NotDependOnOtherLayers()
    {
        var result = Types.InAssembly(DomainAssembly)
            .Should()
            .NotHaveDependencyOnAny(
                "TicketSystem.Application",
                "TicketSystem.Infrastructure",
                "TicketSystem.Api",
                "TicketSystem.Contracts")
            .GetResult();

        result.IsSuccessful.Should().BeTrue(string.Join(", ", result.FailingTypeNames ?? []));
    }

    [Fact]
    public void Contracts_Should_NotDependOnApplicationOrInfrastructureOrApi()
    {
        var result = Types.InAssembly(ContractsAssembly)
            .Should()
            .NotHaveDependencyOnAny(
                "TicketSystem.Application",
                "TicketSystem.Infrastructure",
                "TicketSystem.Api")
            .GetResult();

        result.IsSuccessful.Should().BeTrue(string.Join(", ", result.FailingTypeNames ?? []));
    }

    [Fact]
    public void Application_Should_NotDependOnInfrastructureOrApi()
    {
        var result = Types.InAssembly(ApplicationAssembly)
            .Should()
            .NotHaveDependencyOnAny(
                "TicketSystem.Infrastructure",
                "TicketSystem.Api")
            .GetResult();

        result.IsSuccessful.Should().BeTrue(string.Join(", ", result.FailingTypeNames ?? []));
    }

    [Fact]
    public void Infrastructure_Should_NotDependOnApi()
    {
        var result = Types.InAssembly(InfrastructureAssembly)
            .Should()
            .NotHaveDependencyOn("TicketSystem.Api")
            .GetResult();

        result.IsSuccessful.Should().BeTrue(string.Join(", ", result.FailingTypeNames ?? []));
    }

    [Fact]
    public void ApplicationServices_ShouldResideInFeaturesNamespace()
    {
        var result = Types.InAssembly(ApplicationAssembly)
            .That()
            .HaveNameEndingWith("Service")
            .And()
            .AreClasses()
            .Should()
            .ResideInNamespaceMatching("TicketSystem.Application.Features")
            .GetResult();

        result.IsSuccessful.Should().BeTrue(string.Join(", ", result.FailingTypeNames ?? []));
    }

    [Fact]
    public void EntityConfigurations_ShouldResideInPersistenceConfigurationsNamespace()
    {
        var result = Types.InAssembly(InfrastructureAssembly)
            .That()
            .HaveNameEndingWith("Configuration")
            .And()
            .AreClasses()
            .Should()
            .ResideInNamespace("TicketSystem.Infrastructure.Persistence.Configurations")
            .GetResult();

        result.IsSuccessful.Should().BeTrue(string.Join(", ", result.FailingTypeNames ?? []));
    }

    [Fact]
    public void Controllers_ShouldResideInApiControllersNamespace()
    {
        var result = Types.InAssembly(ApiAssembly)
            .That()
            .HaveNameEndingWith("Controller")
            .Should()
            .ResideInNamespace("TicketSystem.Api.Controllers")
            .GetResult();

        result.IsSuccessful.Should().BeTrue(string.Join(", ", result.FailingTypeNames ?? []));
    }

    [Fact]
    public void Infrastructure_ShouldNotContainApplicationServices()
    {
        var result = Types.InAssembly(InfrastructureAssembly)
            .Should()
            .NotResideInNamespace("TicketSystem.Infrastructure.Services")
            .GetResult();

        result.IsSuccessful.Should().BeTrue(string.Join(", ", result.FailingTypeNames ?? []));
    }
}
