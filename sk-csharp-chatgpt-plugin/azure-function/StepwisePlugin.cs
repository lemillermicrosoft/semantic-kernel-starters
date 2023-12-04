using System.ComponentModel;
using System.Net;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Azure.WebJobs.Extensions.OpenApi.Core.Attributes;
using Microsoft.Extensions.Logging;
using Microsoft.OpenApi.Models;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.Orchestration;
using Microsoft.SemanticKernel.Planning;
using Microsoft.SemanticKernel.SkillDefinition;

public class StepwisePlugin
{
    private readonly ILogger _logger;
    private readonly IKernel _kernel;
    private readonly IDictionary<string, ISKFunction> _stepwiseSkill;

    public StepwisePlugin(IKernel kernel, ILoggerFactory loggerFactory)
    {{
        this._kernel = kernel;
        this._stepwiseSkill = this._kernel.ImportSkill(new StepwiseSkill(this._kernel), "Stepwise");
        this._logger = loggerFactory.CreateLogger(this.GetType());
    }}

    [OpenApiOperation(operationId: "StepwiseRespond", tags: new[] { "ExecuteFunction" }, Description = "Adds two numbers.")]
    [OpenApiParameter(name: "message", Description = "The message to respond to", Required = true, In = ParameterLocation.Query)]
    [OpenApiResponseWithBody(statusCode: HttpStatusCode.OK, contentType: "text/plain", bodyType: typeof(string), Description = "Returns a response to the message.")]
    [OpenApiResponseWithBody(statusCode: HttpStatusCode.BadRequest, contentType: "application/json", bodyType: typeof(string), Description = "Returns the error of the input.")]
    [Function("StepwiseRespond")]
    public async Task<HttpResponseData> RunAsync([HttpTrigger(AuthorizationLevel.Anonymous, "get", "post")] HttpRequestData req)
    {
            var message = req.Query["message"]?.ToString();

            if (!string.IsNullOrWhiteSpace(message))
            {
                HttpResponseData response = req.CreateResponse(HttpStatusCode.OK);
                response.Headers.Add("Content-Type", "text/plain");
                var contextVariables = new ContextVariables();

                contextVariables.Set("message", message);
                contextVariables.Set("history", $"Human: {message}");

                var result = await this._kernel.RunAsync(contextVariables, this._stepwiseSkill["RespondTo"]).ConfigureAwait(false);
                response.WriteString(result.ToString());

                this._logger.LogInformation($"Responded to message {message}: {result}");

                return response;
            }
            else
            {
                HttpResponseData response = req.CreateResponse(HttpStatusCode.BadRequest);
                response.Headers.Add("Content-Type", "application/json");
                response.WriteString("Please pass a message on the query string or in the request body");

                return response;
            }
    }
}
    public class StepwiseSkill
    {
        private readonly IKernel _kernel;
        public StepwiseSkill(IKernel kernel)
        {
            this._kernel = kernel;
        }

        [SKFunction, Description("Respond to a message.")]
        public async Task<SKContext> RespondTo(string message, string history, SKContext context, CancellationToken cancellationToken = default)
        {
            var planner = new StepwisePlanner(this._kernel);

            // Option 1 - Respond to just the message
            // var plan = planner.CreatePlan(message);
            // var messageResult =  await plan.InvokeAsync();

            // Option 2 - Respond to the history
            // var plan = planner.CreatePlan(history);
            // var result = await plan.InvokeAsync();

            // Option 3 - Respond to the history with prompt
            var plan2 = planner.CreatePlan($"{history}\n---\nGiven the conversation history, respond to the most recent message.");
            var result = await this._kernel.RunAsync(plan2, cancellationToken: cancellationToken);

            return result;
        }
    }
