using System;
using System.Collections.Generic;
using GitOut.Features.Git.Diff;

namespace GitOut.Features.Git.Files
{
    public static class GitFileEntryViewModelFactory
    {
        public static async IAsyncEnumerable<IGitFileEntryViewModel> ListIdAsync(GitObjectId id, IGitRepository repository)
        {
            await foreach (GitFileEntry file in repository.ExecuteListTreeAsync(id))
            {
                IGitFileEntryViewModel viewmodel = file.Type switch
                {
                    GitFileType.Tree => GitDirectoryViewModel.Wrap(file, treeId => ListIdAsync(treeId, repository)),
                    GitFileType.Blob => GitFileViewModel.Wrap(repository, file),
                    _ => throw new ArgumentOutOfRangeException($"Cannot create viewmodel for invalid type {file.Type}", nameof(file))
                };
                yield return viewmodel;
            }
        }

        public static async IAsyncEnumerable<IGitFileEntryViewModel> DiffIdAsync(GitObjectId? root, GitObjectId diff, IGitRepository repository)
        {
            await foreach (GitDiffFileEntry entry in repository.ExecuteListDiffChangesAsync(diff, root))
            {
                IGitFileEntryViewModel viewmodel = entry.FileType switch
                {
                    GitFileType.Tree => GitDirectoryViewModel.Wrap(entry, (treeId, destinationId) => entry.Type switch
                    {
                        GitDiffType.Create => ListIdAsync(destinationId, repository),
                        GitDiffType.Delete => ListIdAsync(treeId, repository),
                        _ => DiffIdAsync(treeId, destinationId, repository)
                    }),
                    GitFileType.Blob => GitFileViewModel.Wrap(repository, entry),
                    _ => throw new ArgumentOutOfRangeException($"Cannot create viewmodel for invalid type {entry.FileType}", nameof(entry))
                };
                yield return viewmodel;
            }
        }

        public static async IAsyncEnumerable<IGitFileEntryViewModel> DiffAllAsync(GitCommitId? root, GitCommitId diff, IGitRepository repository)
        {
            await foreach (GitDiffFileEntry entry in repository.ExecuteListDiffChangesAsync(diff, root, DiffOptions.Builder().Recursive().Build()))
            {
                IGitFileEntryViewModel viewmodel = entry.FileType switch
                {
                    GitFileType.Blob => GitFileViewModel.Wrap(repository, entry),
                    _ => throw new ArgumentOutOfRangeException($"Cannot create viewmodel for invalid type {entry.FileType}", nameof(entry))
                };
                yield return viewmodel;
            }
        }
    }
}
