using UnityEngine;

namespace Game.UI
{
    public class ExitButton : MonoBehaviour
    {
        public void QuitApplication()
        {
            Application.Quit();
        }
    }
}