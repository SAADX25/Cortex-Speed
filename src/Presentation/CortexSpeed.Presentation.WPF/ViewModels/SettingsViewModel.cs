using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System;
using System.IO;

namespace CortexSpeed.Presentation.WPF.ViewModels;

public partial class SettingsViewModel : ObservableObject
{
    [ObservableProperty]
    private bool _isOpen;

    [ObservableProperty]
    private string _defaultDownloadFolder = string.Empty;

    [ObservableProperty]
    private int _maxConcurrentDownloads = 5;

    [ObservableProperty]
    private bool _clipboardMonitorEnabled = true;

    [ObservableProperty]
    private bool _startMinimized;

    [ObservableProperty]
    private bool _showNotifications = true;

    public SettingsViewModel()
    {
        _defaultDownloadFolder = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "Downloads", "CortexSpeed");
        Directory.CreateDirectory(_defaultDownloadFolder);
    }

    [RelayCommand]
    private void Open()
    {
        IsOpen = true;
    }

    [RelayCommand]
    private void Close()
    {
        IsOpen = false;
    }

    [RelayCommand]
    private void SelectDefaultFolder()
    {
        var dialog = new Microsoft.Win32.OpenFolderDialog
        {
            Title = "Select Default Download Folder",
            InitialDirectory = DefaultDownloadFolder
        };
        if (dialog.ShowDialog() == true)
        {
            DefaultDownloadFolder = dialog.FolderName;
        }
    }
}