using System;
using System.Drawing;
using System.Threading.Tasks;
using System.Windows.Forms;
using Botclient;
using Engine;

namespace connect4
{
    public partial class game_interface : Form
    {
        private readonly Game game;
        private readonly int players; //All non-bot players 
        private Label[][] labels;
        private Field field;
        private const int x = 140;
        private const int y = 25;
        private const int dx = 25;
        private const int dy = 25;
        private IPlayer alice;
        private IPlayer bob;
        private bool alice_button_clicked = false;
        private bool bob_button_clicked = false;

        /// <summary>
        /// Constructor for the game
        /// </summary>
        /// <param name="players">The amount of players that is gonna play, 0 for bot vs bot, 1 for player vs bot and 2 for player vs player</param>
        public game_interface(int players, Game game)
        {
            InitializeComponent();
            if (players < 1)
            {
                throw new NotImplementedException("There is currently no support for bot vs bot");
            }
            if (players > 2)
            {
                throw new NotImplementedException("You can only play with a maximum of 2 players");
            }
            this.game = game;
            this.players = players;
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            alice = new GUI_player(this, Engine.players.Alice);
            if (players == 1)
            {
                bob = new Bot(Engine.players.Bob);
                numeric_Bob.Visible = false;
                button_Bob.Visible = false;
            }
            else
            {
                bob = new GUI_player(this, Engine.players.Bob);
            }
            create_field();
        }


        private async void loop()
        {
            do
            {
                if (game.next_player == Engine.players.Alice)
                {
                    await Task.Factory.StartNew(() => do_turn(alice));
                    disable_ui(Engine.players.Alice, true);
                }
                else
                {
                    await Task.Factory.StartNew(() => do_turn(bob));
                    disable_ui(Engine.players.Bob, true);
                }
                update_field();
            } while (!check_for_win());
            //TODO: Add replay stuff
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

        private bool check_for_win()
        {
            if (game.has_won(Engine.players.Alice))
            {
                Console.WriteLine("Alice has won");
                disable_ui(Engine.players.Alice);
                disable_ui(Engine.players.Bob);
                return true;
            }
            else if (game.has_won(Engine.players.Bob))
            {
                Console.WriteLine("bob has won");
                disable_ui(Engine.players.Alice);
                disable_ui(Engine.players.Bob);
                return true;
            }
            return false;
        }

        public void create_field()
        {
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
        }
        /// <summary>
        /// Update the field and check if someone has won
        /// </summary>
        public void update_field()
        {
            //Update the color of the tiles of the field
            field = game.get_field();
            int j_offset = field.Height - 1;
            for (int i = 0; i < labels.Length; i++)
            {
                for (int j = 0; j < labels[i].Length; j++)
                {
                    int k = j_offset - j;
                    switch (field.getCellPlayer(i, j))
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
        }

        public void disable_ui(players player, bool enable_other = false)
        {
            if (player == Engine.players.Alice)
            {
                button_Alice.Enabled = false;
                numeric_Alice.Enabled = false;
                if (!enable_other) return;
                button_Bob.Enabled = true;
                numeric_Bob.Enabled = true;
                return;
            }
            button_Bob.Enabled = false;
            numeric_Alice.Enabled = false;
            if (!enable_other) return;
            button_Alice.Enabled = true;
            numeric_Alice.Enabled = true;
        }

        private void button_start_Click(object sender, EventArgs e)
        {
            label1.Visible = true;
            label2.Visible = true;
            button_start.Visible = false;
            button_Alice.Visible = true;
            button_Bob.Visible = true;
            numeric_Alice.Visible = true;
            numeric_Bob.Visible = true;
            disable_ui(Engine.players.Bob, true);
            update_field();
            loop();
        }
        public bool get_button_pressed(players player)
        {
            if (player == Engine.players.Alice)
            {
                if (!alice_button_clicked)
                {
                    return false;
                }
                alice_button_clicked = false;
                return true;
            }

            if (!bob_button_clicked)
            {
                return false;
            }
            bob_button_clicked = false;
            return true;

        }
        public byte get_numeric(players player)
        {
            if (player == Engine.players.Alice)
            {
                return (byte)numeric_Alice.Value;
            }
            return (byte)numeric_Bob.Value;
        }
        private void button_Alice_Click(object sender, EventArgs e)
        {
            alice_button_clicked = true;
        }
        private void button_Bob_Click(object sender, EventArgs e)
        {
            bob_button_clicked = true;
        }
    }

}
