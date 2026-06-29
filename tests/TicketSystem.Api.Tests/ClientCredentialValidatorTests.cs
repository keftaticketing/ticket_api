using FluentAssertions;
using Microsoft.AspNetCore.Http;
using TicketSystem.Api.Options;
using TicketSystem.Api.Security;

namespace TicketSystem.Api.Tests;

public sealed class ClientCredentialValidatorTests
{
    [Fact]
    public void Validate_AcceptsMobileCredentials()
    {
        var request = CreateRequest("ticket-counter", "dev-mobile-client-key");
        var mobile = new MobileClientOptions { ClientId = "ticket-counter", SharedKey = "dev-mobile-client-key" };
        var angular = new AngularClientOptions { ClientId = "ticket-admin", SharedKey = "dev-angular-client-key" };

        ClientCredentialValidator.TryValidate(request, mobile, angular).Should().BeTrue();
    }

    [Fact]
    public void Validate_AcceptsAngularCredentials()
    {
        var request = CreateRequest("ticket-admin", "dev-angular-client-key");
        var mobile = new MobileClientOptions { ClientId = "ticket-counter", SharedKey = "dev-mobile-client-key" };
        var angular = new AngularClientOptions { ClientId = "ticket-admin", SharedKey = "dev-angular-client-key" };

        ClientCredentialValidator.TryValidate(request, mobile, angular).Should().BeTrue();
    }

    [Fact]
    public void Validate_RejectsWrongKey()
    {
        var request = CreateRequest("ticket-admin", "wrong-key");
        var mobile = new MobileClientOptions { ClientId = "ticket-counter", SharedKey = "dev-mobile-client-key" };
        var angular = new AngularClientOptions { ClientId = "ticket-admin", SharedKey = "dev-angular-client-key" };

        ClientCredentialValidator.TryValidate(request, mobile, angular).Should().BeFalse();
    }

    [Fact]
    public void Validate_RejectsMissingHeaders()
    {
        var request = new DefaultHttpContext().Request;
        var mobile = new MobileClientOptions { ClientId = "ticket-counter", SharedKey = "dev-mobile-client-key" };

        ClientCredentialValidator.TryValidate(request, mobile).Should().BeFalse();
    }

    private static HttpRequest CreateRequest(string clientId, string clientKey)
    {
        var context = new DefaultHttpContext();
        context.Request.Headers[ClientCredentialHeaders.ClientId] = clientId;
        context.Request.Headers[ClientCredentialHeaders.ClientKey] = clientKey;
        return context.Request;
    }
}
