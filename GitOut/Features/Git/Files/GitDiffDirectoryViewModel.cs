using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using System.Windows.Data;

namespace GitOut.Features.Git.Files
{
    public class GitDiffDirectoryViewModel : IGitFileEntryViewModel, INotifyPropertyChanged
    {
        private readonly GitDiffFileEntry file;
        private readonly Func<GitObjectId, GitObjectId, IAsyncEnumerable<IGitFileEntryViewModel>> lookup;

        private readonly object entriesLock = new object();
        private readonly ObservableCollection<IGitFileEntryViewModel> entries = new ObservableCollection<IGitFileEntryViewModel>();

        private bool isExpanded;
        private bool isPopulated;

        private GitDiffDirectoryViewModel(GitDiffFileEntry file, Func<GitObjectId, GitObjectId, IAsyncEnumerable<IGitFileEntryViewModel>> lookup)
        {
            if (file.FileType != GitFileType.Tree)
            {
                throw new ArgumentException($"Invalid file type for directory {file.Type}", nameof(file));
            }
            this.file = file;
            this.lookup = lookup;

            BindingOperations.EnableCollectionSynchronization(entries, entriesLock);
            Children = CollectionViewSource.GetDefaultView(entries);
            entries.Add(LoadingViewModel.Proxy);
        }

        public string FileName => file.SourceFileName.ToString();
        public string IconResourceKey => IsExpanded ? "FolderOpen" : "Folder";
        public bool IsExpanded
        {
            get => isExpanded;
            set
            {
                if (SetProperty(ref isExpanded, value) && value && !isPopulated)
                {
                    _ = PopulateChildrenAsync();
                    PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(IconResourceKey)));
                }
            }
        }
        public ICollectionView Children { get; }

        public event PropertyChangedEventHandler? PropertyChanged;

        private async Task PopulateChildrenAsync()
        {
            IAsyncEnumerable<IGitFileEntryViewModel> files = lookup(file.SourceId, file.DestinationId);
            await foreach (IGitFileEntryViewModel entry in files)
            {
                lock (entriesLock)
                {
                    entries.Add(entry);
                }
            }
            if (entries[0] is LoadingViewModel)
            {
                lock (entriesLock)
                {
                    entries.RemoveAt(0);
                }
            }
            isPopulated = true;
        }

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

        public static GitDiffDirectoryViewModel Wrap(GitDiffFileEntry file, Func<GitObjectId, GitObjectId, IAsyncEnumerable<IGitFileEntryViewModel>> lookup) => new GitDiffDirectoryViewModel(file, lookup);
    }
}
