using UnityEngine;

namespace Game.UI
{
    /// <summary>
    /// Handles quitting the application from the main menu.
    /// </summary>
    public class ExitButton : MonoBehaviour
    {
        private void Awake()
        {
#if UNITY_WEBGL
            gameObject.SetActive(false);
#else
            gameObject.SetActive(true);
#endif
        }

        /// <summary>
        /// Quits the application.
        /// </summary>
        public void QuitApplication()
        {
            Application.Quit();
        }
    }
}
