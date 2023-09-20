using System;
using System.Collections.Generic;
using System.Linq;
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
using GMap.NET;
using GMap.NET.MapProviders;
using GMap.NET.WindowsPresentation;
using System.Windows.Media;
using System.Diagnostics;
using Microsoft.Win32;
using HandyControl.Controls;
using System.Security.Cryptography;

namespace AirMonitor
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : System.Windows.Window
    {
        public MainWindow()
        {
            GMap.NET.GMaps.Instance.Mode = GMap.NET.AccessMode.ServerAndCache;
            InitializeComponent();
            KeyDown += (s, e) => { if (e.Key == Key.F1) mainDrawer.IsOpen = !mainDrawer.IsOpen; };
            var db = new Database();
            DataContext = db;
            mainFrame.Navigate(new Pages.Overview(db));
        }

        private void ImportClick(object sender, RoutedEventArgs e)
        {
            try
            {
                OpenFileDialog ofd = new OpenFileDialog();
                ofd.ShowDialog();
                if (!string.IsNullOrEmpty(ofd.FileName))
                {
                    string year = "2022";
                    if (!string.IsNullOrEmpty(year))
                        (this.DataContext as Database).Import(ofd.FileName, int.Parse(year));
                }
            }
            catch (Exception ex)
            {
                Growl.ErrorGlobal(ex.Message);
            }

        }
    }
}
