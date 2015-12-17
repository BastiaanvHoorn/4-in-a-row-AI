using System.Windows.Forms;
using System.Windows.Media;

namespace connect4
{
    public static class Colors
    {
        public static class Alice
        {
            public static Color standard = System.Windows.Media.Colors.DarkRed;
            public static Color highlight = System.Windows.Media.Colors.Red;
            public static Color ghost = System.Windows.Media.Colors.IndianRed;
        }

        public static class Bob
        {
            public static Color standard = System.Windows.Media.Colors.DarkBlue;
            public static Color highlight = System.Windows.Media.Colors.Blue;
            public static Color ghost = System.Windows.Media.Colors.DodgerBlue;
        }

        public static Color empty = System.Windows.Media.Colors.Black;
    }

}