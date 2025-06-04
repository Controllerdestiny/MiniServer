using System.Net;
using System.Reflection;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using MiniServer.Interface;

namespace MiniServer.Network;

public class HttpServer : IHostedService
{
    private readonly HttpListener HttpListener = new();

    private readonly ILogger<HttpServer> _logger;

    private readonly List<IHttpRequestHandler> _httpRequestContexts;

    private readonly IConfiguration _configuration;

    public HttpServer(ILogger<HttpServer> logger, IConfiguration configuration)
    {
        _configuration = configuration;
        _logger = logger;
        _httpRequestContexts = Assembly.GetExecutingAssembly()
            .GetExportedTypes()
            .Where(x => x.IsClass && !x.IsAbstract && typeof(IHttpRequestHandler).IsAssignableFrom(x))
            .Select(x => (IHttpRequestHandler)Activator.CreateInstance(x)!)
            .ToList();

    }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        HttpListener.Prefixes.Add($"http://*:{_configuration["Server:Port"]}/");
        HttpListener.Start();
        _logger.LogInformation("[HttpReceive] 监听中: http://*:{Port}/", _configuration["Server:Port"]);
        try
        {
            while (true)
            {

                await ReceiveLoopAsync(await HttpListener.GetContextAsync().WaitAsync(cancellationToken), cancellationToken);
            }

        }
        catch (TaskCanceledException)
        {
            _logger.LogInformation("[HttpReceive] 监听停止: http://*:{Prot}/", _configuration["Server:Port"]);
        }
        catch (Exception ex)
        {
            _logger.LogError("[HttpReceive] 监听异常: {ErrorMessage}", ex);
        }
    }

    private async Task ReceiveLoopAsync(HttpListenerContext httpListenerContext, CancellationToken cancellationToken)
    {
        var path = httpListenerContext.Request.Url?.AbsolutePath;
        var method = httpListenerContext.Request.HttpMethod;
        var AccessToken = httpListenerContext.Request.Headers["Authorization"] ?? httpListenerContext.Request.QueryString["access_token"];
        var token = _configuration["Server:AccessToken"];
        if (!string.IsNullOrEmpty(token) && !string.IsNullOrEmpty(AccessToken) && AccessToken != token)
        {
            _logger.LogWarning("[HttpReceive] 访问令牌错误: {Path} {Method}", path, method);
            httpListenerContext.Response.StatusCode = (int)HttpStatusCode.Unauthorized;
            httpListenerContext.Response.Close();
            return;
        }
        var context = _httpRequestContexts
            .FirstOrDefault(x => x.Path == path && x.Method == method);
        if (context is null)
        {
            _logger.LogWarning("[HttpReceive] 未找到处理器: {Path} {Method}", path, method);
            httpListenerContext.Response.StatusCode = (int)HttpStatusCode.NotFound;
            httpListenerContext.Response.Close();
            return;
        }
        var args = new HttpRequestArgs(httpListenerContext, _logger);
        try
        {
            _logger.LogInformation("[HttpReceive] 处理请求: {Path} {Method}", path, method);
            await context.InvokeAsync(args, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError("[HttpReceive] 处理请求异常: {Path} {Method} {ErrorMessage}", path, method, ex);
            args.ReplyJson(new { code = 500, msg = $"请求错误: {ex.Message}" }, HttpStatusCode.InternalServerError);
        }
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        _httpRequestContexts.Clear();
        HttpListener.Stop();
        HttpListener.Close();
        _logger.LogInformation("[HttpReceive] 监听停止: http://*:{Port}/", _configuration["Server:Port"]);
        return Task.CompletedTask;
    }
}
