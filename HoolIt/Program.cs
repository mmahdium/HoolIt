using System.Buffers;
using System.Collections.Concurrent;
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

var topicSubscribers = new ConcurrentDictionary<string, ConcurrentDictionary<Guid, ChannelWriter<byte[]>>>();

app.MapGet("/", () => Results.Redirect("https://github.com/mmahdium/HoolIt"));

var createApi = app.MapGroup("/dweet/for");
createApi.MapGet("/{feedId}", (HttpContext context, string feedId) =>
{
    var dweet = new Dweet
    {
        Content = context.Request.Query.ToDictionary(k => k.Key, v => v.Value[0])!,
        Created = DateTime.UtcNow,
        Thing = feedId
    };

    var utf8Bytes = JsonSerializer.SerializeToUtf8Bytes(dweet, AppJsonSerializerContext.Default.Dweet);

    if (topicSubscribers.TryGetValue(feedId, out var subscribers))
    {
        var writers = subscribers.Values.ToArray();

        foreach (var w in writers) w.TryWrite(utf8Bytes);
    }

    return Results.Ok(new AddDweetSucceededResponse
    {
        This = "succeeded",
        By = "dweeting",
        The = "dweet",
        With = dweet
    });
});

// Subscribe endpoint
var getLiveDataApi = app.MapGroup("/listen/for/dweets/from");
getLiveDataApi.MapGet("/{feedId}", async (HttpContext context, string feedId, CancellationToken reqCancellationToken) =>
{
    context.Response.StatusCode = 200;
    context.Response.Headers.ContentType = "text/plain; charset=utf-8";
    context.Response.Headers.CacheControl = "no-cache";
    context.Response.Headers["X-Content-Type-Options"] = "nosniff";

    context.Features.Get<Microsoft.AspNetCore.Http.Features.IHttpResponseBodyFeature>()?
        .DisableBuffering();

    var channel = Channel.CreateBounded<byte[]>(new BoundedChannelOptions(512) // tune buffer count
    {
        SingleReader = true,
        SingleWriter = true,
        FullMode = BoundedChannelFullMode.DropOldest
    });

    var writer = channel.Writer;
    
    var subscribers = topicSubscribers.GetOrAdd(feedId, _ => new ConcurrentDictionary<Guid, ChannelWriter<byte[]>>());
    var subscriberId = Guid.CreateVersion7(DateTimeOffset.UtcNow);
    subscribers.TryAdd(subscriberId, writer);

    try
    {
        var reader = channel.Reader;
        
        var bodyWriter = context.Response.BodyWriter;

        while (await reader.WaitToReadAsync(reqCancellationToken))
        while (reader.TryRead(out var msgBytes))
        {
            var memory = bodyWriter.GetMemory(msgBytes.Length + 1); // +1 for newline
            msgBytes.CopyTo(memory);
            memory.Span[msgBytes.Length] = (byte)'\n';
            bodyWriter.Advance(msgBytes.Length + 1);
            
            await bodyWriter.FlushAsync(reqCancellationToken);
        }
    }
    catch (OperationCanceledException)
    {
        // client disconnected
    }
    finally
    {
        if (topicSubscribers.TryGetValue(feedId, out var existing))
        {
            existing.TryRemove(subscriberId, out var removedWriter);
            removedWriter?.TryComplete();

            if (existing.IsEmpty) topicSubscribers.TryRemove(feedId, out _);
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