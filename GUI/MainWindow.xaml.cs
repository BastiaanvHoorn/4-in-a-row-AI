using System;
using System.CodeDom;
using System.Collections.Generic;
using System.Net;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Shapes;
using Engine;
using Botclient;
using Utility;
using Label = System.Windows.Controls.Label;
using players = Engine.players;

namespace connect4
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private IPlayer alice;
        private IPlayer bob;

        private Label[][] bg_labels;
        private Ellipse[][] stone_elipses;
        private Game game;
        public byte? col_clicked;
        private IPAddress address;
        private ushort port;
        public MainWindow()
        {
            InitializeComponent();

            // Make sure the right controls are visible
            settings_grid.Visibility = Visibility.Visible;
            game_grid.Visibility = Visibility.Collapsed;

        }

        private void Start_Click(object sender, RoutedEventArgs e)
        {
            init_field();
            start_game();
        }

        private void init_field()
        {
            // Clear all previous column and row defenitions
            label_grid.ColumnDefinitions.Clear();
            label_grid.RowDefinitions.Clear();
            byte width = (byte)Width_up_down.Value;
            byte height = (byte)Height_up_down.Value;
            // Create the new column and row defenitions 
            for (int i = 0; i < width; i++)
            {
                var coldef = new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) };
                label_grid.ColumnDefinitions.Add(coldef);
            }
            for (int i = 0; i < height; i++)
            {
                var rowdef = new RowDefinition { Height = new GridLength(1, GridUnitType.Star) };
                label_grid.RowDefinitions.Add(rowdef);
            }

            // Initialize the arrays to store the content for the grid in
            bg_labels = new Label[width][];
            stone_elipses = new Ellipse[width][];
            for (int x = 0; x < width; x++)
            {
                bg_labels[x] = new Label[height];
                stone_elipses[x] = new Ellipse[height];
                for (int y = 0; y < Height_up_down.Value; y++)
                {
                    Label bg = new Label();
                    Ellipse stone = new Ellipse();

                    label_grid.Children.Add(bg);
                    label_grid.Children.Add(stone);

                    // All the listeners must be set for both the background and the elipses as the elipses cover part of the background but not all of it
                    bg.MouseEnter += label_enter;
                    bg.MouseRightButtonDown += label_click;
                    bg.MouseLeftButtonDown += label_click;
                    stone.MouseEnter += label_enter;
                    stone.MouseRightButtonDown += label_click;
                    stone.MouseLeftButtonDown += label_click;

                    bg.SetValue(Grid.ColumnProperty, x);
                    bg.SetValue(Grid.RowProperty, y);
                    stone.SetValue(Grid.ColumnProperty, x);
                    stone.SetValue(Grid.RowProperty, y);

                    bg_labels[x][y] = bg;
                    stone_elipses[x][y] = stone;

                    bg.Background = new SolidColorBrush(Colors.empty);
                    stone.Fill = new SolidColorBrush(Colors.transparent);

                    // Create some tags so we can track them down later
                    bg.Tag = new[] { x, y };
                    stone.Tag = new[] { x, y };
                }
            }
        }
        private void start_game()
        {
            game = new Game((byte)Width_up_down.Value, (byte)Height_up_down.Value);
            settings_button.Visibility = Visibility.Hidden;
            rematch_button.Visibility = Visibility.Hidden;
            alice = new GUI_player(this, players.Alice);
            // Initialize the bot if the player wants one
            if (AI_checkbox.IsChecked.Value)
            {
                string s;
                if (!check_network_input_validity())
                    return;
                if (Requester.ping(address, port, out s))
                {
                    bob = new Database_bot(players.Bob, (byte)difficulty_slider.Value, address, port, (bool)smart_moves_checkbox.IsChecked);
                }
                else
                {
                    message_label.Content = "Cannot connect to server and thus cannot use AI";
                    return;
                }
            }
            else
            {
                bob = new GUI_player(this, players.Bob);
            }
            settings_grid.Visibility = Visibility.Collapsed;
            game_grid.Visibility = Visibility.Visible;
            update_field();
            message_label.Content = "Starting game. Good luck!";
            loop();
        }

        private async void loop()
        {
            do
            {
                if (game.next_player == players.Alice)
                {
                    await Task.Factory.StartNew(() => do_turn(alice));
                }
                else
                {
                    await Task.Factory.StartNew(() => do_turn(bob));
                }
                col_clicked = null;
                update_field();
            } while (!(game.has_won(players.Alice) || game.has_won(players.Bob)));
            message_label.Content = game.has_won(players.Alice) ? "Alice" : "Bob" + " has won";
            rematch_button.Visibility = Visibility.Visible;
            settings_button.Visibility = Visibility.Visible;

        }

        private void label_click(object sender, MouseEventArgs e)
        {
            int[] coords = (int[])((FrameworkElement)sender).Tag;
            int x = coords[0];
            col_clicked = (byte)x;
            update_field(coords[0], coords[1]);
        }

        private void do_turn(IPlayer player)
        {
            string s = "";
            byte column;
            do
            {
                if (s != "")
                {
                    Console.WriteLine(s);
                }
                column = player.get_turn(game.get_field());
            } while (!game.add_stone(column, player.player, ref s));
        }

        private void label_enter(object sender, MouseEventArgs e)
        {
            if (game != null)
            {
                int[] coords = (int[])((FrameworkElement)sender).Tag;
                update_field(coords[0], coords[1]);
            }
        }

        private void update_field(int? mousex = null, int? mousey = null)
        {
            Field field = game.get_field();
            for (int x = 0; x < field.Width; x++)
            {
                for (int y = 0; y < field.Height; y++)
                {
                    //if (mousex == x || mousey == y)
                    //{
                    //    bg_labels[x][y].Background = new SolidColorBrush((game.next_player == players.Alice) ? Colors.Alice.ghost : Colors.Bob.ghost);
                    //}
                    //else
                    //{
                    //    bg_labels[x][y].Background = new SolidColorBrush(Colors.empty);
                    //}
                    
                    // Color the fields with stones in them to the according players
                    if (field[x, y] == players.Alice)
                    {
                        stone_elipses[x][field.Height - y - 1].Fill = new SolidColorBrush(Colors.Alice.standard);
                    }
                    else if (field[x, y] == players.Bob)
                    {
                        stone_elipses[x][field.Height - y - 1].Fill = new SolidColorBrush(Colors.Bob.standard);
                    }

                    //Create a ghost image of the stone in the column that the player is hovering in
                    if (field[x, y] == players.Empty && (y == 0 || field[x, y - 1] != players.Empty))
                    {
                        if (mousex == x)
                        {
                            stone_elipses[x][field.Height - y - 1].Fill = new SolidColorBrush(
                                game.next_player == players.Alice
                                    ? Colors.Alice.ghost
                                    : Colors.Bob.ghost);
                        }

                        //Make sure to keep the rest of the stones transparent
                        else
                        {
                            stone_elipses[x][field.Height - y - 1].Fill = new SolidColorBrush(Colors.transparent);
                        }
                    }
                }
            }
        }

        private void rematch_button_Click(object sender, RoutedEventArgs e)
        {
            message_label.Content = string.Empty;
            start_game();
        }

        private void settings_button_Click(object sender, RoutedEventArgs e)
        {
            message_label.Content = string.Empty;
            game_grid.Visibility = Visibility.Collapsed;
            settings_grid.Visibility = Visibility.Visible;
        }

        private void address_textbox_TextChanged(object sender, TextChangedEventArgs e)
        {
            IPAddress _address;
            if (IPAddress.TryParse(address_textbox.Text, out _address))
            {
                address_textbox.Background = Brushes.White;
                address = _address;
                address_textbox.Tag = ui_codes.valid;
            }
            else
            {
                address_textbox.Background = new SolidColorBrush(Color.FromRgb(225, 110, 110));
                address_textbox.Tag = ui_codes.invalid;
            }
        }

        private void port_textbox_TextChanged(object sender, TextChangedEventArgs e)
        {
            ushort s;
            if (ushort.TryParse(port_textbox.Text, out s))
            {
                port_textbox.Background = new SolidColorBrush(System.Windows.Media.Colors.White);
                port = s;
                port_textbox.Tag = ui_codes.valid;
            }
            else
            {

                port_textbox.Background = new SolidColorBrush(Color.FromRgb(225, 110, 110));
                port_textbox.Tag = ui_codes.invalid;
            }
        }

        private bool check_network_input_validity()
        {
            if ((string)port_textbox.Tag == ui_codes.invalid)
            {
                port_textbox.Background = new SolidColorBrush(Color.FromRgb(225, 110, 110));
                return false;
            }
            if ((string)address_textbox.Tag == ui_codes.invalid)
            {
                address_textbox.Background = new SolidColorBrush(Color.FromRgb(225, 110, 110));
                return false;
            }
            return true;

        }

        private void rating_button_Click(object sender, RoutedEventArgs e)
        {
            rating_label.Content = game.get_field().rate();
        }
    }
    static class ui_codes
    {
        public static string valid = "1";
        public static string invalid = "0";
    }
}
