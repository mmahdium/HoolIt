using System.Collections.Concurrent;
using System.Text;
using System.Text.Json.Serialization;

var builder = WebApplication.CreateSlimBuilder(args);

builder.Services.ConfigureHttpJsonOptions(options =>
{
    options.SerializerOptions.TypeInfoResolverChain.Insert(0, AppJsonSerializerContext.Default);
});

var app = builder.Build();

var subscribers = new ConcurrentDictionary<string, List<StreamWriter>>();

// HAPI!
// https://github.com/jheising/HAPI
var createApi = app.MapGroup("/create/with");
createApi.MapGet("/{feedId}", async (HttpContext context,string feedId) =>
{
    var rawQueryData = context.Request.QueryString.ToString();
    var queryDataDic = context.Request.Query.ToDictionary(k => k.Key, v => v.Value);
    foreach (var a in queryDataDic)
    {
        Console.WriteLine($"""{a.Key}: {a.Value}""");
    }
    
    if (subscribers.TryGetValue(feedId, out var subscribersList))
    {
        foreach (var writer in subscribersList)
        {
            await writer.WriteLineAsync(rawQueryData);
            await writer.FlushAsync();
        }
    }
});

var getLiveDataApi = app.MapGroup("/listen/for/data");
getLiveDataApi.MapGet("/{feedId}", async (CancellationToken cancellationToken,HttpContext context, string feedId) =>
{
    context.Response.Headers.ContentType = "text/event-stream";

    var writer = new StreamWriter(context.Response.Body, Encoding.UTF8);
    subscribers.GetOrAdd(feedId, _ => new List<StreamWriter>()).Add(writer);

    try
    {
        while (!cancellationToken.IsCancellationRequested)
        {
            await Task.Delay(Timeout.Infinite, cancellationToken);
        }
    }
    catch (OperationCanceledException)
    {
    }
    finally
    {
        if (subscribers.TryGetValue(feedId, out var subscribersList))
        {
            subscribersList.Remove(writer);
            if (subscribersList.Count == 0)
            {
                subscribers.TryRemove(feedId, out _);
            }

            await writer.DisposeAsync();
            Console.WriteLine("Removed subscriber from feed " + feedId);
        }
    }
});
app.Run();



[JsonSerializable(typeof(string))]
[JsonSerializable(typeof(Int32))]
internal partial class AppJsonSerializerContext : JsonSerializerContext
{
}