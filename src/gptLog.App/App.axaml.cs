using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using gptLog.App.ViewModels;
using Serilog;
using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace gptLog.App
{
    public partial class App : Application
    {
        public override void Initialize()
        {
            AvaloniaXamlLoader.Load(this);
        }

        public override void OnFrameworkInitializationCompleted()
        {
            if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
            {
                var viewModel = new MainWindowViewModel();

                desktop.MainWindow = new MainWindow
                {
                    DataContext = viewModel
                };

                // Handle command line arguments
                var args = Environment.GetCommandLineArgs();
                if (args.Length > 1)
                {
                    var filePath = args[1];
                    if (File.Exists(filePath) && filePath.EndsWith(".json", StringComparison.OrdinalIgnoreCase))
                    {
                        Log.Information("Loading file from command line: {FilePath}", filePath);

                        // Load the file after the UI is initialized
                        desktop.Startup += async (sender, e) =>
                        {
                            await Task.Delay(100); // Small delay to ensure UI is ready
                            await viewModel.LoadAsync(filePath);
                        };
                    }
                }
            }

            base.OnFrameworkInitializationCompleted();
        }
    }
}