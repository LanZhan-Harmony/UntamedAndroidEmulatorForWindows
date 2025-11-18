using System;
using Microsoft.UI.Xaml.Data;

namespace UntamedAndroidSubsystem.Converters;

internal partial class StartStopBoolToGlyphConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, string language)
    {
        if (value is bool isStarted)
        {
            return isStarted ? "\uE71A" : "\uE768";
        }
        throw new ArgumentException("值不是bool类型", nameof(value));
    }

    public object ConvertBack(object value, Type targetType, object parameter, string language) =>
        throw new NotImplementedException();
}
