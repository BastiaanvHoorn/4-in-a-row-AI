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
        private static Logger logger = LogManager.GetCurrentClassLogger();
        public players player { get; }
        public byte random_chance { get; }
        Random r = new Random();
        public Bot(players player, byte random_chance)
        {
            this.player = player;
            if(random_chance > 100)
                throw new NotSupportedException("Cannot use a random_chance that is bigger then 100");
            this.random_chance = random_chance;

        }

        public byte get_turn(Field field)
        {
            if ((byte) r.Next(100) < random_chance)
                return (byte) r.Next(7);
            for (byte x = 0; x < field.Width; x++)
            {
                byte y = field.getEmptyCell(x);

                if (field.check_for_win(x, y, player))
                    return x;
            }

            players opponent = player == players.Alice ? players.Bob : players.Alice;
            for (byte x = 0; x < field.Width; x++)
            {
                byte y = field.getEmptyCell(x);

                if (field.check_for_win(x, y, opponent))
                    return x;
            }

            var column = Requester.send(field.getStorage(), network_codes.column_request)[0];
            logger.Debug($"Tried to drop a stone in colmun {column}");
            return column;
        }

    }
}
