using Game.CameraLayerSystem;

namespace Game.Player.CameraLayers
{
    /// <summary>
    /// Identifies built-in player camera layers.
    /// </summary>
    public enum PlayerCameraLayer
    {
        Viewroll,
        DEBUG1,
        DEBUG2
    }

    /// <summary>
    /// Specializes the generic camera layer system for player camera effects.
    /// </summary>
    public class PlayerCameraLayerSystem : CameraLayerSystem<PlayerCameraLayer>
    {
        protected override void SetupLayers()
        {
            //noop, modify this method if order of layers matters
        }
    }
}
