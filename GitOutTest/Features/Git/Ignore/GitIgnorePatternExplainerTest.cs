using GitOut.Features.Git.Ignore;
using NUnit.Framework;

namespace GitOut.Features.Git.Ignore;

public class GitIgnorePatternExplainerTest
{
    [TestCase("# comment", "Comment")]
    [TestCase("!negate", "Negates (un-ignores) previous rules")]
    [TestCase("*.log", "Ignores file or directory matching '*.log' (wildcard)")]
    [TestCase("/root", "Ignores file or directory matching 'root' at root")]
    [TestCase("debug/", "Ignores directory matching 'debug'")]
    [TestCase("debug/*.log", "Ignores file or directory matching 'debug/*.log' (wildcard)")]
    [TestCase("/debug/", "Ignores directory matching 'debug' at root")]
    public void ExplainShouldReturnCorrectExplanation(string pattern, string expected)
    {
        string result = GitIgnorePatternExplainer.Explain(pattern);
        Assert.That(result, Is.EqualTo(expected));
    }
}
