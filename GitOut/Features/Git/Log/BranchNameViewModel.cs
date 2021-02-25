using System.Windows.Input;
using GitOut.Features.Wpf;

namespace GitOut.Features.Git.Log
{
    public class BranchNameViewModel
    {
        private BranchNameViewModel(string name, string iconResource)
        {
            Name = name;
            IconResource = iconResource;
            CopyBranchNameCommand = new CopyTextToClipBoardCommand<object>(
                o => name,
                o => true,
                System.Windows.TextDataFormat.UnicodeText
            );
        }

        public string Name { get; }
        public string IconResource { get; }

        public ICommand CopyBranchNameCommand { get; }

        public static BranchNameViewModel FromModel(GitBranchName model) => new BranchNameViewModel(model.Name, model.IconResource);
    }
}
