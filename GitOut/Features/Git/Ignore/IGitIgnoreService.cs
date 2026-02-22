using System.Collections.Generic;
using System.Threading.Tasks;
using GitOut.Features.Git;

namespace GitOut.Features.Git.Ignore;

public interface IGitIgnoreService
{
    Task<IReadOnlyList<GitIgnoreRule>> GetRulesAsync(IGitRepository repository);
    Task AddRuleAsync(IGitRepository repository, string pattern);
    Task RemoveRuleAsync(IGitRepository repository, GitIgnoreRule rule);
}

public record GitIgnoreRule(string Pattern, string Explanation, int LineNumber, string Source);
