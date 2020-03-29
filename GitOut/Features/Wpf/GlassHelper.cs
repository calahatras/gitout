using System;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Interop;

// https://web.archive.org/web/20170701081532/http://withinrafael.com/adding-the-aero-glass-blur-to-your-windows-10-apps/
[StructLayout(LayoutKind.Sequential)]
internal struct WindowCompositionAttributeData
{
    public WindowCompositionAttribute Attribute;
    public IntPtr Data;
    public int SizeOfData;
}

internal enum WindowCompositionAttribute
{
    WCA_ACCENT_POLICY = 19
}

internal enum AccentState
{
    ACCENT_DISABLED = 0,
    ACCENT_ENABLE_GRADIENT = 1,
    ACCENT_ENABLE_TRANSPARENTGRADIENT = 2,
    ACCENT_ENABLE_BLURBEHIND = 3,
    ACCENT_INVALID_STATE = 4
}

[StructLayout(LayoutKind.Sequential)]
internal struct AccentPolicy
{
    public AccentState AccentState;
    public int AccentFlags;
    public int GradientColor;
    public int AnimationId;
}

public static class GlassHelper
{
    [DllImport("user32.dll")]
    internal static extern int SetWindowCompositionAttribute(IntPtr hwnd, ref WindowCompositionAttributeData data);

    public static void EnableBlur(Window associated)
    {
        var windowHelper = new WindowInteropHelper(associated);

        var accent = new AccentPolicy();
        int accentStructSize = Marshal.SizeOf(accent);
        accent.AccentState = AccentState.ACCENT_ENABLE_BLURBEHIND;

        IntPtr accentPtr = Marshal.AllocHGlobal(accentStructSize);
        Marshal.StructureToPtr(accent, accentPtr, false);

        var data = new WindowCompositionAttributeData
        {
            Attribute = WindowCompositionAttribute.WCA_ACCENT_POLICY,
            SizeOfData = accentStructSize,
            Data = accentPtr
        };

        SetWindowCompositionAttribute(windowHelper.Handle, ref data);

        Marshal.FreeHGlobal(accentPtr);
    }
}
