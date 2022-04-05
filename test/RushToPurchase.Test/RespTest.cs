using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc.Testing;
using Xunit;
using Xunit.Abstractions;

namespace RushToPurchase.Test;

public class RespTest
{
    private readonly ITestOutputHelper _testOutputHelper;
    private HttpClient _client;

    public RespTest(ITestOutputHelper testOutputHelper)
    {
        _testOutputHelper = testOutputHelper;
        var application = new MyApplication("Development");
        _client = application.CreateClient();
    }


    [Fact]
    public async Task HelloWorld()
    {
        var response = await _client.GetStringAsync("/");
        Assert.Equal("Hello World", response);
        _testOutputHelper.WriteLine(response);
    }
    
    [Fact]
    public async Task UserConfigTest()
    {
        // Act
        var response = await _client.GetAsync("/Config/getconfig");
        var result = await response.Content.ReadAsStringAsync();
        // Assert
        Assert.NotEmpty(result);
        _testOutputHelper.WriteLine(result);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task WeatherForecastTest()
    {
        var res = await _client.GetStringAsync("/WeatherForecast");
        Assert.NotNull(res);
        _testOutputHelper.WriteLine(res);
    }
}