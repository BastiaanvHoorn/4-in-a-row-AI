using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using lib;

namespace host
{
    public partial class Form1 : Form
    {
        Game game = new Game();
        private Label[][] labels;
        private byte[][] field;
        private int x = 140;
        private int y = 25;
        private int dx = 25;
        private int dy = 25;
        public Form1()
        {
            InitializeComponent();
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            field = game.get_field();
            labels = new Label[field.Length][];
            for(int i = 0; i < labels.Length; i ++)
            {
                labels[i] = new Label[field[i].Length];
                for (int j = 0; j < labels[i].Length; j++)
                {
                    labels[i][j] = new Label();
                    labels[i][j].Location = new Point(x + i * dx, y + j * dy);
                    labels[i][j].BackColor = Color.Wheat;
                    labels[i][j].Size = new Size(dx - 2, dy - 2);
                    Controls.Add(labels[i][j]);
                }
            }
        }
        private void update_field()
        {
            field = game.get_field();
            int j_offset = field[0].Length - 1;
            for (int i = 0; i < labels.Length; i++)
            {
                for (int j = 0; j < labels[i].Length; j++)
                {
                    int k = j_offset - j;
                    if (field[i][j] == 0)
                    {
                        labels[i][k].BackColor = Color.Wheat;
                    }
                    else if (field[i][j] == 1)
                    {
                        labels[i][k].BackColor = Color.Red;
                    }
                    else
                    {
                        labels[i][k].BackColor = Color.Blue;
                    }
                }
            }
            if (game.has_won(true) == true)
            {
                Console.WriteLine("Alice has won");
            }
            else if (game.has_won(true) == true)
            {
                Console.WriteLine("bob has won");
            }
        }

        private void button_Alice_Click(object sender, EventArgs e)
        {
            if(!game.add_stone((byte)numeric_Alice.Value, true))
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
            if(!game.add_stone((byte)numeric_Bob.Value, false))
            {
                Console.WriteLine("faild to drop a stone (is this row already full?)");
            }
            else
            {
                update_field();
            }
        }
    }
}
