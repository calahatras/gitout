using System.Collections.Generic;
using GitOut.Features.IO;

namespace GitOut.Features.Git
{
    public interface IGitPatchBuilder
    {
        GitPatch Build();
        IGitPatchBuilder CreateHeader(RelativeDirectoryPath path, GitStatusChangeType changeType);
        IGitPatchBuilder CreateHunk(int fromFileRange, IEnumerable<PatchLine> lines);
        IGitPatchBuilder SetMode(PatchMode mode);
    }
}
