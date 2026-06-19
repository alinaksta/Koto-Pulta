using System;
using UnityEngine;

namespace Game.Player
{
    [Serializable]
    public sealed class FocusTarget
    {
        public Transform Transform;

        public Vector3 Position => Transform.position;
        public Quaternion Rotation => Transform.rotation;

        public bool ApplyRotation = false;

        public float PositionLerp = 24f;
        public float RotationLerp = 20f;

        public float Fov = 90f;
        public float FovLerp = 20f;

        public CameraSnapshot ToSnapshot()
            => new CameraSnapshot(Position, Rotation, Fov);
    }
}