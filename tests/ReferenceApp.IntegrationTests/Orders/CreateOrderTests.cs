namespace ReferenceApp.IntegrationTests.Orders;

/// <summary>
/// Integration tests for POST /api/orders.
/// 4 scenarios: 201, 400 (empty items), 401 (no auth), 403 (missing permission).
/// </summary>
[Collection("Integration")]
public sealed class CreateOrderTests(ReferenceAppFactory factory) : IAsyncLifetime
{
    public async ValueTask InitializeAsync() => await factory.ResetDatabaseAsync();
    public ValueTask DisposeAsync() => ValueTask.CompletedTask;

    [Fact]
    public async Task CreateOrder_WithValidRequest_Returns201AndId()
    {
        // Arrange
        var client = await factory.CreateAuthenticatedClientAsync(
            TestDataSeeder.AdminEmail, TestDataSeeder.TestPassword);

        var request = new CreateOrderRequest(
        [
            new OrderItemDto(Guid.NewGuid(), Quantity: 2, UnitPrice: 50m),
        ]);

        // Act
        var response = await client.PostAsJsonAsync("/api/orders", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Created);
        var id = await response.Content.ReadFromJsonAsync<Guid>();
        id.Should().NotBeEmpty();
        response.Headers.Location.Should().NotBeNull();
    }

    [Fact]
    public async Task CreateOrder_WithEmptyItems_Returns400()
    {
        // Arrange — validator: "An order must contain at least one item."
        var client = await factory.CreateAuthenticatedClientAsync(
            TestDataSeeder.AdminEmail, TestDataSeeder.TestPassword);

        var request = new CreateOrderRequest(Items: []);

        // Act
        var response = await client.PostAsJsonAsync("/api/orders", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task CreateOrder_WithoutAuth_Returns401()
    {
        // Arrange — anonymous client, no Bearer token.
        var client = factory.CreateClient();

        var request = new CreateOrderRequest(
        [
            new OrderItemDto(Guid.NewGuid(), Quantity: 1, UnitPrice: 10m),
        ]);

        // Act
        var response = await client.PostAsJsonAsync("/api/orders", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task CreateOrder_WithoutPermission_Returns403()
    {
        // Arrange — user exists but has no Orders.Create permission.
        var client = await factory.CreateAuthenticatedClientAsync(
            TestDataSeeder.NoPermEmail, TestDataSeeder.TestPassword);

        var request = new CreateOrderRequest(
        [
            new OrderItemDto(Guid.NewGuid(), Quantity: 1, UnitPrice: 10m),
        ]);

        // Act
        var response = await client.PostAsJsonAsync("/api/orders", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }
}
