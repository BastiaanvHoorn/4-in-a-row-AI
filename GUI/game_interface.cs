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
        private readonly int players; //All non-bot player 
        private Label[][] labels;
        private Field field;
        private const int x = 140;
        private const int y = 25;
        private const int dx = 25;
        private const int dy = 25;
        private Bot bot_bob;

        /// <summary>
        /// Constructor for the game
        /// </summary>
        /// <param name="players">The amount of player that is gonna play, 0 for bot vs bot, 1 for player vs bot and 2 for player vs player</param>
        public game_interface(int players, Game game)
        {
            if (players < 1)
            {
                throw new NotImplementedException("There is currently no support for bot vs bot");
            }
            if (players > 2)
            {
                throw new NotImplementedException("You can only play with a maximum of 2 player");
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
                bot_bob = new Bot(Engine.players.Bob, log_modes.essential);
            }
            field = game.get_field();
            //Display the field
            labels = new Label[field.Width][];
            for (int i = 0; i < labels.Length; i++)
            {
                labels[i] = new Label[field.Height];
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
                int j_offset = field.Height - 1;
                for (int i = 0; i < labels.Length; i++)
                {
                    for (int j = 0; j < labels[i].Length; j++)
                    {
                        int k = j_offset - j;
                        switch (field.getCellPlayer(i,j))
                        {
                            case Engine.players.Empty:
                                labels[i][k].BackColor = Color.Wheat;
                                break;
                            case Engine.players.Alice:
                                labels[i][k].BackColor = Color.Red;
                                break;
                            default:
                                labels[i][k].BackColor = Color.Blue;
                                break;
                        }
                    }
                }
                if (game.has_won(Engine.players.Alice))
                {
                    Console.WriteLine("Alice has won");
                    disable_ui(Engine.players.Alice);
                    disable_ui(Engine.players.Bob);
                    Console.WriteLine(game.history.ToString());
                }
                else if (game.has_won(Engine.players.Bob))
                {
                    Console.WriteLine("bob has won");
                    disable_ui(Engine.players.Alice);
                    disable_ui(Engine.players.Bob);
                }
                else
                {
                    if (game.next_players == Engine.players.Alice)
                    {
                        disable_ui(Engine.players.Bob, true);
                    }
                    else
                    {
                        disable_ui(Engine.players.Alice, true);
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
                                row = bot_bob.get_turn(field);
                            } while (!game.add_stone(row, Engine.players.Bob, ref s));
                            continue;
                        }
                    }
                }
                break;
            }
        }

        private void disable_ui(players player, bool enable_other = false)
        {
            if (player == Engine.players.Alice)
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
            if (!game.add_stone((byte)numeric_Alice.Value, Engine.players.Alice, ref s))
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
            if (!game.add_stone((byte)numeric_Bob.Value, Engine.players.Bob, ref s))
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
