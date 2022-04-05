using System.Net;
using System.Threading.Tasks;
using Xunit;

namespace RushToPurchase.Test;

public partial class Swagger
{
    [Fact]
    public async Task SwaggerUI_Responds_OK_In_Development()
    {
        await using var application = new MyApplication("Development");

        var client = application.CreateClient();
        var response = await client.GetAsync("/swagger/index.html");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }
}