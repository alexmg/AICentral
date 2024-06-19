﻿using AICentralOpenAIMock;
using AICentralTests.TestHelpers;
using AICentralWeb;
using Azure;
using Azure.AI.OpenAI.Assistants;
using Azure.Core.Pipeline;
using OpenAIMock;
using Xunit.Abstractions;

namespace AICentralTests.Assistants;

public class open_ai_assistants : IClassFixture<TestWebApplicationFactory<Program>>, IDisposable
{
    private readonly TestWebApplicationFactory<Program> _factory;
    private readonly ITestOutputHelper _testOutputHelper;
    private readonly HttpClient _httpClient;

    public open_ai_assistants(TestWebApplicationFactory<Program> factory, ITestOutputHelper testOutputHelper)
    {
        _factory = factory;
        _testOutputHelper = testOutputHelper;
        factory.OutputHelper = testOutputHelper;
        _httpClient = factory.CreateClient();
    }

    [Fact]
    public async Task can_be_mapped_to_allow_load_balancing()
    {
        _factory.Services.Seed(
            $"https://{TestPipelines.Endpoint200}/openai/assistants/ass-assistant-123-out?api-version=2024-02-15-preview",
            () => Task.FromResult(OpenAIFakeResponses.FakeAzureOpenAIAssistantResponse("ass-assistant-123-out")));

        _factory.Services.Seed(
            $"https://{TestPipelines.Endpoint200Number2}/openai/assistants/ass-assistant-123-out?api-version=2024-02-15-preview",
            () => Task.FromResult(OpenAIFakeResponses.FakeAzureOpenAIAssistantResponse("ass-assistant-123-out")));

        _httpClient.DefaultRequestHeaders.Add("x-aicentral-affinity-key", Guid.NewGuid().ToString());  
        var client = new AssistantsClient(
            new Uri("http://azure-to-azure-openai-random-with-affinity.localtest.me"),
            new AzureKeyCredential("ignore-fake-key-123"),
            new AssistantsClientOptions(version: AssistantsClientOptions.ServiceVersion.V2024_02_15_Preview)
            {
                Transport = new HttpClientTransport(_httpClient)
            });

        var assistant = await client.GetAssistantAsync("assistant-in");
        
        await Verify(_factory.Services.VerifyRequestsAndResponses(assistant));
        
    }
    
    public void Dispose()
    {
        _factory.Services.ClearSeededMessages();
    }
}