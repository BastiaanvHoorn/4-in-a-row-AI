using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Util;
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
                 Network_codes.column_request,
                0,
                 Network_codes.end_of_stream
            };
            var actual = new byte[] {0};
            actual = Requester.add_header_footer(actual, Network_codes.column_request);
            CollectionAssert.AreEqual(expected, actual);
        }
    }
}
