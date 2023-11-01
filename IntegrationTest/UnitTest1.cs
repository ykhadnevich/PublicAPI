using System;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using Xunit;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
 // Replace with the correct namespace

public class IntegrationTests : IClassFixture<WebApplicationFactory<Startup>>
{
    private readonly WebApplicationFactory<Startup> _factory;

    public IntegrationTests(WebApplicationFactory<Startup> factory)
    {
        _factory = factory;
    }

    [Xunit.Theory]
    [InlineData("/", "Hello World!")]
    [InlineData("/api/stats/users?date=15.10.2023%2015:18:39", "Total online users: 50")]
    // Add more test cases for other endpoints
    public async Task Get_EndpointsReturnSuccessAndExpectedContent(string url, string expectedContent)
    {
        // Arrange
        var client = _factory.CreateClient();

        // Act
        var response = await client.GetAsync(url);

        // Assert
        response.EnsureSuccessStatusCode(); // Status code 200-299

        var responseContent = await response.Content.ReadAsStringAsync();
        Xunit.Assert.Equal(expectedContent, responseContent);
    }

    [Xunit.Theory]
    [InlineData("/api/stats/users?date=invalid-date", HttpStatusCode.NotFound)]
    // Add more test cases for error scenarios
    public async Task Get_EndpointsReturnErrorStatus(string url, HttpStatusCode expectedStatusCode)
    {
        // Arrange
        var client = _factory.CreateClient();

        // Act
        var response = await client.GetAsync(url);

        // Assert
        Xunit.Assert.Equal(expectedStatusCode, response.StatusCode);
    }
}
