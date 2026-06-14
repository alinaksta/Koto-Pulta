using UnityEngine;

namespace Game.Player
{
    public readonly struct CameraSnapshot
    {
        public readonly Vector3 Position;
        public readonly Quaternion Rotation;
        public readonly float Fov;

        public CameraSnapshot(Vector3 position, Quaternion rotation, float fov)
        {
            Position = position;
            Rotation = rotation;
            Fov = fov;
        }
    }
}