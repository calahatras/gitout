namespace GitOut.Features.Git.Log
{
    public class GitLogPageOptions
    {
        public GitLogPageOptions(IGitRepository repository) => Repository = repository;

        public IGitRepository Repository { get; }

        public static GitLogPageOptions OpenRepository(IGitRepository repository) =>
            new(repository);
    }
}
