using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Data;
using System;

namespace VK_UI3.Views.Messenger
{
    /// <summary>
    /// Конвертирует количество непрочитанных сообщений в Visibility.
    /// Показывает элемент только если значение > 0.
    /// </summary>
    public class UnreadCountToVisibilityConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, string language)
        {
            if (value is int count && count > 0)
                return Visibility.Visible;
            return Visibility.Collapsed;
        }

        public object ConvertBack(object value, Type targetType, object parameter, string language)
        {
            throw new NotImplementedException();
        }
    }
}