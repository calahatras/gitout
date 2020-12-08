namespace GitOut.Features.Git.Patch
{
    public class PatchLine
    {
        private PatchLine(DiffLineType type, string line)
        {
            Type = type;
            Line = line;
        }

        public DiffLineType Type { get; }
        public string Line { get; }

        public static PatchLine CreateLine(DiffLineType type, string line) => new PatchLine(type, line);
    }
}
