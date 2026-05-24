using System;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Options;

namespace GitOut.Features.Llm;

public class LlmService : ILlmService
{
    private static readonly HttpClient httpClient = new() { Timeout = Timeout.InfiniteTimeSpan };
    private readonly IOptionsMonitor<LlmOptions> options;

    public LlmService(IOptionsMonitor<LlmOptions> options)
    {
        this.options = options;
    }

    public async Task<string> GenerateCompletionAsync(LlmCompletionRequest request, CancellationToken cancellationToken = default)
    {
        var chatRequest = new
        {
            messages = new[]
            {
                new { role = "system", content = request.SystemPrompt },
                new { role = "user", content = request.Prompt }
            },
            temperature = request.Temperature ?? 0.5f,
            max_tokens = request.MaxTokens
        };

        string json = JsonSerializer.Serialize(chatRequest);
        using var content = new StringContent(json, Encoding.UTF8, "application/json");

        var serverUrl = options.CurrentValue.ServerUrl;
        var endpoint = new Uri(serverUrl, "/v1/chat/completions");

        int timeoutSeconds = options.CurrentValue.TimeoutSeconds;
        using var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        cts.CancelAfter(TimeSpan.FromSeconds(timeoutSeconds));

        try
        {
            using HttpResponseMessage response = await httpClient.PostAsync(endpoint, content, cts.Token).ConfigureAwait(false);
            _ = response.EnsureSuccessStatusCode();

            string responseJson = await response.Content.ReadAsStringAsync(cts.Token).ConfigureAwait(false);
            using JsonDocument doc = JsonDocument.Parse(responseJson);
            JsonElement root = doc.RootElement;
            
            if (root.TryGetProperty("choices", out JsonElement choices) &&
                choices.GetArrayLength() > 0 &&
                choices[0].TryGetProperty("message", out JsonElement message) &&
                message.TryGetProperty("content", out JsonElement contentProp))
            {
                return contentProp.GetString() ?? string.Empty;
            }

            throw new InvalidOperationException("Failed to parse completion from LLM response.");
        }
        catch (OperationCanceledException ex)
        {
            if (cancellationToken.IsCancellationRequested)
            {
                throw;
            }
            throw new TimeoutException($"The LLM request timed out after {timeoutSeconds} seconds.", ex);
        }
    }
}
