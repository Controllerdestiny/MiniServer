using System.Diagnostics;
using System.Net;
using MiniServer.Interface;
using Newtonsoft.Json;

namespace MiniServer.Handlers;

public class CommandHandler : IHttpRequestHandler
{
    public string Path => "/command";
    public string Method => HttpMethod.Post.Method;
    public async Task InvokeAsync(HttpRequestArgs args, CancellationToken cancellationToken)
    {
        using var sr = new StreamReader(args.Context.Request.InputStream, args.Context.Request.ContentEncoding);
        var requestBody = await sr.ReadToEndAsync(cancellationToken);
        var param = JsonConvert.DeserializeObject<CommandRequestArgs>(requestBody) ?? throw new ArgumentNullException("请求参数不能为空");
        var result = Execute(param.Command, param.Arguments, param.WorkingDirectory);
        args.ReplyJson(result, HttpStatusCode.OK);
    }

    public static CommandResult Execute(string command, string arguments = "", string? workingDirectory = null)
    {
        // 配置进程启动信息
        var startInfo = new ProcessStartInfo
        {
            FileName = command,
            Arguments = arguments,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true,
            WorkingDirectory = workingDirectory ?? Environment.CurrentDirectory
        };

        // 处理跨平台差异
        if (OperatingSystem.IsWindows())
        {
            // Windows 使用 cmd.exe 执行命令
            startInfo.FileName = "cmd.exe";
            startInfo.Arguments = $"/c {command} {arguments}";
        }
        else if (OperatingSystem.IsLinux() || OperatingSystem.IsMacOS())
        {
            // Linux/macOS 使用 bash 执行命令
            startInfo.FileName = "/bin/bash";
            startInfo.Arguments = $"-c \"{EscapeForBash(command)} {EscapeForBash(arguments)}\"";
        }

        using var process = new Process { StartInfo = startInfo };

        try
        {
            // 启动进程并等待完成
            process.Start();
            string output = process.StandardOutput.ReadToEnd();
            string error = process.StandardError.ReadToEnd();
            process.WaitForExit();

            return new CommandResult()
            {
                ExitCode = process.ExitCode,
                Output = output.Trim(),
                Error = error.Trim()
            };
        }
        catch (Exception ex)
        {
            return new CommandResult()
            {
                ExitCode = -1,
                Output = string.Empty,
                Error = ex.ToString()
            };
        }
    }

    // 处理 bash 特殊字符转义
    private static string EscapeForBash(string input)
    {
        if (string.IsNullOrEmpty(input)) return string.Empty;
        return input.Replace("\"", "\\\""); // 转义双引号
    }
}

public class CommandRequestArgs
{
    [JsonProperty("cmd")]
    public string Command { get; init; } = string.Empty;

    [JsonProperty("args")]
    public string Arguments { get; init; } = string.Empty;

    [JsonProperty("working_directory")]
    public string WorkingDirectory { get; init; } = string.Empty; // 工作目录，默认为当前目录
}

/// <summary>
/// 命令执行结果封装
/// </summary>
public class CommandResult
{
    [JsonProperty("exit_code")]
    public int ExitCode { get; init; }

    [JsonProperty("output")]
    public string Output { get; init; } = string.Empty;

    [JsonProperty("Error")]
    public string Error { get; init; } = string.Empty;
}