using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using gptLog.App.Model;
using gptLog.App.ViewModels;
using System;
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
            }
        }

        private void OnButtonClick(object? sender, RoutedEventArgs e)
        {
            if (e.Source is Button button && button.Parent is Grid grid && grid.Parent is Border border)
            {
                // Get the message from the DataContext of the Border
                if (border.DataContext is Message message)
                {
                    // Get the index of the button in the Grid
                    var index = Grid.GetColumn(button);

                    // Get the index of the message in the ListBox
                    var listBox = this.FindControl<ListBox>("MessagesList");
                    if (listBox != null)
                    {
                        var messageIndex = listBox.Items.IndexOf(message);

                        switch (index)
                        {
                            case 1: // Move Up button
                                ViewModel?.MoveMessageUpCommand.Execute(null);
                                break;
                            case 2: // Move Down button
                                ViewModel?.MoveMessageDownCommand.Execute(null);
                                break;
                            case 3: // Insert User button
                                ViewModel?.InsertUserMessageCommand.Execute(messageIndex);
                                break;
                            case 4: // Insert Assistant button
                                ViewModel?.InsertAssistantMessageCommand.Execute(messageIndex);
                                break;
                            case 5: // Delete button
                                ViewModel?.DeleteMessageCommand.Execute(null);
                                break;
                        }
                    }
                }
            }
        }

        private async void MainWindow_Closing(object? sender, WindowClosingEventArgs e)
        {
            if (ViewModel != null && ViewModel.IsUnsaved)
            {
                e.Cancel = true;
                var shouldExit = await ViewModel.ConfirmExitAsync();
                if (shouldExit)
                {
                    Close();
                }
            }
        }
    }
}