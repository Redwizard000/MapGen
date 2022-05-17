using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
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

namespace MapGen
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        List<Bitmap> bitOutput = new List<Bitmap>();
        Random rnd = new Random();
        int Seed;
        public MainWindow()
        {
            Seed = rnd.Next(0, int.MaxValue);
            InitializeComponent();
            txtSeed.Text = Seed.ToString();
        }

        private void cmdGo_Click(object sender, RoutedEventArgs e)
        {
            Generator gen = new Generator();
            
            bitOutput = gen.Start(Convert.ToInt32(txtSeed.Text));

            imgMap.Source = BitmapToImageSource(bitOutput[0]);
            imgHeat.Source = BitmapToImageSource(bitOutput[1]);
            imgMoist.Source = BitmapToImageSource(bitOutput[2]);
            imgBiome.Source = BitmapToImageSource(bitOutput[3]);

            if (chkShowHeat.IsChecked == true)
            {
                imgHeat.Visibility = Visibility.Visible;
            }
            else
            {
                imgHeat.Visibility = Visibility.Collapsed;
            }

            if (chkShowMap.IsChecked == true)
            {
                imgMap.Visibility = Visibility.Visible;
            }
            else
            {
                imgMap.Visibility = Visibility.Collapsed;
            }

            if (chkShowMoist.IsChecked == true)
            {
                imgMoist.Visibility = Visibility.Visible;
            }
            else
            {
                imgMoist.Visibility = Visibility.Collapsed;
            }

            if (chkShowBiome.IsChecked == true)
            {
                imgBiome.Visibility = Visibility.Visible;
            }
            else
            {
                imgBiome.Visibility = Visibility.Collapsed;
            }
        }

        BitmapImage BitmapToImageSource(Bitmap bitmap)
        {
            using (MemoryStream memory = new MemoryStream())
            {
                bitmap.Save(memory, System.Drawing.Imaging.ImageFormat.Bmp);
                memory.Position = 0;
                BitmapImage bitmapimage = new BitmapImage();
                bitmapimage.BeginInit();
                bitmapimage.StreamSource = memory;
                bitmapimage.CacheOption = BitmapCacheOption.OnLoad;
                bitmapimage.EndInit();

                return bitmapimage;
            }
        }

        private void cmdRoll_Click(object sender, RoutedEventArgs e)
        {
            Seed = rnd.Next(0, int.MaxValue);
            txtSeed.Text = Seed.ToString();
        }

        private void chkShowMap_Click(object sender, RoutedEventArgs e)
        {
            if (chkShowMap.IsChecked == true)
            {
                imgMap.Visibility = Visibility.Visible;
            }
            else
            {
                imgMap.Visibility = Visibility.Collapsed;
            }
        }

        private void chkShowHeat_Click(object sender, RoutedEventArgs e)
        {
            if (chkShowHeat.IsChecked == true)
            {
                imgHeat.Visibility = Visibility.Visible;
            }
            else
            {
                imgHeat.Visibility = Visibility.Collapsed;
            }
        }

        private void chkShowMoist_Click(object sender, RoutedEventArgs e)
        {
            if (chkShowMoist.IsChecked == true)
            {
                imgMoist.Visibility = Visibility.Visible;
            }
            else
            {
                imgMoist.Visibility = Visibility.Collapsed;
            }
        }

        private void chkShowBiome_Click(object sender, RoutedEventArgs e)
        {
            if (chkShowBiome.IsChecked == true)
            {
                imgBiome.Visibility = Visibility.Visible;
            }
            else
            {
                imgBiome.Visibility = Visibility.Collapsed;
            }
        }

        private void cmdSave_Click(object sender, RoutedEventArgs e)
        {
            SaveFileDialog dialog = new SaveFileDialog();
            bool? result = dialog.ShowDialog();
            if (result == true)
            {
                bitOutput[3].Save(dialog.FileName, ImageFormat.Jpeg);
            }
        }
    }
}
