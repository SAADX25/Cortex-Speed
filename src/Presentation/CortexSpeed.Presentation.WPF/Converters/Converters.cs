using System.Globalization;
using System.Windows;
using System.Windows.Data;
using CortexSpeed.Domain.Enums;

namespace CortexSpeed.Presentation.WPF.Converters;

/// <summary>
/// Converts byte count to human-readable file size string (KB, MB, GB).
/// </summary>
public class FileSizeConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is long bytes)
        {
            if (bytes <= 0) return "—";
            string[] sizes = { "B", "KB", "MB", "GB", "TB" };
            int order = 0;
            double len = bytes;
            while (len >= 1024 && order < sizes.Length - 1)
            {
                order++;
                len /= 1024;
            }
            return $"{len:0.##} {sizes[order]}";
        }
        return "—";
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        => throw new NotImplementedException();
}

/// <summary>
/// Converts DownloadState enum to display-friendly string.
/// </summary>
public class DownloadStateConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is DownloadState state)
        {
            return state switch
            {
                DownloadState.Queued => "Queued",
                DownloadState.Downloading => "Downloading",
                DownloadState.Paused => "Paused",
                DownloadState.Completed => "Completed",
                DownloadState.Error => "Error",
                DownloadState.Canceled => "Canceled",
                DownloadState.Assembling => "Assembling",
                DownloadState.Scheduled => "Scheduled",
                DownloadState.Waiting => "Waiting",
                _ => state.ToString()
            };
        }
        return "—";
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        => throw new NotImplementedException();
}

/// <summary>
/// Converts DownloadState to accent color for status display.
/// </summary>
public class DownloadStateToColorConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is DownloadState state)
        {
            return state switch
            {
                DownloadState.Downloading => "#00D4AA",
                DownloadState.Completed => "#4CAF50",
                DownloadState.Paused => "#FFB74D",
                DownloadState.Error => "#FF5252",
                DownloadState.Canceled => "#FF5252",
                DownloadState.Queued => "#64B5F6",
                DownloadState.Scheduled => "#AB47BC",
                DownloadState.Waiting => "#78909C",
                _ => "#A0A0A0"
            };
        }
        return "#A0A0A0";
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        => throw new NotImplementedException();
}

/// <summary>
/// Converts DownloadCategory to Segoe Fluent Icon hex code.
/// </summary>
public class CategoryToIconConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is DownloadCategory category)
        {
            return category switch
            {
                DownloadCategory.Videos => "\uE8B2",      // Video
                DownloadCategory.Music => "\uE8D5",       // Music
                DownloadCategory.Images => "\uE8B9",      // Pictures
                DownloadCategory.Archives => "\uE7B8",    // Package
                DownloadCategory.Programs => "\uE7FC",    // App
                DownloadCategory.Documents => "\uE8E5",   // Document
                DownloadCategory.Torrents => "\uE71B",    // Link
                _ => "\uE838"                              // Folder
            };
        }
        return "\uE838";
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        => throw new NotImplementedException();
}

/// <summary>
/// Converts a boolean to Visibility (true = Visible, false = Collapsed).
/// </summary>
public class BoolToVisibilityConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is bool boolValue)
        {
            return boolValue ? Visibility.Visible : Visibility.Collapsed;
        }
        return Visibility.Collapsed;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        => throw new NotImplementedException();
}

/// <summary>
/// Converts progress percentage to width for custom progress bar overlay.
/// </summary>
public class ProgressToWidthConverter : IMultiValueConverter
{
    public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
    {
        if (values.Length >= 2 && values[0] is double progress && values[1] is double containerWidth)
        {
            return Math.Max(0, containerWidth * (progress / 100.0));
        }
        return 0.0;
    }

    public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
        => throw new NotImplementedException();
}

/// <summary>
/// Converts a DownloadState to a SolidColorBrush for status dot coloring.
/// </summary>
public class DownloadStateToBrushConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        var hex = value is DownloadState state ? state switch
        {
            DownloadState.Downloading => "#00E5C4",
            DownloadState.Completed   => "#00D68F",
            DownloadState.Paused      => "#FFA940",
            DownloadState.Error       => "#FF4D6A",
            DownloadState.Canceled    => "#FF4D6A",
            DownloadState.Queued      => "#4DB8FF",
            DownloadState.Scheduled   => "#AA6EE8",
            _                         => "#5A6070"
        } : "#5A6070";
        return new System.Windows.Media.SolidColorBrush(
            (System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString(hex));
    }
    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        => throw new NotImplementedException();
}

/// <summary>
/// Maps sidebar nav string to Segoe Fluent Icon hex code.
/// </summary>
public class NavItemIconConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        return (value as string) switch
        {
            "All"       => "\uE8A9",     // Download
            "Active"    => "\uE768",     // Play
            "Completed" => "\uE73E",     // CheckMark
            "Scheduled" => "\uE823",     // Clock
            "Errors"    => "\uE7BA",     // Warning
            "Videos"    => "\uE8B2",     // Video
            "Music"     => "\uE8D5",     // Music
            "Documents" => "\uE8E5",     // Document
            "Archives"  => "\uE7B8",     // Package
            "Programs"  => "\uE7FC",     // App
            "Images"    => "\uE8B9",     // Pictures
            _           => ""
        };
    }
    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        => throw new NotImplementedException();
}

/// <summary>
/// Returns Collapsed for separator items (───), Visible for real nav items.
/// </summary>
public class NavItemVisibilityConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        => (value as string)?.StartsWith("─") == true ? Visibility.Collapsed : Visibility.Visible;
    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        => throw new NotImplementedException();
}

/// <summary>
/// Returns Visible only for separator items (───).
/// </summary>
public class NavSeparatorVisibilityConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        => (value as string)?.StartsWith("─") == true ? Visibility.Visible : Visibility.Collapsed;
    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        => throw new NotImplementedException();
}

/// <summary>
/// Converts int count: returns Collapsed when count > 0 (hides empty state).
/// Supports "inverse" parameter.
/// </summary>
public class CountToVisibilityConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        int count = value is int i ? i : 0;
        bool inverse = parameter?.ToString() == "inverse";
        bool isEmpty = count == 0;
        return (inverse ? !isEmpty : isEmpty) ? Visibility.Visible : Visibility.Collapsed;
    }
    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        => throw new NotImplementedException();
}

/// <summary>
/// Returns Visible only when Status is Downloading or Queued (for Pause button).
/// </summary>
public class IsDownloadingConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is DownloadState state)
        {
            return (state == DownloadState.Downloading || state == DownloadState.Queued)
                ? Visibility.Visible
                : Visibility.Collapsed;
        }
        return Visibility.Collapsed;
    }
    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        => throw new NotImplementedException();
}

/// <summary>
/// Returns Visible only when Status is Paused or Error (for Resume button).
/// </summary>
public class IsPausedConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is DownloadState state)
        {
            return (state == DownloadState.Paused || state == DownloadState.Error)
                ? Visibility.Visible
                : Visibility.Collapsed;
        }
        return Visibility.Collapsed;
    }
    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        => throw new NotImplementedException();
}

/// <summary>
/// Returns Visible only when Status is Completed (for Open File/Folder buttons).
/// </summary>
public class IsCompletedConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is DownloadState state)
        {
            return state == DownloadState.Completed
                ? Visibility.Visible
                : Visibility.Collapsed;
        }
        return Visibility.Collapsed;
    }
    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        => throw new NotImplementedException();
}
