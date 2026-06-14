using UnityEngine;

namespace Game.Movement
{
    public interface IOrientation
    {
        Quaternion RotationFlat { get; }
        Quaternion RotationFull { get; }
        Vector3 Euler { get; }
        float Yaw { get; }
        float Pitch { get; }

        Vector3 ForwardFlat { get; }
        Vector3 RightFlat { get; }
        Vector3 Forward { get; }
        Vector3 Right { get; }

        void ResetRotation();
        Vector3 GetRelativeVelocity(Vector3 worldVelocity);
    }
}