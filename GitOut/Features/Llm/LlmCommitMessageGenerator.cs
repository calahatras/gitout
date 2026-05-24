using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Options;

namespace GitOut.Features.Llm;

public class LlmCommitMessageGenerator
{
    private readonly ILlmService llmService;
    private readonly IOptionsMonitor<LlmOptions> llmOptions;

    public LlmCommitMessageGenerator(ILlmService llmService, IOptionsMonitor<LlmOptions> llmOptions)
    {
        this.llmService = llmService;
        this.llmOptions = llmOptions;
    }

    public async Task<string> GenerateCommitMessageAsync(
        string diffText,
        IEnumerable<string> commitHistory,
        CancellationToken cancellationToken = default)
    {
        var options = llmOptions.CurrentValue;
        var promptBuilder = new StringBuilder();

        // Handle large diffs by truncating if necessary
        const int MaxDiffLength = 15000;
        string formattedDiff = diffText;
        if (diffText.Length > MaxDiffLength)
        {
            formattedDiff = diffText.Substring(0, MaxDiffLength) + "\n\n[Diff truncated for brevity...]";
        }

        promptBuilder.AppendLine("Analyze the following git diff to understand the changes made:");
        promptBuilder.AppendLine("```diff");
        promptBuilder.AppendLine(formattedDiff);
        promptBuilder.AppendLine("```");

        var historyList = commitHistory.ToList();
        if (options.UseHistory && historyList.Any())
        {
            promptBuilder.AppendLine();
            promptBuilder.AppendLine("Here are the messages from recent commits for style reference, mimic the style when outputting your commit message.");
            foreach (string msg in historyList)
            {
                promptBuilder.AppendLine("---");
                promptBuilder.AppendLine(msg);
            }
            promptBuilder.AppendLine("---");
        }

        var request = new LlmCompletionRequest(
            Prompt: promptBuilder.ToString(),
            SystemPrompt: options.SystemPrompt,
            Temperature: 0.2f // low temperature for consistent, structured generation
        );

        string response = await llmService.GenerateCompletionAsync(request, cancellationToken).ConfigureAwait(false);
        return CleanCommitMessage(response);
    }

    private static string CleanCommitMessage(string message)
    {
        if (string.IsNullOrWhiteSpace(message))
        {
            return string.Empty;
        }

        // Clean common LLM formatting remnants if any
        string cleaned = message.Trim();
        if (cleaned.StartsWith("`") && cleaned.EndsWith("`"))
        {
            cleaned = cleaned.Trim('`').Trim();
        }

        return cleaned;
    }
}
