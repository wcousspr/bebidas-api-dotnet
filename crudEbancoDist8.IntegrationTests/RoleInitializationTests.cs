using crudEbancoDist.Data;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;

namespace crudEbancoDist8.IntegrationTests;

public class RoleInitializationTests
{
    [Fact]
    public async Task RepeatedSeedDoesNotRecreateExistingRoles()
    {
        var existing = new HashSet<string>();
        var manager = CreateManager();
        manager.Setup(x => x.RoleExistsAsync(It.IsAny<string>()))
            .ReturnsAsync((string role) => existing.Contains(role));
        manager.Setup(x => x.CreateAsync(It.IsAny<IdentityRole>()))
            .ReturnsAsync((IdentityRole role) => existing.Add(role.Name!)
                ? IdentityResult.Success : IdentityResult.Failed());

        await IdentitySeeder.SeedRolesAsync(manager.Object);
        await IdentitySeeder.SeedRolesAsync(manager.Object);

        Assert.Equal(new[] { "Admin", "User" }, existing.Order());
        manager.Verify(x => x.CreateAsync(It.IsAny<IdentityRole>()), Times.Exactly(2));
    }

    [Fact]
    public async Task FailedCreateWithoutExistingRoleFailsTheSeed()
    {
        var manager = CreateManager();
        manager.Setup(x => x.RoleExistsAsync("Admin")).ReturnsAsync(false);
        manager.Setup(x => x.CreateAsync(It.Is<IdentityRole>(role => role.Name == "Admin")))
            .ReturnsAsync(IdentityResult.Failed(new IdentityError { Code = "ValidationError" }));

        await Assert.ThrowsAsync<InvalidOperationException>(() => IdentitySeeder.SeedRolesAsync(manager.Object));
        manager.Verify(x => x.CreateAsync(It.Is<IdentityRole>(role => role.Name == "User")), Times.Never);
    }

    [Fact]
    public async Task ConcurrentCreationAcceptsDuplicateOnlyAfterConfirmingRoleExists()
    {
        var first = CreateManager();
        var second = CreateManager();
        var bothCreating = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var arrivals = 0;
        var created = 0;
        foreach (var manager in new[] { first, second })
        {
            manager.Setup(x => x.RoleExistsAsync("Admin")).ReturnsAsync(() => Volatile.Read(ref created) == 1);
            manager.Setup(x => x.RoleExistsAsync("User")).ReturnsAsync(true);
            manager.Setup(x => x.CreateAsync(It.Is<IdentityRole>(role => role.Name == "Admin")))
                .Returns(async () =>
                {
                    if (Interlocked.Increment(ref arrivals) == 2)
                        bothCreating.TrySetResult();
                    await bothCreating.Task.WaitAsync(TimeSpan.FromSeconds(2));
                    return Interlocked.Exchange(ref created, 1) == 0
                        ? IdentityResult.Success
                        : IdentityResult.Failed(new IdentityErrorDescriber().DuplicateRoleName("Admin"));
                });
        }

        await Task.WhenAll(IdentitySeeder.SeedRolesAsync(first.Object), IdentitySeeder.SeedRolesAsync(second.Object));
        Assert.Equal(2, arrivals);
        Assert.Equal(1, created);
    }

    private static Mock<RoleManager<IdentityRole>> CreateManager() => new(
        MockBehavior.Loose, Mock.Of<IRoleStore<IdentityRole>>(),
        Array.Empty<IRoleValidator<IdentityRole>>(), new UpperInvariantLookupNormalizer(),
        new IdentityErrorDescriber(), NullLogger<RoleManager<IdentityRole>>.Instance);
}
