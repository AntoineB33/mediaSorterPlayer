using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using System;
using System.Runtime.InteropServices;
using Windows.Foundation;
using Windows.Media.Core;
using WinRT.Interop; // For WindowNative.GetWindowHandle
using Microsoft.UI.Windowing; // For AppWindow and FullScreen API
using Windows.Graphics.Display; // For screen resolution and full screen

namespace App2
{
    public sealed partial class MainWindow : Window
    {
        private bool isDragging = false;
        private bool isFullScreen = false;
        private DateTime lastClickTime = DateTime.MinValue;
        private const int DoubleClickThreshold = 500; // Milliseconds between clicks
        private Windows.Foundation.Point lastPointerPosition;
        private AppWindow appWindow; // Used for full-screen management

        public MainWindow()
        {
            this.InitializeComponent();

            // Make window borderless (removing close, minimize, maximize buttons, and title bar)
            var hwnd = WindowNative.GetWindowHandle(this);
            SetWindowLong(hwnd, GWL_STYLE, GetWindowLong(hwnd, GWL_STYLE) & ~(WS_SYSMENU | WS_CAPTION | WS_THICKFRAME));

            // Remove title bar
            this.ExtendsContentIntoTitleBar = true;
            this.SetTitleBar(null); // No title bar

            // Handle window dragging
            this.mediaPlayerElement.PointerPressed += MediaPlayerElement_PointerPressed;
            this.mediaPlayerElement.PointerMoved += MediaPlayerElement_PointerMoved;
            this.mediaPlayerElement.PointerReleased += MediaPlayerElement_PointerReleased;

            // Handle double click
            this.mediaPlayerElement.DoubleTapped += MediaPlayerElement_DoubleTapped;

            // Play video automatically and make it fill the window
            mediaPlayerElement.Source = MediaSource.CreateFromUri(new Uri("C:/Users/abarb/Documents/health/news_underground/mediaSorter/media/video/virtual/qj0sopzku6kc1.mp4"));
            mediaPlayerElement.MediaPlayer.Play();

            // Initialize AppWindow for full screen control
            appWindow = GetAppWindowForCurrentWindow();
        }

        // Handle PointerPressed (start dragging)
        private void MediaPlayerElement_PointerPressed(object sender, PointerRoutedEventArgs e)
        {
            isDragging = true;
            lastPointerPosition = e.GetCurrentPoint(null).Position;
        }

        // Handle PointerMoved (dragging in progress)
        private void MediaPlayerElement_PointerMoved(object sender, PointerRoutedEventArgs e)
        {
            if (isDragging)
            {
                var currentPointerPosition = e.GetCurrentPoint(null).Position;
                var deltaX = currentPointerPosition.X - lastPointerPosition.X;
                var deltaY = currentPointerPosition.Y - lastPointerPosition.Y;

                MoveWindow((int)deltaX, (int)deltaY);

                lastPointerPosition = currentPointerPosition;
            }
        }

        // Handle PointerReleased (stop dragging)
        private void MediaPlayerElement_PointerReleased(object sender, PointerRoutedEventArgs e)
        {
            isDragging = false;
        }

        // Handle double click for toggling full-screen mode
        private void MediaPlayerElement_DoubleTapped(object sender, DoubleTappedRoutedEventArgs e)
        {
            ToggleFullScreenMode();
        }

        // Toggle between full screen and windowed mode
        private void ToggleFullScreenMode()
        {
            if (!isFullScreen)
            {
                // Enter full-screen mode
                appWindow.SetPresenter(AppWindowPresenterKind.FullScreen);
            }
            else
            {
                // Exit full-screen mode
                appWindow.SetPresenter(AppWindowPresenterKind.Default);
            }
            isFullScreen = !isFullScreen;
        }

        // Utility to get the AppWindow object for managing window properties
        private AppWindow GetAppWindowForCurrentWindow()
        {
            var windowHandle = WindowNative.GetWindowHandle(this);
            var windowId = Win32Interop.GetWindowIdFromWindow(windowHandle);
            return AppWindow.GetFromWindowId(windowId);
        }

        // Move the window using native interop
        private void MoveWindow(int deltaX, int deltaY)
        {
            IntPtr hwnd = WindowNative.GetWindowHandle(this);

            // Get the current window position
            RECT rect;
            if (Win32Interop.GetWindowRect(hwnd, out rect))
            {
                int width = rect.Right - rect.Left;
                int height = rect.Bottom - rect.Top;

                // Move the window based on delta
                Win32Interop.MoveWindow(hwnd, rect.Left + deltaX, rect.Top + deltaY, width, height, true);
            }
        }

        // Win32 API functions to make the window borderless
        [DllImport("user32.dll", SetLastError = true)]
        private static extern int GetWindowLong(IntPtr hWnd, int nIndex);

        [DllImport("user32.dll", SetLastError = true)]
        private static extern int SetWindowLong(IntPtr hWnd, int nIndex, int dwNewLong);

        private const int GWL_STYLE = -16;
        private const int WS_SYSMENU = 0x00080000;
        private const int WS_CAPTION = 0x00C00000;  // Removes the title bar
        private const int WS_THICKFRAME = 0x00040000;  // Removes the resizable border
    }

    // Define RECT struct for GetWindowRect
    [StructLayout(LayoutKind.Sequential)]
    public struct RECT
    {
        public int Left;
        public int Top;
        public int Right;
        public int Bottom;
    }

    public static class Win32Interop
    {
        [DllImport("user32.dll", SetLastError = true)]
        public static extern bool GetWindowRect(IntPtr hWnd, out RECT lpRect);

        [DllImport("user32.dll", SetLastError = true)]
        public static extern bool MoveWindow(IntPtr hWnd, int X, int Y, int nWidth, int nHeight, bool bRepaint);
    }
}
