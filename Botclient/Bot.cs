using System;
using System.Net;
using System.Net.Sockets;
using Engine;
using System.Text;
using System.Threading.Tasks;
using Util;

namespace Botclient
{
    public class Bot : IPlayer
    {
        public players player { get; }
        private byte random_chance;
        private Random random;
        public Bot(players player, byte random_chance = 0)
        {
            this.player = player;
            
            this.random_chance = random_chance;
            if (this.random_chance > 100)
                this.random_chance = 100;
            random = new Random();
        }

        public byte get_turn(Field field, log_modes log_mode)
        {
            if (random.Next(100) < random_chance)
                return (byte)random.Next(field.Width);
            

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

            var column = Requester.send(field.getStorage(), network_codes.column_request, log_mode)[0];
            if(log_mode >= log_modes.debug)
                Console.WriteLine($"Tried to drop a stone in colmun {column}");
            return column;
        }

    }
}
