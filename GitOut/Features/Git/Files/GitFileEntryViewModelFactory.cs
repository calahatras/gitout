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
                    GitFileType.Blob => GitFileViewModel.Wrap(file),
                    _ => throw new ArgumentOutOfRangeException($"Cannot create viewmodel for invalid type {file.Type}", nameof(file))
                };
                yield return viewmodel;
            }
        }
    }
}
