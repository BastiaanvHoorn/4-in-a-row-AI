using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Util;
using Server;

namespace UnitTesting.ServerTest
{
    [TestClass]
    public class Game_history_test
    {
        [TestMethod]
        public void linear_to_parrallel_game_history_test()
        {
            byte[] game1 = { Network_codes.game_history_alice, 1, 8, 2, 5, 4 };
            byte[] game2 = { Network_codes.game_history_bob, 4, 8, 4, 5 };
            byte[][] expected = { game1, game2, game1, game2 };

            List<byte> initial = new List<byte>();
            initial.Add(Network_codes.game_history_array);
            initial.AddRange(game1);
            initial.AddRange(game2);
            initial.AddRange(game1);
            initial.AddRange(game2);
            initial.Add(Network_codes.end_of_stream);
            initial.AddRange(game2);

            byte[][] actual = game_history_util.linear_to_parrallel_game_history(initial);
            for (int i = 0; i < expected.Length; i++)
            {
                CollectionAssert.AreEqual(expected[i], actual[i]);
            }
            Assert.AreEqual(expected.Length, actual.Length);
        }

        [TestMethod]
        public void linear_to_parrallel_game_history_speed_test()
        {
            List<byte> linear = new List<byte>();
            for (int i = 0; i < 42*100000; i++)
            {
                if (i == 0 || i%42 == 0)
                {
                    linear.Add(Network_codes.game_history_alice);
                }
                else
                {
                    linear.Add((byte)((i%42)%6));
                }
            }
            Stopwatch sw = new Stopwatch();
            game_history_util.linear_to_parrallel_game_history(linear);
        }
    }
}

