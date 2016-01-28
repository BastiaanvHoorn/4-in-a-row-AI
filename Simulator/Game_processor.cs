using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using Botclient;
using Engine;
using NLog;
using Utility;
using Logger = NLog.Logger;
using System.Globalization;

namespace Simulator
{
    public class Game_processor
    {
        private static readonly Logger logger = LogManager.GetCurrentClassLogger();
        private static readonly Logger games_logger = LogManager.GetLogger("game_logger");
        private readonly byte width = 7;
        private readonly byte height = 6;
        public int games_won_alice;
        public int games_won_bob;
        private readonly byte random_alice;
        private readonly byte random_bob;
        private readonly int games;
        private delegate string victory_message(int games_won);
        public IPAddress address { get; }
        public ushort port { get; }

        public Game_processor(byte width, byte height, int max_games, byte random_alice, byte random_bob, IPAddress address, ushort port)
        {
            this.width = width;
            this.height = height;
            this.random_alice = random_alice;
            this.random_bob = random_bob;
            this.games = max_games;
            this.address = address;
            this.port = port;
        }
        /// <summary>
        /// Loop through the given amount of games, and log some stuff in the meantime
        /// </summary>
        public List<List<byte>> loop_games()
        {
            //A stopwatch to measure how much time we spend on simulating these games
            var sw = new Stopwatch();
            sw.Start();
            //Jagged List to store the history of all the games
            var histories = new List<List<byte>>();
            for (int game_count = 0; game_count < games; game_count++)
            {
                var game = new Game(width, height);
                var history = new List<byte>();
                players victourious_player = do_game(game, ref history);

                //The amount of turns this game lasted. 1 is subtracted for the winner indication at the start.
                int turns = history.Count - 1;
                
                //Log a nice message for the winner
                victory_message victory_message =
                    games_won => $"\t\t{games_won}th game after \t{(games_won < 10 ? "\t" : "")}{turns} turns";
                string games_left_message = $";\t {games - game_count - 1} of {games} game(s) left";
                switch (victourious_player)
                {
                    case players.Alice:
                        games_won_alice++;
                        histories.Add(history);
                        logger.Debug($"Alice won her {victory_message((int) games_won_alice)}{games_left_message}");
                        games_logger.Debug("A {1}",turns);
                        break;
                    case players.Bob:
                        games_won_bob++;
                        histories.Add(history);
                        logger.Debug($"Bob won his {victory_message((int)games_won_bob)}{games_left_message}");
                        games_logger.Debug("B {1}", turns);
                        break;
                    default:
                        logger.Debug($"The game was a tie\t\t\t\t\t{games_left_message}");
                        break;
                }
            }
            sw.Stop();
            TimeSpan elapsed = sw.Elapsed;

            NumberFormatInfo nfi = new CultureInfo("en-US", false).NumberFormat;
            nfi.PercentDecimalDigits = 2;

            logger.Info($"Simulation of {games} game(s) finished in {elapsed}");
            logger.Info($"Alice won \t{ get_win_percentage(players.Alice).ToString("P", nfi) }\t");
            logger.Info($"Bob won \t{ get_win_percentage(players.Bob).ToString("P", nfi) }\t");
            logger.Info($"Tie \t{ get_win_percentage(players.Empty).ToString("P", nfi) }\t");
            return histories;
        }

        private double get_win_percentage(players player, int digits = 2)
        {
            int player_wins = 0;

            switch (player)
            {
                case players.Alice:
                    player_wins = games_won_alice;
                    break;
                case players.Bob:
                    player_wins = games_won_bob;
                    break;
                default:
                    player_wins = games - games_won_alice - games_won_bob;
                    break;
            }
            return (double)player_wins / (double)games;
        }

        private players do_game(Game game, ref List<byte> history)
        {
            game = new Game(width, height);
            //logger.log($"Created new game of {game.get_field().Width} by {game.get_field().Height}", log_modes.per_game);
            var _players = new List<IPlayer>() //A fancy list to prevent the use of if-statements
            {
                new Database_bot(players.Alice, random_alice, address, port, false),
                new Database_bot(players.Bob, random_bob, address, port, false)
            };

            while (true)
            {
                bool tie = !do_turn(_players.Find(player => player.player == game.next_player), game);
                //Execute the turn the player who's turn it is. If do_turn returns false, it is a tie;
                //First add the indication of the winner of the match to the history-list
                //Then add the history itself
                if (game.has_won(players.Alice))
                {
                    history.Add(Network_codes.game_history_alice);
                    history.AddRange(game.history);
                    return players.Alice;
                }
                if (game.has_won(players.Bob))
                {
                    history.Add(Network_codes.game_history_bob);
                    history.AddRange(game.history);
                    return players.Bob;
                }
                if (tie)
                {
                    return players.Empty;
                }
            }
        }

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
                //logger.log("The field is full, it is a tie if no one has won", log_modes.per_game, log_types.warning);
                return false;
            }
            string s = "";
            int counter = 0;
            while (!game.add_stone(player.get_turn(game.get_field()), player.player, ref s))
            //Try to add a stone fails. If that fails, log the error and try it again.
            {
                counter++;
                //logger.log($"{s} ({counter} tries)", log_modes.debug);
                if (counter < 100) continue;
                logger.Warn("Exceeded maximum of tries for a turn");
                return false;
            }
            return true;
        }

    }
}
