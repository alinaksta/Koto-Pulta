using Game.CameraLayerSystem;
using UnityEngine;

namespace Game.Player.CameraLayers
{
    public abstract class PlayerCameraLayerBase : MonoBehaviour
    {
        [Header("Dependencies")]
        [SerializeField] protected PlayerCameraLayerSystem playerLayerSystem;
        //[SerializeField] protected PlayerController controller;

        [Header("Camera Layer Settings")]
        [SerializeField] protected PlayerCameraLayer targetLayer;
        [SerializeField] protected bool additive = true;
        [SerializeField] protected bool global = false;

        protected CameraLayer layer;

        private void Start()
        {
            layer = playerLayerSystem.AppendLayer(targetLayer, additive, global);
            LateStart();
        }

        protected virtual void LateStart() { }
    }
}