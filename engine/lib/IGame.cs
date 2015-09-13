using System;
using System.ServiceModel;

namespace lib
{
    [ServiceContract]
    public interface IGame
    {
        [OperationContract]
        byte[][] get_field();
        [OperationContract]
        bool add_stone(byte row, bool alice);
        [OperationContract]
        bool has_won(bool alice);

    }
}
