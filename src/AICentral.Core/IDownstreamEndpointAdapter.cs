﻿namespace AICentral.Core;

/// <summary>
/// Represents a downstream AI service that can have requests routed to it.
/// </summary>
public interface IDownstreamEndpointAdapter
{
    string Id { get; }
    Uri BaseUrl { get; }
    string EndpointName { get; }

    /// <summary>
    /// Given an incoming call, build a request to send to the AI service. If you cannot handle it return an IResult.
    /// </summary>
    /// <param name="incomingCall"></param>
    /// <param name="context"></param>
    /// <returns></returns>
    Task<Either<HttpRequestMessage, IResult>> BuildRequest(IncomingCallDetails incomingCall, IRequestContext context);

    /// <summary>
    /// Dispatch the HttpRequestMessage to the downstream AI service.
    /// Layer in retries / circuit breakers, etc into these calls as per your AI Service requirements.
    /// </summary>
    /// <param name="context"></param>
    /// <param name="requestMessage"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    Task<HttpResponseMessage> DispatchRequest(IRequestContext context, HttpRequestMessage requestMessage, CancellationToken cancellationToken);

    /// <summary>
    /// PreProcess the response from the AI service. This is where you can do things like sanitise headers, or extract remaining tokens and requests.
    /// </summary>
    /// <param name="callInformationIncomingCallDetails"></param>
    /// <param name="context"></param>
    /// <param name="openAiResponse"></param>
    /// <returns></returns>
    Task<ResponseMetadata> ExtractResponseMetadata(
        IncomingCallDetails callInformationIncomingCallDetails,
        IRequestContext context,
        HttpResponseMessage openAiResponse);
}