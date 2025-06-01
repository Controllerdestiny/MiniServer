using Microsoft.Extensions.Configuration;

namespace MiniServer.Interface;

public interface IHttpRequestHandler
{
    public string Path { get; }
    public string Method { get; }

    public Task InvokeAsync(HttpRequestArgs args, CancellationToken cancellationToken);

}
