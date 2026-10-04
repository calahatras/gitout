#pragma warning disable RS1035 // Do not do file IO in analyzers
#nullable enable
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using Microsoft.CodeAnalysis;

namespace GitProperties;

[Generator]
public class GitPropertiesGenerator : IIncrementalGenerator
{
    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        IncrementalValueProvider<string?> projectDirProvider =
            context.AnalyzerConfigOptionsProvider.Select(
                (options, ct) =>
                {
                    _ = options.GlobalOptions.TryGetValue(
                        "build_property.projectdir",
                        out string? folder
                    );
                    return folder;
                }
            );

        context.RegisterSourceOutput(
            projectDirProvider,
            (spc, folder) =>
            {
                Properties props;
                try
                {
                    props = folder is not null
                        ? ReadGitProperties(folder, spc.CancellationToken)
                        : new Properties();
                }
                catch (Exception e)
                {
                    Trace.WriteLine(e.ToString());
                    props = new Properties();
                }

                if (spc.CancellationToken.IsCancellationRequested)
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

                spc.AddSource("GitProperties.g.cs", source);
            }
        );
    }

    private Properties ReadGitProperties(string folder, CancellationToken token)
    {
        const string GitConfigurationFolder = ".git";
        const string GitHeadFile = "HEAD";
        const string ReferenceIdentifier = "ref: ";
        const char GitRefSeparatorChar = '/';
        const string LocalRefIdentifier = "refs/heads/";
        const string RemoteRefIdentifier = "refs/remotes/";

        string? rootFolder = TraverseParentFolder(folder)
            .FirstOrDefault(f => Directory.Exists(Path.Combine(f, GitConfigurationFolder)) || File.Exists(Path.Combine(f, GitConfigurationFolder)));
        if (rootFolder is null || token.IsCancellationRequested)
        {
            return new Properties();
        }
        string gitConfigPath = Path.Combine(rootFolder, GitConfigurationFolder);
        string gitDir = Directory.Exists(gitConfigPath)
            ? gitConfigPath
            : (File.ReadAllLines(gitConfigPath, Encoding.UTF8).FirstOrDefault(l => l.StartsWith("gitdir: ")) is { } line
                ? line.Substring("gitdir: ".Length).Trim()
                : null) ?? gitConfigPath;

        string headFilePath = Path.Combine(gitDir, GitHeadFile);
        if (!File.Exists(headFilePath))
        {
            return new Properties();
        }
        string parsedRef = File.ReadLines(headFilePath, Encoding.UTF8).First();
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
            string commitFilePath = Path.Combine(
                gitDir,
                branchRef.Replace(GitRefSeparatorChar, Path.DirectorySeparatorChar)
            );
            string commitId = File.Exists(commitFilePath)
                ? File.ReadLines(commitFilePath).First()
                : string.Empty;
            return new Properties { CommitId = commitId, BranchName = branchName };
        }
        return new Properties { CommitId = parsedRef };
    }

    private IEnumerable<string> TraverseParentFolder(string root)
    {
        DirectoryInfo? directory = new(root);
        while (directory is not null)
        {
            yield return directory.FullName;
            directory = directory.Parent;
        }
    }

    private class Properties
    {
        public string CommitId { get; set; } = string.Empty;
        public string BranchName { get; set; } = string.Empty;
    }
}
