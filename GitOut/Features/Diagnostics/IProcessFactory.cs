using GitOut.Features.IO;

namespace GitOut.Features.Diagnostics;

public interface IProcessFactory<T>
{
    T Create(DirectoryPath workingDirectory, ProcessOptions arguments);
}
