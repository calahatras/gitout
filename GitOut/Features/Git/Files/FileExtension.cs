using System.IO;

namespace GitOut.Features.Git.Files;

public record FileExtension(string Extension)
{
    public string Value => Extension;

    public static FileExtension FromFileInfo(FileInfo info) => new(info.Extension);
}
