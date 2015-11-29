namespace Engine
{
    public enum players
    {
        Empty,
        Alice,          
        Bob
    }
    public enum log_modes
    {
        silent,         //Nothing is logged
        only_errors,    //Only errors are logged
        essential,      //Loggs all less common messages and errors (default)
        verbose,        //Loggs a lot of things
        debug           //Loggs everything
    }
}