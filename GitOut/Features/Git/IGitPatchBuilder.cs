using System.Collections.Generic;

namespace GitOut.Features.Git
{
    public interface IGitPatchBuilder
    {
        GitPatch Build();
        IGitPatchBuilder CreateHeader(string path, GitStatusChangeType changeType);
        IGitPatchBuilder CreateHunk(int fromFileRange, IEnumerable<PatchLine> lines);
        IGitPatchBuilder SetMode(PatchMode mode);
    }
}
