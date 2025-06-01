using System.Net;
using System.Text;
using Newtonsoft.Json;

namespace MiniServer;

public class HttpRequestArgs(HttpListenerContext context)
{
    public HttpListenerContext Context { get; } = context;
    public string Path => Context.Request.RawUrl ?? string.Empty;
    public string Method => Context.Request.HttpMethod;

    public void ReplyJson(object data, HttpStatusCode code)
    {
        string jsonResponse = JsonConvert.SerializeObject(data);
        byte[] buffer = Encoding.UTF8.GetBytes(jsonResponse);
        Reply(buffer, code, "application/json");
    }

    public void ReplyImage(byte[] buffer, HttpStatusCode code)
    {
        Reply(buffer, code, "image/png");
    }

    public void ReplyText(string text, HttpStatusCode code)
    {
        byte[] buffer = Encoding.UTF8.GetBytes(text);
        Reply(buffer, code, "text/plain");
    }

    public void ReplyHtml(string html, HttpStatusCode code)
    {
        byte[] buffer = Encoding.UTF8.GetBytes(html);
        Reply(buffer, code, "text/html");
    }

    public void ReplyFile(string filePath, HttpStatusCode code)
    {
        byte[] buffer = File.ReadAllBytes(filePath);
        Reply(buffer, code, "application/octet-stream");
    }

    public void ReplyFile(byte[] buffer, HttpStatusCode code)
    {
        Reply(buffer, code, "application/octet-stream");
    }

    public void Reply(byte[] buffer, HttpStatusCode code, string contentType)
    {
        Context.Response.StatusCode = (int)code;
        Context.Response.ContentType = contentType;
        Context.Response.OutputStream.Write(buffer, 0, buffer.Length);
        Context.Response.Close();
    }
}
