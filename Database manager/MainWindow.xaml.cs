using System;
using System.Collections.Generic;
using System.Linq;
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
            if (address_textbox.Text == string.Empty)
            {
                address_textbox.BorderBrush = new SolidColorBrush(Colors.Red);
            }
            if (port_textbox.Text == string.Empty)
            {
                port_textbox.BorderBrush = new SolidColorBrush(Colors.Red);
            }
        }

        private void address_textbox_TextChanged(object sender, TextChangedEventArgs e)
        {

        }
    }
}
