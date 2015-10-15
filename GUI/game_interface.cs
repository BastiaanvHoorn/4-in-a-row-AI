using System;
using System.Drawing;
using System.Windows.Forms;
using Botclient;
using Engine;

namespace GUI
{
    public partial class game_interface : Form
    {
        readonly Game game;
        private readonly int players; //All non-bot players 
        private Label[][] labels;
        private byte[][] field;
        private const int x = 140;
        private const int y = 25;
        private const int dx = 25;
        private const int dy = 25;
        private Botclient.Bot bot_bob;

        /// <summary>
        /// Constructor for the game
        /// </summary>
        /// <param name="players">The amount of players that is gonna play, 0 for bot vs bot, 1 for player vs bot and 2 for player vs player</param>
        public game_interface(int players, Game game)
        {
            if (players < 1)
            {
                throw new NotImplementedException("There is currently no support for bot vs bot");
            }
            if (players > 2)
            {
                throw new NotImplementedException("You can only play with a maximum of 2 players");
            }
            InitializeComponent();
            this.game = game;
            this.players = players;
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            if (players == 1)
            {
                numeric_Bob.Visible = false;
                button_Bob.Visible = false;
                bot_bob = new Bot(player.Bob);
            }
            field = game.get_field();
            //Display the field
            labels = new Label[field.Length][];
            for (int i = 0; i < labels.Length; i++)
            {
                labels[i] = new Label[field[i].Length];
                for (int j = 0; j < labels[i].Length; j++)
                {
                    labels[i][j] = new Label
                    {
                        Location = new Point(x + i * dx, y + j * dy),
                        Size = new Size(dx - 2, dy - 2)
                    };
                    Controls.Add(labels[i][j]);
                }
            }
            update_field();
        }

        /// <summary>
        /// Update the field and check if someone has won
        /// </summary>
        private void update_field()
        {
            while (true)
            {
                field = game.get_field();
                int j_offset = field[0].Length - 1;
                for (int i = 0; i < labels.Length; i++)
                {
                    for (int j = 0; j < labels[i].Length; j++)
                    {
                        int k = j_offset - j;
                        switch (field[i][j])
                        {
                            case 0:
                                labels[i][k].BackColor = Color.Wheat;
                                break;
                            case 1:
                                labels[i][k].BackColor = Color.Red;
                                break;
                            default:
                                labels[i][k].BackColor = Color.Blue;
                                break;
                        }
                    }
                }
                if (game.has_won(player.Alice))
                {
                    Console.WriteLine("Alice has won");
                    disable_ui(player.Alice);
                    disable_ui(player.Bob);
                }
                else if (game.has_won(player.Bob))
                {
                    Console.WriteLine("bob has won");
                    disable_ui(player.Alice);
                    disable_ui(player.Bob);
                }
                else
                {
                    if (game.next_player == player.Alice)
                    {
                        disable_ui(player.Bob, true);
                    }
                    else
                    {
                        disable_ui(player.Alice, true);
                        //If there are less then 2 real players, bob is played by a bot
                        if (players < 2)
                        {
                            byte row;
                            string s = "";
                            do
                            {
                                if (s != "")
                                {
                                    Console.WriteLine(s);
                                }
                                row = bot_bob.get_next_move(field);
                            } while (!game.add_stone(row, player.Bob, ref s));
                            continue;
                        }
                    }
                }
                break;
            }
        }

        private void disable_ui(player player, bool enable_other = false)
        {
            if (player == player.Alice)
            {
                button_Alice.Enabled = false;
                numeric_Alice.Enabled = false;
                if (enable_other)
                {
                    button_Bob.Enabled = true;
                    numeric_Bob.Enabled = true;
                }
            }
            else
            {
                button_Bob.Enabled = false;
                numeric_Alice.Enabled = false;
                if (!enable_other) return;
                button_Alice.Enabled = true;
                numeric_Alice.Enabled = true;
            }
        }
        private void button_Alice_Click(object sender, EventArgs e)
        {
            string s = "";
            if (!game.add_stone((byte)numeric_Alice.Value, player.Alice, ref s))
            {
                Console.WriteLine(s);
            }
            else
            {
                update_field();
            }
        }
        private void button_Bob_Click(object sender, EventArgs e)
        {
            string s = "";
            if (!game.add_stone((byte)numeric_Bob.Value, player.Bob, ref s))
            {
                Console.WriteLine(s);
            }
            else
            {
                update_field();
            }
        }
    }
}
