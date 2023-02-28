﻿// <copyright file="Program.cs">
//   CopyCopyDict - Background app that opens a dictionary definition of a selected word by Ctrl+C+C
//   (c) 2023 Artem Avramenko. https://github.com/ArtemAvramenko/CopyCopyDict
//   License: MIT
// </copyright>

using System.Diagnostics;
using System.Threading;
using System.Windows.Forms;

namespace CopyCopyDict
{
    public class Program
    {
        public static void Main()
        {
            using (var mutex = new Mutex(true, "CopyCopyDictRunning", out var mutexCreated))
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
                var trayIcon = new NotifyIcon
                {
                    Icon = Resources.Resources.Icon,
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
    }
}
