using UnityEngine;

namespace Itemworks.Core
{
    /// <summary>
    /// Ignores all log messages.
    /// </summary>
    public class NullLogger : ILogger
    {
        /// <inheritdoc/>
        public void Log(string message) { }

        /// <inheritdoc/>
        public void LogWarning(string message) { }

        /// <inheritdoc/>
        public void LogError(string message) { }
    }

    /// <summary>
    /// Forwards Itemworks log messages to Unity logging.
    /// </summary>
    public class UnityLogger : ILogger
    {
        /// <inheritdoc/>
        public void Log(string message) 
        { 
            Debug.Log(message);
        }

        /// <inheritdoc/>
        public void LogWarning(string message) 
        { 
            Debug.LogWarning(message);
        }

        /// <inheritdoc/>
        public void LogError(string message) 
        { 
            Debug.LogError(message);
        }
    }
}
