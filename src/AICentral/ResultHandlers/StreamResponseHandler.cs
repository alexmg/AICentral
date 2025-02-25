using AICentral.Core;
using Microsoft.Net.Http.Headers;

namespace AICentral.ResultHandlers;

public class StreamResponseHandler: IResponseHandler
{
    public async Task<AICentralResponse> Handle(
        IRequestContext context,
        CancellationToken cancellationToken,
        HttpResponseMessage openAiResponse,
        DownstreamRequestInformation requestInformation,
        ResponseMetadata responseMetadata)
    {
        //send the headers down to the client
        context.Response.StatusCode = (int)openAiResponse.StatusCode;
        context.Response.SetHeader(HeaderNames.ContentType, openAiResponse.Content.Headers.ContentType?.ToString());

        //squirt the response as it comes in:
        await openAiResponse.Content.CopyToAsync(context.Response.Body, cancellationToken);
        await context.Response.Body.FlushAsync(cancellationToken);

        var chatRequestInformation = new DownstreamUsageInformation(
            requestInformation.LanguageUrl,
            requestInformation.InternalEndpointName,
            null,
            requestInformation.DeploymentName,
            context.GetClientForLoggingPurposes(),
            requestInformation.CallType,
            null,
            requestInformation.Prompt,
            null,
            null,
            null,
            responseMetadata,
            context.RemoteIpAddress,
            requestInformation.StartDate,
            requestInformation.Duration,
            openAiResponse.IsSuccessStatusCode,
            requestInformation.RawRequest?.ToJsonString());

        return new AICentralResponse(chatRequestInformation, new ResponseAlreadySentResultHandler());
    }
}