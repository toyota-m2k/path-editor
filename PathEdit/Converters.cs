using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Data;
using System;

namespace PathEdit;

public class BoolVisibilityConverter : IValueConverter {
    public object Convert(object value, Type targetType, object parameter, string language) {
        return ((bool)value) ? Visibility.Visible : Visibility.Collapsed;
    }

    public object ConvertBack(object value, Type targetType, object parameter, string language) {
        throw new NotImplementedException();
    }
}

public class NegBoolVisibilityConverter : IValueConverter {
    public object Convert(object value, Type targetType, object parameter, string language) {
        return (!(bool)value) ? Visibility.Visible : Visibility.Collapsed;
    }

    public object ConvertBack(object value, Type targetType, object parameter, string language) {
        throw new NotImplementedException();
    }
}

public class NegBoolConverter : IValueConverter {
    public object Convert(object value, Type targetType, object parameter, string language) {
        return !(bool)value;
    }

    public object ConvertBack(object value, Type targetType, object parameter, string language) {
        return value as bool? != true;
    }
}

