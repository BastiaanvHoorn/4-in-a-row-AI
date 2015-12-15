using Util;
namespace Engine
{
    public interface IPlayer
    {
        players player { get; }
        byte get_turn(Field field, log_modes log_mode);
    }
}
