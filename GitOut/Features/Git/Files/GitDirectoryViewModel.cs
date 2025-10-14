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
    public class GitDirectoryViewModel
        : IGitDirectoryEntryViewModel,
            INotifyCollectionChanged,
            INotifyPropertyChanged
    {
        private readonly IGitRepository repository;
        private readonly ICollection<IGitFileEntryViewModel> entries;
        private bool isExpanded;

        private GitDirectoryViewModel(
            IGitRepository repository,
            FileName fileName,
            RelativeDirectoryPath parent,
            SortedObservableCollection<IGitFileEntryViewModel> entries
        )
        {
            this.repository = repository;
            FileName = fileName;
            Path = parent;
            this.entries = entries;
            entries.CollectionChanged += (o, e) => CollectionChanged?.Invoke(this, e);
        }

        public GitDirectoryViewModel(
            IGitRepository repository,
            FileName fileName,
            RelativeDirectoryPath parent,
            IEnumerable<IGitFileEntryViewModel> children
        )
            : this(
                repository,
                fileName,
                parent,
                new SortedObservableCollection<IGitFileEntryViewModel>(
                    children,
                    IGitDirectoryEntryViewModel.CompareItems
                )
            ) { }

        private GitDirectoryViewModel(
            IGitRepository repository,
            FileName fileName,
            RelativeDirectoryPath parent,
            Func<RelativeDirectoryPath, IAsyncEnumerable<IGitFileEntryViewModel>> lookup
        )
            : this(
                repository,
                fileName,
                parent,
                new SortedLazyAsyncCollection<IGitFileEntryViewModel, RelativeDirectoryPath>(
                    lookup,
                    IGitDirectoryEntryViewModel.CompareItems
                )
                {
                    LoadingViewModel.Proxy,
                }
            ) { }

        public RelativeDirectoryPath Path { get; }
        public string RootPath => repository.WorkingDirectory.ToString();
        public string RelativePath => FullPath[RootPath.ToString().Length..];
        public string RelativeDirectory => FullPath[..^FileName.ToString().Length];
        public string FullPath =>
            System.IO.Path.Combine(
                repository.WorkingDirectory.ToString(),
                Path.ToString(),
                System.IO.Path.GetFileName(
                    FileName.ToString().Replace("/", "\\", StringComparison.InvariantCulture)
                )
            );

        public FileName FileName { get; }
        public string IconResourceKey => IsExpanded ? "FolderOpen" : "Folder";
        public bool IsExpanded
        {
            get => isExpanded;
            set
            {
                if (SetProperty(ref isExpanded, value))
                {
                    PropertyChanged?.Invoke(
                        this,
                        new PropertyChangedEventArgs(nameof(IconResourceKey))
                    );
                    if (
                        value
                        && entries
                            is ILazyAsyncEnumerable<
                                IGitFileEntryViewModel,
                                RelativeDirectoryPath
                            > lazy
                        && !lazy.IsMaterialized
                    )
                    {
#pragma warning disable CA2012 // Use ValueTasks correctly
                        _ = lazy.MaterializeAsync(Path.Append(FileName.ToString())).AsTask();
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

        public static GitDirectoryViewModel Snapshot(
            IGitRepository repository,
            GitFileEntry file,
            RelativeDirectoryPath parent,
            Func<
                GitObjectId,
                RelativeDirectoryPath,
                IAsyncEnumerable<IGitFileEntryViewModel>
            > lookup
        ) =>
            file.Type != GitFileType.Tree
                ? throw new ArgumentException(
                    $"Invalid file type for directory {file.Type}",
                    nameof(file)
                )
                : new GitDirectoryViewModel(
                    repository,
                    file.FileName,
                    parent,
                    relativePath => lookup(file.Id, relativePath)
                );

        public static GitDirectoryViewModel Difference(
            IGitRepository repository,
            GitDiffFileEntry file,
            RelativeDirectoryPath parent,
            Func<
                GitObjectId,
                GitObjectId,
                RelativeDirectoryPath,
                IAsyncEnumerable<IGitFileEntryViewModel>
            > lookup
        ) =>
            file.FileType != GitFileType.Tree
                ? throw new ArgumentException(
                    $"Invalid file type for directory {file.Type}",
                    nameof(file)
                )
                : new GitDirectoryViewModel(
                    repository,
                    file.Source.FileName,
                    parent,
                    relativePath => lookup(file.Source.Id, file.Destination.Id, relativePath)
                );

        private bool SetProperty<T>(
            ref T prop,
            T value,
            [CallerMemberName] string? propertyName = null
        )
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
