using GMap.NET;
using GMap.NET.MapProviders;
using GMap.NET.WindowsPresentation;
using MySqlX.XDevAPI;
using Org.BouncyCastle.Crmf;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Drawing;
using System.Globalization;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using ZstdSharp.Unsafe;
using Color = System.Windows.Media.Color;

namespace AirMonitor.Pages
{
    public class PollutionToImageConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is double pollution && parameter is double maxAmount)
            {
                return pollution > maxAmount;
            }
            return false;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
    public partial class Overview : Page
    {
        public Overview(Database db)
        {

            _database = db;
            InitializeComponent();
            gmap.MapProvider = GMapProviders.GoogleMap;
            gmap.MinZoom = 9; gmap.MaxZoom = 9;
            gmap.Zoom = 9;
            gmap.Position = new PointLatLng(50.4501, 30.5234);
            gmap.CenterCrossPen.Brush = Brushes.Transparent;
            gmap.UpdateLayout();
            Update(2021);
        }
        private Database _database;

        private static readonly DependencyPropertyKey MyPropertyPropertyKey =
        DependencyProperty.RegisterReadOnly("Records", typeof(ObservableCollection<Record>), typeof(Overview),
            new FrameworkPropertyMetadata(new ObservableCollection<Record>()));

        public static readonly DependencyProperty MyPropertyProperty = MyPropertyPropertyKey.DependencyProperty;

        public ObservableCollection<Record> Records
        {
            get { return (ObservableCollection<Record>)GetValue(MyPropertyProperty); }
            private set { SetValue(MyPropertyPropertyKey, value); }
        }
        async public void Update(int year)
        {
            loading.Visibility = Visibility.Visible;
            await Task.Run(() =>
            {
                var records = new ObservableCollection<Record>();
                _database.GetRecords(records, year);
                Application.Current.Dispatcher.Invoke(() => { Records = records; loading.Visibility = Visibility.Collapsed; });
            });
            gmap.Markers.Clear();
            string current = "";
            foreach (var record in Records)
            {
                if (current != record.Factory.Name)
                {
                    current = record.Factory.Name;
                    if (record.Factory.Lat != 0.0f & record.Factory.Lng != 0.0f)
                    {
                        CreateCircle(record.Factory.Lat, record.Factory.Lng, 3000.0);
                    }
                }
            }

        }

        private void CreateCircle(Double lat, Double lon, double radius)
        {
            PointLatLng point = new PointLatLng(lat, lon);
            int segments = 1000;

            List<PointLatLng> gpollist = new List<PointLatLng>();

            for (int i = 0; i < segments; i++)
                gpollist.Add(FindPointAtDistanceFrom(point, i, radius / 1000));

            GMapPolygon gpol = new GMapPolygon(gpollist);
            Brush brush = new SolidColorBrush(Color.FromArgb(150, 255, 0, 0));
            gpol.Shape = new Path() { Fill = brush, StrokeThickness = 0.001 };

            gmap.Markers.Add(gpol);
        }

        public static GMap.NET.PointLatLng FindPointAtDistanceFrom(GMap.NET.PointLatLng startPoint, double initialBearingRadians, double distanceKilometres)
        {
            GMapMarker h = new GMapMarker(startPoint);
            const double radiusEarthKilometres = 6371.01;
            var distRatio = distanceKilometres / radiusEarthKilometres;
            var distRatioSine = Math.Sin(distRatio);
            var distRatioCosine = Math.Cos(distRatio);

            var startLatRad = DegreesToRadians(startPoint.Lat);
            var startLonRad = DegreesToRadians(startPoint.Lng);

            var startLatCos = Math.Cos(startLatRad);
            var startLatSin = Math.Sin(startLatRad);

            var endLatRads = Math.Asin((startLatSin * distRatioCosine) + (startLatCos * distRatioSine * Math.Cos(initialBearingRadians)));

            var endLonRads = startLonRad + Math.Atan2(
                          Math.Sin(initialBearingRadians) * distRatioSine * startLatCos,
                          distRatioCosine - startLatSin * Math.Sin(endLatRads));

            return new GMap.NET.PointLatLng(RadiansToDegrees(endLatRads), RadiansToDegrees(endLonRads));
        }

        public static double DegreesToRadians(double degrees)
        {
            const double degToRadFactor = Math.PI / 180;
            return degrees * degToRadFactor;
        }

        public static double RadiansToDegrees(double radians)
        {
            const double radToDegFactor = 180 / Math.PI;
            return radians * radToDegFactor;
        }

        private void ComboBoxItem_Selected(object sender, RoutedEventArgs e)
        {
            var item = sender as ComboBoxItem;
            if (item.Content != null)
                Update(int.Parse(item.Content.ToString()));
        }
    }
}
