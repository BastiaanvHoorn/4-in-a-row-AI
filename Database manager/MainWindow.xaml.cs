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
        private List<Field> fields = new List<Field>();
        private List<Grid> grids = new List<Grid>();
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
            //Return immediatly if the input isn't correct
            if (length_up_down.Value == null || end_up_down.Value == null || start_up_down.Value == null || start_up_down.Value > end_up_down.Value)
            {
                return;
            }

            //Parse the input to one byte-array
            byte[] data = new byte[12];
            byte[] length = BitConverter.GetBytes((int) length_up_down.Value);
            byte[] start = BitConverter.GetBytes((int) start_up_down.Value);
            byte[] amount = BitConverter.GetBytes((int) end_up_down.Value);
            length.CopyTo(data, 0);
            start.CopyTo(data, 4);
            amount.CopyTo(data, 8);

            //Get the fields
            byte[] field_data = Requester.send(data, network_codes.range_request);

            //The amount of bytes one field takes up (required for parsing the fields
            int field_length = (int) length_up_down.Value;

            //Loop through the total amount of fields
            for (int i = 0; i < field_data.Length/field_length; i++)
            {

                Grid grid = new Grid(); // Grid in which we will put all stones
                #region grid creation
                // Add 7 colums and 6 rows
                for (int j = 0; j < 7; j++)
                {
                    var coldef = new ColumnDefinition {Width = new GridLength(1, GridUnitType.Star)};
                    grid.ColumnDefinitions.Add(coldef);
                }
                for (int j = 0; j < 6; j++)
                {
                    var rowdef = new RowDefinition {Height = new GridLength(1, GridUnitType.Star)};
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
                // Get the right bytes for the current field and parse that array to a field
                byte[] field_bytes = new byte[field_length];
                Buffer.BlockCopy(field_data, field_length*i, field_bytes, 0, field_length);
                Field field = field_bytes.decompressField();
                container.Tag = fields.Count;
                fields.Add(field);
                //Loop through the whole field to set all the appropriate colors in the grid
                #region grid filling
                for (int x = 0; x < field.Width; x++)
                {
                    for (int y = 0; y < field.Height; y++)
                    {
                        Ellipse ellipse = new Ellipse();
                        grid.Children.Add(ellipse);
                        Color color;
                        switch (field.getCellPlayer(x, field.Height - y - 1))
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

                #endregion
            }
        }

        private void field_select(object sender, MouseEventArgs e)
        {
            // Change the previous selected grid to transparent and the current one to darkblue
            if (selected_grid != null) 
                selected_grid.Background = Brushes.Transparent;
            selected_grid = (Grid) sender;
            (selected_grid).Background = Brushes.DarkBlue; 

            // Get the associated field of the grid
            int index = (int) (selected_grid).Tag;
            Field field = fields[index];
            Winning_chances.Children.Clear();

            // Get the data of that field and parse it
            byte[] data = Requester.send(field.getStorage(), network_codes.details_request);
            if (data.Length % 4 != 0)
                throw new FormatException("Byte array not dividable by 4 and thus cannot contain only integers");

            //The first 7 integers are the total games played in those columns
            int[] total = new int[7];
            for (int i = 0; i < data.Length / 8; i++)
            {
                byte[] arr = new byte[4];
                Array.Copy(data, i * 4, arr, 0, 4);
                total[i] = BitConverter.ToInt32(arr, 0);
            }

            //The second 7 integers are the winning games in those columns
            int[] wins = new int[7];
            for (int i = 0; i < data.Length / 8; i++)
            {
                byte[] arr = new byte[4];
                Array.Copy(data, (i + 7) * 4, arr, 0, 4);
                wins[i] = BitConverter.ToInt32(arr, 0);
            }

            //Display the data
            StackPanel panel_title = new StackPanel();
            Winning_chances.Children.Add(panel_title);
            panel_title.Children.Add(new Label { Content = " ", FontSize = 9 });
            panel_title.Children.Add(new Label { Content = "wins:" });
            panel_title.Children.Add(new Label { Content = "total:" });
            for (int i = 0; i < total.Length; i++)
            {
                StackPanel panel = new StackPanel();
                Winning_chances.Children.Add(panel);
                panel.Children.Add(new Label { Content = $"Col {i+1}", FontSize = 9 });
                panel.Children.Add(new Label { Content = wins[i] });
                panel.Children.Add(new Label { Content = total[i] });
            }
        }

        private void slider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            // Set the global size to the new value so all new field_grids will have this size too
            size = (int) slider.Value;

            // Change the size of all existing field_grids
            foreach (var grid in grids)
            {
                grid.Width = size;
                grid.Height = size;
            }
        }
    }
}
