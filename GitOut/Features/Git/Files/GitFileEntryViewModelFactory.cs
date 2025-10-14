using System;
using System.Collections.Generic;
using GitOut.Features.Git.Diff;
using GitOut.Features.IO;

namespace GitOut.Features.Git.Files
{
    public static class GitFileEntryViewModelFactory
    {
        public static async IAsyncEnumerable<IGitFileEntryViewModel> ListIdAsync(
            GitObjectId id,
            IGitRepository repository,
            RelativeDirectoryPath currentPath
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
                        (treeId, relativePath) => ListIdAsync(treeId, repository, relativePath)
                    ),
                    GitFileType.Blob => GitFileViewModel.Snapshot(repository, file, currentPath),
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
            RelativeDirectoryPath currentPath
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
                                    relativePath
                                ),
                                GitDiffType.Delete => ListIdAsync(treeId, repository, relativePath),
                                _ => DiffIdAsync(treeId, destinationId, repository, relativePath),
                            }
                    ),
                    GitFileType.Blob => GitFileViewModel.Difference(repository, entry, currentPath),
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
            IGitRepository repository
        )
        {
            await foreach (
                GitDiffFileEntry entry in repository.ListDiffChangesAsync(
                    diff,
                    root,
                    DiffOptions.Builder().Recursive().Build()
                )
            )
            {
                IGitFileEntryViewModel viewmodel = entry.FileType switch
                {
                    GitFileType.Blob => GitFileViewModel.RelativeDifference(repository, entry),
                    _ => throw new ArgumentOutOfRangeException(
                        $"Cannot create viewmodel for invalid type {entry.FileType}",
                        nameof(entry)
                    ),
                };
                yield return viewmodel;
            }
        }
    }
}
