using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Windows.Input;
using GitOut.Features.IO;
using GitOut.Features.Wpf;

namespace GitOut.Features.Git;

public partial class GitDeleteResult
{
    [GeneratedRegex("^error: [Cc]annot delete branch")]
    private static partial Regex CanForceBranchRemoval();

    private GitDeleteResult(
        string message,
        ICommand? undo = null,
        ICommand? additional = null,
        string? additionalLabel = null,
        bool isBranchDeleted = false
    )
    {
        Message = message;
        UndoCommand = undo;
        AdditionalCommand = additional;
        AdditionalCommandLabel = additionalLabel;
        IsBranchDeleted = isBranchDeleted;
    }

    public string Message { get; }
    public ICommand? UndoCommand { get; }
    public ICommand? AdditionalCommand { get; }
    public string? AdditionalCommandLabel { get; }

    public bool IsBranchDeleted { get; }

    public static GitDeleteResult Parse(
        GitBranchName name,
        IGitRepository repository,
        IReadOnlyCollection<string> outputLines,
        IReadOnlyCollection<string> errorLines
    )
    {
        if (outputLines.Count == 1 && outputLines.First().StartsWith("Deleted branch"))
        {
            string response = outputLines.First();
            string shortCommitId = response[^9..^2];
            return new GitDeleteResult(
                "Deleted branch",
                new AsyncCallbackCommand(async () =>
                {
                    GitCommitId? id = await repository.GetCommitIdAsync(shortCommitId);
                    if (id is not null)
                    {
                        await repository.CreateBranchAsync(name, new GitCreateBranchOptions(id));
                    }
                }),
                isBranchDeleted: true
            );
        }
        else if (errorLines.Count >= 1)
        {
            string first = errorLines.First();
            if (
                errorLines.Count == 3
                && first.StartsWith($"error: the branch '{name.Name}' is not fully merged")
            )
            {
                return new GitDeleteResult(
                    $"The branch is not fully merged to remote, are you sure you want to delete {name.Name}",
                    additional: new AsyncCallbackCommand(() =>
                        repository.DeleteBranchAsync(name, new GitDeleteBranchOptions(true))
                    )
                );
            }
            else if (CanForceBranchRemoval().IsMatch(first))
            {
                if (
                    first.Contains(
                        " used by worktree at ",
                        System.StringComparison.InvariantCulture
                    )
                )
                {
                    string worktreePath = first.Split('\'')[3];
                    return new GitDeleteResult(
                        $"Cannot delete branch '{name.Name}' because it is used by a worktree!",
                        additional: new AsyncCallbackCommand(async () =>
                        {
                            await repository.WorktreeRemoveAsync(
                                DirectoryPath.Create(worktreePath)
                            );
                            _ = await repository.DeleteBranchAsync(name);
                        }),
                        additionalLabel: "REMOVE WORKTREE"
                    );
                }
                else
                {
                    return new GitDeleteResult(
                        $"Cannot delete branch '{name.Name}' because it is checked out!"
                    );
                }
            }
        }
        return new GitDeleteResult("Unknown issue");
    }
}
