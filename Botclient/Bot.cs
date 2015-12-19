using System;
using System.Net;
using System.Net.Sockets;
using Engine;
using System.Text;
using System.Threading.Tasks;
using Util;
using NLog;
using Logger = NLog.Logger;

namespace Botclient
{
    public class Bot : IPlayer
    {
        private static readonly Logger logger = LogManager.GetCurrentClassLogger();
        public players player { get; }
        public int random_chance { get; }
        public bool smart_moves { get; }
        Random r = new Random();
        public Bot(players player, int random_chance, bool smart_moves = true)
        {
            this.player = player;
            if (random_chance > 100)
                throw new NotSupportedException("Cannot use a random_chance that is bigger then 100");
            this.random_chance = random_chance;
            this.smart_moves = smart_moves;

        }

        public byte get_turn(Field field)
        {
            if (r.Next(100) < random_chance)
                return field.getRandomColumn();

            if (smart_moves)
            {
                for (byte x = 0; x < field.Width; x++)
                {
                    byte y = field.getEmptyCell(x);

                    if (field.check_for_win(x, y, player))
                    {
                        logger.Debug($"Placing a stone in column {x} because this turn was randomized (random chance is {random_chance}%");
                        return x;
                    }
                }

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

            var column = Requester.send(field.getStorage(), network_codes.column_request)[0];
            logger.Debug($"Placing a stone in column {column} because of a response from the server");
            return column;
        }

    }
}
