namespace Utility
{
    public static class Network_codes
    {
        public static byte ping_respond = 42;
        public static byte end_of_stream = 255;        //The last byte of a sent message
        public static byte ping = 200;                 //The first (and only) sent when pinging the server
        public static byte column_request = 201;       //The first (and only) byte when the bot wants to recieve a column from the server
        public static byte game_history_array = 210;   //The first byte when the bot sends an array of game_history
        public static byte game_history_alice = 211;   //The first byte of the history of a game which Alice has won
        public static byte game_history_bob = 212;     //The first byte of the history of a game which Bob has won
        public static byte range_request = 220;        //The first byte when a range of fields is requested from the server

        public static byte details_request = 221;
            //The first byte when details of a further specified field is requested from the server
    }
}
