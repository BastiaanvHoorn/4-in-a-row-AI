using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.ServiceModel;
using System.Text;

namespace _4_in_a_row_lib
{
    // NOTE: You can use the "Rename" command on the "Refactor" menu to change the interface name "IService1" in both code and config file together.
    [ServiceContract]
    public interface IGame
    {
        [OperationContract]
        byte[][] get_field();
        [OperationContract]
        void add_stone(byte row, bool alice);
        [OperationContract]
        bool has_won(bool alice);

    }
}
