namespace GitOut.Features.Git.Diff
{
    public class LineNumberViewModel
    {
        public LineNumberViewModel(int? workingTreeLineNumber, int? indexLineNumber)
        {
            WorkingTreeLineNumber = workingTreeLineNumber;
            IndexLineNumber = indexLineNumber;
        }

        public int? WorkingTreeLineNumber { get; }
        public int? IndexLineNumber { get; }
    }
}
