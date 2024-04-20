using System.Runtime.InteropServices;
using System;
using System.Windows;
using System.Windows.Interop;

namespace DigitalBattleMap.Views;

/// <summary>
/// Interaction logic for MapWindow.xaml
/// </summary>
public partial class MapWindow : Window
{
    [Flags]
    public enum DwmWindowAttribute : uint
    {
        DWMWA_NCRENDERING_ENABLED = 1,
        DWMWA_NCRENDERING_POLICY,
        DWMWA_TRANSITIONS_FORCEDISABLED,
        DWMWA_ALLOW_NCPAINT,
        DWMWA_CAPTION_BUTTON_BOUNDS,
        DWMWA_NONCLIENT_RTL_LAYOUT,
        DWMWA_FORCE_ICONIC_REPRESENTATION,
        DWMWA_FLIP3D_POLICY,
        DWMWA_EXTENDED_FRAME_BOUNDS,
        DWMWA_HAS_ICONIC_BITMAP,
        DWMWA_DISALLOW_PEEK,
        DWMWA_EXCLUDED_FROM_PEEK,
        DWMWA_LAST
    }

    [Flags]
    public enum DWMNCRenderingPolicy : uint
    {
        UseWindowStyle,
        Disabled,
        Enabled,
        Last
    }

    [DllImport("dwmapi.dll", PreserveSig = false)]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static extern bool DwmIsCompositionEnabled();

    [DllImport("dwmapi.dll", PreserveSig = true)]
    public static extern int DwmSetWindowAttribute(IntPtr hwnd, DwmWindowAttribute dwmAttribute, IntPtr pvAttribute, uint cbAttribute);

    public MapWindow()
    {
        InitializeComponent();
        DisableWindowsAreoPeek();
    }

    private void DisableWindowsAreoPeek()
    {
        var helper = new WindowInteropHelper(this);
        helper.EnsureHandle();
        if (DwmIsCompositionEnabled())
        {
            var status = Marshal.AllocCoTaskMem(sizeof(uint));
            Marshal.Copy(new[] { (int)DWMNCRenderingPolicy.Enabled }, 0, status, 1);
            DwmSetWindowAttribute(helper.Handle, DwmWindowAttribute.DWMWA_EXCLUDED_FROM_PEEK, status, sizeof(uint));
        }
    }
}
