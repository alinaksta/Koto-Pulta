using Game.Player;
using UnityEngine;

namespace Game.Interaction
{
    public class SimpleFocusInteractable : MonoBehaviour, IFocusInteractable
    {
        [SerializeField] private CameraTarget _cameraTarget;
        [SerializeField] private float _resetTransitionDuratin = 2f;

        public CameraTarget CameraTarget => _cameraTarget;
        public float ResetTransitionDuration => _resetTransitionDuratin;

        public void BeginInteraction()
        {
            Debug.Log("Entered Computer");
        }

        public void EndInteraction()
        {
            Debug.Log("Exited Computer");
        }
    }
}