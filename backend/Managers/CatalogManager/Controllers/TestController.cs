﻿using AutoMapper;
using Dapr.Client;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using System;
using System.Text.Json.Nodes;

namespace CatalogManager.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class TestController : ControllerBase
    {
        private readonly ILogger<TestController> _logger;

        private readonly DaprClient _daprClient;
        private readonly IMapper _mapper;
        public TestController(ILogger<TestController> logger, DaprClient daprClient, IMapper mapper)
        {
            _logger = logger;
            _daprClient = daprClient;
            _mapper = mapper;
        }

        [HttpPost("/test")]
        public async Task<IActionResult> PostNewTestToQueue()
        {

            return await PostNewOrUpdateTestToQueue(false);
        }

        [HttpPut("/test")]
        public async Task<IActionResult> PutNewTestToQueue()
        {
            return await PostNewOrUpdateTestToQueue(true);
        }


        [HttpGet("/tests")]
        public async Task<IActionResult> GetAll(int? offset, int? limit, string []? tag)
        {
            try
            {
                  var tags = (tag!.Any() ? "&tags=" + string.Join(",", tag!) : string.Empty);
                  var methodName = $"testaccessor/tests?offset={offset ?? 0}&limit={limit ?? 100}{tags}";
                  _logger?.LogInformation($"calling method : {methodName}");
                var allAccessorTests = await _daprClient.InvokeMethodAsync<List<Models.AccessorResults.Test>>(HttpMethod.Get, "testaccessor", methodName);
                var frontendAllTestModelResult = _mapper.Map<List<Models.FrontendResponses.Test>>(allAccessorTests);
                // ReSharper disable once ConditionalAccessQualifierIsNonNullableAccordingToAPIContract
                _logger?.LogInformation($"returned {frontendAllTestModelResult.Count} tests");
                return new OkObjectResult(frontendAllTestModelResult);
            }
            catch (Exception ex)
            {
                _logger!.LogError($"Error while getting all tests: {ex.Message}");
            }
            return Problem(statusCode: StatusCodes.Status500InternalServerError);
        }

        [HttpGet("/testquestions/{id:Guid}")]
        public async Task<IActionResult> GetQuestionsByTestId(Guid id)
        {
            try
            {
                var allTestQuestionsIds = await _daprClient.InvokeMethodAsync<JsonArray>(HttpMethod.Get, "testaccessor", $"testquestions/{id}");

                var allTestQuestions = await _daprClient.InvokeMethodAsync<JsonArray>(HttpMethod.Get, "questionaccessor", $"testquestions?questionsids={allTestQuestionsIds}");

                return new OkObjectResult(allTestQuestions);
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error while getting all test questions: {ex.Message}");
            }
            return Problem(statusCode: (int)StatusCodes.Status500InternalServerError);
        }


        [HttpGet("/test/{id:Guid}")]

        public async Task<IActionResult> GetTest(Guid id)
        {
            try
            {
                var accessorTest = await _daprClient.InvokeMethodAsync<Models.AccessorResults.Test>(HttpMethod.Get, "testaccessor", $"test/{id}");
                if (accessorTest == null)
                {
                    _logger.LogWarning($"GetTest: accessor returns null for test: {id}");
                    return new NotFoundObjectResult(id);
                }
                var frontendTestModelResult = _mapper.Map<Models.FrontendResponses.Test>(accessorTest);
                _logger.LogInformation($"returned test id: {id}");
                return new OkObjectResult(frontendTestModelResult);
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error while getting a test: {id} Error: {ex.Message}");
            }
            return Problem(statusCode: StatusCodes.Status500InternalServerError);
        }

        private async Task<IActionResult> PostNewOrUpdateTestToQueue(bool isUpdate)
        {
            try
            {
                
                using var streamReader = new StreamReader(Request.Body);
                var body = await streamReader.ReadToEndAsync();
                var requestTestModel = JsonConvert.DeserializeObject<Models.FrontendRequests.Test>(body);

                _logger.LogInformation("the message is going to queue");
                var submitTestModel = _mapper.Map<Models.AccessorSubmits.Test>(requestTestModel);
                submitTestModel.TestVersion = "1.0";
                submitTestModel.MessageType = isUpdate ? "UpdateTest" : "CreateTest";
                await _daprClient.InvokeBindingAsync("testaccessorrequestqueue", "create", submitTestModel);
                if (submitTestModel.Tags.Length != 0)
                {
                     await _daprClient.InvokeBindingAsync("tagaccessorrequestqueue", "create", submitTestModel.Tags);
                     _logger.LogInformation($"pushing {submitTestModel.Tags.Length} tags to the tagaccessorrequestqueue");
                }
               
                return Ok("Accepted");
            }
            catch (Exception ex)
            {
                _logger.LogError("Error when {0} ending add test post: {1}", isUpdate ? "updating" : "creating", ex.Message);
            }
            return Problem(statusCode: StatusCodes.Status500InternalServerError);
        }

        [HttpGet("/tests/count")]
        public async Task<IActionResult> GetTotalTests()
        {

            try
            {
                var dbTotalTests = await _daprClient.InvokeMethodAsync<int>(HttpMethod.Get, "testaccessor", "/tests/count");

                _logger.LogInformation($"GetTotalTests: accessor returns {dbTotalTests}");

                return new OkObjectResult(dbTotalTests);
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error while getting total tests number. Error: {ex.Message}");
            }
            return Problem(statusCode: StatusCodes.Status500InternalServerError);

        }
    }
}
