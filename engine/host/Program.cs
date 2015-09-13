using System.Windows.Forms;

namespace _4_in_a_row
{
    static class Program
    {
        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        static void Main()
        {
            Game game = new Game(7,6);
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Application.Run(new game_interface(2, game));

        }
    }
}
