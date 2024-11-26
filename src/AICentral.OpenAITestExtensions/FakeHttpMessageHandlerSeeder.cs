using System.Collections.Concurrent;
using System.Net;

namespace OpenAIMock;

public class FakeHttpMessageHandlerSeeder
{
    private ConcurrentDictionary<string, Func<HttpRequestMessage, Task<HttpResponseMessage>>> SeededResponses { get; } = new();
    public List<(HttpRequestMessage, byte[])> IncomingRequests { get; } = new();

    public async Task<HttpResponseMessage?> TryGet(HttpRequestMessage request)
    {
        var key = request.RequestUri!.AbsoluteUri;
        if (SeededResponses.TryGetValue(key, out var responseFunction))
        {
            var response = await responseFunction(request);
            if (response.IsSuccessStatusCode || response.StatusCode == HttpStatusCode.BadRequest)
            {
                IncomingRequests.Add((request, request.Content?.ReadAsByteArrayAsync().Result ?? Array.Empty<byte>()));
            }
            return response;
        }
        return null;
    }

    public void Seed(string url, Func<HttpRequestMessage, Task<HttpResponseMessage>> response)
    {
        if (SeededResponses.ContainsKey(url)) SeededResponses.Remove(url, out _);
        SeededResponses.TryAdd(url, response);
    }

    public void SeedChatCompletions(string endpoint, string modelName, Func<Task<HttpResponseMessage>> response,
        string apiVersion = OpenAITestEx.OpenAIClientApiVersion)
    {
        var url = $"https://{endpoint}/openai/deployments/{modelName}/chat/completions?api-version={apiVersion}";
        if (SeededResponses.ContainsKey(url)) SeededResponses.Remove(url, out _);
        SeededResponses.TryAdd(url, _ => response());
    }

    public void SeedInferenceChatCompletions(string endpoint, Func<Task<HttpResponseMessage>> response,
        string apiVersion = OpenAITestEx.OpenAIClientApiVersion)
    {
        var url = $"https://{endpoint}/models/chat/completions?api-version={apiVersion}";
        if (SeededResponses.ContainsKey(url)) SeededResponses.Remove(url, out _);
        SeededResponses.TryAdd(url, _ => response());
    }

    public void SeedCompletions(string endpoint, string modelName, Func<Task<HttpResponseMessage>> response,
        string apiVersion = OpenAITestEx.OpenAIClientApiVersion)
    {
        var url = $"https://{endpoint}/openai/deployments/{modelName}/completions?api-version={apiVersion}";
        if (SeededResponses.ContainsKey(url)) SeededResponses.Remove(url, out _);
        SeededResponses.TryAdd(url, _ => response());
    }

    public void Clear()
    {
        SeededResponses.Clear();
        IncomingRequests.Clear();
    }
}