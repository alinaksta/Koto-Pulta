using UnityEngine;

namespace Game.Movement
{
    /// <summary>
    /// Exposes a transform orientation used for movement and camera logic.
    /// </summary>
    public interface IOrientation
    {
        /// <summary>
        /// Gets the yaw-only rotation.
        /// </summary>
        Quaternion RotationFlat { get; }

        /// <summary>
        /// Gets the full pitch and yaw rotation.
        /// </summary>
        Quaternion RotationFull { get; }

        /// <summary>
        /// Gets the current Euler rotation in pitch-yaw-roll order.
        /// </summary>
        Vector3 Euler { get; }

        /// <summary>
        /// Gets the current yaw angle in degrees.
        /// </summary>
        float Yaw { get; }

        /// <summary>
        /// Gets the current pitch angle in degrees.
        /// </summary>
        float Pitch { get; }

        /// <summary>
        /// Gets the forward direction projected onto the horizontal plane.
        /// </summary>
        Vector3 ForwardFlat { get; }

        /// <summary>
        /// Gets the right direction projected onto the horizontal plane.
        /// </summary>
        Vector3 RightFlat { get; }

        /// <summary>
        /// Gets the full forward direction.
        /// </summary>
        Vector3 Forward { get; }

        /// <summary>
        /// Gets the full right direction.
        /// </summary>
        Vector3 Right { get; }

        /// <summary>
        /// Resets the tracked rotation back to zero yaw and pitch.
        /// </summary>
        void ResetRotation();

        /// <summary>
        /// Converts world velocity into local flat-orientation space.
        /// </summary>
        /// <param name="worldVelocity">Velocity in world space.</param>
        /// <returns>Velocity relative to the flat orientation.</returns>
        Vector3 GetRelativeVelocity(Vector3 worldVelocity);
    }
}
