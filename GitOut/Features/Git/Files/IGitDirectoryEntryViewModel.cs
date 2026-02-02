using System.Collections.Generic;

namespace GitOut.Features.Git.Files;

public interface IGitDirectoryEntryViewModel
    : IReadOnlyCollection<IGitFileEntryViewModel>,
        IGitFileEntryViewModel
{
    bool IsExpanded { get; set; }

    static int CompareItems(IGitFileEntryViewModel a, IGitFileEntryViewModel b) =>
        a is IGitDirectoryEntryViewModel && b is IGitDirectoryEntryViewModel
            ? string.Compare(a.FileName.ToString(), b.FileName.ToString(), true)
        : a is IGitDirectoryEntryViewModel ? -1
        : b is IGitDirectoryEntryViewModel ? 1
        : string.Compare(a.FileName.ToString(), b.FileName.ToString(), true);
}
