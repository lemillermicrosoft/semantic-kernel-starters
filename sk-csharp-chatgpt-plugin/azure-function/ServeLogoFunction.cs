using System.Net;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Azure.WebJobs.Extensions.OpenApi.Core.Attributes;
using Microsoft.Extensions.Logging;
using Microsoft.OpenApi.Models;
using Models;

public static class ServeLogoFunction
{
    // [Function("ServeLogo")]
    // public static IActionResult Run(
    //     [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "logo.png")] HttpRequestData req)
    // {
    //     var filePath = Path.Combine(Environment.CurrentDirectory, "logo.png");
    //     if (!File.Exists(filePath))
    //     {
    //         return new NotFoundResult();
    //     }

    //     // var fileBytes = File.ReadAllBytes(filePath);
    //     // return new FileContentResult(fileBytes, "image/png");

    //     var fileStream = new FileStream(filePath, FileMode.Open, FileAccess.Read);
    //     return new FileStreamResult(fileStream, "image/png");
    // }

    [Function("ServeLogo")]
    public static async Task<HttpResponseData> Run(
        [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "logo.png")] HttpRequestData req,
        FunctionContext executionContext)
    {
        var logger = executionContext.GetLogger("ServeLogo");
        logger.LogInformation("ServeLogo function processed a request.");

        var filePath = Path.Combine(Environment.CurrentDirectory, "logo.png");
        if (!File.Exists(filePath))
        {
            var notFoundResponse = req.CreateResponse(HttpStatusCode.NotFound);
            return notFoundResponse;
        }

        var fileBytes = File.ReadAllBytes(filePath);
        var response = req.CreateResponse(HttpStatusCode.OK);
        response.Headers.Add("Content-Type", "image/png");
        await response.Body.WriteAsync(fileBytes, 0, fileBytes.Length);

        return response;
    }
}
