using System;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Media.Imaging;

namespace UntamedAndroidSubsystem.Converters;

internal sealed partial class StringToImageSourceConverter : IValueConverter
{
    public object? Convert(object value, Type targetType, object parameter, string language)
    {
        if (value is not string uri || string.IsNullOrWhiteSpace(uri))
        {
            return null;
        }

        return new BitmapImage(new Uri(uri, UriKind.Absolute));
    }

    public object ConvertBack(object value, Type targetType, object parameter, string language) =>
        throw new NotImplementedException();
}
