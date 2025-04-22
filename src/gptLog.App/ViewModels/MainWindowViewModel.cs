using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Platform.Storage;
using Avalonia.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using gptLog.App.Model;

namespace gptLog.App.ViewModels
{
    public class MainWindowViewModel : ViewModelBase
    {
        private readonly DispatcherTimer _clipboardTimer;
        private string _clipboardText = string.Empty;
        private string _currentFilePath = string.Empty;
        private bool _isUnsaved;
        private bool _clearClipboardAfterPaste = true;
        private Message? _selectedMessage;
        private int _selectedIndex = -1;

        public MainWindowViewModel()
        {
            Messages = new ObservableCollection<Message>();

            // Initialize commands
            AddUserMessageCommand = new RelayCommand(AddUserMessage, CanAddMessage);
            AddAssistantMessageCommand = new RelayCommand(AddAssistantMessage, CanAddMessage);
            MoveMessageUpCommand = new RelayCommand(MoveMessageUp, CanMoveMessageUp);
            MoveMessageDownCommand = new RelayCommand(MoveMessageDown, CanMoveMessageDown);
            DeleteMessageCommand = new RelayCommand(DeleteMessage, CanDeleteMessage);
            SaveCommand = new AsyncRelayCommand(SaveAsync);
            InsertUserMessageCommand = new RelayCommand<int>(InsertUserMessage);
            InsertAssistantMessageCommand = new RelayCommand<int>(InsertAssistantMessage);

            // Initialize clipboard timer
            _clipboardTimer = new DispatcherTimer
            {
                Interval = TimeSpan.FromMilliseconds(500)
            };
            _clipboardTimer.Tick += ClipboardTimer_Tick;
            _clipboardTimer.Start();
        }

        public ObservableCollection<Message> Messages { get; }

        public string ClipboardText
        {
            get => _clipboardText;
            set => SetProperty(ref _clipboardText, value);
        }

        public string CurrentFilePath
        {
            get => _currentFilePath;
            set => SetProperty(ref _currentFilePath, value);
        }

        public bool IsUnsaved
        {
            get => _isUnsaved;
            set => SetProperty(ref _isUnsaved, value);
        }

        public bool ClearClipboardAfterPaste
        {
            get => _clearClipboardAfterPaste;
            set => SetProperty(ref _clearClipboardAfterPaste, value);
        }

        public Message? SelectedMessage
        {
            get => _selectedMessage;
            set => SetProperty(ref _selectedMessage, value);
        }

        public int SelectedIndex
        {
            get => _selectedIndex;
            set => SetProperty(ref _selectedIndex, value);
        }

        public IRelayCommand AddUserMessageCommand { get; }
        public IRelayCommand AddAssistantMessageCommand { get; }
        public IRelayCommand MoveMessageUpCommand { get; }
        public IRelayCommand MoveMessageDownCommand { get; }
        public IRelayCommand DeleteMessageCommand { get; }
        public IAsyncRelayCommand SaveCommand { get; }
        public IRelayCommand<int> InsertUserMessageCommand { get; }
        public IRelayCommand<int> InsertAssistantMessageCommand { get; }

