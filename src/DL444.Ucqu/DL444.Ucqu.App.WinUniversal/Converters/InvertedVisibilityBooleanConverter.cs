using System;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Data;

namespace DL444.Ucqu.App.WinUniversal.Converters
{
    internal class InvertedVisibilityBooleanConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, string language)
        {
            bool visible;
            if (value.GetType() == typeof(bool))
            {
                visible = (bool)value;
            }
            else if (value.GetType() == typeof(Visibility))
            {
                visible = (Visibility)value == Visibility.Visible ? true : false;
            }
            else
            {
                throw new NotSupportedException($"Conversion from {value.GetType()} to {targetType} is not supported by this converter.");
            }

            visible = !visible;
            if (targetType == typeof(bool))
            {
                return visible;
            }
            else if (targetType == typeof(Visibility))
            {
                return visible ? Visibility.Visible : Visibility.Collapsed;
            }
            else
            {
                throw new NotSupportedException($"Conversion from {value.GetType()} to {targetType} is not supported by this converter.");
            }
        }

        public object ConvertBack(object value, Type targetType, object parameter, string language)
        {
            return Convert(value, targetType, parameter, language);
        }
    }
}
