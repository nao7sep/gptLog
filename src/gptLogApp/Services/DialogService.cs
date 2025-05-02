using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using gptLogApp.ViewModels;
using Serilog;
using System;
using System.Threading.Tasks;

namespace gptLogApp.Services
{
    public class DialogService
    {
        public enum DialogType
        {
            Ok,
            YesNo
        }

        /// <summary>
        /// Shows a dialog with the specified title and message.
        /// </summary>
        /// <param name="title">The dialog title.</param>
        /// <param name="message">The dialog message.</param>
        /// <param name="dialogType">The type of dialog to show (Ok or YesNo).</param>
        /// <returns>True for OK button or Yes button, False for No button or if dialog fails.</returns>
        public async Task<bool> ShowDialogAsync(string title, string message, DialogType dialogType = DialogType.Ok)
        {
            if (string.IsNullOrEmpty(title))
                title = "Information";

            if (string.IsNullOrEmpty(message))
                message = "No additional information provided.";

            try
            {
                if (Application.Current?.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop &&
                    desktop.MainWindow != null)
                {
                    // Create the view model
                    var viewModel = new DialogWindowViewModel(
                        title,
                        message,
                        dialogType == DialogType.YesNo);

                    // Create and show the dialog
                    var dialog = new DialogWindow(viewModel);
                    await dialog.ShowDialog(desktop.MainWindow);

                    // Return the dialog result
                    return await viewModel.DialogResultTask;
                }
                else
                {
                    Log.Warning("Could not show dialog - no main window available");
                    return dialogType == DialogType.YesNo ? false : true; // Default to No for YesNo dialogs, Yes for Ok dialogs
                }
            }
            catch (Exception ex)
            {
                // Log any errors
                Log.Error(ex, "Error showing dialog: {Title}", title);
                return dialogType == DialogType.YesNo ? false : true; // Default to No for YesNo dialogs, Yes for Ok dialogs
            }
        }

        /// <summary>
        /// Shows an error dialog with the specified title and message.
        /// </summary>
        /// <param name="title">The dialog title.</param>
        /// <param name="message">The error message.</param>
        /// <returns>Always returns true when the dialog is closed.</returns>
        public async Task<bool> ShowErrorDialogAsync(string title, string message)
        {
            return await ShowDialogAsync(title, message, DialogType.Ok);
        }

        /// <summary>
        /// Shows a confirmation dialog with the specified title and message.
        /// </summary>
        /// <param name="title">The dialog title.</param>
        /// <param name="message">The confirmation message.</param>
        /// <returns>True if the user confirmed, false otherwise.</returns>
        public async Task<bool> ShowConfirmationDialogAsync(string title, string message)
        {
            return await ShowDialogAsync(title, message, DialogType.YesNo);
        }
    }
}