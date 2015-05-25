using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using _4_in_a_row_lib;

namespace _4_in_a_row_
{
    public partial class Form1 : Form
    {
        Game game = new Game();
        private Label[][] labels;
        private byte[][] field;
        private int x = 100;
        private int y = 100;
        private int dx = 30;
        private int dy = 30;
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
                    labels[i][j].BackColor = Color.Black;
                    labels[i][j].Size = new Size(dx - 2, dy - 2);
                    Controls.Add(labels[i][j]);
                }
            }
            //if (field[i][j] == 0)
            //{
            //    labels[i][j].BackColor = Color.Wheat;
            //}
            //else if (field[i][j] == 1)
            //{
            //    labels[i][j].BackColor = Color.Azure;
            //}
            //else
            //{
            //    labels[i][j].BackColor = Color.Blue;
            //}
        }
    }
}
