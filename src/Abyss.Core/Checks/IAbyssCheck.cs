namespace Abyss
{
    /// <summary>
    ///     The base interface for Abyss checks.
    /// </summary>
    public interface IAbyssCheck
    {
        /// <summary>
        ///         Returns the friendly name of the check, an instruction to the user about what this check entails.
        /// </summary>
        /// <param name="commandContext">The request context, passed in in-case it is needed.</param>
        /// <returns>A string representing the friendly name of the check.</returns>
        string GetDescription(AbyssCommandContext commandContext);
    }
}