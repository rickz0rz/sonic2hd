// Minimal static file server for testing the web build locally.
//   dotnet run --project tools/web-server -- <wwwroot> [port] [log-file]
// Serves files with WebAssembly-friendly MIME types and cross-origin isolation headers, and appends any
// text POSTed to /log to the log file (and the console), so the page can report diagnostics.
using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Text;
using System.Threading.Tasks;

string root = Path.GetFullPath(args.Length > 0 ? args[0] : ".");
int port = args.Length > 1 ? int.Parse(args[1]) : 8080;
string logFile = args.Length > 2 ? Path.GetFullPath(args[2]) : null;

var mimeTypes = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
{
    [".html"] = "text/html; charset=utf-8",
    [".js"] = "text/javascript",
    [".mjs"] = "text/javascript",
    [".json"] = "application/json",
    [".wasm"] = "application/wasm",
    [".css"] = "text/css",
    [".png"] = "image/png",
    [".ico"] = "image/x-icon",
    [".dat"] = "application/octet-stream",
    [".pdb"] = "application/octet-stream",
    [".dll"] = "application/octet-stream",
};

var listener = new HttpListener();
listener.Prefixes.Add($"http://localhost:{port}/");
listener.Start();
Console.WriteLine($"Serving {root} at http://localhost:{port}/" + (logFile != null ? $", logging to {logFile}" : ""));

while (true)
{
    HttpListenerContext context = await listener.GetContextAsync();
    _ = Task.Run(() => Handle(context));
}

void Handle(HttpListenerContext context)
{
    HttpListenerRequest request = context.Request;
    HttpListenerResponse response = context.Response;
    try
    {
        response.Headers["Cross-Origin-Opener-Policy"] = "same-origin";
        response.Headers["Cross-Origin-Embedder-Policy"] = "require-corp";
        response.Headers["Cache-Control"] = "no-store";
        if (request.HttpMethod == "POST" && request.Url.AbsolutePath == "/log")
        {
            using var reader = new StreamReader(request.InputStream, Encoding.UTF8);
            string text = reader.ReadToEnd();
            lock (listener)
            {
                Console.WriteLine(text);
                if (logFile != null)
                    File.AppendAllText(logFile, text + Environment.NewLine);
            }
            response.StatusCode = 204;
            return;
        }
        string relative = Uri.UnescapeDataString(request.Url.AbsolutePath).TrimStart('/');
        if (relative.Length == 0)
            relative = "index.html";
        string path = Path.GetFullPath(Path.Combine(root, relative));
        if (!path.StartsWith(root, StringComparison.OrdinalIgnoreCase) || !File.Exists(path))
        {
            response.StatusCode = 404;
            return;
        }
        response.ContentType = mimeTypes.TryGetValue(Path.GetExtension(path), out string type) ? type : "application/octet-stream";
        using FileStream file = File.OpenRead(path);
        response.ContentLength64 = file.Length;
        file.CopyTo(response.OutputStream);
    }
    catch (Exception ex)
    {
        Console.Error.WriteLine($"{request.Url}: {ex.Message}");
    }
    finally
    {
        response.Close();
    }
}
