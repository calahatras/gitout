namespace GitOut.Features.Git
{
    public class GitAuthor
    {
        private GitAuthor(string name, string email)
        {
            Name = name;
            Email = email;
        }

        public string Name { get; }
        public string Email { get; }

        public static GitAuthor Create(string name, string email) => new GitAuthor(name, email);
    }
}
