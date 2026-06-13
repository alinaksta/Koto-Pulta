using Game.CameraLayerSystem;

namespace Game.Player.CameraLayers
{
    public enum PlayerCameraLayer
    {
        Viewroll,
        DEBUG1,
        DEBUG2
    }

    public class PlayerCameraLayerSystem : CameraLayerSystem<PlayerCameraLayer>
    {
        protected override void SetupLayers()
        {
            //noop, modify this method if order of layers matters
        }
    }
}