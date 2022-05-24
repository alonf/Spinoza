using Microsoft.AspNetCore.Mvc;
using AutoMapper;
using CatalogManager.Helpers;
using Dapr;
using Dapr.Client;
using Microsoft.AspNetCore.Mvc;

namespace CatalogManager.Controllers;

[ApiController]
[Route("[controller]")]
public class IntergationEventsTagController : ControllerBase
{

        private readonly ILogger<IntergationEventsTagController> _logger;
        private readonly DaprClient _daprClient;
        private readonly IMapper _mapper;

        public IntergationEventsTagController(ILogger<IntergationEventsTagController> logger, DaprClient daprClient, IMapper mapper)
        {

            _logger = logger;
            _daprClient = daprClient;
            _mapper = mapper;
        }

        [Topic("pubsub", "tag-topic")]
        public async Task<IActionResult> OnTagCreated([FromBody] Models.AccessorResults.TagChangeResult accessorTagChangeResult)
        {
            //System.Diagnostics.Debugger.Launch();
            try
            {
                var frontendTagChangeResult = _mapper.Map<Models.FrontendResponses.TagChangeResult>(accessorTagChangeResult);
                _logger?.LogInformation($"OnTagCreated : Message received: {frontendTagChangeResult.Id}");
                await PublishTagMessageToSignalRAsync(frontendTagChangeResult);

                return Ok();
            }
            catch (Exception ex)
            {
                _logger.LogError($"OnTagCreated: Error Pubsub receiver: {ex.Message}");
                if (ex.InnerException != null)
                    _logger.LogError($"OnTagCreated: Error Pubsub reciver, inner exception: {ex.InnerException.Message}");
        }
            return Problem(statusCode: (int)StatusCodes.Status500InternalServerError);
        }


        private async Task<IActionResult> PublishTagMessageToSignalRAsync(Models.FrontendResponses.TagChangeResult frontendTagChangeResult)
        {
            Data data = new();
            Argument argument = new Argument();
            argument.Sender = "dapr";
            argument.Text = frontendTagChangeResult;
            data.Arguments = new Argument[] { argument };
            //Dictionary<string, string> newmetadata = new Dictionary<string, string>() { { "hub", "spinozahub" } };
            //var metadata = new Dictionary<string, string>() { { "spinozaHub", "Test" } };
            await _daprClient.InvokeBindingAsync("azuresignalroutput", "create", data);
            return Ok();
        }
    }
