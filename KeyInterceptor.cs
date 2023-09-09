// <copyright file="KeyInterceptor.cs">
//   CopyCopyDict - Background app that opens a dictionary definition of a selected word by Ctrl+C+C
//   (c) 2023 Artem Avramenko. https://github.com/ArtemAvramenko/CopyCopyDict
//   License: MIT
// </copyright>

using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Threading;
using System.Windows.Forms;

namespace CopyCopyDict
{
    class KeyInterceptor : IDisposable
    {
        private const int WH_KEYBOARD_LL = 13;

        private const int WM_KEYDOWN = 0x0100;

        [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        private static extern IntPtr SetWindowsHookEx(int idHook,
            LowLevelKeyboardProc lpfn, IntPtr hMod, uint dwThreadId);

        [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool UnhookWindowsHookEx(IntPtr hhk);

        [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        private static extern IntPtr CallNextHookEx(IntPtr hhk, int nCode,
            IntPtr wParam, IntPtr lParam);

        [DllImport("kernel32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        private static extern IntPtr GetModuleHandle(string lpModuleName);

        private delegate IntPtr LowLevelKeyboardProc(int nCode, IntPtr wParam, IntPtr lParam);

        private readonly LowLevelKeyboardProc _proc;

        private readonly AutoResetEvent _listenerEvent = new AutoResetEvent(false);

        private readonly HotKeyWindow _hotKeyWindow = new HotKeyWindow();

        private readonly IntPtr _hookID;

        private string _keySequence = "";

        private bool _isDisposed;

        public KeyInterceptor()
        {
            _hotKeyWindow.Pressed += () =>
            {
                ResetHotKey();
                _listenerEvent.Set();
            };

            var thread = new Thread(ListenerWorker);
            thread.SetApartmentState(ApartmentState.STA);
            thread.IsBackground = true;
            thread.Start();

            _proc = HookCallback;
            using (Process curProcess = Process.GetCurrentProcess())
            using (ProcessModule curModule = curProcess.MainModule)
            {
                _hookID = SetWindowsHookEx(
                    WH_KEYBOARD_LL,
                    _proc,
                    GetModuleHandle(curModule.ModuleName),
                    0);
            }
        }

        private IntPtr HookCallback(int nCode, IntPtr wParam, IntPtr lParam)
        {
            if (nCode >= 0)
            {
                if ((Control.ModifierKeys & Keys.Control) == 0)
                {
                    ResetHotKey();
                }
                else
                {
                    var key = (Keys)Marshal.ReadInt32(lParam);
                    var isKeyDown = wParam == (IntPtr)WM_KEYDOWN;
                    if (key == Keys.Insert)
                    {
                        _keySequence += isKeyDown ? "I" : "i";
                    }
                    else if (key == Keys.C)
                    {
                        _keySequence += isKeyDown ? "C" : "c";
                    }
                    else
                    {
                        ResetHotKey();
                    }

                    if (_keySequence == "Cc" || _keySequence == "Ii")
                    {
                        SetHotKey(key);
                    }
                }
            }

            return CallNextHookEx(_hookID, nCode, wParam, lParam);
        }

        private void SetHotKey(Keys key)
        {
            ResetHotKey();
            _hotKeyWindow.Register(KeyModifiers.Control, key);
        }

        private void ResetHotKey()
        {
            _keySequence = "";
            _hotKeyWindow.Unregister();
        }

        private void ListenerWorker()
        {
            while (!_isDisposed)
            {
                _listenerEvent.WaitOne();
                if (!_isDisposed && Pressed != null)
                {
                    Pressed();
                }
            }
        }

        public event MethodInvoker Pressed;

        public void Dispose()
        {
            if (!_isDisposed)
            {
                _isDisposed = true;
                UnhookWindowsHookEx(_hookID);
                _listenerEvent.Set();
            }
        }
    }
}
