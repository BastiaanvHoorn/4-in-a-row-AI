using System;
using System.Collections.Generic;
using System.Linq;
using Engine;
using Botclient;

namespace Simulator
{
    class Simulator
    {

        static Game game;
        private static int width = 7;
        private static int height = 6;
        private static int max_games = 1;
        private static int games_played;
        private static int games_won_alice;
        private static int games_won_bob;
        private enum log_mode
        {
            silent,         //Nothing is logged
            only_errors,    //Only errors are logged
            essential,      //Loggs all less common messages and errors (default)
            verbose,        //Loggs a lot of things
            debug           //Loggs everything
        }

        static private log_mode mode = log_mode.essential;
        /// <summary>
        /// Tries to execute the given turn from the given player.
        /// If the given turn is not possible for whatever reason, the player is asked again.
        /// Returns false if it fails to drop a stone;
        /// </summary>
        /// <param name="player"></param>
        static bool do_turn(IPlayer player)
        {
            if (game.stones_count == game.width * game.height) //If the whole field is full of stones and no one has won, it's a tie
            {
                return false;
            }
            string s = "";
            int counter = 0;
            while (!game.add_stone(player.get_turn(game.get_field()), player.player, ref s)) //Try to add a stone fails. If that fails, log the error and try it again.
            {
                counter++;
                Console.WriteLine($"{s} ({counter} tries)");
                if (counter >= 100) //If we tried enough times to add a stone, exit the game to prevent infinite looping
                {
                    Console.WriteLine("Exceeded maximum of tries for a turn");
                    return false;
                }
            }
            return true;
        }

        static void do_game()
        {
            game = new Game((byte)width, (byte)height);
            Console.WriteLine($"Created new game of {game.get_field().Width} by {game.get_field().Height}");
            List<IPlayer> _players = new List<IPlayer>() //A fancy list to prevent the use of if-statements
                {
                    new Bot(players.Alice),
                    new Bot(players.Bob)
                };
            bool tie = false;
            do
            {
                tie = !do_turn(_players.Find(player => player.player == game.next_players)); //Execute the turn the player who's turn it is. If do_turn returns false, it is a tie;
            } while (!(game.has_won(players.Alice) || game.has_won(players.Bob) || !tie)); //Keep processing turns if no one has won and it isn't a tie
            if (tie)
            {
                Console.WriteLine("The game is a tie");
            }
            else if (game.has_won(players.Alice))
            {
                Console.WriteLine("Alice won the game");
            }
            else
            {
                Console.WriteLine("Bob won the game");
            }
            Console.ReadLine();
        }

        private static void init(string[] _args)
        {
            List<string> args = new List<string>(_args);
            int log_mode_index = (args.IndexOf("-l"));
            if (log_mode_index != -1)
            {
                try
                {
                    byte log_mode_arg = byte.Parse(args[log_mode_index + 1]);
                    if (log_mode_arg < 5)
                    {
                        mode = (log_mode) log_mode_arg;
                        Console.WriteLine($"Log mode set to {mode}");
                    }
                    else
                    {
                        Console.WriteLine($"{log_mode_arg} is no valid log mode.");
                        Console.WriteLine($"Log mode defaulted to {mode} (essential)");
                    }
                }
                catch (FormatException)
                {
                    Console.WriteLine("log mode wasn't given in the right format.");
                    Console.WriteLine($"Log mode defaulted to {mode}");
                }
            }

            int width_index = (args.IndexOf("-w"));
            if (width_index != -1)
            {
                try
                {
                    width = byte.Parse(args[width_index + 1]);
                    Console.WriteLine($"Set width to {width}");
                }
                catch (FormatException)
                {
                    Console.WriteLine("Width wasn't given in the right format.");
                    Console.WriteLine($"Width defaulted to {width}");
                }
            }

            int height_index = (args.IndexOf("-h"));
            if (height_index != -1)
            {
                try
                {
                    height = byte.Parse(args[height_index + 1]);
                    Console.WriteLine($"Set height to {height}");
                }
                catch (FormatException)
                {
                    Console.WriteLine("Height wasn't given in the right format.");
                    Console.WriteLine($"Height defaulted to {height}");
                }
            }

            int max_games_index = (args.IndexOf("-g"));
            if (max_games_index != -1)
            {
                try
                {
                    max_games = byte.Parse(args[max_games_index + 1]);
                    Console.WriteLine($"Set amount of games to {max_games}");
                }
                catch (FormatException)
                {
                    Console.WriteLine("Amount of games wasn't given in the right format.");
                    Console.WriteLine($"Amount of games defaulted to {max_games}");
                }
            }

            Console.ReadLine();
        }


        /// <param name="args">
        /// -l  [0-5]   log mode (default 2) <see cref="log_mode"/>
        /// -w  [>0]    width  of the playing field (default 7)
        /// -h  [>0]    height of the playing field (default 6)
        /// -g  [>0]    amount of games to simulate (default 1)
        /// </param>
        private static void Main(string[] args)
        {
            init(args);
            do_game();

        }
    }
}
