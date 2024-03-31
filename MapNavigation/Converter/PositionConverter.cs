using System;
using Windows.Devices.Geolocation;
using Windows.UI.Xaml.Data;

namespace MapNavigation.Converter
{
    class PositionConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, string language)
        {
            return new Geopoint((BasicGeoposition)value);
        }

        public object ConvertBack(object value, Type targetType, object parameter, string language)
        {
            return ((Geopoint)value).Position;
        }
    }
}
