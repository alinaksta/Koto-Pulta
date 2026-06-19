using UnityEngine;

namespace Game.Player
{
    /// <summary>
    /// Smoothly follows a target transform for the player rig.
    /// </summary>
    public class PlayerRig : MonoBehaviour
    {
        [SerializeField] private Transform _target;
        [SerializeField] private float _lerpFactor = 20f;

        private void Update()
        {
            transform.position = Vector3.Lerp(transform.position, _target.position, Time.deltaTime * _lerpFactor);
        }
    }
}
