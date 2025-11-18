using System;
using CommunityToolkit.WinUI;
using Microsoft.UI.Xaml.Data;

namespace UntamedAndroidSubsystem.Converters;

internal partial class StartStopBoolToTextConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, string language)
    {
        if (value is bool isStarted)
        {
            return isStarted ? "Devices_Shutdown".GetLocalized()! : "Devices_Start".GetLocalized()!;
        }
        throw new ArgumentException("值不是bool类型", nameof(value));
    }

    public object ConvertBack(object value, Type targetType, object parameter, string language) =>
        throw new NotImplementedException();
}
