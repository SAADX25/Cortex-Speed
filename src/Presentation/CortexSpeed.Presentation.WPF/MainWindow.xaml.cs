using CortexSpeed.Presentation.WPF.ViewModels;
using System.Windows;
using System.Windows.Input;

namespace CortexSpeed.Presentation.WPF;

public partial class MainWindow : Window
{
    public MainWindow(MainViewModel viewModel)
    {
        InitializeComponent();
        DataContext = viewModel;
    }

    private void MinimizeClick(object sender, RoutedEventArgs e)
    {
        WindowState = WindowState.Minimized;
    }

    private void MaximizeClick(object sender, RoutedEventArgs e)
    {
        WindowState = WindowState == WindowState.Maximized ? WindowState.Normal : WindowState.Maximized;
    }

    private void CloseClick(object sender, RoutedEventArgs e)
    {
        System.Windows.Application.Current.Shutdown();
    }

    /// <summary>
    /// Closes the Add Download dialog when clicking on the dark overlay background.
    /// </summary>
    private void CloseDialogOnBackgroundClick(object sender, MouseButtonEventArgs e)
    {
        if (DataContext is MainViewModel viewModel && viewModel.CloseAddDialogCommand.CanExecute(null))
        {
            viewModel.CloseAddDialogCommand.Execute(null);
        }
    }

    /// <summary>
    /// Prevents the MouseLeftButtonDown event from propagating to the background when clicking inside the dialog content.
    /// </summary>
    private void DialogContent_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        e.Handled = true;
    }

    /// <summary>
    /// Closes the Settings panel when clicking on the dark overlay background.
    /// </summary>
    private void CloseSettingsOnBackgroundClick(object sender, MouseButtonEventArgs e)
    {
        if (DataContext is MainViewModel viewModel && viewModel.CloseSettingsCommand.CanExecute(null))
        {
            viewModel.CloseSettingsCommand.Execute(null);
        }
    }

    /// <summary>
    /// Prevents the MouseLeftButtonDown event from propagating to the background when clicking inside the settings content.
    /// </summary>
    private void SettingsContent_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        e.Handled = true;
    }
}