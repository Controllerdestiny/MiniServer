using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using MiniServer.Network;

namespace MiniServer;

public class MiniServer
{
    private static HostApplicationBuilder _hostApplicationBuilder = Host.CreateApplicationBuilder();

    public static IHost? IHost { get; private set; } = null;

    public static void Start()
    {
        _hostApplicationBuilder.Services.AddHostedService<HttpServer>();
        IHost = _hostApplicationBuilder.Build();
        IHost.Run();
    }
}
