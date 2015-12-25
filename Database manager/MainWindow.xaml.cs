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
using Engine;
using Util;
namespace Database_manager
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        Dictionary<Grid, Field> fields = new Dictionary<Grid, Field>(); 
        private int size = 30; // Width and height of the rendered field_grids
        private Grid selected_grid;
        public MainWindow()
        {
            InitializeComponent();
        }

        private void address_textbox_TextChanged(object sender, TextChangedEventArgs e)
        {
            // Split the string with dots since an IPv4 address is seperated by dots
            string[] s = address_textbox.Text.Split('.');
            // If there aren't 4 elements in the array, we don't have a valid IPv4 address
            if (s.Length != 4)
            {
                address_textbox.Background = new SolidColorBrush(Color.FromRgb(225, 110, 110));
                return;
            }

            // Try to parse each element of the array to a byte. If all succeed we have a valid IPv4 address
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
            short s;

            // If the text is parseable as a short, it is a valid port number

            if (short.TryParse(port_textbox.Text, out s))
            {
                port_textbox.Background = new SolidColorBrush(Colors.White);
                return;
            }
            port_textbox.Background = new SolidColorBrush(Color.FromRgb(225, 110, 110));
        }

        private void retrieve_clicked(object sender, RoutedEventArgs e)
        {
            if (length_up_down.Value == null || end_up_down.Value == null || start_up_down.Value == null)
            {
                return;
            }
            byte[] data = new byte[12];
            byte[] length = BitConverter.GetBytes((int)length_up_down.Value);
            byte[] start = BitConverter.GetBytes((int)start_up_down.Value);
            byte[] amount = BitConverter.GetBytes((int)end_up_down.Value);
            length.CopyTo(data, 0);
            start.CopyTo(data, 4);
            amount.CopyTo(data, 8);
            byte[] field_data = Requester.send(data, network_codes.range_request);

            int field_length = (int)length_up_down.Value;

            for (int i = 0; i < field_data.Length / field_length; i++)
            {
                byte[] field_bytes = new byte[field_length];
                Buffer.BlockCopy(field_data, field_length * i, field_bytes, 0, field_length);
                Field field = field_bytes.decompressField();
                #region grid creation
                Grid grid = new Grid(); // Grid in which we will put all squares

                // Add 7 colums and 6 rows
                for (int j = 0; j < 7; j++)
                {
                    var coldef = new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) };
                    grid.ColumnDefinitions.Add(coldef);
                }
                for (int j = 0; j < 6; j++)
                {
                    var rowdef = new RowDefinition { Height = new GridLength(1, GridUnitType.Star) };
                    grid.RowDefinitions.Add(rowdef);
                }

                Grid container = new Grid(); // A wrapper around each field so we can add a border
                container.Children.Add(grid); // Add the field to the container
                scrollwrapper.Children.Add(container); // Add the container to the scrollwrapper
                container.MouseLeftButtonDown += field_select; // Subscribe to the clickevent

                grid.Margin = new Thickness(5, 5, 5, 5); // Make it so we can actually see the border

                // Scale the field to the same size as the other field_grids
                grid.Width = size;
                grid.Height = size;
                #endregion
                fields.Add(grid, field);
                
                for (int x = 0; x < field.Width; x++)
                {
                    for (int y = 0; y < field.Height; y++)
                    {
                        Ellipse ellipse = new Ellipse();
                        grid.Children.Add(ellipse);

                        #region colors
                        //// Add a nice color to the stone
                        ////int r = random.Next(2);
                        //switch (r)
                        //{
                        //    case 0:
                        //        color = Colors.Red;
                        //        break;
                        //    case 1:
                        //        color = Colors.DodgerBlue;
                        //        break;
                        //    case 2:
                        //        color = Colors.Bisque;
                        //        break;
                        //    case 3:
                        //        color = Colors.Yellow;
                        //        break;
                        //    case 4:
                        //        color = Colors.Green;
                        //        break;
                        //    case 5:
                        //        color = Colors.Purple;
                        //        break;
                        //    case 6:
                        //        color = Colors.DeepPink;
                        //        break;
                        //    case 7:
                        //        color = Colors.Orange;
                        //        break;
                        //    case 8:
                        //        color = Colors.Cyan;
                        //        break;
                        //    case 9:
                        //    default:
                        //        color = Colors.LawnGreen;
                        //        break;
                        //}
                        #endregion

                        Color color;

                        switch (field.getCellPlayer(x,field.Height- y-1))
                        {
                            case players.Alice:
                                color = Colors.Red;
                                break;
                            case players.Bob:
                                color = Colors.DodgerBlue;
                                break;
                            default:
                                color = Colors.Transparent;
                                break;

                        }
                        ellipse.Fill = new SolidColorBrush(color);

                        // Place the stone in the right cell
                        ellipse.SetValue(Grid.ColumnProperty, x);
                        ellipse.SetValue(Grid.RowProperty, y);
                    }
                }
            }
        }

        private void field_select(object sender, MouseEventArgs e)
        {
            get_data_button.IsEnabled = true;
            if (selected_grid != null)
            {
                // Change the previous selected grid back to transparent
                selected_grid.Background = Brushes.Transparent;
            }

            ((Grid)sender).Background = Brushes.DarkBlue; // Change this grid to darkblue
            selected_grid = ((Grid)sender); // Set this grid as the new selected grid

        }

        private void slider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            // Set the global size to the new value so all new field_grids will have this size too
            size = (int)slider.Value;

            // Change the size of all existing field_grids
            foreach (var pair in fields)
            {
                
                pair.Key.Width = size;
                pair.Key.Height = size;
            }
        }

        private void get_data_button_Click(object sender, RoutedEventArgs e)
        {
            Field field = fields[selected_grid];
        }
    }

}
