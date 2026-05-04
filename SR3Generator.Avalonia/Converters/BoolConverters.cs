using Avalonia.Data.Converters;
using System;
using System.Globalization;

namespace SR3Generator.Avalonia.Converters;

public class BoolToChevronConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is bool isExpanded)
        {
            return isExpanded ? "▼" : "▶";
        }
        return "▶";
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}

public class BoolToOpacityConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is bool isPurchased)
        {
            return isPurchased ? 0.4 : 1.0;
        }
        return 1.0;
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}

public class IntEqualsConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is int iv && parameter is string pStr && int.TryParse(pStr, out var pv))
        {
            return iv == pv;
        }
        return false;
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}

public class EdgeFlawTypeToBrushConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is SR3Generator.Data.Character.EdgeFlawType type)
        {
            return type == SR3Generator.Data.Character.EdgeFlawType.Edge
                ? new global::Avalonia.Media.SolidColorBrush(global::Avalonia.Media.Color.Parse("#22c55e"))  // karma green
                : new global::Avalonia.Media.SolidColorBrush(global::Avalonia.Media.Color.Parse("#ef4444")); // destructive red
        }
        return new global::Avalonia.Media.SolidColorBrush(global::Avalonia.Media.Color.Parse("#9ca3af"));
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}
