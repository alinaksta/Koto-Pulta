using UnityEngine;

namespace Game.Movement
{
    public interface IOrientation
    {
        Quaternion Rotation { get; }
        Quaternion RotationFlat { get; }
        Quaternion RotationFull { get; }
        Vector3 Euler { get; }
        float Yaw { get; }
        float Pitch { get; }

        Vector3 ForwardFlat { get; }
        Vector3 RightFlat { get; }
        Vector3 Forward { get; }
        Vector3 Right { get; }

        Quaternion ViewRotationFlat { get; }
        Quaternion ViewRotationFull { get; }
        Vector3 ViewEuler { get; }
        float ViewYaw { get; }
        float ViewPitch { get; }

        Vector3 ViewForwardFlat { get; }
        Vector3 ViewRightFlat { get; }
        Vector3 ViewForward { get; }
        Vector3 ViewRight { get; }

        void ResetRotation();
        Vector3 GetRelativeVelocity(Vector3 worldVelocity);
    }
}