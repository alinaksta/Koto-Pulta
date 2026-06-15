using UnityEngine;

namespace Itemworks.Core
{
    // Default no-op logger for when none is set
    public class NullLogger : ILogger
    {
        public void Log(string message) { }
        public void LogWarning(string message) { }
        public void LogError(string message) { }
    }

    // Default no-op logger for when none is set
    public class UnityLogger : ILogger
    {
        public void Log(string message) 
        { 
            Debug.Log(message);
        }
        public void LogWarning(string message) 
        { 
            Debug.LogWarning(message);
        }
        public void LogError(string message) 
        { 
            Debug.LogError(message);
        }
    }
}