using System;
using System.Net;
using System.Net.Sockets;
using System.Reflection;
using Engine;
using System.Text;
using System.Threading.Tasks;
using Utility;
using NLog;
using Logger = NLog.Logger;

namespace Botclient
{
    public class Database_bot : IPlayer
    {
        private static readonly Logger logger = LogManager.GetCurrentClassLogger();
        public players player { get; }
        public int random_chance { get; }
        public bool smart_moves { get; }
        public IPAddress address { get; }
        public ushort port { get; }
        Random r = new Random();
        public Database_bot(players player, int random_chance, IPAddress address, ushort port, bool smart_moves = true)
        {
            this.player = player;
            if (random_chance > 100)
                throw new NotSupportedException("Cannot use a random_chance that is bigger then 100");
            this.random_chance = random_chance;
            this.address = address;
            this.port = port;
            this.smart_moves = smart_moves;

        }

        public byte get_turn(Field field)
        {
            // First of all, check if we'll maybe perform a random move.
            if (r.Next(100) < random_chance)
            {
                byte x = field.getRandomColumn();
                logger.Debug($"Placing a stone in column {x} because this turn was randomized (random chance is {random_chance}%");
                return x;
            }

            // If smart-moves is enabled, check if we can perform a 'smart move'.
            if (smart_moves)
            {
                // Check if we can win by placing 1 stone in a certain column.
                for (byte x = 0; x < field.Width; x++)
                {
                    byte y = field.getEmptyCell(x);

                    if (field.check_for_win(x, y, player))
                    {
                        logger.Debug($"Placing a stone in column {x} because smart-moves is turned on");
                        return x;
                    }
                }

                // Check if we the opponent can win by placing 1 stone in a column. If so, prevent it.
                players opponent = player == players.Alice ? players.Bob : players.Alice;
                for (byte x = 0; x < field.Width; x++)
                {
                    byte y = field.getEmptyCell(x);

                    if (field.check_for_win(x, y, opponent))
                    {
                        logger.Debug($"Placing a stone in column {x} because smart-moves is turned on");
                        return x;
                    }
                }
            }

            // Get the first byte from the response from the server.
            var column = Requester.send(field.getStorage(), Network_codes.column_request, address, port)[0];
            logger.Debug($"Placing a stone in column {column} because of a response from the server");
            return column;
        }

    }
}
