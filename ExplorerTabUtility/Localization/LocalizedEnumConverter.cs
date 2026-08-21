using System;
using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;

namespace ExplorerTabUtility.Localization;

public sealed class LocalizedEnumConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is ComboBoxItem { Content: not null } item)
            return item.Content;

        return value is Enum enumValue
            ? LocalizationManager.Instance[$"Enum.{enumValue.GetType().Name}.{enumValue}"]
            : value ?? DependencyProperty.UnsetValue;
    }
    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) => Binding.DoNothing;
}
