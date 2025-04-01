using System.Collections.Concurrent;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using HoolIt.Models;

var builder = WebApplication.CreateSlimBuilder(args);

builder.Services.ConfigureHttpJsonOptions(options =>
{
    options.SerializerOptions.TypeInfoResolverChain.Insert(0, AppJsonSerializerContext.Default);
});

var app = builder.Build();
app.Urls.Clear();
app.Urls.Add("http://0.0.0.0:5030");

var subscribers = new ConcurrentDictionary<string, List<StreamWriter>>();
var cancellationSources =
    new ConcurrentDictionary<string, CancellationTokenSource>(); // To manage cancellation per feedId

app.MapGet("/", () => Results.Redirect("https://github.com/mmahdium/HoolIt"));

// HAPI!
// https://github.com/jheising/HAPI
var createApi = app.MapGroup("/dweet/for");
createApi.MapGet("/{feedId}", async (HttpContext context, string feedId) =>
{
    var queryDataDic = context.Request.Query.ToDictionary(k => k.Key, v => v.Value[0]);
    var dweet = new Dweet
    {
        Content = queryDataDic,
        Created = DateTime.UtcNow,
        Thing = feedId
    };

    try
    {
        var chunkedQueryData = JsonSerializer.Serialize(dweet, AppJsonSerializerContext.Default.Dweet);

        if (subscribers.TryGetValue(feedId, out var subscribersList))
            foreach (var writer in subscribersList)
            {
                await writer.WriteLineAsync(chunkedQueryData);
                await writer.FlushAsync();
            }
    }
    catch (Exception e)
    {
        var faultResponse = new AddDweetFailedResponse()
        {
            This = "failed",
            With = "WeMessedUp",
            Because = "IDK, we couldnt dweet it. Report it at: https://github.com/mmahdium/HoolIt/issues"
        };
        var addFailedResponse =
            JsonSerializer.Serialize(faultResponse, AppJsonSerializerContext.Default.AddDweetFailedResponse);
        context.Response.StatusCode = 500;
        context.Response.ContentType = "application/json";
        await context.Response.WriteAsync(addFailedResponse);
        await context.Response.CompleteAsync();

    }

    var addSuccessResponse = new AddDweetSucceededResponse
    {
        This = "succeeded",
        By = "dweeting",
        The = "dweet",
        With = dweet
    };
    return Results.Ok(addSuccessResponse);
});

var getLiveDataApi = app.MapGroup("/listen/for/dweets/from");
getLiveDataApi.MapGet("/{feedId}",
    async (HttpContext context, string feedId, IHostApplicationLifetime appLifetime,
        CancellationToken reqCancellationToken) =>
    {
        context.Response.Headers.ContentType = "text/event-stream";

        var writer = new StreamWriter(context.Response.Body, Encoding.UTF8);
        subscribers.GetOrAdd(feedId, _ => new List<StreamWriter>()).Add(writer);

        // How this cancellation token mess works:
        // - reqCancellationToken is the cancellation token from the client request, it is used when a client closes the request.
        // - feedCts is the cancellation token from the feedId, it is used when the app is shutting down.
        // - linkedCts is a linked token source that combines both reqCancellationToken and feedCts and gets canceled when either of them does.
        // When the app is shutting down, the feedCts token source is canceled which means everything gets canceled altogether (Even that date you have been planning for the past few months; c'mon, you are probably a computer science student with no friends who barely touches grass).

        var feedCts = cancellationSources.GetOrAdd(feedId, _ => new CancellationTokenSource());
        using var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(reqCancellationToken, feedCts.Token);
        var linkedToken = linkedCts.Token;

        appLifetime.ApplicationStopping.Register(() =>
        {
            if (cancellationSources.TryGetValue(feedId, out var existingFeedCts) &&
                !existingFeedCts.IsCancellationRequested)
            {
                // It cancels 
                existingFeedCts.Cancel();
                Console.WriteLine($"Cancellation signaled for feedId: {feedId} due to app shutdown.");
            }
        });

        try
        {
            while (!linkedToken.IsCancellationRequested) await Task.Delay(Timeout.Infinite, linkedToken);
        }
        catch (OperationCanceledException)
        {
            Console.WriteLine($"SSE connection for feedId: {feedId} canceled.");
        }
        finally
        {
            if (subscribers.TryGetValue(feedId, out var subscribersList))
            {
                subscribersList.Remove(writer);
                if (subscribersList.Count == 0)
                {
                    subscribers.TryRemove(feedId, out _);
                    cancellationSources.TryRemove(feedId, out _);
                    Console.WriteLine($"No more subscribers for feedId: {feedId}. CTS removed.");
                }

                await writer.DisposeAsync();
                Console.WriteLine("Removed subscriber from feed " + feedId);
            }
        }
    });

app.Run();

[JsonSerializable(typeof(Dweet))]
[JsonSerializable(typeof(AddDweetSucceededResponse))]
[JsonSerializable(typeof(AddDweetFailedResponse))]
internal partial class AppJsonSerializerContext : JsonSerializerContext
{
}