        private async void ClipboardTimer_Tick(object? sender, EventArgs e)
        {
            try
            {
                if (Application.Current?.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
                {
                    var mainWindow = desktop.MainWindow;
                    var clipboard = mainWindow?.Clipboard;
                    if (clipboard != null)
                    {
                        var text = await clipboard.GetTextAsync();
                        if (!string.IsNullOrWhiteSpace(text) && text.Trim() != ClipboardText.Trim())
                        {
                            ClipboardText = text.Trim();
                        }
                    }
                }
            }
            catch (Exception)
            {
                // Ignore clipboard access errors
            }
        }

        private bool CanAddMessage() => !string.IsNullOrWhiteSpace(ClipboardText);

        private void AddUserMessage()
        {
            AddMessage(Role.User);
        }

        private void AddAssistantMessage()
        {
            AddMessage(Role.Assistant);
        }

        private void AddMessage(Role role)
        {
            if (!CanAddMessage())
                return;

            Messages.Add(new Message
            {
                Role = role,
                Text = ClipboardText
            });

            IsUnsaved = true;

            if (ClearClipboardAfterPaste)
            {
                ClearClipboard();
            }
        }

        private async void ClearClipboard()
        {
            try
            {
                if (Application.Current?.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
                {
                    var mainWindow = desktop.MainWindow;
                    var clipboard = mainWindow?.Clipboard;
                    if (clipboard != null)
                    {
                        await clipboard.ClearAsync();
                        ClipboardText = string.Empty;
                    }
                }
            }
            catch (Exception)
            {
                // Ignore clipboard access errors
            }
        }

        private bool CanMoveMessageUp() => SelectedIndex > 0;

        private void MoveMessageUp()
        {
            if (!CanMoveMessageUp())
                return;

            var index = SelectedIndex;
            var message = Messages[index];
            Messages.RemoveAt(index);
            Messages.Insert(index - 1, message);
            SelectedIndex = index - 1;
            IsUnsaved = true;
        }

        private bool CanMoveMessageDown() => SelectedIndex >= 0 && SelectedIndex < Messages.Count - 1;

        private void MoveMessageDown()
        {
            if (!CanMoveMessageDown())
                return;

            var index = SelectedIndex;
            var message = Messages[index];
            Messages.RemoveAt(index);
            Messages.Insert(index + 1, message);
            SelectedIndex = index + 1;
            IsUnsaved = true;
        }

        private bool CanDeleteMessage() => SelectedIndex >= 0;

        private void DeleteMessage()
        {
            if (!CanDeleteMessage())
                return;

            var index = SelectedIndex;
            Messages.RemoveAt(index);
            IsUnsaved = true;

            // Adjust selection
            if (Messages.Count > 0)
            {
                SelectedIndex = Math.Min(index, Messages.Count - 1);
            }
            else
            {
                SelectedIndex = -1;
            }
        }

        private void InsertUserMessage(int index)
        {
            if (index < 0 || index > Messages.Count || string.IsNullOrWhiteSpace(ClipboardText))
                return;

            Messages.Insert(index, new Message
            {
                Role = Role.User,
                Text = ClipboardText
            });

            IsUnsaved = true;

            if (ClearClipboardAfterPaste)
            {
                ClearClipboard();
            }
        }

        private void InsertAssistantMessage(int index)
        {
            if (index < 0 || index > Messages.Count || string.IsNullOrWhiteSpace(ClipboardText))
                return;

            Messages.Insert(index, new Message
            {
                Role = Role.Assistant,
                Text = ClipboardText
            });

            IsUnsaved = true;

            if (ClearClipboardAfterPaste)
            {
                ClearClipboard();
            }
        }

        public async Task SaveAsync()
        {
            if (string.IsNullOrEmpty(CurrentFilePath))
            {
                // Show save file dialog
                var result = string.Empty;
                if (Application.Current?.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop && desktop.MainWindow != null)
                {
                    var dialog = new SaveFileDialog
                    {
                        Title = "Save gptLog File",
                        Filters = new List<FileDialogFilter>
                        {
                            new FileDialogFilter { Name = "JSON Files", Extensions = new List<string> { "json" } }
                        },
                        DefaultExtension = "json"
                    };

                    result = await dialog.ShowAsync(desktop.MainWindow);
                }

                if (string.IsNullOrEmpty(result))
                    return;

                CurrentFilePath = result;
            }

            try
            {
                await JsonHelper.SaveMessagesToFileAsync(Messages, CurrentFilePath);
                IsUnsaved = false;
            }
            catch (Exception ex)
            {
                await ShowErrorDialog("Save Error", $"Failed to save file: {ex.Message}");
            }
        }

        public async Task LoadAsync(string filePath)
        {
            try
            {
                var messages = await JsonHelper.LoadMessagesFromFileAsync(filePath);
                Messages.Clear();
                foreach (var message in messages)
                {
                    Messages.Add(message);
                }
                CurrentFilePath = filePath;
                IsUnsaved = false;
            }
            catch (Exception ex)
            {
                await ShowErrorDialog("Load Error", $"Failed to load file: {ex.Message}");
            }
        }

        private async Task ShowErrorDialog(string title, string message)
        {
            if (Application.Current?.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop && desktop.MainWindow != null)
            {
                var messageBox = new Window
                {
                    Title = title,
                    Width = 400,
                    Height = 200,
                    WindowStartupLocation = WindowStartupLocation.CenterOwner,
                    Content = new StackPanel
                    {
                        Margin = new Thickness(20),
                        Children =
                        {
                            new TextBlock { Text = message, TextWrapping = TextWrapping.Wrap },
                            new Button { Content = "OK", HorizontalAlignment = HorizontalAlignment.Center, Margin = new Thickness(0, 20, 0, 0) }
                        }
                    }
                };

                var button = ((StackPanel)messageBox.Content).Children[1] as Button;
                if (button != null)
                {
                    button.Click += (s, e) => messageBox.Close();
                }

                await messageBox.ShowDialog(desktop.MainWindow);
            }
        }

        public async Task<bool> ConfirmExitAsync()
        {
            if (!IsUnsaved)
                return true;

            var result = false;

            if (Application.Current?.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop && desktop.MainWindow != null)
            {
                var tcs = new TaskCompletionSource<bool>();

                var messageBox = new Window
                {
                    Title = "Unsaved Changes",
                    Width = 400,
                    Height = 200,
                    WindowStartupLocation = WindowStartupLocation.CenterOwner,
                    Content = new StackPanel
                    {
                        Margin = new Thickness(20),
                        Children =
                        {
                            new TextBlock { Text = "You have unsaved changes. Exit anyway?", TextWrapping = TextWrapping.Wrap },
                            new StackPanel
                            {
                                Orientation = Orientation.Horizontal,
                                HorizontalAlignment = HorizontalAlignment.Center,
                                Margin = new Thickness(0, 20, 0, 0),
                                Spacing = 10,
                                Children =
                                {
                                    new Button { Content = "Yes" },
                                    new Button { Content = "No" }
                                }
                            }
                        }
                    }
                };

                var buttonPanel = ((StackPanel)messageBox.Content).Children[1] as StackPanel;
                if (buttonPanel != null)
                {
                    var yesButton = buttonPanel.Children[0] as Button;
                    var noButton = buttonPanel.Children[1] as Button;

                    if (yesButton != null && noButton != null)
                    {
                        yesButton.Click += (s, e) =>
                        {
                            tcs.SetResult(true);
                            messageBox.Close();
                        };

                        noButton.Click += (s, e) =>
                        {
                            tcs.SetResult(false);
                            messageBox.Close();
                        };
                    }
                }

                await messageBox.ShowDialog(desktop.MainWindow);
                result = await tcs.Task;
            }

            return result;
        }
    }
}