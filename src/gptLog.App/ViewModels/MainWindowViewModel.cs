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
using Serilog;

namespace gptLog.App.ViewModels
{
    public class MainWindowViewModel : ViewModelBase
    {
        private readonly DispatcherTimer _clipboardTimer;
        private string _clipboardText = string.Empty;
        private string _currentFilePath = string.Empty;
        private string _conversationTitle = string.Empty;
        private bool _isUnsaved;
        private bool _clearClipboardAfterPaste = true;
        private bool _stayOnTop = false;
        private Message? _selectedMessage;
        private int _selectedIndex = -1;
        private ListBox? _messagesListBox;

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
            OpenCommand = new AsyncRelayCommand(OpenAsync);
            ExitCommand = new AsyncRelayCommand(ExitAsync);

            // These commands were updated to accept Message parameters
            InsertUserMessageCommand = new RelayCommand<Message>(InsertUserMessage);
            InsertAssistantMessageCommand = new RelayCommand<Message>(InsertAssistantMessage);

            // Initialize clipboard timer
            _clipboardTimer = new DispatcherTimer
            {
                Interval = TimeSpan.FromMilliseconds(500)
            };
            _clipboardTimer.Tick += ClipboardTimer_Tick;
            _clipboardTimer.Start();

            // Initialize the ConfiguredFontFamily property
            ConfiguredFontFamily = new FontFamily(App.Settings?.FontFamily ?? ApplicationDefaults.DefaultFontFamily);
        }

        public ObservableCollection<Message> Messages { get; }

        public string ClipboardText
        {
            get => _clipboardText;
            set
            {
                if (SetProperty(ref _clipboardText, value))
                {
                    // Notify that CanAddClipboardText may have changed
                    OnPropertyChanged(nameof(CanAddClipboardText));

                    // Also notify commands that they may need to reevaluate if they can execute
                    AddUserMessageCommand.NotifyCanExecuteChanged();
                    AddAssistantMessageCommand.NotifyCanExecuteChanged();
                }
            }
        }

        public string CurrentFilePath
        {
            get => _currentFilePath;
            set
            {
                if (SetProperty(ref _currentFilePath, value))
                {
                    // Notify that HasOpenFile may have changed
                    OnPropertyChanged(nameof(HasOpenFile));
                }
            }
        }

