using Photino.Blazor;
using System.IO;
using System.Text.Json;
using Photino.NET;
using System;

namespace Microsoft.Extensions.DependencyInjection
{
    public static class WindowUtils
    {
        public static void AddWindowStateHandler(this PhotinoBlazorApp app)
        {
            WindowState windowState = LoadWindowState();
            if (windowState != null)
            {
                app.MainWindow.SetSize(windowState.Width, windowState.Height);
                app.MainWindow.SetMaximized(windowState.IsMaximized);
            }

            windowState = new WindowState();

            app.MainWindow.RegisterSizeChangedHandler((sender, e) =>
            {
                if (!app.MainWindow.Maximized)
                {
                    windowState.Width = app.MainWindow.Width;
                    windowState.Height = app.MainWindow.Height;
                }
                windowState.IsMaximized = app.MainWindow.Maximized;
            });

            app.MainWindow.RegisterWindowClosingHandler((sender, e) =>
            {
                SaveWindowState(app.MainWindow, windowState);
                return false;
            });
        }

        private static void SaveWindowState(PhotinoWindow window, WindowState windowState)
        {
            var json = JsonSerializer.Serialize(windowState);
            File.WriteAllText("windowState.json", json);
        }

        private static WindowState LoadWindowState()
        {
            if (File.Exists("windowState.json"))
            {
                var json = File.ReadAllText("windowState.json");
                return JsonSerializer.Deserialize<WindowState>(json);
            }
            return null;
        }
    }

    public class WindowState
    {
        public int Width { get; set; }
        public int Height { get; set; }
        public bool IsMaximized { get; set; }
    }
}
