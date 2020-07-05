using NUnit.Framework;

namespace GitOut.Features.Git
{
    public class GitFileEntryTest
    {
        [Test]
        public void ParseShouldParseGitFileOutput()
        {
            string input = "100644 blob 96d80cd6c4e7158dbebd0849f4fb7ce513e5828c\tf.txt";

            var entry = GitFileEntry.Parse(input);

            CollectionAssert.AreEqual(entry.FileModes, new[] { PosixFileModes.Read | PosixFileModes.Write, PosixFileModes.Read, PosixFileModes.Read });
            Assert.That(entry.Type, Is.EqualTo(GitFileType.Blob));
            Assert.That(entry.Id, Is.EqualTo(GitFileId.FromHash("96d80cd6c4e7158dbebd0849f4fb7ce513e5828c")));
            Assert.That(entry.FileName, Is.EqualTo("f.txt"));
        }

        [Test]
        public void ParseShouldParseGitDirectoryOutput()
        {
            string input = "040000 tree 22af1686db046317f3eea156ca1c547c63febd1b\tGitOut";

            var entry = GitFileEntry.Parse(input);

            CollectionAssert.AreEqual(entry.FileModes, new[] { PosixFileModes.None, PosixFileModes.None, PosixFileModes.None });
            Assert.That(entry.Type, Is.EqualTo(GitFileType.Tree));
            Assert.That(entry.Id, Is.EqualTo(GitFileId.FromHash("22af1686db046317f3eea156ca1c547c63febd1b")));
            Assert.That(entry.FileName, Is.EqualTo("GitOut"));
        }
    }
}
