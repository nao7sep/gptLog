using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using gptLog.App.ViewModels;
using System;

namespace gptLog.App
{
    public partial class DialogWindow : Window
    {
        public DialogWindow()
        {
            InitializeComponent();
        }

        public DialogWindow(DialogWindowViewModel viewModel) : this()
        {
            DataContext = viewModel;

            // Subscribe to the RequestClose event
            if (viewModel != null)
            {
                viewModel.RequestClose += ViewModel_RequestClose;
            }

            // Apply font settings from the App
            App.ApplyFontSettingsToWindow(this);
        }

        private void ViewModel_RequestClose(object? sender, EventArgs e)
        {
            Close();
        }

        protected override void OnClosed(EventArgs e)
        {
            base.OnClosed(e);

            // Unsubscribe from the event when the window is closed
            if (DataContext is DialogWindowViewModel viewModel)
            {
                viewModel.RequestClose -= ViewModel_RequestClose;
            }
        }
    }
}