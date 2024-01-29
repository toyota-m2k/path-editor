using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Data;
using System;
using Windows.Globalization.NumberFormatting;

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

public class PointStringConverter : IValueConverter {
    public object Convert(object value, Type targetType, object parameter, string language) {
        var point = (System.Windows.Point)value;
        return $"( {Math.Round(point.X, 4)} ,  {Math.Round(point.Y, 4)} )";
    }
    public object ConvertBack(object value, Type targetType, object parameter, string language) {
        throw new NotImplementedException();

        //var str = (string)value;
        //var parts = str.Split(',');
        //if (parts.Length != 2) {
        //    throw new Exception($"Invalid point string: {str}");
        //}
        //return new Point(double.Parse(parts[0]), double.Parse(parts[1]));
    }
}

public static class ConvertUtils {
    public static DecimalFormatter CoordinateFormatter { get; } = new() {
        FractionDigits = 2,
        IsZeroSigned = false,
        NumberRounder = new IncrementNumberRounder() {
            Increment = 0.0001,
            RoundingAlgorithm = RoundingAlgorithm.RoundHalfUp,
        }
    };


}