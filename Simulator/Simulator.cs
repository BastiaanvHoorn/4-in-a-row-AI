using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Xml.Schema;
using Engine;
using Botclient;
using Util;

namespace Simulator
{
    internal class Simulator
    {

        private readonly byte width = 7;
        private readonly byte height = 6;
        private readonly uint max_games = 1;
        private uint games_won_alice;
        private uint games_won_bob;
        private static Logger logger;
        /// <summary>
        /// Tries to execute the given turn from the given player.
        /// If the given turn is not possible for whatever reason, the player is asked again.
        /// Returns false if it fails to drop a stone;
        /// </summary>
        /// <param name="player"></param>
        private static bool do_turn(IPlayer player, Game game)
        {
            if (game.stones_count == game.width * game.height)
            //If the whole field is full of stones and no one has won, it's a tie
            {
                logger.log("The field is full, it is a tie if no one has won", log_modes.per_game, log_types.warning);
                return false;
            }
            string s = "";
            int counter = 0;
            while (!game.add_stone(player.get_turn(game.get_field(), logger.log_mode), player.player, ref s))
            //Try to add a stone fails. If that fails, log the error and try it again.
            {
                counter++;
                logger.log($"{s} ({counter} tries)", log_modes.debug);
                if (counter < 100) continue;
                logger.log("Exceeded maximum of tries for a turn", log_modes.debug, log_types.error);
                return false;
            }
            return true;
        }

        private players do_game(Game game, ref List<byte> history)
        {
            game = new Game(width, height);
            logger.log($"Created new game of {game.get_field().Width} by {game.get_field().Height}", log_modes.per_game);
            var _players = new List<IPlayer>() //A fancy list to prevent the use of if-statements
            {
                new Bot(players.Alice),
                new Bot(players.Bob)
            };
            while (true)
            {
                bool tie = !do_turn(_players.Find(player => player.player == game.next_player), game);
                //Execute the turn the player who's turn it is. If do_turn returns false, it is a tie;
                //First add the indication of the winner of the match to the history-list
                //Then add the history itself
                if (game.has_won(players.Alice))
                {
                    history.Add((byte)network_codes.game_history_alice);
                    history.AddRange(game.history);
                    return players.Alice;
                }
                if (game.has_won(players.Bob))
                {
                    history.Add((byte)network_codes.game_history_alice);
                    history.AddRange(game.history);
                    return players.Bob;
                }
                if (tie)
                {
                    return players.Empty;
                }
            }
        }

        private static void send_history(List<List<byte>> histories)
        {
            var data = new List<byte>();
            //Concatenate all the game-histories into one byte-array;
            foreach (var history in histories)
            {
                data.AddRange(history);
            }
            Stopwatch sw = new Stopwatch();
            logger.log($"Created game_history in {sw.ElapsedMilliseconds}ms. Starting to send now", log_modes.essential);
            Requester.send(data.ToArray(), network_codes.game_history_array, log_modes.essential);

        }

        private delegate string victory_message(int games_won);

        /// <summary>
        /// Loop through the given amount of games, and log some stuff in the meantime
        /// </summary>
        private void loop_games()
        {
            //A stopwatch to measure how much time we spend on simulating these games
            var sw = new Stopwatch();
            sw.Start();
            //Jagged array to store the history of all the games
            //TODO switch list to array
            var histories = new List<List<byte>>();
            for (int game_count = 0; game_count < max_games; game_count++)
            {
                var game = new Game(width, height);
                var history = new List<byte>();
                players victourious_player = do_game(game, ref history);

                int turns = history.Count - 1;
                //The amount of turns this game lasted. 1 is subtracted for the winner indication at the start.
                victory_message victory_message =
                    games_won => $"\t\t{games_won}th game after \t{(games_won < 10 ? "\t" : "")}{turns} turns";
                string games_left_message = $";\t {max_games - game_count - 1} of {max_games} game(s) left";
                switch (victourious_player)
                {
                    case players.Alice:
                        games_won_alice++;

                        histories.Add(history);

                        logger.log($"Alice won her {victory_message((int)games_won_alice)}{games_left_message}",
                            log_modes.per_game);
                        break;
                    case players.Bob:
                        games_won_bob++;

                        histories.Add(history);

                        logger.log($"Bob won his {victory_message((int)games_won_bob)}{games_left_message}",
                            log_modes.per_game);
                        break;
                    default:
                        logger.log($"The game was a tie\t\t\t\t\t{games_left_message}", log_modes.per_game);
                        break;
                }
            }
            sw.Stop();
            send_history(histories);
            TimeSpan elapsed = sw.Elapsed;
            logger.log($"Simulation of {max_games} game(s) finished in {elapsed}", log_modes.essential);
            logger.log(
                $"Alice won {games_won_alice} games, Bob won {games_won_bob} and {max_games - games_won_alice - games_won_bob} were a tie;",
                log_modes.essential);

        }
        /// <summary>
        /// Initialize 1 option from a list of command-line arguments and return the value.
        /// </summary>
        /// <param name="args">List of all arguments and options</param>
        /// <param name="cmd_char">The character which indicates the option</param>
        /// <param name="arg_name">A readable name of the option which is printed on the screen</param>
        /// <param name="min">The minimum value of the argument</param>
        /// <param name="max">The maximum value of the argument</param>
        /// <param name="default_value">The default value of the argument. This value will be returned if the argument was invalid</param>
        /// <returns></returns>
        private static uint parse_arg(List<string> args, string cmd_char, string arg_name, byte min, uint max, byte default_value)
        {
            int index = args.IndexOf("-" + cmd_char);
            if (index != -1)
            {
                try
                {
                    uint option = uint.Parse(args[index + 1]); //As the output of this relies on user input, this can give errors.
                    if (option >= min && option <= max)
                    {
                        logger.log($"{arg_name} is set to {option}", log_modes.essential);
                        return option;
                    }

                    logger.log($"{arg_name} was given outside of the boundaries {min} and {max}", log_modes.essential,
                        log_types.error);
                    logger.log($"{arg_name} defaulted to {default_value}", log_modes.essential);
                }
                catch (FormatException) //Formatexception for the uint.parse
                {
                    logger.log($"{arg_name} was given in the wrong format", log_modes.essential, log_types.error);
                }
            }
            //Return the default value when we had an exception or the found parameter was outside of the given boundaries
            return default_value;
        }
        /// <summary>
        /// Initializes the simulator
        /// </summary>
        /// <param name="_args">The command-line arguments</param>
        public Simulator(string[] _args)
        {
            List<string> args = new List<string>(_args);
            log_modes mode = (log_modes)parse_arg(args, "m", "log _modes", 0, 5, 2);
            logger = new Logger(mode);
            width = (byte)parse_arg(args, "w", "width", 2, 20, 7);
            height = (byte)parse_arg(args, "h", "height", 2, 20, 6);
            max_games = parse_arg(args, "g", "maximum of games", 1, uint.MaxValue, 1);
            
            Console.ReadLine();
        }
        /// <param name="args">
        /// -l  [0-5]                   log _modes (default 2) <see cref="log_modes"/>
        /// -w  [>0]                    width  of the playing field (default 7)
        /// -h  [>0]                    height of the playing field (default 6)
        /// 
        /// -g  [>0]                    amount of games to simulate (default 1)
        /// -d  [date in the future]    date at which the simulation will be cutt off (default null)
        /// </param>
        private static void Main(string[] args)
        {
            Simulator sim = new Simulator(args);
            sim.loop_games();
            Console.ReadLine();
        }
    }
}
