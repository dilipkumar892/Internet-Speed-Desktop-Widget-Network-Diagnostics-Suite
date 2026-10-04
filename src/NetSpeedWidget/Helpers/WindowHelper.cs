using System;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Interop;

namespace NetSpeedWidget.Helpers;

public static class WindowHelper
{
    private const int GWL_EXSTYLE = -20;
    private const int WS_EX_TRANSPARENT = 0x00000020;
    private const int WS_EX_LAYERED = 0x00080000;

    [DllImport("user32.dll", EntryPoint = "GetWindowLong")]
    private static extern int GetWindowLong32(IntPtr hWnd, int nIndex);

    [DllImport("user32.dll", EntryPoint = "SetWindowLong")]
    private static extern int SetWindowLong32(IntPtr hWnd, int nIndex, int dwNewLong);

    [DllImport("user32.dll", EntryPoint = "GetWindowLongPtr")]
    private static extern IntPtr GetWindowLongPtr64(IntPtr hWnd, int nIndex);

    [DllImport("user32.dll", EntryPoint = "SetWindowLongPtr")]
    private static extern IntPtr SetWindowLongPtr64(IntPtr hWnd, int nIndex, IntPtr dwNewLong);

    [DllImport("user32.dll")]
    public static extern int SetWindowCompositionAttribute(IntPtr hwnd, ref WindowCompositionAttributeData data);

    [StructLayout(LayoutKind.Sequential)]
    public struct WindowCompositionAttributeData
    {
        public WindowCompositionAttribute Attribute;
        public IntPtr Data;
        public int SizeOfData;
    }

    public enum WindowCompositionAttribute
    {
        WCA_ACCENT_POLICY = 19
    }

    public enum AccentState
    {
        ACCENT_DISABLED = 0,
        ACCENT_ENABLE_GRADIENT = 1,
        ACCENT_ENABLE_TRANSPARENTGRADIENT = 2,
        ACCENT_ENABLE_BLURBEHIND = 3,
        ACCENT_ENABLE_ACRYLICBLURBEHIND = 4,
        ACCENT_INVALID_STATE = 5
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct AccentPolicy
    {
        public AccentState AccentState;
        public int AccentFlags;
        public int GradientColor;
        public int AnimationId;
    }

    public static void EnableBlur(Window window, bool isDark)
    {
        try
        {
            var windowHelper = new WindowInteropHelper(window);
            var accent = new AccentPolicy
            {
                AccentState = AccentState.ACCENT_ENABLE_BLURBEHIND,
                AccentFlags = 2,
                // Color format: AABBGGRR (with subtle tint)
                GradientColor = isDark ? 0x661e1e1e : 0x66f5f5f5
            };

            int accentStructSize = Marshal.SizeOf(accent);
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
        catch
        {
            // Fallback gracefully on older OS or if restricted
        }
    }

    public static void SetClickThrough(Window window, bool clickThrough)
    {
        try
        {
            var hwnd = new WindowInteropHelper(window).Handle;
            if (hwnd == IntPtr.Zero) return;

            long currentExStyle;
            if (IntPtr.Size == 8)
                currentExStyle = GetWindowLongPtr64(hwnd, GWL_EXSTYLE).ToInt64();
            else
                currentExStyle = GetWindowLong32(hwnd, GWL_EXSTYLE);

            if (clickThrough)
            {
                currentExStyle |= WS_EX_TRANSPARENT | WS_EX_LAYERED;
            }
            else
            {
                currentExStyle &= ~WS_EX_TRANSPARENT;
            }

            if (IntPtr.Size == 8)
                SetWindowLongPtr64(hwnd, GWL_EXSTYLE, new IntPtr(currentExStyle));
            else
                SetWindowLong32(hwnd, GWL_EXSTYLE, (int)currentExStyle);
        }
        catch { }
    }

    public static void SnapToScreenEdges(Window window, double snapMargin = 20)
    {
        var screen = SystemParameters.WorkArea;

        // Snap Left
        if (Math.Abs(window.Left - screen.Left) < snapMargin)
            window.Left = screen.Left;
        // Snap Right
        else if (Math.Abs((window.Left + window.ActualWidth) - screen.Right) < snapMargin)
            window.Left = screen.Right - window.ActualWidth;

        // Snap Top
        if (Math.Abs(window.Top - screen.Top) < snapMargin)
            window.Top = screen.Top;
        // Snap Bottom
        else if (Math.Abs((window.Top + window.ActualHeight) - screen.Bottom) < snapMargin)
            window.Top = screen.Bottom - window.ActualHeight;

        // Ensure window never moves completely off-screen
        if (window.Left < screen.Left) window.Left = screen.Left;
        if (window.Top < screen.Top) window.Top = screen.Top;
        if (window.Left + window.ActualWidth > screen.Right) window.Left = screen.Right - window.ActualWidth;
        if (window.Top + window.ActualHeight > screen.Bottom) window.Top = screen.Bottom - window.ActualHeight;
    }
}
