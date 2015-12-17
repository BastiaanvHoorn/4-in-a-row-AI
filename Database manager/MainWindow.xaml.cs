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

namespace Database_manager
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
        }

        private void button_Click(object sender, RoutedEventArgs e)
        {
        }

        private void address_textbox_TextChanged(object sender, TextChangedEventArgs e)
        {
            string[] s = address_textbox.Text.Split('.');
            if (s.Length != 4)
            {
                address_textbox.Background = new SolidColorBrush(Color.FromRgb(225, 110, 110));
                return;
            }

            foreach (var n in s)
            {
                byte b;
                if (byte.TryParse(n, out b)) continue;
                address_textbox.Background = new SolidColorBrush(Color.FromRgb(225, 110, 110));
                return;
            }
            address_textbox.Background = new SolidColorBrush(Colors.White);

        }

        private void port_textbox_TextChanged(object sender, TextChangedEventArgs e)
        {
            int i;
            if (int.TryParse(port_textbox.Text, out i))
            {
                if (i > 0 && i < 100000)
                {
                    port_textbox.Background = new SolidColorBrush(Colors.White);
                    return;
                }
            }
            port_textbox.Background = new SolidColorBrush(Color.FromRgb(225, 110, 110));
        }
    }
}
