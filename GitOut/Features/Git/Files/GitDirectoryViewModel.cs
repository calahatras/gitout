using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using GitOut.Features.Collections;
using GitOut.Features.Git.Diff;
using GitOut.Features.IO;

namespace GitOut.Features.Git.Files
{
    public class GitDirectoryViewModel : IGitDirectoryEntryViewModel, INotifyCollectionChanged, INotifyPropertyChanged
    {
        private readonly ICollection<IGitFileEntryViewModel> entries;
        private bool isExpanded;

        private GitDirectoryViewModel(FileName fileName, RelativeDirectoryPath parent, SortedObservableCollection<IGitFileEntryViewModel> entries)
        {
            FileName = fileName;
            Path = parent;
            this.entries = entries;
            entries.CollectionChanged += (o, e) => CollectionChanged?.Invoke(this, e);
        }

        public GitDirectoryViewModel(FileName fileName, RelativeDirectoryPath parent, IEnumerable<IGitFileEntryViewModel> children)
            : this(fileName, parent, new SortedObservableCollection<IGitFileEntryViewModel>(children, IGitDirectoryEntryViewModel.CompareItems)) { }

        private GitDirectoryViewModel(FileName fileName, RelativeDirectoryPath parent, Func<IAsyncEnumerable<IGitFileEntryViewModel>> lookup)
            : this(fileName, parent, new SortedLazyAsyncCollection<IGitFileEntryViewModel>(lookup, IGitDirectoryEntryViewModel.CompareItems) { LoadingViewModel.Proxy }) { }

        public RelativeDirectoryPath Path { get; }
        public FileName FileName { get; }
        public string IconResourceKey => IsExpanded ? "FolderOpen" : "Folder";
        public bool IsExpanded
        {
            get => isExpanded;
            set
            {
                if (SetProperty(ref isExpanded, value))
                {
                    PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(IconResourceKey)));
                    if (value && entries is ILazyAsyncEnumerable<IGitFileEntryViewModel> lazy && !lazy.IsMaterialized)
                    {
#pragma warning disable CA2012 // Use ValueTasks correctly
                        _ = lazy.MaterializeAsync();
#pragma warning restore CA2012 // Use ValueTasks correctly
                        entries.Remove(LoadingViewModel.Proxy);
                        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Count)));
                    }
                }
            }
        }

        public int Count => entries.Count;

        public event PropertyChangedEventHandler? PropertyChanged;
        public event NotifyCollectionChangedEventHandler? CollectionChanged;

        public IEnumerator<IGitFileEntryViewModel> GetEnumerator() => entries.GetEnumerator();
        IEnumerator IEnumerable.GetEnumerator() => entries.GetEnumerator();

        public static GitDirectoryViewModel Wrap(GitFileEntry file, Func<GitObjectId, IAsyncEnumerable<IGitFileEntryViewModel>> lookup) => file.Type != GitFileType.Tree
            ? throw new ArgumentException($"Invalid file type for directory {file.Type}", nameof(file))
            : new GitDirectoryViewModel(file.FileName, file.Directory, () => lookup(file.Id));

        public static GitDirectoryViewModel Wrap(GitDiffFileEntry file, Func<GitObjectId, GitObjectId, IAsyncEnumerable<IGitFileEntryViewModel>> lookup) => file.FileType != GitFileType.Tree
            ? throw new ArgumentException($"Invalid file type for directory {file.Type}", nameof(file))
            : new GitDirectoryViewModel(file.Source.FileName, file.Source.Directory, () => lookup(file.Source.Id, file.Destination.Id));

        private bool SetProperty<T>(ref T prop, T value, [CallerMemberName] string? propertyName = null)
        {
            if (!ReferenceEquals(prop, value))
            {
                prop = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
                return true;
            }
            return false;
        }
    }
}
