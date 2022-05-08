﻿using AutoMapper;
using Dapr.Client;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using System;

namespace CatalogManager.Controllers
{
    public class QuestionController : Controller
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


            [HttpPost("/question")]
            public async Task<IActionResult> PostNewQuestionToQueue()
            {
                return await PostNewOrUpdateQuestionToQueue(false);
            }


            [HttpPut("/question")]
            public async Task<IActionResult> PutNewQuestiontToQueue()
            {
                return await PostNewOrUpdateQuestionToQueue(true);
            }


            [HttpGet("/questions")]
            public async Task<IActionResult> GetAll(int? offset, int? limit)
            {
                try
                {
                    var allAccessorQuestions = await _daprClient.InvokeMethodAsync<List<Models.AccessorResults.IQuestion>>(HttpMethod.Get, "questionaccessor", $"questionaccessor/questions?offset={offset ?? 0 }&limit={limit ?? 100}");
                    var questionsModelResult = _mapper.Map<List<Models.FrontendResponses.IQuestion>>(allAccessorQuestions);
                    _logger?.LogInformation($"returned {questionsModelResult.Count} questions");
                    return new OkObjectResult(questionsModelResult);
                }
                catch (Exception ex)
                {
                    _logger.LogError($"Error while getting all questions: {ex.Message}");
                }
                return Problem(statusCode: (int)StatusCodes.Status500InternalServerError);
            }

            [HttpGet("/question/{id:Guid}")]

            public async Task<IActionResult> GetQuestionById(Guid id)
            {
                try
                {
                    var accessorQuestionType = await _daprClient.InvokeMethodAsync<Models.AccessorResults.CommonQuestion>(HttpMethod.Get, "questionaccessor", $"question/{id}");

                    if (accessorQuestionType == null)
                    {
                        _logger.LogWarning($"GetQuestionById: accessor returnes null for question: {id}");
                        return new NotFoundObjectResult(id);
                    }

                    switch (accessorQuestionType.Type)
                    {
                        case "MultipleChoiceQuestion":

                            return await GetQuestion<Models.AccessorResults.MultipleChoiceQuestion>();

                        case "OpenTextQuestion":

                            return await GetQuestion<Models.AccessorResults.OpenTextQuestion>();

                        default:

                            _logger?.LogInformation($"Question type: {accessorQuestionType.Type} is incompatible");

                            return new BadRequestObjectResult(id);
                    }


                    async Task<IActionResult> GetQuestion<TQuestion>() where TQuestion : Models.AccessorResults.IQuestion
                    {
                        var questionModel = await _daprClient.InvokeMethodAsync<TQuestion>(HttpMethod.Get, "questionaccessor", $"question/{id}");

                        var questionResult = _mapper.Map<TQuestion>(questionModel);

                        _logger?.LogInformation($"returned question id: {id}");

                        return new OkObjectResult(questionResult);
                    }

                }
                catch (Exception ex)
                {
                    _logger.LogError($"Error while getting a question: {id} Error: {ex.Message}");
                }
                return Problem(statusCode: (int)StatusCodes.Status500InternalServerError);
            }


            private async Task<IActionResult> PostNewOrUpdateQuestionToQueue(bool isUpdate)
            {
                try
                {
                    using var streamReader = new StreamReader(Request.Body);
                    var body = await streamReader.ReadToEndAsync();
                    var requestQuestionType = JsonConvert.DeserializeObject<Models.FrontendRequests.CommonQuestion>(body);

                    if (requestQuestionType == null)
                    {
                        _logger.LogWarning($"Request question type is missing");
                        return new NotFoundObjectResult(body);
                    }

                    switch (requestQuestionType.Type)
                    {
                        case "MultipleChoiceQuestion":

                            return await PostQueue<Models.FrontendRequests.MultipleChoiceQuestion>();

                        case "OpenTextQuestion":

                            return await PostQueue<Models.FrontendRequests.OpenTextQuestion>();

                        default:
                            break;
                    }

                    async Task<IActionResult> PostQueue<TQuestion>() where TQuestion : Models.FrontendRequests.IQuestion
                    {
                        var questionRequest = JsonConvert.DeserializeObject<TQuestion>(body);

                        var questionModel = _mapper.Map<TQuestion>(questionRequest);

                        _logger.LogInformation("the message is going to queue");

                        questionModel.QuestionVersion = "1.0";
                        questionModel.MessageType = isUpdate ? "UpdateQuestion" : "AddQuestion";
                        await _daprClient.InvokeBindingAsync("azurequeueoutput", "create", questionModel);

                        return Ok("Accepted");
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError("Error when {0} ending addquestion post: {1}", isUpdate ? "updating" : "creating", ex.Message);
                }
                return Problem(statusCode: (int)StatusCodes.Status500InternalServerError);
            }
        }
    }
}