using UnityEngine;

namespace Game.UI
{
    /// <summary>
    /// Handles quitting the application from the main menu.
    /// </summary>
    public class ExitButton : MonoBehaviour
    {
        /// <summary>
        /// Quits the application.
        /// </summary>
        public void QuitApplication()
        {
            Application.Quit();
        }
    }
}