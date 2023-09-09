// <copyright file="Program.cs">
//   CopyCopyDict - Background app that opens a dictionary definition of a selected word by Ctrl+C+C
//   (c) 2023 Artem Avramenko. https://github.com/ArtemAvramenko/CopyCopyDict
//   License: MIT
// </copyright>

using System;
using System.Diagnostics;
using System.Drawing;
using System.Runtime.InteropServices;
using System.Threading;
using System.Windows.Forms;

namespace CopyCopyDict
{
    public class Program
    {
        public static void Main()
        {
            bool mutexCreated;
            using (var mutex = new Mutex(true, "CopyCopyDictRunning", out mutexCreated))
            {
                if (!mutexCreated)
                {
                    return;
                }

                var keyInterceptor = new KeyInterceptor();
                keyInterceptor.Pressed += BrowseClipboardText;

                var menu = new ContextMenuStrip();
                var openItem = new ToolStripMenuItem("Open Dictionary", null, (e, a) => Browse())
                {
                    ShortcutKeyDisplayString = "Ctrl+C+C",
                };
                menu.Items.Add(openItem);
                menu.Items.Add("-");
                menu.Items.Add("Exit", null, (e, a) => Application.Exit());

                var icon = Resources.Resources.Icon;
                using (var g = Graphics.FromHwnd(IntPtr.Zero))
                {
                    var desktop = g.GetHdc();
                    var logicalScreenHeight = GetDeviceCaps(desktop, (int)DeviceCap.VERTRES);
                    var physicalScreenHeight = GetDeviceCaps(desktop, (int)DeviceCap.DESKTOPVERTRES);
                    var dpiY = GetDeviceCaps(desktop, (int)DeviceCap.LOGPIXELSY);
                    var scale = Math.Max(
                        (double)physicalScreenHeight / logicalScreenHeight,
                        (double)dpiY / 96);
                    var iconSize = (int)Math.Round(16 * scale);
                    if (Array.IndexOf(new[] { 16, 20, 24 }, iconSize) >= 0)
                    {
                        icon = new Icon(icon, iconSize, iconSize);
                    }
                }

                var trayIcon = new NotifyIcon
                {
                    Icon = icon,
                    ContextMenuStrip = menu,
                    Visible = true
                };
                trayIcon.DoubleClick += (e, a) => Browse();
                trayIcon.Text = "CopyCopyDict";

                Application.Run();
                keyInterceptor.Dispose();
                trayIcon.Dispose();
            }
        }

        private static void Browse(string text = null)
        {
            foreach (var urlPattern in new[] {
                Properties.Settings.Default.UrlPattern,
                Properties.Settings.Default.UrlPattern2,
                Properties.Settings.Default.UrlPattern3,
                Properties.Settings.Default.UrlPattern4
            })
            {
                if (urlPattern == null || urlPattern.Trim().Length == 0)
                {
                    continue;
                }
                var url = string.Format(
                    urlPattern.Trim(),
                    text ?? string.Empty);
                Process.Start(url);
            }
        }

        private static void BrowseClipboardText()
        {
            string text = null;
            try
            {
                text = Clipboard.GetText();
            }
            catch { }
            Browse(text);
        }

        [DllImport("gdi32.dll")]
        static extern int GetDeviceCaps(IntPtr hdc, int nIndex);
        public enum DeviceCap
        {
            VERTRES = 10,
            DESKTOPVERTRES = 117,
            LOGPIXELSY = 90
        }
    }
}
