using System;
using Microsoft.UI.Xaml.Data;

namespace TaskManager.Classes
{
    public class BytesToMBConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, string language)
        {
            if (value is long bytes)
            {
                double mb = bytes / (1024.0 * 1024.0);
                return $"{mb:F2} MB";
            }
            return "0 MB";
        }

        public object ConvertBack(object value, Type targetType, object parameter, string language)
        {
            throw new NotImplementedException();
        }
    }
}
