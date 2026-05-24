using System.Threading;
using System.Threading.Tasks;

namespace GitOut.Features.Llm;

public record LlmCompletionRequest(
    string Prompt,
    string SystemPrompt,
    float? Temperature = null,
    int? MaxTokens = null
);

public interface ILlmService
{
    Task<string> GenerateCompletionAsync(LlmCompletionRequest request, CancellationToken cancellationToken = default);
}
