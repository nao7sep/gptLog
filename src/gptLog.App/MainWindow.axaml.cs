using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using Avalonia.Threading; // Added for Dispatcher
using gptLog.App.Model;
using gptLog.App.Services;
using gptLog.App.ViewModels;
using Serilog;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace gptLog.App
{
    public partial class MainWindow : Window
    {
        private MainWindowViewModel? ViewModel => DataContext as MainWindowViewModel;
        private readonly DialogService _dialogService = new DialogService();

        public MainWindow()
        {
            InitializeComponent();
            // DataContext is set by App.axaml.cs when creating the window

            Closing += MainWindow_Closing;

            // Set up property change notification for StayOnTop
            if (DataContext is MainWindowViewModel vm)
            {
                vm.PropertyChanged += (sender, e) =>
                {
                    if (e.PropertyName == nameof(MainWindowViewModel.StayOnTop))
                    {
                        Topmost = vm.StayOnTop;
                    }
                };

                // Initialize Topmost property
                Topmost = vm.StayOnTop;

                // We'll set the MessagesListBox property in OnOpened
            }
        }

        // Drag and drop functionality has been removed

        private bool _isExitConfirmed = false;

        private async void MainWindow_Closing(object? sender, WindowClosingEventArgs e)
        {
            try
            {
                // If exit is already confirmed, allow the window to close
                if (_isExitConfirmed)
                    return;

                // If there are unsaved changes, show confirmation dialog
                if (ViewModel != null && ViewModel.IsUnsaved)
                {
                    e.Cancel = true;
                    var shouldExit = await ViewModel.ConfirmExitAsync();

                    if (shouldExit)
                    {
                        _isExitConfirmed = true;
                        Close();
                    }
                }
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Error during window closing");
                e.Cancel = true;
            }
        }

        // Method to manually refresh the ListBox reference
        private void RefreshMessagesListBoxReference()
        {
            try
            {
                if (DataContext is MainWindowViewModel vm)
                {
                    var listBox = this.FindControl<ListBox>("MessagesList");
                    if (listBox != null)
                    {
                        // Set the reference in the ViewModel
                        vm.MessagesListBox = listBox;
                        Log.Debug("MessagesList manually refreshed");
                    }
                    else
                    {
                        Log.Warning("Failed to find MessagesList control during refresh attempt");
                    }
                }
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Error refreshing MessagesList reference");
            }
        }

        protected override void OnOpened(EventArgs e)
        {
            try
            {
                base.OnOpened(e);

                // Ensure the reference is set once the window is fully opened
                Dispatcher.UIThread.Post(RefreshMessagesListBoxReference);
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Error in OnOpened event");
            }
        }
    }
}