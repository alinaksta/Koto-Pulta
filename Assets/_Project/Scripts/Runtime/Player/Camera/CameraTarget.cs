using System;
using UnityEngine;

namespace Game.Player
{
    [Serializable]
    public sealed class CameraTarget
    {
        public Transform Target;

        public Vector3 Position => Target.position;
        public Quaternion Rotation => Target.rotation;

        public bool ApplyRotation = false;

        public float PositionLerp = 24f;
        public float RotationLerp = 20f;

        public float Fov = 90f;
        public float FovLerp = 20f;
    }

    public readonly struct CameraTransiton
    {
        public readonly Vector3 Position;
        public readonly Quaternion Rotation;
        public readonly float Fov;
        public readonly float Duration;
        public readonly float StartTime;

        public float EndTime => Duration + StartTime;

        public CameraTransiton(Vector3 position, Quaternion rotation, float fov, float duration, float startTime)
        {
            Position = position;
            Rotation = rotation;
            Fov = fov;
            Duration = duration;
            StartTime = startTime;
        }
    }
}