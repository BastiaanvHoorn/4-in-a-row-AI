namespace Engine
{
    public interface IPlayer
    {
        players player { get; }
        byte get_turn(Field field);
    }
}
