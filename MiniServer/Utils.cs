using System.Diagnostics;
using Markdig;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using MiniServer.Handlers;
using PuppeteerSharp;
using PuppeteerSharp.Media;
namespace MiniServer;

public class Utils
{
    private static IBrowser? browser = null;

    private static IPage? Page = null;

    private static Dictionary<string, string> ReplaceDic = new()
    {
        { "\n", "\\n" },
        { "\r\n", "\\n" },
        { "\r", "\\n" },
        { "'", "\\'" },
        { "\"", "\\\"" }
    };


    public static void Kill()
    {
        foreach (var process in Process.GetProcesses())
        {
            if (process.ProcessName.Contains("chrome"))
            {
                process.Kill();
            }
        }
    }

    public static async Task<(byte[] buffer, TimeSpan Take)> Markdown(MarkdownRequestArgs args)
    {
        Stopwatch stopwatch = new();
        stopwatch.Start();
        if (browser == null || !browser.IsConnected || browser.IsClosed || browser.Process.HasExited)
        {
            await new BrowserFetcher().DownloadAsync();
            var config = MiniServer.IHost?.Services.GetRequiredService<IConfiguration>();
            var argsList = config?.GetSection("Args").Get<string[]>() ?? [];
            Console.WriteLine(argsList);
            var executablePath = config?.GetValue<string>("ExecutablePath");
            var option = new LaunchOptions()
            {
                Headless = config?.GetValue<bool>("EnableHeadLess") ?? true,
                Args = argsList,
            };
            if (!string.IsNullOrEmpty(executablePath))
            {
                option.ExecutablePath = executablePath;
            }
            browser = await Puppeteer.LaunchAsync(option);
        }
        if (Page == null || Page.IsClosed || Page.Browser.Process.HasExited)
        {
            Page = await browser.NewPageAsync();
        }
        var opt = new MarkdownPipelineBuilder()
            .UseAdvancedExtensions()
            .UseAlertBlocks()
            .UsePipeTables()
            .UseEmphasisExtras()
            .UseListExtras()
            .UseSoftlineBreakAsHardlineBreak()
            .UseFootnotes()
            .UseFooters()
            .UseCitations()
            .UseGenericAttributes()
            .UseGridTables()
            .UseAbbreviations()
            .UseEmojiAndSmiley()
            .UseDefinitionLists()
            .UseCustomContainers()
            .UseFigures()
            .UseMathematics()
            .UseBootstrap()
            .UseMediaLinks()
            .UseSmartyPants()
            .UseAutoIdentifiers()
            .UseTaskLists()
            .UseDiagrams()
            .UseYamlFrontMatter()
            .UseNonAsciiNoEscape()
            .UseAutoLinks()
            .UseGlobalization()
            .Build();
        var postData = Markdig.Markdown.ToHtml(args.MarkdownContent, opt);
        await Page.WaitForNetworkIdleAsync(new()
        {
            Timeout = args.TimeOut
        });
        foreach (var (oldChar, newChar) in ReplaceDic)
        {
            postData = postData.Replace(oldChar, newChar);
        }
        await Page.GoToAsync($"http://docs.oiapi.net/view.php?theme={(args.Dark ? "dark" : "light")}", args.TimeOut).ConfigureAwait(false);
        await Page.EvaluateExpressionAsync("document.body.style.backgroundColor = 'white'");
        await Page.EvaluateExpressionAsync($"document.querySelector('#app').innerHTML = '{postData}'");
        var app = await Page.QuerySelectorAsync("body").ConfigureAwait(false);
        await Page.EvaluateExpressionAsync("document.querySelector(\"#app\").style.backgroundColor = '#ccc'");
        await app.EvaluateFunctionAsync("element => element.style.backgroundColor = '#ccc'");
        if (args.AutoWidth)
            await app.EvaluateFunctionAsync("element => element.style.width = 'fit-content'");
        var clip = await app!.BoundingBoxAsync().ConfigureAwait(false);
        var ret = await Page.ScreenshotDataAsync(new()
        {
            Clip = new Clip
            {
                Width = clip!.Width,
                Height = clip.Height,
                X = clip.X,
                Y = clip.Y
            },
            Type = ScreenshotType.Png
        });
        stopwatch.Stop();
        return (ret, stopwatch.Elapsed);
    }
}
