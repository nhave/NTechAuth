using System;
using Microsoft.Extensions.DependencyInjection;
using NTechAuth.WebView.Components;
using Photino.Blazor;

namespace NTechAuth.WebView.Photino
{
    class Program
    {
        [STAThread]
        static void Main(string[] args)
        {
            var appBuilder = PhotinoBlazorAppBuilder.CreateDefault(args);

            appBuilder.Services
                .AddLogging();

            appBuilder.Services.AddSharedServices();

            // register root component and selector
            appBuilder.RootComponents.Add<Routes>("app");

            var app = appBuilder.Build();

            // customize window
            app.MainWindow
                .SetIconFile("favicon.ico")
                .SetTitle("NTech Authentication");

            app.AddWindowStateHandler();

            AppDomain.CurrentDomain.UnhandledException += (sender, error) =>
            {
                app.MainWindow.ShowMessage("Fatal exception", error.ExceptionObject.ToString());
            };

            app.Run();

        }
    }
}
