using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Media;
using Avalonia.Platform.Storage;
using Avalonia.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using gptLog.App.Model;
using gptLog.App.Services;
using Serilog;

namespace gptLog.App.ViewModels
{
    public class MainWindowViewModel : ViewModelBase
    {
        private readonly DispatcherTimer _clipboardTimer;
        private readonly DialogService _dialogService;
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
            Messages.CollectionChanged += Messages_CollectionChanged;
            _dialogService = new DialogService();

            // Initialize commands
            AddUserMessageCommand = new RelayCommand(AddUserMessage, CanAddMessage);
            AddAssistantMessageCommand = new RelayCommand(AddAssistantMessage, CanAddMessage);
            // Use lambda expressions that return the property values
            MoveMessageUpCommand = new RelayCommand(MoveMessageUp, () => SelectedIndex > 0);
            MoveMessageDownCommand = new RelayCommand(MoveMessageDown, () => SelectedIndex >= 0 && SelectedIndex < Messages.Count - 1);
            DeleteMessageCommand = new RelayCommand(DeleteMessage, () => SelectedIndex >= 0);
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
                    // Notify that clipboard-related properties may have changed
                    OnPropertyChanged(nameof(CanAddClipboardText));
                    OnPropertyChanged(nameof(CanInsertMessage));

                    // Also notify commands that they may need to reevaluate if they can execute
                    AddUserMessageCommand.NotifyCanExecuteChanged();
                    AddAssistantMessageCommand.NotifyCanExecuteChanged();
                    InsertUserMessageCommand.NotifyCanExecuteChanged();
                    InsertAssistantMessageCommand.NotifyCanExecuteChanged();
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
            set
            {
                if (SetProperty(ref _selectedMessage, value))
                {
                    // Notify that the can-execute properties may have changed
                    OnPropertyChanged(nameof(CanMoveMessageUp));
                    OnPropertyChanged(nameof(CanMoveMessageDown));
                    OnPropertyChanged(nameof(CanDeleteMessage));
                    OnPropertyChanged(nameof(CanInsertMessage));

                    // Notify commands to reevaluate
                    MoveMessageUpCommand.NotifyCanExecuteChanged();
                    MoveMessageDownCommand.NotifyCanExecuteChanged();
                    DeleteMessageCommand.NotifyCanExecuteChanged();
                    InsertUserMessageCommand.NotifyCanExecuteChanged();
                    InsertAssistantMessageCommand.NotifyCanExecuteChanged();
                }
            }
        }

        public int SelectedIndex
        {
            get => _selectedIndex;
            set
            {
                if (SetProperty(ref _selectedIndex, value))
                {
                    // Notify that the can-execute properties may have changed
                    OnPropertyChanged(nameof(CanMoveMessageUp));
                    OnPropertyChanged(nameof(CanMoveMessageDown));
                    OnPropertyChanged(nameof(CanDeleteMessage));
                }
            }
        }

        public bool HasOpenFile => !string.IsNullOrEmpty(CurrentFilePath);

        public bool CanAddClipboardText => !string.IsNullOrWhiteSpace(ClipboardText);

        // Can move up if there's a selected item and it's not the first item
        public bool CanMoveMessageUp => SelectedIndex > 0;

        // Can move down if there's a selected item and it's not the last item
        public bool CanMoveMessageDown => SelectedIndex >= 0 && SelectedIndex < Messages.Count - 1;

        // Can delete if there's a selected item
        public bool CanDeleteMessage => SelectedIndex >= 0;

        // Can insert if there's clipboard text and a selected item
        public bool CanInsertMessage => SelectedIndex >= 0 && !string.IsNullOrWhiteSpace(ClipboardText);

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

            // Update selected index to the new message
            SelectedIndex = Messages.Count - 1;
            SelectedMessage = message;

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

        // Method removed as we're now using the public property directly

        private void MoveMessageUp()
        {
            if (!CanMoveMessageUp)
                return;

            var index = SelectedIndex;
            var message = Messages[index];
            Messages.RemoveAt(index);
            Messages.Insert(index - 1, message);
            SelectedIndex = index - 1;
            IsUnsaved = true;
        }

        // Method removed as we're now using the public property directly

        private void MoveMessageDown()
        {
            if (!CanMoveMessageDown)
                return;

            var index = SelectedIndex;
            var message = Messages[index];
            Messages.RemoveAt(index);
            Messages.Insert(index + 1, message);
            SelectedIndex = index + 1;
            IsUnsaved = true;
        }

        // Method removed as we're now using the public property directly

        private async void DeleteMessage()
        {
            if (!CanDeleteMessage)
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
                var shouldDelete = await _dialogService.ShowConfirmationDialogAsync(title, confirmMessage);

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
                await _dialogService.ShowErrorDialogAsync("Delete Error", "An error occurred while deleting the message.");
            }
        }

        private void InsertUserMessage(Message? message)
        {
            InsertMessage(message, Role.User);
        }

        private void InsertAssistantMessage(Message? message)
        {
            InsertMessage(message, Role.Assistant);
        }

        private void InsertMessage(Message? message, Role role)
        {
            if (!CanInsertMessage || message == null)
                return;

            var index = Messages.IndexOf(message);
            if (index < 0)
                return;

            var newMessage = new Message
            {
                Role = role,
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
                await _dialogService.ShowErrorDialogAsync("Save Error", $"Failed to save file: {ex.Message}");
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
                await _dialogService.ShowErrorDialogAsync("Load Error", $"Failed to load file: {ex.Message}");
            }
        }


        public async Task<bool> ConfirmExitAsync()
        {
            if (!IsUnsaved)
                return true;

            return await _dialogService.ShowConfirmationDialogAsync("Unsaved Changes", "You have unsaved changes. Exit anyway?");
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
                await _dialogService.ShowErrorDialogAsync("Open Error", $"Failed to open file: {ex.Message}");
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
                Log.Debug("Scrolling to specific message");
                _messagesListBox.ScrollIntoView(message);
            }
        }

        // Helper method to update command states when collection changes
        private void Messages_CollectionChanged(object? sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
        {
            // Update command states when collection changes
            OnPropertyChanged(nameof(CanMoveMessageUp));
            OnPropertyChanged(nameof(CanMoveMessageDown));
            OnPropertyChanged(nameof(CanDeleteMessage));
            OnPropertyChanged(nameof(CanInsertMessage));

            // Notify commands to reevaluate
            MoveMessageUpCommand.NotifyCanExecuteChanged();
            MoveMessageDownCommand.NotifyCanExecuteChanged();
            DeleteMessageCommand.NotifyCanExecuteChanged();
            InsertUserMessageCommand.NotifyCanExecuteChanged();
            InsertAssistantMessageCommand.NotifyCanExecuteChanged();
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


        // Property for configured font family from settings
        public FontFamily ConfiguredFontFamily { get; }

        // Property for configured font size from settings
        public double ConfiguredFontSize => App.Settings?.FontSize ?? ApplicationDefaults.DefaultFontSize;

        // Property for title font size from settings
        public int TitleFontSize => App.Settings?.TitleFontSize ?? ApplicationDefaults.DefaultTitleFontSize;
    }
}