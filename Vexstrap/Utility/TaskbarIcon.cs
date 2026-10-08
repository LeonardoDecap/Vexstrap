using System;
using System.Drawing;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Interop;

namespace Vexstrap.Utility
{
    internal static class TaskbarIcon
    {
        private const int WM_SETICON = 0x0080;
        private static readonly IntPtr ICON_SMALL = IntPtr.Zero;
        private static readonly IntPtr ICON_BIG = new IntPtr(1);

        private const int SHCNE_UPDATEITEM = 0x00002000;
        private const uint SHCNF_PATHW = 0x0005;

        [DllImport("user32.dll", CharSet = CharSet.Auto)]
        private static extern IntPtr SendMessage(IntPtr hWnd, int msg, IntPtr wParam, IntPtr lParam);

        [DllImport("shell32.dll", CharSet = CharSet.Unicode)]
        private static extern void SHChangeNotify(int eventId, uint flags, IntPtr item1, IntPtr item2);

        // Icons must stay alive while Windows uses them, so we hold them and dispose the old ones on change.
        private static Icon? _big;
        private static Icon? _small;

        /// <summary>
        /// Sets the taskbar and title bar icon for a window from a multi-size .ico file.
        /// Call again whenever the selected theme changes.
        /// </summary>
        
        public static string GetCurrentIconPath()
        {
            if (App.Settings.Prop.BootstrapperIcon == Enums.BootstrapperIcon.IconVexstrap)
                return Paths.Application;
                
            string shortcutIco = System.IO.Path.Combine(Paths.Base, "Icons", "Shortcut.ico");
            return System.IO.File.Exists(shortcutIco) ? shortcutIco : Paths.Application;
        }

        public static void Apply(Window window, string icoPath)
        {
            IntPtr hwnd = new WindowInteropHelper(window).Handle;
            if (hwnd == IntPtr.Zero || !System.IO.File.Exists(icoPath))
                return;

            try
            {
                var newBig = new System.Drawing.Icon(new System.Drawing.Icon(icoPath), new System.Drawing.Size(256, 256));
                var newSmall = new System.Drawing.Icon(new System.Drawing.Icon(icoPath), new System.Drawing.Size(16, 16));

                // WM_SETICON does not take ownership, so the old handles can be released after the swap
                SendMessage(hwnd, WM_SETICON, ICON_BIG, newBig.Handle);
                SendMessage(hwnd, WM_SETICON, ICON_SMALL, newSmall.Handle);

                _big?.Dispose();
                _small?.Dispose();
                _big = newBig;
                _small = newSmall;
            }
            catch (Exception ex)
            {
                App.Logger.WriteException("TaskbarIcon::Apply", ex);
            }
        }

        /// <summary>
        /// Tells Explorer that a .lnk file changed, so pinned and Start Menu icons refresh.
        /// </summary>
        public static void NotifyShortcutChanged(string lnkPath)
        {
            IntPtr ptr = Marshal.StringToHGlobalUni(lnkPath);
            try
            {
                SHChangeNotify(SHCNE_UPDATEITEM, SHCNF_PATHW, ptr, IntPtr.Zero);
            }
            finally
            {
                Marshal.FreeHGlobal(ptr);
            }
        }
    }
}
