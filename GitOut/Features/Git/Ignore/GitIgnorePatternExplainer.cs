using System;
using System.Text;

namespace GitOut.Features.Git.Ignore;

public static class GitIgnorePatternExplainer
{
    public static string Explain(string pattern)
    {
        if (pattern.StartsWith("#", StringComparison.Ordinal))
            return "Comment";
        if (pattern.StartsWith("!", StringComparison.Ordinal))
            return "Negates (un-ignores) previous rules";

        var sb = new StringBuilder("Ignores ");
        bool isDirectory = pattern.EndsWith("/", StringComparison.Ordinal);
        sb.Append(isDirectory ? "directory " : "file or directory ");

        string param = pattern.TrimStart('!', '/');
        if (isDirectory)
            param = param.TrimEnd('/');

        sb.Append($"matching '{param}'");

        if (pattern.StartsWith("/", StringComparison.Ordinal))
            sb.Append(" at root");
        if (pattern.Contains("*", StringComparison.Ordinal))
            sb.Append(" (wildcard)");

        return sb.ToString();
    }
}
