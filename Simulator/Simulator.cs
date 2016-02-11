using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net;
using NLog;
using Logger = NLog.Logger;
using Utility;

namespace Simulator
{
    internal class Simulator
    {

        private readonly byte width = 7;
        private readonly byte height = 6;
        private readonly uint max_games = 1;
        private readonly int cycle_length = 10000;
        private readonly byte random_alice = 0;
        private readonly byte random_bob = 0;
        private readonly DateTime end_time;
        private static readonly Logger logger = LogManager.GetCurrentClassLogger();
        private IPAddress address;
        private ushort port;
        /// <summary>
        /// Initializes the simulator
        /// </summary>
        /// <param name="_args">The command-line arguments
        /// -w  [byte >0]               width  of the playing field (default 7)
        /// -h  [byte >0]               height of the playing field (default 6)
        /// -g  [int >0]                amount of games to simulate (default 1)
        /// -e  [date in the future]    date at which the simulation will be cutt off (default null)
        /// -l  [0-g]                   Length of a sub-simulation of games
        /// -ra [0-100]                 The chance for alice to random a turn
        /// -rb [0-100]                 The chance for bob to random a turn
        /// -ip [ip-address]            The ip-address of the server
        /// -p  [ushort >0]              The port the server should be listening to
        /// </param>
        public Simulator(string[] _args)
        {
            List<string> args = _args.ToList();
            width =        (byte)Args_parser.parse_int_arg(args, "w",  "width",        2, 20, 7);
            height =       (byte)Args_parser.parse_int_arg(args, "h",  "height",       2, 20, 6);
            max_games =          Args_parser.parse_int_arg(args, "g",  "max games",    1, uint.MaxValue, 1);
            cycle_length =  (int)Args_parser.parse_int_arg(args, "l",  "cycle length", 1, max_games, max_games);
            random_alice = (byte)Args_parser.parse_int_arg(args, "ra", "random_alice", 0, 100, 0);
            random_bob =   (byte)Args_parser.parse_int_arg(args, "rb", "random_bob",   0, 100, 0);
            port =       (ushort)Args_parser.parse_int_arg(args, "p",  "port",         0, ushort.MaxValue, 11000);
            if (DateTime.TryParse(
                Args_parser.parse_arg(args, "e", "end time",
                    DateTime.MaxValue.ToString()), out end_time))
            {

                if (end_time < DateTime.Now)
                    end_time = DateTime.MaxValue;
            }
            else
            {
                end_time = DateTime.MaxValue;
            }

            if (!IPAddress.TryParse(
                Args_parser.parse_arg(args, "ip", "ip-address",
                    Dns.Resolve(Dns.GetHostName()).AddressList[0].ToString()), out address))
            {
                address = Dns.GetHostEntry(Dns.GetHostName()).AddressList[1];
            }
            logger.Info($"Width is set to {width}");
            logger.Info($"Height is set to {height}");
            logger.Info($"Maximum of games is set to {max_games}");
            logger.Info($"Cycle length is set to {cycle_length}");
            logger.Info($"Ending time set to {end_time}");
            logger.Info($"The ip-address is set to {address}:{port}");
            Console.WriteLine("Press any key to start the simulation");
            Console.ReadLine();
        }
        public void loop_games()
        {
            Stopwatch sw = new Stopwatch();
            sw.Start();
            bool finished = true;
            int cycles = (int)(max_games - (max_games % cycle_length)) / cycle_length;
            for (int i = 1; i <= cycles; i++)
            {
                logger.Info($"Starting {i}/{cycles} cycle");
                Game_processor gp = new Game_processor(width, height, cycle_length, random_alice, random_bob, address, port);
                List<List<byte>> history = gp.loop_games();
                send_history(history);
                if (end_time < DateTime.Now)
                {
                    finished = false;
                    logger.Info($"Stopped simulating because the ending time has passed. Simulated {i} of {max_games} games");
                    break;
                }
            }
            if (finished)
                logger.Info("Finished simulating all games");
            logger.Info($"Simulation took {sw.Elapsed}");
            

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
            Requester.send(data.ToArray(), Network_codes.game_history_array, address, port);

        }
        private static void Main(string[] args)
        {
            Console.Clear();
            Simulator sim = new Simulator(args);
            string s;
            if (!Requester.ping(sim.address, sim.port, out s))
            {
                Console.WriteLine("Press any key to exit...");
                Console.Read();
                return;
            }
            sim.loop_games();
            Console.WriteLine("Press any key to exit...");
            Console.ReadLine();
        }
    }
}
