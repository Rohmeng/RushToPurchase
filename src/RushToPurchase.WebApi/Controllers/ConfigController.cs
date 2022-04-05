using Microsoft.AspNetCore.Mvc;
using RushToPurchase.Application.DTO;
using Serilog;

namespace RushToPurchase.WebApi.Controllers;

[ApiController]
[Route("[controller]")]
public class ConfigController : ControllerBase
{
    private readonly IConfiguration _configuration;

    public ConfigController(IConfiguration configuration)
    {
        _configuration = configuration;
    }
    
    [HttpGet("getconfig")]
    public UserInfo GetConfig()
    {
        var userInfo1 = _configuration.GetSection("UserInfo").Get<UserInfo>();

        var commonValue = _configuration["commonkey"];
        var demoValue = _configuration["demokey"];
        Log.Information("commonkey:" + commonValue);
        Log.Information("demokey:" + demoValue);

        return userInfo1;
    }
}