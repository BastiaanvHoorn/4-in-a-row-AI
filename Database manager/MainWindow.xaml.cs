using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
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
using Xceed.Wpf.AvalonDock.Controls;

namespace Database_manager
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        List<Grid> fields = new List<Grid>();
        private int size = 30;
        private Grid selected_grid;
        public MainWindow()
        {
            InitializeComponent();
        }

        private void address_textbox_TextChanged(object sender, TextChangedEventArgs e)
        {
            string[] s = address_textbox.Text.Split('.');
            if (s.Length != 4)
            {
                address_textbox.Background = new SolidColorBrush(Color.FromRgb(225, 110, 110));
                return;
            }

            foreach (var n in s)
            {
                byte b;
                if (byte.TryParse(n, out b)) continue;
                address_textbox.Background = new SolidColorBrush(Color.FromRgb(225, 110, 110));
                return;
            }
            address_textbox.Background = Brushes.White;
        }
        private void port_textbox_TextChanged(object sender, TextChangedEventArgs e)
        {
            int i;
            if (int.TryParse(port_textbox.Text, out i))
            {
                if (i > 0 && i < 100000)
                {
                    port_textbox.Background = new SolidColorBrush(Colors.White);
                    return;
                }
            }
            port_textbox.Background = new SolidColorBrush(Color.FromRgb(225, 110, 110));
        }

        private void retrieve_clicked(object sender, RoutedEventArgs e)
        {
            Random random = new Random();
            for (int i = 0; i < 10; i++)
            {
                Grid field = new Grid();
                for (int j = 0; j < 7; j++)
                {
                    var coldef = new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) };
                    field.ColumnDefinitions.Add(coldef);
                }
                for (int j = 0; j < 6; j++)
                {
                    var rowdef = new RowDefinition { Height = new GridLength(1, GridUnitType.Star) };
                    field.RowDefinitions.Add(rowdef);
                }
                Grid container = new Grid();
                container.Children.Add(field);
                scrollwrapper.Children.Add(container);
                container.MouseLeftButtonDown += delegate(object o, MouseButtonEventArgs args)
                {
                    if (selected_grid != null)
                    {
                        selected_grid.Background = Brushes.Transparent;
                    }
                    ((Grid) o).Background = Brushes.DarkBlue;
                    selected_grid = ((Grid) o);

                };
                field.Margin = new Thickness(5, 5, 5, 5);
                field.Width = size;
                field.Height = size;
                for (int x = 0; x < 7; x++)
                {
                    for (int y = 0; y < 6; y++)
                    {
                        Rectangle rectangle = new Rectangle();
                        field.Children.Add(rectangle);
                        Color color;
                        int r = random.Next(2);
                        #region colors
                        switch (r)
                        {
                            case 0:
                                color = Colors.Red;
                                break;
                            case 1:
                                color = Colors.Blue;
                                break;
                            case 2:
                                color = Colors.Bisque;
                                break;
                            case 3:
                                color = Colors.Yellow;
                                break;
                            case 4:
                                color = Colors.Green;
                                break;
                            case 5:
                                color = Colors.Purple;
                                break;
                            case 6:
                                color = Colors.DeepPink;
                                break;
                            case 7:
                                color = Colors.Orange;
                                break;
                            case 8:
                                color = Colors.Cyan;
                                break;
                            case 9:
                            default:
                                color = Colors.LawnGreen;
                                break;
                        }
                        #endregion
                        rectangle.Fill = new SolidColorBrush(color);
                        rectangle.SetValue(Grid.ColumnProperty, x);
                        rectangle.SetValue(Grid.RowProperty, y);
                    }
                }
                fields.Add(field);
            }
        }

        private void container_click(object sender, MouseEventArgs e)
        {
            
        }
        private void slider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            size = (int) slider.Value;
            foreach (Grid field in fields)
            {
                field.Width = size;
                field.Height = size;
            }
        }
    }
}
