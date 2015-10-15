using System;

namespace GUI
{
    class Bot
    {
        public readonly player player;
        private Random r = new Random();
        public Bot(player player)
        {
            this.player = player;
        }

        public byte get_next_move(byte[][] field)
        {
            return (byte)(r.Next(8) -1);
        }
    }
}
