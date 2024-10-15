using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Navigation;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Foundation.Collections;
using System.Runtime.InteropServices;
using Windows.Media.Core;
using WinRT.Interop;  // For WindowNative.GetWindowHandle

using Microsoft.UI.Windowing; // For AppWindow and FullScreen API
using Windows.Graphics.Display; // For screen resolution and full screen

using System.Diagnostics;
using Windows.Media.Playback;
using Windows.System.Display;
using System.Threading.Tasks;

using Microsoft.UI.Dispatching; // Add this line


// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace App2
{
    public sealed partial class MainWindow : Window
    {
        private int vidInd = 0;

        private bool isDragging = false;
        private bool isFullScreen = false;
        private DateTime lastClickTime = DateTime.MinValue;
        private const int DoubleClickThreshold = 500; // Milliseconds between clicks
        private Windows.Foundation.Point lastPointerPosition;
        private AppWindow appWindow; // Used for full-screen management

        private DisplayRequest appDisplayRequest = null; // Declare here
        private DispatcherQueue dispatcherQueue;
        public MainWindow()
        {
            this.InitializeComponent();
            //mediaPlayerElement.MediaPlayer.PlaybackSession.BufferingStarted += MediaPlaybackSession_BufferingStarted;
            //mediaPlayerElement.MediaPlayer.PlaybackSession.BufferingEnded += MediaPlaybackSession_BufferingEnded;
            //mediaPlayerElement.MediaPlayer.PlaybackSession.PlaybackStateChanged += PlaybackSession_PlaybackStateChanged;

            dispatcherQueue = DispatcherQueue.GetForCurrentThread();

            // Make window borderless (removing close, minimize, maximize buttons, and title bar)
            var hwnd = WindowNative.GetWindowHandle(this);
            SetWindowLong(hwnd, GWL_STYLE, GetWindowLong(hwnd, GWL_STYLE) & ~(WS_SYSMENU | WS_CAPTION | WS_THICKFRAME));

            // Remove title bar
            AppWindow.TitleBar.ExtendsContentIntoTitleBar = true;

            // Handle window dragging
            this.mediaPlayerElement.PointerPressed += MediaPlayerElement_PointerPressed;
            this.mediaPlayerElement.PointerMoved += MediaPlayerElement_PointerMoved;
            this.mediaPlayerElement.PointerReleased += MediaPlayerElement_PointerReleased;

            // Handle double click
            this.mediaPlayerElement.DoubleTapped += MediaPlayerElement_DoubleTapped;

            // Add KeyDown event handler for MainGrid
            MainGrid.KeyDown += MainGrid_KeyDown;
            MainGrid.Focus(FocusState.Programmatic); // Set focus to the Grid to ensure it receives key events

            // Assuming mediaPlayerElement is your MediaPlayerElement instance
            mediaPlayerElement.MediaPlayer.RealTimePlayback = true;

            // Play video automatically and make it fill the window
            mediaPlayerElement.Source = MediaSource.CreateFromUri(new Uri("C:/Users/abarb/Documents/health/news_underground/mediaSorter/media/video/virtual/i-m-just-here-for-vacation-white-aphy3d-4k_1080p.mp4"));
            

            mediaPlayerElement.MediaPlayer.Play();

            // Initialize AppWindow for full screen control
            appWindow = GetAppWindowForCurrentWindow();
        }


        public void ProcessArguments(string[] args)
        {
            string mediaClass = "";
            if (args.Length >= 3)
            {
                mediaClass = args[1];
                if (int.TryParse(args[2], out int intArg))
                {
                    vidInd = intArg;
                }
            }
        }




        private void PlaybackSession_PlaybackStateChanged(MediaPlaybackSession sender, object args)
        {
            DispatcherQueue.TryEnqueue(() =>
            {
                if (sender.PlaybackState == MediaPlaybackState.Playing)
                {
                    if (appDisplayRequest == null)
                    {
                        appDisplayRequest = new DisplayRequest();
                        appDisplayRequest.RequestActive();
                    }
                }
                else
                {
                    if (appDisplayRequest != null)
                    {
                        appDisplayRequest.RequestRelease();
                        appDisplayRequest = null;
                    }
                }
            });
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

        private void MainGrid_KeyDown(object sender, KeyRoutedEventArgs e)
        {
            // Handle the key input with a slight delay to prevent rapid state changes
            //await Task.Delay(100); // Adjust the delay as needed

            if (e.Key == Windows.System.VirtualKey.Escape)
            {
                Application.Current.Exit();
            }
            else if (e.Key == Windows.System.VirtualKey.Space)
            {
                // Toggle pause/play on the media player
                if (mediaPlayerElement.MediaPlayer.PlaybackSession.PlaybackState == Windows.Media.Playback.MediaPlaybackState.Playing)
                {
                    mediaPlayerElement.MediaPlayer.Pause();
                }
                else
                {
                    mediaPlayerElement.MediaPlayer.Play();
                }
            }
            else if (e.Key == Windows.System.VirtualKey.Left)
            {
                // Rewind the video by 5 seconds
                var session = mediaPlayerElement.MediaPlayer.PlaybackSession;
                session.Position = session.Position - TimeSpan.FromSeconds(5) < TimeSpan.Zero
                    ? TimeSpan.Zero
                    : session.Position - TimeSpan.FromSeconds(5);
            }
            else if (e.Key == Windows.System.VirtualKey.Right)
            {
                // Advance the video by 5 seconds
                var session = mediaPlayerElement.MediaPlayer.PlaybackSession;
                var newPosition = session.Position + TimeSpan.FromSeconds(5);
                var duration = session.NaturalDuration;

                session.Position = newPosition > duration
                    ? duration
                    : newPosition;
            }
        }

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
            var windowId = Microsoft.UI.Win32Interop.GetWindowIdFromWindow(windowHandle);
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