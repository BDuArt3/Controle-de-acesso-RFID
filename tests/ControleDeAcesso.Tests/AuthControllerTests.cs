using ControleDeAcesso.Api.Controllers;
using Microsoft.AspNetCore.Authorization;
using Xunit;

namespace ControleDeAcesso.Tests;

public sealed class AuthControllerTests
{
    [Fact]
    public void LogoutCanRevokeRefreshTokenAfterAccessTokenExpires()
    {
        var method = typeof(AuthController).GetMethod(nameof(AuthController.Logout));

        Assert.NotNull(method);
        Assert.NotNull(method!.GetCustomAttributes(typeof(AllowAnonymousAttribute), inherit: true).SingleOrDefault());
        Assert.Null(method.GetCustomAttributes(typeof(AuthorizeAttribute), inherit: true).SingleOrDefault());
    }
}
