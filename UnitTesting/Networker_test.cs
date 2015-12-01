using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Networker;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace UnitTesting
{
    [TestClass]
    public class Networker_test
    {
        [TestMethod]
        public void test_add_header_footer()
        {
            var expected = new byte[]
            {
                (byte) network_codes.column_request,
                0,
                (byte) network_codes.end_of_stream
            };
            var actual = new byte[] {0};
            actual = Requester.add_header_footer(actual, network_codes.column_request);
            CollectionAssert.AreEqual(expected, actual);
        }
    }
}
