using System.Collections.Generic;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.DurableTask;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Azure.WebJobs.Host;
using Microsoft.Extensions.Logging;

namespace Example
{
    public static class Function1
    {
        [FunctionName("Function1")]
        public static async Task<List<string>> RunOrchestrator(
            [OrchestrationTrigger] IDurableOrchestrationContext context,
            ILogger log)
        {
            var input=context.GetInput<DataInput>();
            log.LogInformation($"------------------------------------------------------------------------");
            log.LogInformation($"Input Durable {input.Data}.");
            log.LogInformation($"------------------------------------------------------------------------");

            var outputs = new List<string>();

            // Replace "hello" with the name of your Durable Activity Function.
            outputs.Add(await context.CallActivityAsync<string>(nameof(SayHello), $"Tokyo {input.Data}"));
            outputs.Add(await context.CallActivityAsync<string>(nameof(SayHello), $"Seattle {input.Data}"));
            outputs.Add(await context.CallActivityAsync<string>(nameof(SayHello), $"London {input.Data}"));
            log.LogInformation($"------------------------------------------------------------------------");

            // returns ["Hello Tokyo!", "Hello Seattle!", "Hello London!"]
            return outputs;
        }

        [FunctionName(nameof(SayHello))]
        public static string SayHello([ActivityTrigger] string name, ILogger log)
        {
            log.LogInformation($"Saying hello to {name}.");
            int milliseconds = 10000;
            Thread.Sleep(milliseconds);
            return $"Hello {name}!";
        }

        [FunctionName("Function1_HttpStart")]
        public static async Task<HttpResponseMessage> HttpStart(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", "post")] HttpRequestMessage req,
            [DurableClient] IDurableOrchestrationClient starter,
            ILogger log)
        {
            var dataInput = new DataInput("httpStart");
            // Function input comes from the request content.
            string instanceId = await starter.StartNewAsync("Function1", dataInput);

            log.LogInformation($"Started orchestration with ID = '{instanceId}'.");

            return starter.CreateCheckStatusResponse(req, instanceId);
        }

        [FunctionName("QueueStart")]
        public static async Task Run(
        [QueueTrigger("durable-function-trigger")] string input,
        [DurableClient] IDurableOrchestrationClient starter,
        ILogger log)
        {
            var dataInput = new DataInput(input);
            // Orchestration input comes from the queue message content.
            log.LogInformation($"------------------------------------------------------------------------");
            log.LogInformation($"Started orchestration with ID = '{input}'.");
            string instanceId =await  starter.StartNewAsync("Function1", dataInput);
            log.LogInformation($"End orchestration with ID = '{instanceId}' - {input}.");
            log.LogInformation($"------------------------------------------------------------------------");
        }

        public record DataInput(string Data);
    }
}