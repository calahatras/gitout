using System.Threading;
using System.Windows.Input;
using GitOut.Features.Material.Snackbar;
using GitOut.Features.Wpf;

namespace GitOut.Features.Git.Log
{
    public class BranchNameViewModel
    {
        public BranchNameViewModel(
            GitBranchName model,
            IGitRepository repository,
            IGitRepositoryNotifier notifier,
            ISnackbarService snack
        )
        {
            Name = model.Name;
            IconResource = model.IconResource;
            CopyBranchNameCommand = new CopyTextToClipBoardCommand<object>(
                o => model.Name,
                o => true,
                System.Windows.TextDataFormat.UnicodeText
            );
            DeleteBranchCommand = new AsyncCallbackCommand(
                async () =>
                {
                    GitDeleteResult? result = await repository.DeleteBranchAsync(model);
                    const string undoActionText = "UNDO";
                    const string forceDeleteActionText = "FORCE";
                    ISnackBuilder? builder = Snack.Builder()
                        .WithMessage(result.Message)
                        .WithDuration(Timeout.InfiniteTimeSpan);
                    if (result.UndoCommand is not null)
                    {
                        builder.AddAction(undoActionText);
                    }
                    if (result.ForceDeleteCommand is not null)
                    {
                        builder.AddAction(forceDeleteActionText);
                    }
                    SnackAction? action = await snack.ShowAsync(builder);
                    if (action is not null)
                    {
                        switch (action.Text)
                        {
                            case undoActionText: result.UndoCommand!.Execute(null); break;
                            case forceDeleteActionText: result.ForceDeleteCommand!.Execute(null); break;
                        }
                        notifier.NotifyLogChanged();
                    }
                },
                () => model.IsLocalBranchType
            );
        }

        public string Name { get; }
        public string IconResource { get; }

        public ICommand CopyBranchNameCommand { get; }
        public ICommand DeleteBranchCommand { get; }
    }
}