        public string ConversationTitle
        {
            get => _conversationTitle;
            set
            {
                if (SetProperty(ref _conversationTitle, value))
                {
                    IsUnsaved = true;
                }
            }
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

        public bool StayOnTop
        {
            get => _stayOnTop;
            set
            {
                if (SetProperty(ref _stayOnTop, value))
                {
                    // Ensure the main window gets updated
                    if (Application.Current?.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
                    {
                        if (desktop.MainWindow != null)
                        {
                            desktop.MainWindow.Topmost = value;
                        }
                    }
                }
            }
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

        public bool HasOpenFile => !string.IsNullOrEmpty(CurrentFilePath);

        public bool CanAddClipboardText => !string.IsNullOrWhiteSpace(ClipboardText);

        public ListBox? MessagesListBox
        {
            get => _messagesListBox;
            set
            {
                if (SetProperty(ref _messagesListBox, value))
                {
                    // When the ListBox is set, ensure it's correctly assigned
                    // and scroll to the last message if there are any
                    if (_messagesListBox != null && Messages.Count > 0)
                    {
                        ScrollToMessage();
                    }
                }
            }
        }

        public IRelayCommand AddUserMessageCommand { get; }
        public IRelayCommand AddAssistantMessageCommand { get; }
        public IRelayCommand MoveMessageUpCommand { get; }
        public IRelayCommand MoveMessageDownCommand { get; }
        public IRelayCommand DeleteMessageCommand { get; }
        public IAsyncRelayCommand SaveCommand { get; }
        public IAsyncRelayCommand OpenCommand { get; }
        public IAsyncRelayCommand ExitCommand { get; }
        public IRelayCommand<Message> InsertUserMessageCommand { get; }
        public IRelayCommand<Message> InsertAssistantMessageCommand { get; }

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
            catch (Exception ex)
            {
                // Log clipboard access errors but don't disrupt the UI
                Log.Warning(ex, "Error accessing clipboard");

                // Temporarily pause clipboard checking if there's a persistent error
                _clipboardTimer.Stop();

                // Restart after a short delay to avoid error spam
                await Task.Delay(2000);
                _clipboardTimer.Start();
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
            // The button should be disabled if clipboard is empty, so we don't need to check
            var message = new Message
            {
                Role = role,
                Text = ClipboardText
            };

            Messages.Add(message);
            IsUnsaved = true;

            // Scroll to the newly added message
            ScrollToMessage();

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
                        Log.Debug("Clipboard cleared successfully");
                    }
                }
            }
            catch (Exception ex)
            {
                // Log but don't throw - clipboard clearing is non-critical
                Log.Warning(ex, "Failed to clear clipboard");

                // Try to set the clipboard text to empty string directly
                try {
                    ClipboardText = string.Empty;
                }
                catch (Exception innerEx) {
                    Log.Error(innerEx, "Failed to reset clipboard text property");
                }
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

        private async void DeleteMessage()
        {
            if (!CanDeleteMessage())
                return;

            var index = SelectedIndex;
            var message = Messages[index];

            try
            {
                // Get a preview of the message using our utility method
                var preview = Message.TrimMessageText(message.Text);

                // Show confirmation dialog with preview
                var title = $"Delete {message.Role} Message";
                var confirmMessage = $"Are you sure you want to delete this message?\n\n{message.Role}: {preview}";
                var shouldDelete = await ShowDialogAsync(title, confirmMessage, DialogType.YesNo);

                if (!shouldDelete)
                    return;

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
            catch (Exception ex)
            {
                Log.Error(ex, "Error deleting message at index {Index}", index);
                await ShowErrorDialog("Delete Error", "An error occurred while deleting the message.");
            }
        }

        private void InsertUserMessage(Message? message)
        {
            if (message == null || string.IsNullOrWhiteSpace(ClipboardText))
                return;

            var index = Messages.IndexOf(message);
            if (index < 0)
                return;

            var newMessage = new Message
            {
                Role = Role.User,
                Text = ClipboardText
            };

            Messages.Insert(index, newMessage);
            SelectedIndex = index;
            SelectedMessage = newMessage;
            IsUnsaved = true;

            // Scroll to the newly inserted message
            ScrollToMessage(newMessage);

            if (ClearClipboardAfterPaste)
            {
                ClearClipboard();
            }
        }

        private void InsertAssistantMessage(Message? message)
        {
            if (message == null || string.IsNullOrWhiteSpace(ClipboardText))
                return;

            var index = Messages.IndexOf(message);
            if (index < 0)
                return;

            var newMessage = new Message
            {
                Role = Role.Assistant,
                Text = ClipboardText
            };

            Messages.Insert(index, newMessage);
            SelectedIndex = index;
            SelectedMessage = newMessage;
            IsUnsaved = true;

            // Scroll to the newly inserted message
            ScrollToMessage(newMessage);

            if (ClearClipboardAfterPaste)
            {
                ClearClipboard();
            }
        }

        public async Task SaveAsync()
        {
            if (string.IsNullOrEmpty(CurrentFilePath))
            {
                // Show save file dialog using the newer API
                var result = string.Empty;
                if (Application.Current?.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop && desktop.MainWindow != null)
                {
                    var storageProvider = desktop.MainWindow.StorageProvider;
                    var fileTypes = new FilePickerFileType("JSON Files")
                    {
                        Patterns = new[] { "*.json" },
                        MimeTypes = new[] { "application/json" }
                    };

                    var options = new FilePickerSaveOptions
                    {
                        Title = "Save gptLog File",
                        FileTypeChoices = new[] { fileTypes },
                        DefaultExtension = "json",
                        SuggestedFileName = SuggestedFileName()
                    };

                    var file = await storageProvider.SaveFilePickerAsync(options);
                    if (file != null)
                    {
                        result = file.Path.LocalPath;
                    }
                }

                if (string.IsNullOrEmpty(result))
                    return;

                CurrentFilePath = result;
            }

            try
            {
                Log.Information("Saving messages to file: {FilePath}", CurrentFilePath);
                // Pass the conversation title to the save method
                await JsonHelper.SaveMessagesToFileAsync(Messages, CurrentFilePath, ConversationTitle);
                IsUnsaved = false;
                Log.Information("Successfully saved {Count} messages", Messages.Count);
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Failed to save file: {FilePath}", CurrentFilePath);
                await ShowErrorDialog("Save Error", $"Failed to save file: {ex.Message}");
            }
        }

        public async Task LoadAsync(string filePath)
        {
            try
            {
                Log.Information("Loading messages from file: {FilePath}", filePath);
                var (messages, title) = await JsonHelper.LoadMessagesFromFileAsync(filePath);
                Messages.Clear();
                foreach (var message in messages)
                {
                    Messages.Add(message);
                }

                // Set the conversation title from metadata, defaulting to string.Empty if null
                ConversationTitle = title ?? string.Empty;

                CurrentFilePath = filePath;
                IsUnsaved = false;
                Log.Information("Successfully loaded {Count} messages", messages.Count);
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Failed to load file: {FilePath}", filePath);
                await ShowErrorDialog("Load Error", $"Failed to load file: {ex.Message}");
            }
        }

        private async Task ShowErrorDialog(string title, string message)
        {
            await ShowDialogAsync(title, message, DialogType.Ok);
        }

        public async Task<bool> ConfirmExitAsync()
        {
            if (!IsUnsaved)
                return true;

            return await ShowDialogAsync("Unsaved Changes", "You have unsaved changes. Exit anyway?", DialogType.YesNo);
        }

        public async Task OpenAsync()
        {
            if (IsUnsaved)
            {
                var shouldContinue = await ConfirmExitAsync();
                if (!shouldContinue)
                    return;
            }

            try
            {
                // Show open file dialog
                if (Application.Current?.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop && desktop.MainWindow != null)
                {
                    var storageProvider = desktop.MainWindow.StorageProvider;
                    var fileTypes = new FilePickerFileType("JSON Files")
                    {
                        Patterns = new[] { "*.json" },
                        MimeTypes = new[] { "application/json" }
                    };

                    var options = new FilePickerOpenOptions
                    {
                        Title = "Open gptLog File",
                        FileTypeFilter = new[] { fileTypes },
                        AllowMultiple = false
                    };

                    var files = await storageProvider.OpenFilePickerAsync(options);
                    if (files.Count > 0)
                    {
                        await LoadAsync(files[0].Path.LocalPath);
                    }
                }
            }
            catch (Exception ex)
            {
                await ShowErrorDialog("Open Error", $"Failed to open file: {ex.Message}");
            }
        }

        public async Task ExitAsync()
        {
            if (IsUnsaved)
            {
                var shouldExit = await ConfirmExitAsync();
                if (!shouldExit)
                    return;
            }

            if (Application.Current?.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
            {
                desktop.Shutdown();
            }
        }

        // Simplified scrolling helper method that handles all scrolling scenarios
        private void ScrollToMessage(Message? message = null)
        {
            if (_messagesListBox == null || Messages.Count == 0)
            {
                Log.Debug("Cannot scroll: MessagesListBox is null or Messages collection is empty");
                return;
            }

            // If no specific message is provided, scroll to the last message
            if (message == null)
            {
                Log.Debug("Scrolling to last message");
                _messagesListBox.ScrollIntoView(Messages[Messages.Count - 1]);
            }
            // Otherwise scroll to the specific message
            else
            {
                Log.Debug("Scrolling to specific message: {Role}: {Preview}", message.Role, message.PreviewText);
                _messagesListBox.ScrollIntoView(message);
            }
        }

        // Helper method to generate suggested filename from title
        private string SuggestedFileName()
        {
            if (string.IsNullOrWhiteSpace(ConversationTitle))
                return "conversation.json";

            // Convert to lowercase
            var fileName = ConversationTitle.ToLowerInvariant();

            // Replace all sequences of whitespace and symbols with a single hyphen
            fileName = System.Text.RegularExpressions.Regex.Replace(fileName, @"[\s\W]+", "-");

            // Trim any leading/trailing hyphens
            fileName = fileName.Trim('-');

            // If nothing remains after processing, fallback to default
            if (string.IsNullOrEmpty(fileName))
                return "conversation.json";

            return fileName + ".json";
        }

        public async Task<bool> ShowDialogAsync(string title, string message, DialogType dialogType = DialogType.Ok)
        {
            if (string.IsNullOrEmpty(title))
                title = "Information";

            if (string.IsNullOrEmpty(message))
                message = "No additional information provided.";

            try
            {
                if (Application.Current?.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop && desktop.MainWindow != null)
                {
                    var tcs = new TaskCompletionSource<bool>();

                    try
                    {
                        var messageBox = new Window
                        {
                            Title = title,
                            MaxWidth = 600,
                            SizeToContent = SizeToContent.WidthAndHeight,
                            WindowStartupLocation = WindowStartupLocation.CenterOwner,
                            Content = new StackPanel
                            {
                                Margin = new Thickness(20),
                                Children =
                                {
                                    new TextBlock
                                    {
                                        Text = message,
                                        TextWrapping = TextWrapping.Wrap,
                                        MaxWidth = 560
                                    }
                                }
                            }
                        };

                        // Apply font settings using the App helper method
                        App.ApplyFontSettingsToWindow(messageBox);

                        var stackPanel = (StackPanel)messageBox.Content;

                        // Create button panel based on dialog type
                        if (dialogType == DialogType.YesNo)
                        {
                            var buttonPanel = new StackPanel
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
                            };

                            stackPanel.Children.Add(buttonPanel);

                            var yesButton = buttonPanel.Children[0] as Button;
                            var noButton = buttonPanel.Children[1] as Button;

                            if (yesButton != null && noButton != null)
                            {
                                yesButton.Click += (s, e) =>
                                {
                                    try
                                    {
                                        tcs.SetResult(true);
                                        messageBox.Close();
                                    }
                                    catch (Exception ex)
                                    {
                                        Log.Error(ex, "Error handling Yes button click");
                                        tcs.TrySetResult(false);
                                    }
                                };

                                noButton.Click += (s, e) =>
                                {
                                    try
                                    {
                                        tcs.SetResult(false);
                                        messageBox.Close();
                                    }
                                    catch (Exception ex)
                                    {
                                        Log.Error(ex, "Error handling No button click");
                                        tcs.TrySetResult(false);
                                    }
                                };
                            }
                            else
                            {
                                Log.Warning("Failed to get references to dialog buttons");
                            }
                        }
                        else // DialogType.Ok
                        {
                            var button = new Button { Content = "OK", HorizontalAlignment = HorizontalAlignment.Center, Margin = new Thickness(0, 20, 0, 0) };
                            stackPanel.Children.Add(button);

                            button.Click += (s, e) =>
                            {
                                try
                                {
                                    tcs.SetResult(true);
                                    messageBox.Close();
                                }
                                catch (Exception ex)
                                {
                                    Log.Error(ex, "Error handling OK button click");
                                    tcs.TrySetResult(true);
                                }
                            };
                        }

                        // Handle window closing without button press
                        messageBox.Closed += (s, e) =>
                        {
                            tcs.TrySetResult(false);
                        };

                        await messageBox.ShowDialog(desktop.MainWindow);
                        return await tcs.Task;
                    }
                    catch (Exception ex)
                    {
                        Log.Error(ex, "Error showing dialog window: {Title}", title);
                        tcs.TrySetResult(dialogType == DialogType.YesNo ? false : true);
                        return await tcs.Task;
                    }
                }
                else
                {
                    Log.Warning("Could not show dialog - no main window available");
                    return dialogType == DialogType.YesNo ? false : true; // Default to No for YesNo dialogs, Yes for Ok dialogs
                }
            }
            catch (Exception ex)
            {
                // Ultimate fallback for any errors in the dialog system
                Log.Error(ex, "Critical error in dialog system");
                return dialogType == DialogType.YesNo ? false : true;
            }
        }

        public enum DialogType
        {
            Ok,
            YesNo
        }

        // Property for configured font family from settings
        public FontFamily ConfiguredFontFamily { get; }

        // Property for configured font size from settings
        public double ConfiguredFontSize => App.Settings?.FontSize ?? ApplicationDefaults.DefaultFontSize;

        // Property for title font size from settings
        public int TitleFontSize => App.Settings?.TitleFontSize ?? ApplicationDefaults.DefaultTitleFontSize;
    }
}