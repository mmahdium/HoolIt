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

var subscribers = new ConcurrentDictionary<string, ConcurrentDictionary<ChannelWriter<string>, byte>>();
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

        if (subscribers.TryGetValue(feedId, out var feedSubs))
            foreach (var writer in feedSubs.Keys)
                await writer.WriteAsync(jsonQueryData);


        return Results.Ok(new AddDweetSucceededResponse
            { This = "succeeded", By = "dweeting", The = "dweet", With = dweet });
    }
    catch (Exception e)
    {
        return Results.Json(
            new AddDweetFailedResponse
            {
                This = "failed", With = "WeMessedUp",
                Because = "IDK, we couldnt dweet it. Report it at: https://github.com/mmahdium/HoolIt/issues"
            }, statusCode: 500);
    }
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

        var feedSubs = subscribers.GetOrAdd(feedId, _ => new ConcurrentDictionary<ChannelWriter<string>, byte>());
        feedSubs.TryAdd(writer, 0);

        //Console.WriteLine($"Added subscriber to feed {feedId}");

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
            //Console.WriteLine($"Cancellation requested for feed {feedId}");
        }
        finally
        {
            if (subscribers.TryGetValue(feedId, out var list))
            {
                feedSubs.TryRemove(writer, out _);
                //Console.WriteLine($"Removed subscriber from feed {feedId}");
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