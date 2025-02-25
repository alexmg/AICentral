﻿using AICentral.Core;

namespace AICentral;

public class HostNameMatchRouter
{
    public const string EmbeddingsRoute = "openai/deployments/{deploymentName}/embeddings";
    public const string ChatCompletionsRoute = "openai/deployments/{deploymentName}/chat/completions";
    public const string CompletionsRoute = "openai/deployments/{deploymentName}/completions";

    private readonly string[] _hostNames;

    public HostNameMatchRouter(string hostName)
    {
        _hostNames = hostName == "*" ? [] : [hostName];
    }

    public object WriteDebug()
    {
        return new { Host = _hostNames };
    }

    public IEnumerable<RouteHandlerBuilder> BuildRoutes(
        WebApplication application,
        AIHandler handler,
        IRouteProxy[] routeProxies)
    {
        yield return application.MapMethods(
                "/openai/images/generations:submit",
                new[] { "Post" },
                async (HttpContext ctx, CancellationToken cancellationToken) =>
                    (await handler(WrapContext(ctx), null, null, AICallType.DALLE2, cancellationToken)).ResultHandler)
            .RequireHost(_hostNames);

        yield return application.MapMethods(
                "/openai/operations/{*:rest}",
                new[] { "Get" },
                async (HttpContext ctx, CancellationToken cancellationToken) =>
                    (await handler(WrapContext(ctx), null, null, AICallType.Operations, cancellationToken)).ResultHandler)
            .RequireHost(_hostNames);

        yield return application.MapMethods(
                "/openai/deployments/{deploymentName}/images/generations",
                new[] { "Post" },
                async (HttpContext ctx, CancellationToken cancellationToken, string deploymentName) =>
                    (await handler(WrapContext(ctx), deploymentName, null, AICallType.DALLE3, cancellationToken)).ResultHandler)
            .RequireHost(_hostNames);

        yield return application.MapMethods(
                "/openai/deployments/{deploymentName}/audio/transcriptions",
                new[] { "Post" },
                async (HttpContext ctx, CancellationToken cancellationToken, string deploymentName) =>
                    (await handler(WrapContext(ctx), deploymentName, null, AICallType.Transcription, cancellationToken)).ResultHandler)
            .RequireHost(_hostNames);

        yield return application.MapMethods(
                "/openai/deployments/{deploymentName}/audio/translations",
                new[] { "Post" },
                async (HttpContext ctx, CancellationToken cancellationToken, string deploymentName) =>
                    (await handler(WrapContext(ctx), deploymentName, null, AICallType.Translation, cancellationToken)).ResultHandler)
            .RequireHost(_hostNames);

        //support the Inference API
        yield return application.MapMethods(
                "/models/chat/completions",
                new[] { "Post" },
                async (HttpContext ctx, CancellationToken cancellationToken) =>
                    (await handler(WrapContext(ctx), null, null, AICallType.Chat, cancellationToken)).ResultHandler)
            .RequireHost(_hostNames);
        
        yield return application.MapMethods(
                "/models/embeddings",
                new[] { "Post" },
                async (HttpContext ctx, CancellationToken cancellationToken) =>
                    (await handler(WrapContext(ctx), null, null, AICallType.Embeddings, cancellationToken)).ResultHandler)
            .RequireHost(_hostNames);

        yield return application.MapMethods(
                ChatCompletionsRoute,
                new[] { "Post" },
                async (HttpContext ctx, CancellationToken cancellationToken, string deploymentName) =>
                    (await handler(WrapContext(ctx), deploymentName, null, AICallType.Chat, cancellationToken)).ResultHandler)
            .RequireHost(_hostNames);

        yield return application.MapMethods(
                    EmbeddingsRoute,
                    new[] { "Post" },
                    async (HttpContext ctx, CancellationToken cancellationToken, string deploymentName) =>
                        (await handler(WrapContext(ctx), deploymentName, null, AICallType.Embeddings, cancellationToken)).ResultHandler)
            .RequireHost(_hostNames);

        yield return application.MapMethods(
                CompletionsRoute,
                new[] { "Post" },
                async (HttpContext ctx, CancellationToken cancellationToken, string deploymentName) =>
                    (await handler(WrapContext(ctx), deploymentName, null, AICallType.Completions, cancellationToken)).ResultHandler)
            .RequireHost(_hostNames);

        yield return application.MapMethods(
                "/openai/threads/{*:rest}",
                new[] { "Get", "Delete", "Post" },
                async (HttpContext ctx, CancellationToken cancellationToken) =>
                    (await handler(WrapContext(ctx), null, null, AICallType.Threads, cancellationToken)).ResultHandler)
            .RequireHost(_hostNames);

        yield return application.MapMethods(
                "/openai/assistants/{assistantId}/{*:rest}",
                new[] { "Get", "Delete", "Post" },
                async (HttpContext ctx, CancellationToken cancellationToken, string assistantId) =>
                    (await handler(WrapContext(ctx), null, assistantId, AICallType.Assistants, cancellationToken)).ResultHandler)
            .RequireHost(_hostNames);

        yield return application.MapMethods(
                "/openai/files",
                new[] { "Post" },
                async (HttpContext ctx, CancellationToken cancellationToken) =>
                    (await handler(WrapContext(ctx), null, null, AICallType.Files, cancellationToken)).ResultHandler)
            .RequireHost(_hostNames);

        yield return application.MapMethods(
                "/openai/assistants",
                new[] { "Post" },
                async (HttpContext ctx, CancellationToken cancellationToken) =>
                    (await handler(WrapContext(ctx), null, null, AICallType.Assistants, cancellationToken)).ResultHandler)
            .RequireHost(_hostNames);

        yield return application.MapMethods(
                "/openai/{*:rest}",
                new[] { "Get", "Post", "Delete" },
                async (HttpContext ctx, CancellationToken cancellationToken) =>
                    (await handler(WrapContext(ctx), null, null, AICallType.Other, cancellationToken)).ResultHandler)
            .RequireHost(_hostNames);

        foreach (var additionalRoute in routeProxies)
        {
            yield return additionalRoute.MapRoute(application, handler)
                .RequireHost(_hostNames);
        }
    }

    private IRequestContext WrapContext(HttpContext ctx)
    {
        return new HttpContextWrapper(ctx);
    }

    public static HostNameMatchRouter WithHostHeader(string host)
    {
        return new HostNameMatchRouter(Guard.NotNullOrEmptyOrWhitespace(host, nameof(host)));
    }
}