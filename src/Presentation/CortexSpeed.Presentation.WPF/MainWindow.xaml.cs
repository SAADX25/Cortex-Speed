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
        if (DataContext is MainViewModel viewModel && viewModel.AddDownload.CloseCommand.CanExecute(null))
        {
            viewModel.AddDownload.CloseCommand.Execute(null);
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
        if (DataContext is MainViewModel viewModel && viewModel.Settings.CloseCommand.CanExecute(null))
        {
            viewModel.Settings.CloseCommand.Execute(null);
        }
    }

    /// <summary>
    /// Prevents the MouseLeftButtonDown event from propagating to the background when clicking inside the settings content.
    /// </summary>
    private void SettingsContent_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        e.Handled = true;
    }

    /// <summary>
    /// Opens the delete popup menu at the click position.
    /// </summary>
    private void OpenDeleteMenu_Click(object sender, RoutedEventArgs e)
    {
        if (sender is System.Windows.Controls.Button btn)
        {
            _pendingDeleteItem = btn.Tag as CortexSpeed.Presentation.WPF.ViewModels.DownloadItemViewModel;
            DeletePopup.IsOpen = true;
        }
    }

    private CortexSpeed.Presentation.WPF.ViewModels.DownloadItemViewModel? _pendingDeleteItem;

    /// <summary>
    /// Removes the download from the list only (file stays on disk).
    /// </summary>
    private void RemoveFromList_Click(object sender, RoutedEventArgs e)
    {
        DeletePopup.IsOpen = false;
        if (DataContext is MainViewModel vm && _pendingDeleteItem != null)
        {
            if (vm.RemoveDownloadCommand.CanExecute(_pendingDeleteItem))
                vm.RemoveDownloadCommand.Execute(_pendingDeleteItem);
            _pendingDeleteItem = null;
        }
    }

    /// <summary>
    /// Removes the download from the list AND deletes the file from disk.
    /// </summary>
    private void DeleteFromDisk_Click(object sender, RoutedEventArgs e)
    {
        DeletePopup.IsOpen = false;
        if (DataContext is MainViewModel vm && _pendingDeleteItem != null)
        {
            if (vm.RemoveWithFileCommand.CanExecute(_pendingDeleteItem))
                vm.RemoveWithFileCommand.Execute(_pendingDeleteItem);
            _pendingDeleteItem = null;
        }
    }
}