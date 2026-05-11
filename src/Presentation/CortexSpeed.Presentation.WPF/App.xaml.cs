using CortexSpeed.Application;
using CortexSpeed.Infrastructure;
using CortexSpeed.Presentation.WPF.ViewModels;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using System.Windows;

namespace CortexSpeed.Presentation.WPF;

public partial class App : System.Windows.Application
{
    public static IHost? AppHost { get; private set; }

    public App()
    {
        AppHost = Host.CreateDefaultBuilder()
            .ConfigureServices((context, services) =>
            {
                // Register Core Application and Infrastructure Layers
                services.AddApplication();
                services.AddInfrastructure();

                // Register Presentation Views and ViewModels
                services.AddSingleton<MainWindow>();
                services.AddSingleton<MainViewModel>();
            })
            .Build();
    }

    private async void OnStartup(object sender, StartupEventArgs e)
    {
        await AppHost!.StartAsync();

        var mainWindow = AppHost.Services.GetRequiredService<MainWindow>();
        var viewModel  = AppHost.Services.GetRequiredService<MainViewModel>();

        // Wire the browser-extension callback → opens the Add Download dialog
        CortexSpeed.Infrastructure.BrowserExtensions.LocalHttpServer.ShowDownloadDialog =
            (url, filename) => viewModel.OpenAddDialogFromBrowser(url, filename);

        mainWindow.Show();
    }

    private async void OnExit(object sender, ExitEventArgs e)
    {
        await AppHost!.StopAsync();
        AppHost.Dispose();
    }
}
