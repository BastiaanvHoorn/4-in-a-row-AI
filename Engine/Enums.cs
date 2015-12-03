namespace Engine
{
    public enum players
    {
        Empty,
        Alice,          
        Bob
    }
    /// <summary>
    /// <
    /// </summary>
    public enum log_modes
    {
        /// <summary> Nothing is logged </summary>
        silent,
        /// <summary> Only errors are logged </summary>
        only_errors,
        /// <summary> Loggs most important info (per game info) </summary>
        essential,
        /// <summary> Loggs a lot of things (per turn info) </summary>
        verbose,
        /// <summary> Loggs everything </summary>
        debug
    }
}