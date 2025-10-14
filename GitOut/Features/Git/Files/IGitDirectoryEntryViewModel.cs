using System.Collections.Generic;

namespace GitOut.Features.Git.Files
{
    public interface IGitDirectoryEntryViewModel
        : IReadOnlyCollection<IGitFileEntryViewModel>,
            IGitFileEntryViewModel
    {
        bool IsExpanded { get; set; }

        static int CompareItems(IGitFileEntryViewModel a, IGitFileEntryViewModel b)
        {
            if (a is IGitDirectoryEntryViewModel && b is IGitDirectoryEntryViewModel)
            {
                return string.Compare(a.FileName.ToString(), b.FileName.ToString(), true);
            }
            if (a is IGitDirectoryEntryViewModel)
            {
                return -1;
            }
            if (b is IGitDirectoryEntryViewModel)
            {
                return 1;
            }
            return string.Compare(a.FileName.ToString(), b.FileName.ToString(), true);
        }
    }
}
