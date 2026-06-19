namespace Itemworks.Core
{
    /// <summary>
    /// Writes diagnostic messages for Itemworks operations.
    /// </summary>
    public interface ILogger
    {
        /// <summary>
        /// Writes a regular informational message.
        /// </summary>
        /// <param name="message">Message text to record.</param>
        void Log(string message);

        /// <summary>
        /// Writes a warning about a recoverable issue.
        /// </summary>
        /// <param name="message">Warning text to record.</param>
        void LogWarning(string message);

        /// <summary>
        /// Writes an error for a failed operation.
        /// </summary>
        /// <param name="message">Error text to record.</param>
        void LogError(string message);
    }
}
