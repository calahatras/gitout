using System;
using System.Collections.Generic;
using GitOut.Features.Git.Diff;
using GitOut.Features.IO;

namespace GitOut.Features.Git.Files;

public static class GitFileEntryViewModelFactory
{
    public static async IAsyncEnumerable<IGitFileEntryViewModel> ListIdAsync(
        GitObjectId id,
        IGitRepository repository,
        RelativeDirectoryPath currentPath,
        GitCommitId? commitId = null
    )
    {
        await foreach (GitFileEntry file in repository.ListTreeAsync(id))
        {
            IGitFileEntryViewModel viewmodel = file.Type switch
            {
                GitFileType.Tree => GitDirectoryViewModel.Snapshot(
                    repository,
                    file,
                    currentPath,
                    (treeId, relativePath) =>
                        ListIdAsync(treeId, repository, relativePath, commitId)
                ),
                GitFileType.Blob => GitFileViewModel.Snapshot(
                    repository,
                    file,
                    currentPath,
                    commitId: commitId
                ),
                _ => throw new ArgumentOutOfRangeException(
                    $"Cannot create viewmodel for invalid type {file.Type}",
                    nameof(file)
                ),
            };
            yield return viewmodel;
        }
    }

    public static async IAsyncEnumerable<IGitFileEntryViewModel> DiffIdAsync(
        GitObjectId? root,
        GitObjectId diff,
        IGitRepository repository,
        RelativeDirectoryPath currentPath,
        DiffOptions? options = null,
        GitCommitId? commitId = null
    )
    {
        await foreach (GitDiffFileEntry entry in repository.ListDiffChangesAsync(diff, root))
        {
            IGitFileEntryViewModel viewmodel = entry.FileType switch
            {
                GitFileType.Tree => GitDirectoryViewModel.Difference(
                    repository,
                    entry,
                    currentPath,
                    (treeId, destinationId, relativePath) =>
                        entry.Type switch
                        {
                            GitDiffType.Create => ListIdAsync(
                                destinationId,
                                repository,
                                relativePath,
                                commitId
                            ),
                            GitDiffType.Delete => ListIdAsync(
                                treeId,
                                repository,
                                relativePath,
                                commitId
                            ),
                            _ => DiffIdAsync(
                                treeId,
                                destinationId,
                                repository,
                                relativePath,
                                options,
                                commitId
                            ),
                        }
                ),
                GitFileType.Blob => GitFileViewModel.Difference(
                    repository,
                    entry,
                    currentPath,
                    options,
                    commitId
                ),
                _ => throw new ArgumentOutOfRangeException(
                    $"Cannot create viewmodel for invalid type {entry.FileType}",
                    nameof(entry)
                ),
            };
            yield return viewmodel;
        }
    }

    public static async IAsyncEnumerable<IGitFileEntryViewModel> DiffAllAsync(
        GitCommitId? root,
        GitCommitId diff,
        IGitRepository repository,
        DiffOptions? options = null,
        GitCommitId? commitId = null
    )
    {
        IDiffOptionsBuilder builder = DiffOptions.Builder().Recursive();
        if (options?.Cached == true)
        {
            _ = builder.Cached();
        }

        await foreach (
            GitDiffFileEntry entry in repository.ListDiffChangesAsync(diff, root, builder.Build())
        )
        {
            if (entry.FileType == GitFileType.Blob)
            {
                yield return GitFileViewModel.RelativeDifference(
                    repository,
                    entry,
                    options,
                    commitId
                );
            }
        }
    }
}
