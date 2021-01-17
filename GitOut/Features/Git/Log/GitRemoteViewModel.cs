using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace GitOut.Features.Git.Log
{
    public class GitRemoteViewModel : INotifyPropertyChanged
    {
        private bool isSelected;

        public GitRemoteViewModel(GitRemote model)
        {
            Name = model.Name;
            Model = model;
        }

        public string Name { get; }
        public bool IsSelected
        {
            get => isSelected;
            set => SetProperty(ref isSelected, value);
        }
        public GitRemote Model { get; }

        public event PropertyChangedEventHandler? PropertyChanged;

        public static GitRemoteViewModel From(GitRemote model) => new GitRemoteViewModel(model)
        {
            IsSelected = true
        };

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
