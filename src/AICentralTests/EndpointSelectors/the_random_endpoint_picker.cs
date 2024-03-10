using System.Net;
using System.Text;
using AICentralTests.TestHelpers;
using AICentralWeb;
using Newtonsoft.Json;
using Shouldly;
using Xunit.Abstractions;

namespace AICentralTests.EndpointSelectors;

public class the_random_endpoint_picker : IClassFixture<TestWebApplicationFactory<Program>>

{
    private readonly TestWebApplicationFactory<Program> _factory;
    private readonly HttpClient _httpClient;

    public the_random_endpoint_picker(TestWebApplicationFactory<Program> factory, ITestOutputHelper testOutputHelper)
    {
        _factory = factory;
        factory.OutputHelper = testOutputHelper;
        _httpClient = factory.CreateClient();
    }

    [Fact]
    public async Task works_with_a_single_endpoint()
    {
        _factory.SeedChatCompletions(AICentralFakeResponses.Endpoint200, "random",
            () => Task.FromResult(AICentralFakeResponses.FakeChatCompletionsResponse()));
        _factory.SeedChatCompletions(AICentralFakeResponses.Endpoint200Number2, "random",
            () => Task.FromResult(AICentralFakeResponses.FakeChatCompletionsResponse()));

        var result = await _httpClient.PostAsync("http://azure-to-azure-openai.localtest.me/openai/deployments/random/chat/completions?api-version=2024-02-15-preview",
            new StringContent(JsonConvert.SerializeObject(new
            {
                messages = new[]
                {
                    new { role = "system", content = "You are a helpful assistant." },
                    new { role = "user", content = "Does Azure OpenAI support customer managed keys?" },
                    new { role = "assistant", content = "Yes, customer managed keys are supported by Azure OpenAI." },
                    new { role = "user", content = "Do other Azure AI services support this too?" }
                },
                max_tokens = 5
            }), Encoding.UTF8, "application/json"));
        
        result.StatusCode.ShouldBe(HttpStatusCode.OK);
    }
}