using System;
using System.Globalization;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc.Testing;
using Xunit;

public class IntegrationTests : IClassFixture<WebApplicationFactory<Startup>>
{
    private readonly WebApplicationFactory<Startup> _factory;

    public IntegrationTests(WebApplicationFactory<Startup> factory)
    {
        _factory = factory;
    }

    [Xunit.Theory]
    [InlineData("/api/stats/users?date=08.10.2023%2018:09:15")]
    [InlineData("/api/stats/user?date=12.11.2023%2011:03:55&userId=2fba2529-c166-8574-2da2-eac544d82634")]
    [InlineData("/api/predictions/users?date=15.10.2024%2015:18:39&tolerance=0.4&userId=2fba2529-c166-8574-2da2-eac544d82634")]
    [InlineData("/api/predictions/user?date=15.12.2024%2015:18:39&tolerance=0.4&userId=2fba2529-c166-8574-2da2-eac544d82634")]
    public async Task Endpoints_ReturnSuccess(string url)
    {
        // Arrange
        var client = _factory.CreateClient();

        // Act
        var response = await client.GetAsync(url);

        // Assert
        response.EnsureSuccessStatusCode(); // Ensure a 2xx status code
    }
}
