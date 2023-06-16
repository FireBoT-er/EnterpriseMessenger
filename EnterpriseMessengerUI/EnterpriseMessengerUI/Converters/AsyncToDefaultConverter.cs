using AsyncImageLoader;
using Avalonia.Controls;
using Avalonia.Data;
using Avalonia.Data.Converters;
using Avalonia.LogicalTree;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using Avalonia.Threading;
using System;
using System.Globalization;
using System.Linq;
using System.Threading;

namespace EnterpriseMessengerUI.Converters
{
    public class AsyncToDefaultConverter : IValueConverter
    {
        public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            var image = ((Border)((Binding)parameter!).DefaultAnchor!.Target!).GetLogicalDescendants().OfType<AdvancedImage>().ElementAt(0);
            ThreadPool.QueueUserWorkItem(ConverterThreadWorker, new { ((Binding)parameter!).DefaultAnchor!.Target, image });
            return null;
        }

        private static void ConverterThreadWorker(object? obj)
        {
            while (((AdvancedImage)((dynamic)obj!).image).CurrentImage == null) { }

            Dispatcher.UIThread.InvokeAsync(() =>
            {
                ((ImageBrush)((Border)((dynamic)obj!).Target).Background!).Source = (Bitmap)((AdvancedImage)((dynamic)obj!).image).CurrentImage!;
            });
        }

        public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
