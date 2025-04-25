using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using gptLog.App.ViewModels;
using gptLog.App.Model;
using Microsoft.Extensions.Configuration;
using Serilog;
using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace gptLog.App
{
    public partial class App : Application
    {
        // Add a static property to access the configuration anywhere in the app
        public static IConfiguration? Configuration { get; private set; }

        // Add a static property for AppSettings
        public static AppSettings? Settings { get; private set; }

        public override void Initialize()
        {
            // Initialize configuration
            InitializeConfiguration();

            AvaloniaXamlLoader.Load(this);
        }

        private void InitializeConfiguration()
        {
            try
            {
                var builder = new ConfigurationBuilder()
                    .SetBasePath(AppContext.BaseDirectory);

                string appSettingsPath = Path.Combine(AppContext.BaseDirectory, "appsettings.json");
                if (File.Exists(appSettingsPath))
                {
                    builder.AddJsonFile("appsettings.json", optional: true, reloadOnChange: true);
                    Log.Information("Configuration loaded from appsettings.json");
                }
                else
                {
                    Log.Information("appsettings.json not found, using default settings");
                }

                Configuration = builder.Build();

                // Load app settings from configuration
                Settings = new AppSettings();
                if (Configuration != null)
                {
                    Configuration.GetSection("AppSettings").Bind(Settings);
                }

                Log.Information("Application settings loaded successfully");
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Error loading configuration");
                // Use default settings if configuration loading fails
                Settings = new AppSettings();
            }
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

                // Apply font settings to the main window
                ApplyFontSettings(desktop.MainWindow);

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

        private void ApplyFontSettings(Avalonia.Controls.Window window)
        {
            try
            {
                // Apply font family and size from settings
                if (Settings != null)
                {
                    window.FontFamily = new Avalonia.Media.FontFamily(Settings.FontFamily);
                    window.FontSize = Settings.FontSize;
                    Log.Information("Font settings applied to window successfully");
                }
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Error applying font settings");
            }
        }

        /// <summary>
        /// Helper method to apply font settings to any window
        /// </summary>
        public static void ApplyFontSettingsToWindow(Window window)
        {
            if (window != null && Settings != null)
            {
                try
                {
                    window.FontFamily = new Avalonia.Media.FontFamily(Settings.FontFamily);
                    window.FontSize = Settings.FontSize;
                    Log.Debug("Font settings applied to window {WindowTitle}", window.Title);
                }
                catch (Exception ex)
                {
                    Log.Error(ex, "Error applying font settings to window {WindowTitle}", window.Title);
                }
            }
        }
    }
}