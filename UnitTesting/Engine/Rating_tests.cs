using System;
using System.Diagnostics;
using Engine;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace UnitTesting
{
    [TestClass]
    public class Rate_columns_tests
    {
        [TestMethod]
        public void test_no_depth()
        {
            Field field = new Field();
            int[] actual = field.rate_columns(players.Alice, 0);
            int[] expected = { 3, 3, 3, 4, 3, 3, 3 };
            CollectionAssert.AreEqual(expected, actual);
        }

        [TestMethod]
        public void depth_1()
        {
            Field field = new Field();
            int[] actual = field.rate_columns(players.Alice, 1);
            int[] expected = { -2, -2, -2, -1, -2, -2, -2 };
            CollectionAssert.AreEqual(expected, actual);
        }

        [TestMethod]
        public void depth_2()
        {
            Field field = new Field();
            int[] actual = field.rate_columns(players.Alice, 2);
            int[] expected = {3,3,3,4,3,3,3};
            CollectionAssert.AreEqual(expected, actual);
        }
        [TestMethod]
        public void depth_5()
        {
            Field field = new Field();
            int[] actual = field.rate_columns(players.Alice, 5);
            int[] expected = { -2,-2,-3,0,-3,-2,-2 };
            CollectionAssert.AreEqual(expected, actual);
        }

        [TestMethod]
        public void depth_7()
        {
            Field field = new Field();
            int[] actual = field.rate_columns(players.Alice, 7);
            int[] expected = { -1, -2, -2, -1, -2, -2, -1 };
            CollectionAssert.AreEqual(expected, actual);
        }

        [TestMethod]
        public void speed_test()
        {
            Field field = new Field();
            Stopwatch sw = new Stopwatch();
            sw.Start();
            for (int i = 0; i < 100; i++)
            {
                field.rate_columns(players.Alice, 7);
            }
            sw.Stop();
        }
        [TestMethod]
        public void speed_depth_6()
        {
            Field field = new Field();
            Stopwatch sw = new Stopwatch();
            sw.Start();
            for (int i = 0; i < 1; i++)
            {
                field.rate_columns(players.Alice, 6);
            }
            sw.Stop();
        }
    }
}
