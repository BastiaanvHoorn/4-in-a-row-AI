using Engine;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Server
{
    public static class RequestHandler
    {
        public static byte get_column(Field field)
        {
            FieldData fieldData = field.readFieldData();

            float bestChance = 0;
            byte bestColumn = 0;

            for (byte i = 0; i < 7; i++)
            {
                float chance = fieldData.getWinningChance(i);
                if (chance > bestChance)
                {
                    bestChance = chance;
                    bestColumn = i;
                }
            }

            return bestColumn;
        }
    }
}
