using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using System.Windows.Data;

namespace GitOut.Features.Git.Files
{
    public class GitDirectoryViewModel : IGitFileEntryViewModel, INotifyPropertyChanged
    {
        private readonly GitFileEntry file;
        private readonly Func<GitObjectId, IAsyncEnumerable<IGitFileEntryViewModel>> lookup;

        private readonly object entriesLock = new object();
        private readonly ObservableCollection<IGitFileEntryViewModel> entries = new ObservableCollection<IGitFileEntryViewModel>();

        private bool isExpanded;
        private bool isPopulated;

        private GitDirectoryViewModel(GitFileEntry file, Func<GitObjectId, IAsyncEnumerable<IGitFileEntryViewModel>> lookup)
        {
            if (file.Type != GitFileType.Tree)
            {
                throw new ArgumentException($"Invalid file type for directory {file.Type}", nameof(file));
            }
            this.file = file;
            this.lookup = lookup;

            BindingOperations.EnableCollectionSynchronization(entries, entriesLock);
            Children = CollectionViewSource.GetDefaultView(entries);
            entries.Add(LoadingViewModel.Proxy);
        }

        public string FileName => file.FileName;
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
            IAsyncEnumerable<IGitFileEntryViewModel> files = lookup(file.Id);
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

        public static GitDirectoryViewModel Wrap(GitFileEntry file, Func<GitObjectId, IAsyncEnumerable<IGitFileEntryViewModel>> lookup) => new GitDirectoryViewModel(file, lookup);
    }
}
