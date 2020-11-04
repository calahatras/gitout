using System;
using System.Collections.Generic;

namespace GitOut.Features.Git.Files
{
    public static class GitFileEntryViewModelFactory
    {
        public static async IAsyncEnumerable<IGitFileEntryViewModel> ListIdAsync(GitObjectId id, IGitRepository repository)
        {
            await foreach (GitFileEntry file in repository.ExecuteListFilesAsync(id))
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

        public static async IAsyncEnumerable<IGitFileEntryViewModel> DiffIdAsync(GitObjectId root, GitObjectId? diff, IGitRepository repository)
        {
            await foreach (GitDiffFileEntry entry in repository.ExecuteListDiffChangesAsync(root, diff))
            {
                IGitFileEntryViewModel viewmodel = entry.FileType switch
                {
                    GitFileType.Tree => GitDiffDirectoryViewModel.Wrap(entry, (treeId, destinationId) => entry.Type switch
                    {
                        GitDiffType.Create => ListIdAsync(destinationId, repository),
                        GitDiffType.Delete => ListIdAsync(treeId, repository),
                        _ => DiffIdAsync(treeId, destinationId, repository)
                    }),
                    GitFileType.Blob => GitDiffFileViewModel.Wrap(repository, entry),
                    _ => throw new ArgumentOutOfRangeException($"Cannot create viewmodel for invalid type {entry.FileType}", nameof(entry))
                };
                yield return viewmodel;
            }
        }
    }
}
