namespace ReferenceApp.IntegrationTests.Authorization;

/// <summary>
/// Integration tests for permission enforcement on protected endpoints.
/// 2 scenarios: 403 without grant, 2xx with grant.
///
/// Permission mechanism: claims-based via NacIdentityClaims.Permission on role claims.
/// PermissionAuthorizationHandler checks HttpContext.User for claim type "permission"
/// matching the policy name (e.g. "Orders.Create").
/// Source: src/Nac.Identity/Permissions/PermissionAuthorizationHandler.cs +
///         samples/ReferenceApp/src/Host/Seeding/AdminSeeder.cs.
/// </summary>
[Collection("Integration")]
public sealed class PermissionEnforcementTests(ReferenceAppFactory factory) : IAsyncLifetime
{
    public async ValueTask InitializeAsync() => await factory.ResetDatabaseAsync();
    public ValueTask DisposeAsync() => ValueTask.CompletedTask;

    [Fact]
    public async Task EndpointRequiringPermission_UserWithoutGrant_Returns403()
    {
        // Arrange — noperm@test.com has no roles, no permission claims.
        var client = await factory.CreateAuthenticatedClientAsync(
            TestDataSeeder.NoPermEmail, TestDataSeeder.TestPassword);

        var request = new CreateOrderRequest(
        [
            new OrderItemDto(Guid.NewGuid(), Quantity: 1, UnitPrice: 10m),
        ]);

        // Act — POST /api/orders requires [Authorize(Policy = "Orders.Create")].
        var response = await client.PostAsJsonAsync("/api/orders", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden,
            because: "user has no Orders.Create permission claim");
    }

    [Fact]
    public async Task EndpointRequiringPermission_UserWithGrant_Returns2xx()
    {
        // Arrange — admin@test.com has all permissions via "admin" role.
        var client = await factory.CreateAuthenticatedClientAsync(
            TestDataSeeder.AdminEmail, TestDataSeeder.TestPassword);

        var request = new CreateOrderRequest(
        [
            new OrderItemDto(Guid.NewGuid(), Quantity: 2, UnitPrice: 15m),
        ]);

        // Act
        var response = await client.PostAsJsonAsync("/api/orders", request);

        // Assert — 201 Created confirms both authentication and authorisation passed.
        response.StatusCode.Should().Be(HttpStatusCode.Created,
            because: "admin user has Orders.Create permission claim via admin role");
    }
}
