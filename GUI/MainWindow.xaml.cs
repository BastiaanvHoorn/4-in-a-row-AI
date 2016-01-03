using System;
using System.CodeDom;
using System.Collections.Generic;
using System.Net;
using System.Net.NetworkInformation;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
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

        private Label[][] labels;
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

            // Create the grid for the game
            for (int i = 0; i < Width_up_down.Value; i++)
            {
                var coldef = new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) };
                label_grid.ColumnDefinitions.Add(coldef);
            }
            for (int i = 0; i < Height_up_down.Value; i++)
            {
                var rowdef = new RowDefinition { Height = new GridLength(1, GridUnitType.Star) };
                label_grid.RowDefinitions.Add(rowdef);
            }
            labels = new Label[(uint) Width_up_down.Value][];

            // TODO: add circle labels for the stones
            // Add some properties to all the labels
            for (int x = 0; x < Width_up_down.Value; x++)
            {
                labels[x] = new Label[(uint)Height_up_down.Value];
                for (int y = 0; y < Height_up_down.Value; y++)
                {
                    Label label = new Label();
                    label_grid.Children.Add(label);

                    label.MouseEnter += label_enter;
                    label.MouseRightButtonDown += label_click;
                    label.MouseLeftButtonDown += label_click;
                    label.SetValue(Grid.ColumnProperty, x);
                    label.SetValue(Grid.RowProperty, y);
                    labels[x][y] = label;

                    // Add a tag so we can retrieve the label later again
                    label.Tag = new byte[] { (byte)x, (byte)y };
                }
            }
        }

        private void Start_Click(object sender, RoutedEventArgs e)
        {
            start_game();
        }

        private void start_game()
        {
            game = new Game((byte)Width_up_down.Value, (byte)Height_up_down.Value);
            settings_button.Visibility = Visibility.Hidden;
            rematch_button.Visibility = Visibility.Hidden;
            alice = new GUI_player(this, players.Alice);
            if (AI_checkbox.IsChecked.Value)
            {
                string s;
                if (!check_network_input_validity())
                    return;
                if (Requester.ping(address, port, out s))
                {
                    bob = new Bot(players.Bob, (byte) difficulty_slider.Value, address, port, (bool)smart_moves_checkbox.IsChecked);
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
            byte x = ((byte[])((Label)sender).Tag)[0];
            col_clicked = x;
            update_field(((byte[])((Label)sender).Tag)[0], ((byte[])((Label)sender).Tag)[1]);
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
                byte[] coords = (byte[])((Label)sender).Tag;
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
                    switch (field.getCellPlayer(x, field.Height - 1 - y))
                    {
                        case players.Alice:
                            if (mousex == x || mousey == y)
                            {
                                labels[x][y].Background = new SolidColorBrush(Colors.Alice.highlight);
                            }
                            else
                            {
                                labels[x][y].Background = new SolidColorBrush(Colors.Alice.standard);
                            }
                            break;
                        case players.Bob:
                            if (mousex == x || mousey == y)
                            {
                                labels[x][y].Background = new SolidColorBrush(Colors.Bob.highlight);
                            }
                            else
                            {
                                labels[x][y].Background = new SolidColorBrush(Colors.Bob.standard);
                            }
                            break;
                        default:
                            if (mousex == x || mousey == y)
                            {
                                labels[x][y].Background = new SolidColorBrush((game.next_player == players.Alice) ? Colors.Alice.ghost : Colors.Bob.ghost);
                            }
                            else
                            {
                                labels[x][y].Background = new SolidColorBrush(Colors.empty);
                            }
                            break;
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
    }
    static class ui_codes
    {
        public static string valid = "1";
        public static string invalid = "0";
    }
}
