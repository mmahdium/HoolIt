using System.Collections.Concurrent;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Channels;
using HoolIt.Models;

var builder = WebApplication.CreateSlimBuilder(args);

builder.Services.ConfigureHttpJsonOptions(options =>
{
    options.SerializerOptions.TypeInfoResolverChain.Insert(0, AppJsonSerializerContext.Default);
});

var app = builder.Build();
app.Urls.Clear();
app.Urls.Add("http://0.0.0.0:5030");

var subscribers = new ConcurrentDictionary<string, List<ChannelWriter<string>>>();
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
        var jsonQueryData = JsonSerializer.Serialize(dweet, AppJsonSerializerContext.Default.Dweet);

        if (subscribers.TryGetValue(feedId, out var subscribersList))
            foreach (var writer in subscribersList)
                await writer.WriteAsync(jsonQueryData);
    }
    catch (Exception e)
    {
        var faultResponse = new AddDweetFailedResponse
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
        context.Response.StatusCode = 200;
        context.Response.Headers.ContentType = "text/plain";
        context.Response.Headers.CacheControl = "no-cache";
        context.Response.Headers["X-Content-Type-Options"] = "nosniff";

        // Disable response buffering
        context.Features.Get<Microsoft.AspNetCore.Http.Features.IHttpResponseBodyFeature>()?
            .DisableBuffering();

        var channel = Channel.CreateUnbounded<string>();
        var writer = channel.Writer;

        subscribers.GetOrAdd(feedId, _ => new List<ChannelWriter<string>>()).Add(writer);
        Console.WriteLine($"Added subscriber to feed {feedId}");

        try
        {
            var reader = channel.Reader;

            while (!reqCancellationToken.IsCancellationRequested &&
                   await reader.WaitToReadAsync(reqCancellationToken))
            while (reader.TryRead(out var msg))
            {
                await context.Response.WriteAsync(msg + "\n", reqCancellationToken);
                await context.Response.Body.FlushAsync(reqCancellationToken);
            }
        }
        catch (OperationCanceledException)
        {
            Console.WriteLine($"Cancellation requested for feed {feedId}");
        }
        finally
        {
            if (subscribers.TryGetValue(feedId, out var list))
            {
                list.Remove(writer);
                Console.WriteLine($"Removed subscriber from feed {feedId}");
                if (list.Count == 0) subscribers.TryRemove(feedId, out _);
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