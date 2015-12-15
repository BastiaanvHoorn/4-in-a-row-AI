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
        private readonly uint cycle_length = 10000;
        private readonly byte random_alice = 0;
        private readonly byte random_bob = 0;
        private DateTime end_time;
        /// <summary>
        /// Initializes the simulator
        /// </summary>
        /// <param name="_args">The command-line arguments
        /// -l  [0-5]                   log _modes (default 2) <see cref="log_modes"/>
        /// -w  [>0]                    width  of the playing field (default 7)
        /// -h  [>0]                    height of the playing field (default 6)
        /// 
        /// -g  [>0]                    amount of games to simulate (default 1)
        /// -e  [date in the future]    date at which the simulation will be cutt off (default null)
        /// -l  [>0, -g>]               Length of a sub-simulation of games
        /// -ra [>0, 100>]              The chance for alice to random a turn
        /// -rb [>0, 100>]              The chance for bob to random a turn
        /// </param>
        public Simulator(string[] _args)
        {
            List<string> args = _args.ToList();
            width = (byte)Args_processor.parse_int_arg(args, "w", "width", 2, 20, 7);
            Console.WriteLine($"Width is set to {width}");
            height = (byte)Args_processor.parse_int_arg(args, "h", "height", 2, 20, 6);
            Console.WriteLine($"Height is set to {height}");
            max_games = Args_processor.parse_int_arg(args, "g", "maximum of games", 1, uint.MaxValue, 1);
            Console.WriteLine($"Maximum of games is set to {max_games}");
            cycle_length = Args_processor.parse_int_arg(args, "l", "cycle length", 1, max_games,
                cycle_length);
            Console.WriteLine($"Cycle length is set to {cycle_length}");
            random_alice = (byte)Args_processor.parse_int_arg(args, "ra", "random_alice", 0, 100, 0);
            random_bob = (byte)Args_processor.parse_int_arg(args, "rb", "random_bob", 0, 100, 0);
            try
            {
                end_time = DateTime.Parse(Args_processor.parse_arg(args, "e", "end time", DateTime.MaxValue.ToString()));
            }
            catch (Exception)
            {
                end_time = DateTime.MaxValue;
            }
            Console.WriteLine($"Ending time set to {end_time}");

            Console.ReadLine();
        }
        public void loop_games()
        {
            uint cycles = (max_games - (max_games%cycle_length)/cycle_length) + 1;
            for (int i = 0; i <= cycles; i++)
            {
                Console.WriteLine("Starting new cycle");
                Game_processor gp = new Game_processor(width,height,cycle_length, random_alice, random_bob);
                List<List<byte> > history = gp.loop_games();
                send_history(history);
                if (end_time < DateTime.Now)
                    break;
            }
        }
        private void send_history(List<List<byte>> histories)
        {
            var data = new List<byte>();
            //Concatenate all the game-histories into one byte-array;
            foreach (var history in histories)
            {
                data.AddRange(history);
            }
            Stopwatch sw = new Stopwatch();
            //logger.log($"Created game_history in {sw.ElapsedMilliseconds}ms. Starting to send now", log_modes.essential);
            Requester.send(data.ToArray(), network_codes.game_history_array, log_modes.essential);

        }
        private static void Main(string[] args)
        {
            Simulator sim = new Simulator(args);
            sim.loop_games();
            Console.ReadLine();
        }
    }
}
