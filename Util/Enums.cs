namespace Util
{
    public enum network_codes
    {
        end_of_stream = 255,        //The last byte of a sent message
        column_request = 200,       //The first (and only) byte when the bot wants to recieve a column from the server
        game_history_array = 210,   //The first byte when the bot sends an array of game_history
        game_history_alice = 211,   //The first byte of the history of a game which Alice has won
        game_history_bob = 212,     //The first byte of the history of a game which Bob has won
        range_request = 220,        //The first byte when a range of fields is requested from the server
        details_request = 221,      //The first byte when details of a further specified field is requested from the server
    }
}