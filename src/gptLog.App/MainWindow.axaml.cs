using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using Avalonia.Platform.Storage;
using gptLog.App.Model;
using gptLog.App.ViewModels;
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
                    var listBox = this.FindControl<ListBox>("MessagesList");
                    if (listBox != null)
                    {
                        vm.MessagesListBox = listBox;
                    }
                };
            }
        }

        private void OnButtonClick(object? sender, RoutedEventArgs e)
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

        private void DragOver(object? sender, DragEventArgs e)
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

        private async void Drop(object? sender, DragEventArgs e)
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
                    }
                }
            }

            e.Handled = true;
        }

        private bool _isExitConfirmed = false;

        private async void MainWindow_Closing(object? sender, WindowClosingEventArgs e)
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
    }
}