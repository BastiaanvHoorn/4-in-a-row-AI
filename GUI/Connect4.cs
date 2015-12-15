using System;
using System.Windows.Forms;
using Engine;
using Util;

namespace connect4
{
    static class connect4
    {
        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        static void Main(string[] args)
        {
            var game = new Game(7,6);
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            game_interface gui = new game_interface(byte.Parse(args[0]), log_modes.essential, game);
            Application.Run(gui);

        }
    }
}
