namespace GitOut.Features.Git;

public sealed record GitCherryPickOptions(
    bool Edit,
    bool NoCommit,
    int? MainlineParentNumber,
    bool AppendCherryPickLine,
    bool FastForward
);
