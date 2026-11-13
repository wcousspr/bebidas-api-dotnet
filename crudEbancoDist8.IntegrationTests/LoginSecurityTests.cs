using crudEbancoDist.Data;
using crudEbancoDist8.Controllers;
using crudEbancoDist8.DTOs.Auth;
using crudEbancoDist8.Interfaces;
using crudEbancoDist8.Models;
using crudEbancoDist8.Extensions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using System.Text.Json;

namespace crudEbancoDist8.IntegrationTests;

// No application host/configuration is loaded; all data lives in local SQLite memory.
public sealed class LoginSecurityTests : IAsyncLifetime
{
    private readonly SqliteConnection _connection = new("Data Source=:memory:");
    private ServiceProvider _provider = null!;
    private UserManager<Usuario> _users = null!;
    private AuthController _controller = null!;
    private readonly Mock<ITokenService> _tokens = new(MockBehavior.Strict);
    private Usuario _user = null!;

    public async Task InitializeAsync()
    {
        await _connection.OpenAsync();
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddAuthentication();
        services.AddDbContext<AppDbContext>(o => o.UseSqlite(_connection));
        services.AddIdentityCore<Usuario>(LoginSecurityExtensions.ConfigureLoginLockout)
            .AddRoles<IdentityRole>().AddEntityFrameworkStores<AppDbContext>().AddSignInManager();
        _provider = services.BuildServiceProvider();
        await _provider.GetRequiredService<AppDbContext>().Database.EnsureCreatedAsync();
        _users = _provider.GetRequiredService<UserManager<Usuario>>();
        _user = new Usuario { UserName = "tester", Email = "tester@example.test" };
        Assert.True((await _users.CreateAsync(_user, "Valid123!")).Succeeded);
        var roles = _provider.GetRequiredService<RoleManager<IdentityRole>>();
        Assert.True((await roles.CreateAsync(new IdentityRole("User"))).Succeeded);
        Assert.True((await _users.AddToRoleAsync(_user, "User")).Succeeded);
        var signIn = _provider.GetRequiredService<SignInManager<Usuario>>();
        signIn.Context = new DefaultHttpContext { RequestServices = _provider };
        _controller = new AuthController(_users, NullLogger<AuthController>.Instance,
            _tokens.Object, signIn);
    }

    private Task<IActionResult> Login(string password, string email = "tester@example.test") =>
        _controller.Login(new LoginDto { Email = email, Password = password });

    private static string Error(IActionResult result)
    {
        var response = Assert.IsType<UnauthorizedObjectResult>(result);
        Assert.Equal(401, response.StatusCode);
        return JsonSerializer.Serialize(response.Value);
    }

    [Fact]
    public async Task WrongPassword_CountsFailure_WithoutToken()
    {
        Assert.Contains("Credenciais", Error(await Login("wrong")));
        Assert.Equal(1, await _users.GetAccessFailedCountAsync(_user));
        _tokens.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task FiveFailures_LockAccount_RejectEvenCorrectPassword_ThenAllowAfterExpiry()
    {
        for (var i = 0; i < 5; i++) Error(await Login("wrong"));
        Assert.True(await _users.IsLockedOutAsync(_user));
        var end = await _users.GetLockoutEndDateAsync(_user);
        Assert.InRange((end!.Value - DateTimeOffset.UtcNow).TotalMinutes, 14, 15);
        Assert.Equal(Error(await Login("wrong", "missing@example.test")),
            Error(await Login("Valid123!")));
        _tokens.VerifyNoOtherCalls();
        await _users.SetLockoutEndDateAsync(_user, DateTimeOffset.UtcNow.AddSeconds(-1));
        ExpectToken();
        Assert.IsType<OkObjectResult>(await Login("Valid123!"));
    }

    [Fact]
    public async Task MissingUser_HasSameErrorAsWrongPassword_WithoutToken()
    {
        Assert.Equal(Error(await Login("wrong")),
            Error(await Login("wrong", "missing@example.test")));
        _tokens.VerifyNoOtherCalls();
    }

    private void ExpectToken() => _tokens.Setup(t => t.GenerateToken(_user,
        It.Is<IList<string>>(r => r.SequenceEqual(new[] { "User" }))))
        .Returns("test-jwt");

    [Fact]
    public async Task ValidLogin_ResetsFailures_PreservesResponseAndRoles()
    {
        Error(await Login("wrong"));
        ExpectToken();
        var result = Assert.IsType<OkObjectResult>(await Login("Valid123!", " TESTER@example.test "));
        var body = JsonSerializer.SerializeToElement(result.Value);
        Assert.Equal("Bearer", body.GetProperty("tokenType").GetString());
        Assert.Equal("test-jwt", body.GetProperty("token").GetString());
        Assert.Equal("Login realizado com sucesso.", body.GetProperty("message").GetString());
        Assert.Equal(_user.Id, body.GetProperty("usuario").GetProperty("Id").GetString());
        Assert.Equal("User", body.GetProperty("usuario").GetProperty("roles")[0].GetString());
        Assert.Equal(0, await _users.GetAccessFailedCountAsync(_user));
        _tokens.Verify(t => t.GenerateToken(_user, It.IsAny<IList<string>>()), Times.Once);
    }

    public async Task DisposeAsync()
    {
        await _provider.DisposeAsync();
        await _connection.DisposeAsync();
    }
}
