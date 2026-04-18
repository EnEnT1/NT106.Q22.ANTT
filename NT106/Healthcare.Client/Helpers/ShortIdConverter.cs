using Microsoft.UI.Xaml.Data;
using System;

namespace Healthcare.Client.Helpers
{
    public class ShortIdConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, string language)
        {
            if (value is string id && !string.IsNullOrEmpty(id))
            {
                if (id.Length <= 6) return id.ToUpper();
                // Lấy 6 ký tự đầu của UUID
                return id.Substring(0, 6).ToUpper();
            }
            return value;
        }

        public object ConvertBack(object value, Type targetType, object parameter, string language)
        {
            throw new NotImplementedException();
        }
    }
}
