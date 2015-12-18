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
namespace Database_manager
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        List<field_grid> fields = new List<field_grid>();
        private int size = 30; // Width and height of the rendered fields
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
            Random random = new Random();
            for (int i = 0; i < 10; i++)
            {
                Grid field = new Grid(); // Grid in which we will put all squares

                // Add 7 colums and 6 rows
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

                Grid container = new Grid(); // A wrapper around each field so we can add a border
                container.Children.Add(field); // Add the field to the container
                scrollwrapper.Children.Add(container); // Add the container to the scrollwrapper
                container.MouseLeftButtonDown += field_select; // Subscribe to the clickevent

                field.Margin = new Thickness(5, 5, 5, 5); // Make it so we can actually see the border

                // Scale the field to the same size as the other fields
                field.Width = size;
                field.Height = size;

                // Add the stones to the field
                for (int x = 0; x < 7; x++)
                {
                    for (int y = 0; y < 6; y++)
                    {
                        Ellipse ellipse = new Ellipse();
                        field.Children.Add(ellipse);
                        Color color;

                        // Add a nice color to the stone
                        int r = random.Next(2);
                        #region colors
                        switch (r)
                        {
                            case 0:
                                color = Colors.Red;
                                break;
                            case 1:
                                color = Colors.DodgerBlue;
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
                        ellipse.Fill = new SolidColorBrush(color);

                        // Place the stone in the right cell
                        ellipse.SetValue(Grid.ColumnProperty, x);
                        ellipse.SetValue(Grid.RowProperty, y);
                    }
                }
                field_grid field_grid = new field_grid {grid = field};
                fields.Add(field_grid);
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
            // Set the global size to the new value so all new fields will have this size too
            size = (int)slider.Value;

            // Change the size of all existing fields
            foreach (field_grid field in fields)
            {
                field.grid.Width = size;
                field.grid.Height = size;
            }
        }

        private void get_data_button_Click(object sender, RoutedEventArgs e)
        {

        }
    }

    struct field_grid
    {
        public Grid grid;
        public Field field;
    }
}
