using System.Globalization;
using System.Windows;
using System.Windows.Data;
using System.Windows.Media;

namespace Imprelia.PrintAgent.Converters;

[ValueConversion(typeof(bool), typeof(Visibility))]
public sealed class BoolToVisibilityConverter : IValueConverter
{
    public object Convert(object value, Type t, object p, CultureInfo c) =>
        value is true ? Visibility.Visible : Visibility.Collapsed;

    public object ConvertBack(object value, Type t, object p, CultureInfo c) =>
        value is Visibility.Visible;
}

[ValueConversion(typeof(bool), typeof(Visibility))]
public sealed class InverseBoolToVisibilityConverter : IValueConverter
{
    public object Convert(object value, Type t, object p, CultureInfo c) =>
        value is true ? Visibility.Collapsed : Visibility.Visible;

    public object ConvertBack(object value, Type t, object p, CultureInfo c) =>
        value is not Visibility.Visible;
}

[ValueConversion(typeof(string), typeof(Visibility))]
public sealed class StringToVisibilityConverter : IValueConverter
{
    public object Convert(object value, Type t, object p, CultureInfo c) =>
        string.IsNullOrWhiteSpace(value as string) ? Visibility.Collapsed : Visibility.Visible;

    public object ConvertBack(object value, Type t, object p, CultureInfo c) =>
        throw new NotSupportedException();
}

[ValueConversion(typeof(bool), typeof(SolidColorBrush))]
public sealed class BoolToBrushConverter : IValueConverter
{
    public SolidColorBrush TrueBrush { get; set; } = new(Colors.Green);
    public SolidColorBrush FalseBrush { get; set; } = new(Colors.Gray);

    public object Convert(object value, Type t, object p, CultureInfo c) =>
        value is true ? TrueBrush : FalseBrush;

    public object ConvertBack(object value, Type t, object p, CultureInfo c) =>
        throw new NotSupportedException();
}

[ValueConversion(typeof(int), typeof(string))]
public sealed class NavIndexToVisibilityConverter : IValueConverter
{
    public int TargetIndex { get; set; }

    public object Convert(object value, Type t, object p, CultureInfo c) =>
        value is int i && i == TargetIndex ? Visibility.Visible : Visibility.Collapsed;

    public object ConvertBack(object value, Type t, object p, CultureInfo c) =>
        throw new NotSupportedException();
}
