// <copyright file="HotKeyWindow.cs">
//   CopyCopyDict - Background app that opens a dictionary definition of a selected word by Ctrl+C+C
//   (c) 2019 Artem Avramenko. https://github.com/ArtemAvramenko/CopyCopyDict
//   License: MIT
// </copyright>

using System;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace CopyCopyDict
{
    internal class HotKeyWindow : NativeWindow
    {
        private const int WM_HOTKEY = 0x312;

        private const int HotKeyId = 1;

        [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        private static extern bool RegisterHotKey(IntPtr hWnd, int id, KeyModifiers fsModifiers, Keys vk);

        [DllImport("user32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool UnregisterHotKey(IntPtr hWnd, int id);

        public HotKeyWindow()
        {
            CreateHandle(new CreateParams());
        }

        public event MethodInvoker Pressed;

        private bool _isRegistered;

        protected override void WndProc(ref Message m)
        {
            if (m.Msg == WM_HOTKEY &&
                _isRegistered &&
                m.WParam.ToInt32() == HotKeyId)
            {
                Pressed?.Invoke();
            }

            base.WndProc(ref m);
        }

        public void Register(KeyModifiers modifiers, Keys key)
        {
            if (_isRegistered)
            {
                Unregister();
            }
            RegisterHotKey(Handle, HotKeyId, modifiers, key);
            _isRegistered = true;
        }

        public void Unregister()
        {
            if (_isRegistered)
            {
                UnregisterHotKey(Handle, HotKeyId);
                _isRegistered = false;
            }
        }
    }
}
