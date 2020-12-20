using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace GitOut.Features.Git.Stage
{
    public class StatusChangeViewModel : INotifyPropertyChanged
    {
        private bool isSelected;

        private StatusChangeViewModel(GitStatusChange model, StatusChangeLocation location)
        {
            Model = model;
            Location = location;
            Path = model.Path.ToString();
            if (model.Type == GitStatusChangeType.Untracked)
            {
                Status = GitModifiedStatusType.Added;
                IconResourceKey = "FilePlus";
            }
            else
            {
                GitModifiedStatusType? status = location == StatusChangeLocation.Workspace
                    ? model.WorkspaceStatus
                    : model.IndexStatus;
                if (status == null)
                {
                    throw new ArgumentNullException(nameof(model), "Got null status for tracked file");
                }
                Status = status.Value;
                IconResourceKey = status switch
                {
                    GitModifiedStatusType.Added => "FilePlus",
                    GitModifiedStatusType.Deleted => "FileRemove",
                    GitModifiedStatusType.Modified => "FileEdit",
                    GitModifiedStatusType.Renamed => "FileMove",
                    GitModifiedStatusType.Copied => "FileReplace",
                    _ => "FileHidden"
                };
            }
        }

        public string Path { get; }
        public GitModifiedStatusType Status { get; }
        public string IconResourceKey { get; }

        public GitStatusChange Model { get; }
        public StatusChangeLocation Location { get; }

        public bool IsSelected
        {
            get => isSelected;
            set => SetProperty(ref isSelected, value);
        }

        public event PropertyChangedEventHandler? PropertyChanged;

        public static StatusChangeViewModel AsStaged(GitStatusChange change) => new StatusChangeViewModel(change, StatusChangeLocation.Index);

        public static StatusChangeViewModel AsWorkspace(GitStatusChange change) => new StatusChangeViewModel(change, StatusChangeLocation.Workspace);

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
