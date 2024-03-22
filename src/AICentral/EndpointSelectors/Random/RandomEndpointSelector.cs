﻿using AICentral.Core;
using Microsoft.Extensions.Primitives;

namespace AICentral.EndpointSelectors.Random;

public class RandomEndpointSelector : IEndpointSelector
{
    private readonly System.Random _rnd = new(Environment.TickCount);
    private readonly IEndpointDispatcher[] _openAiServers;

    public RandomEndpointSelector(IEndpointDispatcher[] openAiServers)
    {
        _openAiServers = openAiServers;
    }

    public async Task<AICentralResponse> Handle(HttpContext context,
        IncomingCallDetails aiCallInformation,
        bool isLastChance,
        IResponseGenerator responseGenerator,
        CancellationToken cancellationToken)
    {
        var logger = context.RequestServices.GetRequiredService<ILogger<RandomEndpointSelector>>();
        var toTry = _openAiServers.ToList();
        logger.LogDebug("Random Endpoint selector is handling request");
        do
        {
            var chosen = toTry.ElementAt(_rnd.Next(0, toTry.Count));
            toTry.Remove(chosen);
            try
            {
                return await chosen.Handle(
                    context,
                    aiCallInformation,
                    isLastChance && !toTry.Any(),
                    responseGenerator,
                    cancellationToken); //awaiting to unwrap any Aggregate Exceptions
            }
            catch (HttpRequestException e)
            {
                if (!toTry.Any())
                {
                    logger.LogError(e, "Failed to handle request. Exhausted endpoints");
                    throw;
                }

                logger.LogWarning(e, "Failed to handle request. Trying another endpoint");
            }
        } while (toTry.Count > 0);

        throw new InvalidOperationException("Failed to satisfy request");
    }

    public IEnumerable<IEndpointDispatcher> ContainedEndpoints()
    {
        return _openAiServers;
    }
    
    public Task BuildResponseHeaders(HttpContext context, HttpResponseMessage rawResponse, Dictionary<string, StringValues> rawHeaders)
    {
        rawHeaders.Remove("x-ratelimit-remaining-tokens");
        rawHeaders.Remove("x-ratelimit-remaining-requests");
        return Task.CompletedTask;
    }

}