using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using Avalonia.Platform.Storage;
using Avalonia.Threading; // Added for Dispatcher
using gptLog.App.Model;
using gptLog.App.ViewModels;
using Serilog;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace gptLog.App
{
    public partial class MainWindow : Window
    {
        private MainWindowViewModel? ViewModel => DataContext as MainWindowViewModel;

        public MainWindow()
        {
            InitializeComponent();
            DataContext = new MainWindowViewModel();

            Closing += MainWindow_Closing;
            AddHandler(Button.ClickEvent, OnButtonClick);

            // Set up drag and drop for file opening
            AddHandler(DragDrop.DragOverEvent, DragOver);
            AddHandler(DragDrop.DropEvent, Drop);

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

                // Set the MessagesListBox property in the ViewModel
                this.AttachedToVisualTree += (s, e) =>
                {
                    // Use a small delay to ensure the visual tree is fully constructed
                    Dispatcher.UIThread.Post(() =>
                    {
                        var listBox = this.FindControl<ListBox>("MessagesList");
                        if (listBox != null)
                        {
                            vm.MessagesListBox = listBox;
                            Log.Debug("MessagesList found and assigned to ViewModel");
                        }
                        else
                        {
                            Log.Warning("MessagesList not found in the visual tree");
                        }
                    });
                };
            }
        }

        private void OnButtonClick(object? sender, RoutedEventArgs e)
        {
            try
            {
                if (e.Source is Button button && button.Parent is Grid grid && grid.Parent is Border border)
                {
                    // Get the message from the DataContext of the Border
                    if (border.DataContext is Message message)
                    {
                        // Handle based on the button Tag
                        switch (button.Tag as string)
                        {
                            case "MoveUp":
                                if (ViewModel != null)
                                {
                                    // Select the message first
                                    ViewModel.SelectedMessage = message;
                                    ViewModel.MoveMessageUpCommand.Execute(null);
                                }
                                break;

                            case "MoveDown":
                                if (ViewModel != null)
                                {
                                    // Select the message first
                                    ViewModel.SelectedMessage = message;
                                    ViewModel.MoveMessageDownCommand.Execute(null);
                                }
                                break;

                            case "InsertUser":
                                ViewModel?.InsertUserMessageCommand.Execute(message);
                                break;

                            case "InsertAssistant":
                                ViewModel?.InsertAssistantMessageCommand.Execute(message);
                                break;

                            case "Delete":
                                if (ViewModel != null)
                                {
                                    // Select the message first
                                    ViewModel.SelectedMessage = message;
                                    ViewModel.DeleteMessageCommand.Execute(null);
                                }
                                break;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Error handling button click event");
            }
        }

        private void DragOver(object? sender, DragEventArgs e)
        {
            try
            {
                // Only accept drag if we don't have an open file
                if (ViewModel != null && !ViewModel.HasOpenFile)
                {
                    // Only accept files
                    if (e.Data.Contains(DataFormats.FileNames))
                    {
                        e.DragEffects = DragDropEffects.Copy;
                    }
                    else
                    {
                        e.DragEffects = DragDropEffects.None;
                    }
                }
                else
                {
                    e.DragEffects = DragDropEffects.None;
                }

                e.Handled = true;
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Error during drag over operation");
                e.DragEffects = DragDropEffects.None;
                e.Handled = true;
            }
        }

        private async void Drop(object? sender, DragEventArgs e)
        {
            try
            {
                // Only accept drop if we don't have an open file
                if (ViewModel != null && !ViewModel.HasOpenFile)
                {
                    if (e.Data.Contains(DataFormats.FileNames))
                    {
                        // Use the newer pattern to get data from drag drop
                        var fileNames = e.Data.GetFiles()?.Select(f => f.Path.LocalPath);
                        if (fileNames != null && fileNames.Any())
                        {
                            var filePath = fileNames.First();
                            if (filePath.EndsWith(".json", StringComparison.OrdinalIgnoreCase))
                            {
                                await ViewModel.LoadAsync(filePath);
                            }
                            else
                            {
                                Log.Warning("User attempted to drop non-JSON file: {FilePath}", filePath);
                            }
                        }
                    }
                }

                e.Handled = true;
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Error processing dropped file");
                e.Handled = true;

                if (ViewModel != null)
                {
                    await ViewModel.ShowDialogAsync("File Drop Error", "Could not process the dropped file. Please try again or use the Open button instead.", MainWindowViewModel.DialogType.Ok);
                }
            }
        }

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