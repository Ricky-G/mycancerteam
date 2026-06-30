using System.Collections.Concurrent;
using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using MyCancerTeam.Core.Configuration;
using MyCancerTeam.Core.Notes;

namespace MyCancerTeam.App;

public static class SimpleWebUiHost
{
    public static async Task RunAsync(AppConfiguration configuration, INoteStore noteStore, CancellationToken cancellationToken)
    {
        var builder = WebApplication.CreateBuilder();
        builder.Services.AddCors();

        var app = builder.Build();

        // Redirect console output
        var logInterceptor = new ConsoleLogInterceptor();
        Console.SetOut(logInterceptor);

        app.MapGet("/", async context =>
        {
            context.Response.ContentType = "text/html; charset=utf-8";
            await context.Response.WriteAsync(GetHtmlContent());
        });

        app.MapGet("/api/summary", async context =>
        {
            var summary = await noteStore.ReadSummaryAsync(context.RequestAborted);
            context.Response.ContentType = "application/json";
            await context.Response.WriteAsync(JsonSerializer.Serialize(new { summary }));
        });

        app.MapPost("/api/upload", async context =>
        {
            var form = await context.Request.ReadFormAsync(context.RequestAborted);
            foreach (var file in form.Files)
            {
                var filePath = Path.Combine(configuration.MedicalNotesFolderPath, file.FileName);
                await using var stream = new FileStream(filePath, FileMode.Create);
                await file.CopyToAsync(stream, context.RequestAborted);
                Console.WriteLine($"[WebUI] Uploaded file to: {filePath}");
            }
            context.Response.StatusCode = 200;
        });

        app.MapPost("/api/text", async context =>
        {
            using var reader = new StreamReader(context.Request.Body);
            var body = await reader.ReadToEndAsync(context.RequestAborted);
            var data = JsonSerializer.Deserialize<TextInputRequest>(body, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
            if (!string.IsNullOrWhiteSpace(data?.Text))
            {
                var filename = $"note_{DateTime.Now:yyyyMMdd_HHmmss}.txt";
                var filePath = Path.Combine(configuration.MedicalNotesFolderPath, filename);
                await File.WriteAllTextAsync(filePath, data.Text, context.RequestAborted);
                Console.WriteLine($"[WebUI] Saved text note to: {filePath}");
            }
            context.Response.StatusCode = 200;
        });

        app.MapGet("/api/logs", async context =>
        {
            context.Response.Headers.Append("Content-Type", "text/event-stream");
            context.Response.Headers.Append("Cache-Control", "no-cache");
            context.Response.Headers.Append("Connection", "keep-alive");

            var tcs = CancellationTokenSource.CreateLinkedTokenSource(context.RequestAborted, cancellationToken);
            
            // Send initial connection event
            await context.Response.WriteAsync("data: connected\n\n", tcs.Token);
            await context.Response.Body.FlushAsync(tcs.Token);

            var queue = logInterceptor.Subscribe();
            try
            {
                while (!tcs.Token.IsCancellationRequested)
                {
                    var msg = await queue.Reader.ReadAsync(tcs.Token);
                    var payload = JsonSerializer.Serialize(msg);
                    await context.Response.WriteAsync($"data: {payload}\n\n", tcs.Token);
                    await context.Response.Body.FlushAsync(tcs.Token);
                }
            }
            catch (OperationCanceledException)
            {
                // Client disconnected
            }
            finally
            {
                logInterceptor.Unsubscribe(queue);
            }
        });

        // Run on specified port
        await app.RunAsync("http://127.0.0.1:5078");
    }

    private class TextInputRequest
    {
        public string? Text { get; set; }
    }

    private class ConsoleLogInterceptor : TextWriter
    {
        private readonly TextWriter _originalConsole;
        private readonly ConcurrentBag<System.Threading.Channels.Channel<string>> _subscribers = new();

        public ConsoleLogInterceptor()
        {
            _originalConsole = Console.Out;
        }

        public override Encoding Encoding => _originalConsole.Encoding;

        public override void Write(char value)
        {
            _originalConsole.Write(value);
            Broadcast(value.ToString());
        }

        public override void Write(string? value)
        {
            _originalConsole.Write(value);
            if (value != null)
                Broadcast(value);
        }

        public override void WriteLine(string? value)
        {
            _originalConsole.WriteLine(value);
            if (value != null)
                Broadcast(value + Environment.NewLine);
        }

        private void Broadcast(string message)
        {
            foreach (var channel in _subscribers)
            {
                channel.Writer.TryWrite(message);
            }
        }

        public System.Threading.Channels.Channel<string> Subscribe()
        {
            var channel = System.Threading.Channels.Channel.CreateUnbounded<string>();
            _subscribers.Add(channel);
            return channel;
        }

        public void Unsubscribe(System.Threading.Channels.Channel<string> channel)
        {
            // ConcurrentBag doesn't easily support removal, but in our simple case, 
            // since we don't expect many connections, we can just let it be, or complete it
            channel.Writer.TryComplete();
        }
    }

    private static string GetHtmlContent() => @"
<!DOCTYPE html>
<html lang='en'>
<head>
    <meta charset='UTF-8'>
    <meta name='viewport' content='width=device-width, initial-scale=1.0'>
    <title>My Cancer Team App</title>
    <style>
        body { font-family: system-ui, -apple-system, sans-serif; margin: 0; padding: 0; display: flex; flex-direction: column; height: 100vh; background-color: #f3f4f6; }
        header { background: #1f2937; color: white; padding: 1rem; text-align: center; }
        main { flex: 1; display: flex; flex-direction: column; padding: 1rem; overflow-y: auto; gap: 1rem; }
        .card { background: white; padding: 1rem; border-radius: 8px; box-shadow: 0 1px 3px rgba(0,0,0,0.1); }
        .card h2 { margin-top: 0; font-size: 1.25rem; }
        textarea { width: 100%; height: 100px; margin-bottom: 0.5rem; padding: 0.5rem; border: 1px solid #d1d5db; border-radius: 4px; box-sizing: border-box; }
        button { background: #3b82f6; color: white; border: none; padding: 0.5rem 1rem; border-radius: 4px; cursor: pointer; }
        button:hover { background: #2563eb; }
        .summary-content { white-space: pre-wrap; font-family: monospace; font-size: 0.9rem; background: #f9fafb; padding: 1rem; border-radius: 4px; overflow-y: auto; max-height: 200px; }
        #logs-container { height: 200px; background: #1e1e1e; color: #d4d4d4; font-family: 'Courier New', Courier, monospace; font-size: 0.85rem; overflow-y: auto; padding: 0.5rem; border-top: 2px solid #000; }
        .log-entry { margin: 0; white-space: pre-wrap; }
    </style>
</head>
<body>
    <header>
        <h1>My Cancer Team</h1>
    </header>
    <main>
        <div class='card'>
            <h2>Current Summary</h2>
            <div id='summary' class='summary-content'>Loading...</div>
            <button onclick='loadSummary()' style='margin-top: 0.5rem;'>Refresh Summary</button>
        </div>

        <div class='card'>
            <h2>Add Text Note</h2>
            <textarea id='text-input' placeholder='Type new patient context or notes here...'></textarea>
            <button onclick='submitText()'>Save as Text Note</button>
        </div>

        <div class='card'>
            <h2>Upload Context File</h2>
            <input type='file' id='file-upload' multiple />
            <button onclick='uploadFiles()'>Upload</button>
        </div>
    </main>

    <div id='logs-container'></div>

    <script>
        async function loadSummary() {
            try {
                const res = await fetch('/api/summary');
                const data = await res.json();
                document.getElementById('summary').innerText = data.summary || 'No summary available yet.';
            } catch (e) {
                document.getElementById('summary').innerText = 'Error loading summary.';
            }
        }

        async function submitText() {
            const text = document.getElementById('text-input').value;
            if (!text) return;
            await fetch('/api/text', {
                method: 'POST',
                headers: { 'Content-Type': 'application/json' },
                body: JSON.stringify({ text })
            });
            document.getElementById('text-input').value = '';
        }

        async function uploadFiles() {
            const input = document.getElementById('file-upload');
            if (input.files.length === 0) return;
            const formData = new FormData();
            for (const file of input.files) {
                formData.append('files', file);
            }
            await fetch('/api/upload', {
                method: 'POST',
                body: formData
            });
            input.value = '';
        }

        function setupLogs() {
            const evtSource = new EventSource('/api/logs');
            const container = document.getElementById('logs-container');
            
            evtSource.onmessage = function(e) {
                if (e.data === 'connected') return;
                try {
                    const msg = JSON.parse(e.data);
                    const span = document.createElement('span');
                    span.className = 'log-entry';
                    span.innerText = msg;
                    container.appendChild(span);
                    container.scrollTop = container.scrollHeight;
                } catch (err) {}
            };
        }

        loadSummary();
        setupLogs();
    </script>
</body>
</html>
";
}
