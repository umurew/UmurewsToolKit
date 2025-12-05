using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Navigation;
using Microsoft.UI.Xaml.Shapes;
using Windows.ApplicationModel;
using Windows.ApplicationModel.Activation;
using Windows.Foundation;
using Windows.Foundation.Collections;

namespace TaskManager
{
    public partial class App : Application
    {
        private Window? _window;

        public App()
        {
            InitializeComponent();
        }

        protected override void OnLaunched(Microsoft.UI.Xaml.LaunchActivatedEventArgs args)
        {
            _window = new MainWindow();

            SetWindowDetails(WinRT.Interop.WindowNative.GetWindowHandle(_window), 800, 600);

            _window.Activate();
        }

        private static void SetWindowDetails(IntPtr hwnd, int width, int height)
        {
            var dpi = Windows.Win32.PInvoke.GetDpiForWindow((Windows.Win32.Foundation.HWND)hwnd);
            float scalingFactor = (float)dpi / 96;
            width = (int)(width * scalingFactor);
            height = (int)(height * scalingFactor);

            _ = Windows.Win32.PInvoke.SetWindowPos((Windows.Win32.Foundation.HWND)hwnd,
                                        Windows.Win32.Foundation.HWND.HWND_TOP,
                                        0, 0, width, height,
                                        Windows.Win32.UI.WindowsAndMessaging.SET_WINDOW_POS_FLAGS.SWP_NOMOVE);

            var nIndex = Windows.Win32.PInvoke.GetWindowLong((Windows.Win32.Foundation.HWND)hwnd,
                      Windows.Win32.UI.WindowsAndMessaging.WINDOW_LONG_PTR_INDEX.GWL_STYLE) &
                      ~(int)Windows.Win32.UI.WindowsAndMessaging.WINDOW_STYLE.WS_MINIMIZEBOX &
                      ~(int)Windows.Win32.UI.WindowsAndMessaging.WINDOW_STYLE.WS_MAXIMIZEBOX;

            _ = Windows.Win32.PInvoke.SetWindowLong((Windows.Win32.Foundation.HWND)hwnd,
                   Windows.Win32.UI.WindowsAndMessaging.WINDOW_LONG_PTR_INDEX.GWL_STYLE,
                   nIndex);
        }
    }
}
