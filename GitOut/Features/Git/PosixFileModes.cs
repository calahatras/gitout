using System;

namespace GitOut.Features.Git
{
    [Flags]
    public enum PosixFileModes
    {
        None,
        Execute = 1,
        Write = 2,
        Read = 4,
    }
}
