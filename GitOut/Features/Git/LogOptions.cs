namespace GitOut.Features.Git
{
    public class LogOptions
    {
        private LogOptions(bool includeRemotes) => IncludeRemotes = includeRemotes;

        public bool IncludeRemotes { get; }

        public static LogOptions OnlyLocalBranches() => new LogOptions(false);
        public static LogOptions WithRemoteBranches() => new LogOptions(true);
    }
}
