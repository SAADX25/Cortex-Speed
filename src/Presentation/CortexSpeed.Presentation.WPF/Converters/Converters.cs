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
                DownloadState.Queued => "⏳ Queued",
                DownloadState.Downloading => "⬇ Downloading",
                DownloadState.Paused => "⏸ Paused",
                DownloadState.Completed => "✅ Completed",
                DownloadState.Error => "❌ Error",
                DownloadState.Canceled => "🚫 Canceled",
                DownloadState.Assembling => "🔧 Assembling",
                DownloadState.Scheduled => "📅 Scheduled",
                DownloadState.Waiting => "⏱ Waiting",
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
/// Converts DownloadCategory to icon character.
/// </summary>
public class CategoryToIconConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is DownloadCategory category)
        {
            return category switch
            {
                DownloadCategory.Videos => "🎬",
                DownloadCategory.Music => "🎵",
                DownloadCategory.Images => "🖼",
                DownloadCategory.Archives => "📦",
                DownloadCategory.Programs => "💿",
                DownloadCategory.Documents => "📄",
                DownloadCategory.Torrents => "🔗",
                _ => "📁"
            };
        }
        return "📁";
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
/// Returns true (Visibility.Visible) when DownloadState equals the parameter.
/// </summary>
public class StateEqualsToVisibilityConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is DownloadState state && parameter is string stateStr)
        {
            if (Enum.TryParse<DownloadState>(stateStr, out var target))
            {
                return state == target ? Visibility.Visible : Visibility.Collapsed;
            }
        }
        return Visibility.Collapsed;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        => throw new NotImplementedException();
}
