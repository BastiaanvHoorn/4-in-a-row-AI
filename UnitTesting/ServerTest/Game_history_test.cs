using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Networker;
using Server;

namespace UnitTesting.ServerTest
{
    [TestClass]
    public class Game_history_test
    {
        [TestMethod]
        public void linear_to_parrallel_game_history_test()
        {
            byte[] exp_1 = { (byte)network_codes.game_history_alice, 1, 8, 2, 5, 4 };
            byte[] exp_2 = { (byte)network_codes.game_history_bob, 4, 8, 4, 5 };
            byte[][] expected = { exp_1, exp_2 };

            List<byte> initial = new List<byte>
            {
                (byte)network_codes.game_history_array,
                (byte)network_codes.game_history_alice, 1, 8, 2, 5, 4,
                (byte)network_codes.game_history_bob, 4, 8, 4, 5,
                (byte)network_codes.end_of_stream,
                (byte)network_codes.game_history_bob, 4, 8, 4, 5,
            };
            byte[][] actual = AsynchronousSocketListener.linear_to_parrallel_game_history(initial);
            for (int i = 0; i < expected.Length; i++)
            {
                CollectionAssert.AreEqual(expected[i], actual[i]);
            }
            Assert.AreEqual(expected.Length, actual.Length);
        }
    }
}

