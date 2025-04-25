using System;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace gptLog.App.ViewModels
{
    public class DialogWindowViewModel : ViewModelBase
    {
        private string _title = string.Empty;
        private string _message = string.Empty;
        private bool _isYesNoDialog;
        private TaskCompletionSource<bool> _dialogResult;

        public DialogWindowViewModel(string title, string message, bool isYesNoDialog)
        {
            _title = title;
            _message = message;
            _isYesNoDialog = isYesNoDialog;
            _dialogResult = new TaskCompletionSource<bool>();

            // Initialize commands
            OkCommand = new RelayCommand(OnOkClicked);
            YesCommand = new RelayCommand(OnYesClicked);
            NoCommand = new RelayCommand(OnNoClicked);
            CloseCommand = new RelayCommand(OnCloseClicked);
        }

        public string Title
        {
            get => _title;
            set => SetProperty(ref _title, value);
        }

        public string Message
        {
            get => _message;
            set => SetProperty(ref _message, value);
        }

        public bool IsYesNoDialog
        {
            get => _isYesNoDialog;
            set => SetProperty(ref _isYesNoDialog, value);
        }

        public IRelayCommand OkCommand { get; }
        public IRelayCommand YesCommand { get; }
        public IRelayCommand NoCommand { get; }
        public IRelayCommand CloseCommand { get; }

        public Task<bool> DialogResultTask => _dialogResult.Task;

        private void OnOkClicked()
        {
            _dialogResult.TrySetResult(true);
            RequestClose?.Invoke(this, EventArgs.Empty);
        }

        private void OnYesClicked()
        {
            _dialogResult.TrySetResult(true);
            RequestClose?.Invoke(this, EventArgs.Empty);
        }

        private void OnNoClicked()
        {
            _dialogResult.TrySetResult(false);
            RequestClose?.Invoke(this, EventArgs.Empty);
        }

        private void OnCloseClicked()
        {
            // Default to false for Yes/No dialogs, true for OK dialogs
            _dialogResult.TrySetResult(!IsYesNoDialog);
            RequestClose?.Invoke(this, EventArgs.Empty);
        }

        // Event to request window closure
        public event EventHandler? RequestClose;
    }
}