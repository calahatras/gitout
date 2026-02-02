using System;
using System.Runtime.InteropServices;

namespace GitOut.Features.Native.Shell32;

public static class FileOperations
{
    // https://www.fluxbytes.com/csharp/delete-files-or-folders-to-recycle-bin-in-c/
    private const int FO_DELETE = 0x0003;
    private const int FOF_ALLOWUNDO = 0x0040;
    private const int FOF_NOCONFIRMATION = 0x0010;

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Auto)]
    public struct SHFILEOPSTRUCT
    {
        public IntPtr hwnd;

        [MarshalAs(UnmanagedType.U4)]
        public int wFunc;
        public string pFrom;
        public string pTo;
        public short fFlags;

        [MarshalAs(UnmanagedType.Bool)]
        public bool fAnyOperationsAborted;
        public IntPtr hNameMappings;
        public string lpszProgressTitle;
    }

    [DllImport("shell32.dll", CharSet = CharSet.Auto)]
    private static extern int SHFileOperation(ref SHFILEOPSTRUCT FileOp);

    public static void MoveFileToRecycleBin(string path)
    {
        var fileop = new SHFILEOPSTRUCT
        {
            wFunc = FO_DELETE,
            pFrom = path + '\0' + '\0',
            fFlags = FOF_ALLOWUNDO | FOF_NOCONFIRMATION,
        };
        _ = SHFileOperation(ref fileop);
    }
}
