using System;
using System.Drawing;
using System.Text;
using System.Windows.Forms;

namespace _4_in_a_row
{
    public partial class game_interface : Form
    {
        readonly Game game;
        private readonly int players;
        private Label[][] labels;
        private byte[][] field;
        private const int x = 140;
        private const int y = 25;
        private const int dx = 25;
        private const int dy = 25;

        /// <summary>
        /// Constructor for the game
        /// </summary>
        /// <param name="players">The amount of players that is gonna play, 0 for bot vs bot, 1 for player vs bot and 2 for player vs player</param>
        public game_interface(int players, Game _game)
        {
            if(players < 1)
            {
                throw new NotImplementedException("There is currently no support for bot vs bot");
            }
            if(players > 2)
            {
                throw new NotImplementedException("You can only play with a maximum of 2 players");
            }
            InitializeComponent();
            game = _game;
            this.players = players;
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            if (players == 1)
            {
                numeric_Bob.Enabled = false;
                button_Bob.Enabled = false;
            }
            field = game.get_field();
            //Display the field
            labels = new Label[field.Length][];
            for(int i = 0; i < labels.Length; i ++)
            {
                labels[i] = new Label[field[i].Length];
                for (int j = 0; j < labels[i].Length; j++)
                {
                    labels[i][j] = new Label
                    {
                        Location = new Point(x + i*dx, y + j*dy),
                        BackColor = Color.Wheat,
                        Size = new Size(dx - 2, dy - 2)
                    };
                    Controls.Add(labels[i][j]);
                }
            }
        }
        /// <summary>
        /// Update the field and check if someone has won
        /// </summary>
        private void update_field()
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
                button_Alice.Enabled = false;
                button_Bob.Enabled = false;
                numeric_Alice.Enabled = false;
                numeric_Bob.Enabled = false;
            }
            else if (game.has_won(player.Bob))
            {
                Console.WriteLine("bob has won");
                button_Alice.Enabled = false;
                button_Bob.Enabled = false;
                numeric_Alice.Enabled = false;
                numeric_Bob.Enabled = false;
            }
        }

        private void button_Alice_Click(object sender, EventArgs e)
        {
            if(!game.add_stone((byte)numeric_Alice.Value, player.Alice))
            {
                Console.WriteLine("failed to drop a stone (is this row already full?");
            }
            else
            {
                update_field();
            }
        }

        private void button_Bob_Click(object sender, EventArgs e)
        {
            if(!game.add_stone((byte)numeric_Bob.Value, player.Bob))
            {
                Console.WriteLine("failed to drop a stone (is this row already full?)");
            }
            else
            {
                update_field();
            }
        }
    }
}
