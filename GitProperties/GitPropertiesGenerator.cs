using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using Microsoft.CodeAnalysis;

namespace GitOut.Features.Generators;

[Generator]
public class GitPropertiesGenerator : ISourceGenerator
{
    public void Execute(GeneratorExecutionContext context)
    {
        const string ProjectDirOptionsKey = "build_property.projectdir";
        if (
            !context.AnalyzerConfigOptions.GlobalOptions.TryGetValue(
                ProjectDirOptionsKey,
                out string folder
            )
        )
        {
            return;
        }
        try
        {
            Properties props = ReadGitProperties(folder, context.CancellationToken);
            if (context.CancellationToken.IsCancellationRequested)
            {
                return;
            }

            string source =
                $@"// Auto generated code
namespace GitOut.Features.Git.Properties
{{
    public static class GitProperties
    {{
        public static string CommitId {{ get; }} = ""{props.CommitId}"";
        public static string BranchName {{ get; }} = ""{props.BranchName}"";
    }}
}}
";

            context.AddSource($"GitProperties.g.cs", source);
        }
        catch (Exception e)
        {
            Trace.WriteLine(e.ToString());
        }
    }

    public void Initialize(GeneratorInitializationContext context) { }

    private Properties ReadGitProperties(string folder, CancellationToken token)
    {
        const string GitConfigurationFolder = ".git";
        const string GitHeadFile = "HEAD";
        const string ReferenceIdentifier = "ref: ";
        const char GitRefSeparatorChar = '/';
        const string LocalRefIdentifier = "refs/heads/";
        const string RemoteRefIdentifier = "refs/remotes/";

        string rootFolder = TraverseParentFolder(folder)
            .FirstOrDefault(f => Directory.Exists(Path.Combine(f, GitConfigurationFolder)));
        if (rootFolder is null || token.IsCancellationRequested)
        {
            return new Properties();
        }
        string parsedRef = File.ReadLines(
                Path.Combine(rootFolder, GitConfigurationFolder, GitHeadFile),
                Encoding.UTF8
            )
            .First();
        if (token.IsCancellationRequested)
        {
            return new Properties();
        }
        if (parsedRef.StartsWith(ReferenceIdentifier))
        {
            string branchRef = parsedRef.Substring(ReferenceIdentifier.Length);
            string branchName = branchRef
                .Replace(LocalRefIdentifier, string.Empty)
                .Replace(RemoteRefIdentifier, string.Empty);
            string commitId = File.ReadLines(
                    Path.Combine(
                        rootFolder,
                        GitConfigurationFolder,
                        branchRef.Replace(GitRefSeparatorChar, Path.DirectorySeparatorChar)
                    )
                )
                .First();
            return new Properties { CommitId = commitId, BranchName = branchName };
        }
        return new Properties { CommitId = parsedRef };
    }

    private IEnumerable<string> TraverseParentFolder(string root)
    {
        DirectoryInfo directory = new(root);
        do
        {
            yield return directory.FullName;
        } while ((directory = directory.Parent) is not null);
    }

    private class Properties
    {
        public string CommitId { get; set; } = string.Empty;
        public string BranchName { get; set; } = string.Empty;
    }
}
