using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Security.Policy;
using System.Text;
using System.Threading.Tasks;
using Engine;
using Botclient;

namespace Simulator
{
    class Simulator
    {
        static Game game;
        private static bool tie = false;

        /// <summary>
        /// 
        /// </summary>
        /// <param name="args"></param>
        private static void Main(string[] args)
        {
            while (true)
            {

                game = new Game(7, 6);
                Console.WriteLine($"Created new game of {game.get_field().Width} by {game.get_field().Height}");
                List<IPlayer> players = new List<IPlayer>()
                {
                    new Bot(Engine.players.Alice),
                    new Bot(Engine.players.Bob)
                };

                do
                {
                    do_turn(players.Find(player => player.player == game.next_players));
                } while (!(check_for_win() || tie));
                if (tie)
                {
                    Console.WriteLine("The game is a tie");
                }
                else if (game.has_won(Engine.players.Alice))
                {
                    Console.WriteLine("Alice won the game");
                }
                else
                {
                    Console.WriteLine("Bob won the game");
                }
                Console.ReadLine();
            }

        }
        /// <summary>
        /// Tries to execute the given turn from the given player.
        /// If the given turn is not possible for whatever reason, the player is asked again.
        /// This continues forever;
        /// TODO: prevent infinite loop
        /// </summary>
        /// <param name="player"></param>
        static void do_turn(IPlayer player)
        {
            string s = "";
            int counter = 0;
            while (!game.add_stone(player.get_turn(game.get_field()), player.player, ref s))
            {
                if (s != "")
                {
                    counter++;
                    Console.WriteLine($"{s} ({counter} tries)");
                    if (counter > 100)
                    {
                        tie = true;
                        break;
                    }
                }
            }
        }
        static bool check_for_win()
        {
            return game.has_won(players.Alice) || game.has_won(players.Bob);
        }
    }
}
