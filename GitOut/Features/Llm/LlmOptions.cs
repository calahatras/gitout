namespace GitOut.Features.Llm;

public class LlmOptions
{
    public const string SectionKey = "llm";

    public System.Uri ServerUrl { get; set; } = new System.Uri("http://localhost:5100");
    public string SystemPrompt { get; set; } = "You are a helpful assistant that generates clean, concise git commit messages. Analyze the provided git diff and staged file names, look at the previous commits for formatting style (such as Conventional Commits), and write a suitable commit message. Return only the commit message text. Do not wrap it in quotes, markdown code blocks, or include any extra conversational intro/outro.";
    public bool UseHistory { get; set; } = true;
    public int HistoryCount { get; set; } = 10;
    public int TimeoutSeconds { get; set; } = 300;
}
