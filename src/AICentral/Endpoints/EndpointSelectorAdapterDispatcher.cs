﻿using AICentral.Core;

namespace AICentral.Endpoints;

public class EndpointSelectorAdapterDispatcher : IEndpointDispatcher
{
    private readonly IEndpointSelectorFactory _endpointSelectorFactory;

    public EndpointSelectorAdapterDispatcher(IEndpointSelectorFactory endpointSelectorFactory)
    {
        _endpointSelectorFactory = endpointSelectorFactory;
    }

    /// <summary>
    /// Don't worry about the response handler
    /// </summary>
    /// <param name="context"></param>
    /// <param name="callInformation"></param>
    /// <param name="isLastChance"></param>
    /// <param name="responseGenerator"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    public Task<AICentralResponse> Handle(
        IRequestContext context,
        IncomingCallDetails callInformation,
        bool isLastChance,
        IResponseGenerator responseGenerator,
        CancellationToken cancellationToken)
    {
        return _endpointSelectorFactory.Build().Handle(context, callInformation, isLastChance, responseGenerator, cancellationToken);
    }

    public bool IsAffinityRequestToMe(string affinityHeaderValue)
    {
        return false;
    }

    public IEnumerable<IEndpointDispatcher> ContainedEndpoints()
    {
        foreach (var endpoint in _endpointSelectorFactory.Build().ContainedEndpoints())
        {
            if (endpoint is EndpointSelectorAdapterDispatcher endpointSelectorAdapter)
            {
                foreach (var wrappedEndpoint in endpointSelectorAdapter.ContainedEndpoints())
                {
                    yield return wrappedEndpoint;
                }
            }
            else
            {
                yield return endpoint;
            }
        }
    }

}