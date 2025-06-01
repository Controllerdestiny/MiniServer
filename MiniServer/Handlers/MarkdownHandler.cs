using System.Net;
using MiniServer.Interface;
using Newtonsoft.Json;

namespace MiniServer.Handlers;

public class MarkdownHandler : IHttpRequestHandler
{
    public string Path => "/markdown";

    public string Method => HttpMethod.Post.Method;

    public async Task InvokeAsync(HttpRequestArgs args, CancellationToken cancellationToken)
    {
        using var sr = new StreamReader(args.Context.Request.InputStream, args.Context.Request.ContentEncoding);
        var requestBody = await sr.ReadToEndAsync(cancellationToken);
        var param = JsonConvert.DeserializeObject<MarkdownRequestArgs>(requestBody) ?? throw new ArgumentNullException("请求参数不能为空");
        var (buffer, _) = await Utils.Markdown(param);
        args.ReplyImage(buffer, HttpStatusCode.OK);
    }
}

public class MarkdownRequestArgs
{
    [JsonProperty("auto_width")]
    public bool AutoWidth { get; init; } = true;

    [JsonProperty("auto_height")]
    public bool AutoHeight { get; init; } = true;

    [JsonProperty("timeout")]
    public int TimeOut { get; init; } = 5000;

    [JsonProperty("enable_dark")]
    public bool Dark { get; init; } = false;

    [JsonProperty("content")]
    public string MarkdownContent { get; init; } = string.Empty;
}
