namespace GitOut.Features.Git;

public class GitCherryPickOptions
{
    public bool Edit { get; set; }
    public bool NoCommit { get; set; }
    public int? MainlineParentNumber { get; set; }
    public bool AppendCherryPickLine { get; set; }
    public bool FastForward { get; set; }
}
