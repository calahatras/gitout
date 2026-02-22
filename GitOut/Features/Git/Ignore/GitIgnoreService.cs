using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using GitOut.Features.IO;

namespace GitOut.Features.Git.Ignore;

public class GitIgnoreService : IGitIgnoreService
{
    public async Task<IReadOnlyList<GitIgnoreRule>> GetRulesAsync(IGitRepository repository)
    {
        var rules = new List<GitIgnoreRule>();
        string gitIgnorePath = Path.Combine(repository.WorkingDirectory.ToString(), ".gitignore");

        if (!File.Exists(gitIgnorePath))
        {
            return rules;
        }

        string[] lines = await File.ReadAllLinesAsync(gitIgnorePath);
        for (int i = 0; i < lines.Length; i++)
        {
            string line = lines[i];
            if (string.IsNullOrWhiteSpace(line))
            {
                continue;
            }

            string explanation = GitIgnorePatternExplainer.Explain(line);
            rules.Add(new GitIgnoreRule(line, explanation, i + 1, ".gitignore"));
        }

        return rules;
    }

    public async Task AddRuleAsync(IGitRepository repository, string pattern)
    {
        string gitIgnorePath = Path.Combine(repository.WorkingDirectory.ToString(), ".gitignore");
        await File.AppendAllTextAsync(gitIgnorePath, Environment.NewLine + pattern);
    }

    public async Task RemoveRuleAsync(IGitRepository repository, GitIgnoreRule rule)
    {
        string gitIgnorePath = Path.Combine(repository.WorkingDirectory.ToString(), ".gitignore");
        if (!File.Exists(gitIgnorePath))
        {
            return;
        }

        var lines = (await File.ReadAllLinesAsync(gitIgnorePath)).ToList();

        int index = rule.LineNumber - 1;
        if (index >= 0 && index < lines.Count && lines[index] == rule.Pattern)
        {
            lines.RemoveAt(index);
        }
        else
        {
            lines.Remove(rule.Pattern);
        }

        await File.WriteAllLinesAsync(gitIgnorePath, lines);
    }
}
