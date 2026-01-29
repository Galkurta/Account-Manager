using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Collections.Generic;
using System.Linq;

namespace RobloxAccountManager.Services
{
    public class WindowLayoutService
    {
        // Win32 Imports
        [DllImport("user32.dll")]
        private static extern bool EnumWindows(EnumWindowsProc enumProc, IntPtr lParam);

        [DllImport("user32.dll")]
        private static extern uint GetWindowThreadProcessId(IntPtr hWnd, out uint lpdwProcessId);

        [DllImport("user32.dll")]
        private static extern bool IsWindowVisible(IntPtr hWnd);

        [DllImport("user32.dll")]
        private static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);

        [DllImport("user32.dll")]
        private static extern bool MoveWindow(IntPtr hWnd, int X, int Y, int nWidth, int nHeight, bool bRepaint);

        [DllImport("user32.dll")]
        private static extern IntPtr GetForegroundWindow();

        [DllImport("user32.dll")]
        private static extern bool SetForegroundWindow(IntPtr hWnd);

        private delegate bool EnumWindowsProc(IntPtr hWnd, IntPtr lParam);

        private const int SW_MINIMIZE = 6;
        private const int SW_RESTORE = 9;

        public void TileWindows()
        {
            var windows = GetRobloxWindows();
            if (windows.Count == 0) return;

            int count = windows.Count;
            int cols = (int)Math.Ceiling(Math.Sqrt(count));
            int rows = (int)Math.Ceiling((double)count / cols);

            // Get screen bounds (primary screen for now)
            double screenWidth = System.Windows.SystemParameters.WorkArea.Width;
            double screenHeight = System.Windows.SystemParameters.WorkArea.Height;

            int width = (int)(screenWidth / cols);
            int height = (int)(screenHeight / rows);

            for (int i = 0; i < count; i++)
            {
                int row = i / cols;
                int col = i % cols;

                int x = col * width;
                int y = row * height;

                MoveWindow(windows[i], x, y, width, height, true);
                ShowWindow(windows[i], SW_RESTORE); // Restore if minimized
            }
        }

        public void CascadeWindows()
        {
            var windows = GetRobloxWindows();
            if (windows.Count == 0) return;

            int x = 50;
            int y = 50;
            int width = 800;
            int height = 600;
            int offset = 30;

            for (int i = 0; i < windows.Count; i++)
            {
                MoveWindow(windows[i], x + (i * offset), y + (i * offset), width, height, true);
                ShowWindow(windows[i], SW_RESTORE);
                SetForegroundWindow(windows[i]); // Bring to front
            }
        }

        public void MinimizeAll()
        {
            var windows = GetRobloxWindows();
            foreach (var hWnd in windows)
            {
                ShowWindow(hWnd, SW_MINIMIZE);
            }
        }

        private List<IntPtr> GetRobloxWindows()
        {
            var handles = new List<IntPtr>();
            var robloxProcesses = Process.GetProcessesByName("RobloxPlayerBeta");
            var pids = new HashSet<int>(robloxProcesses.Select(p => p.Id));

            EnumWindows(delegate (IntPtr hWnd, IntPtr lParam)
            {
                if (IsWindowVisible(hWnd))
                {
                    GetWindowThreadProcessId(hWnd, out uint processId);
                    if (pids.Contains((int)processId))
                    {
                        handles.Add(hWnd);
                    }
                }
                return true;
            }, IntPtr.Zero);

            return handles;
        }
    }
}
