using UnityEngine;

namespace Game.Utils
{

    /// <summary>
    /// Provides helper methods for Quake-style movement calculations.
    /// </summary>
    public static class MovementMath
    {
        /// <summary>
        /// Accelerates velocity toward the desired direction and speed.
        /// </summary>
        /// <param name="velocity">Current world velocity.</param>
        /// <param name="wishDir">Normalized desired movement direction.</param>
        /// <param name="wishSpeed">Desired movement speed.</param>
        /// <param name="accel">Acceleration strength.</param>
        /// <param name="deltaTime">Simulation step.</param>
        /// <returns>Adjusted velocity after acceleration.</returns>
        public static Vector3 Accelerate(Vector3 velocity, Vector3 wishDir, float wishSpeed, float accel, float deltaTime)
        {
            if (wishSpeed <= 0f || wishDir.sqrMagnitude < 0.0001f)
                return velocity;

            float currentSpeed = Vector3.Dot(velocity, wishDir);
            float addSpeed = wishSpeed - currentSpeed;
            if (addSpeed <= 0f)
                return velocity;

            float accelSpeed = accel * wishSpeed * deltaTime;
            if (accelSpeed > addSpeed)
                accelSpeed = addSpeed;

            return velocity + wishDir * accelSpeed;
        }

        /// <summary>
        /// Applies friction to the current velocity magnitude.
        /// </summary>
        /// <param name="velocity">Current velocity.</param>
        /// <param name="friction">Friction strength.</param>
        /// <param name="stopSpeed">Minimum speed used when calculating friction.</param>
        /// <param name="deltaTime">Simulation step.</param>
        /// <returns>Velocity after friction is applied.</returns>
        public static Vector3 ApplyFriction(Vector3 velocity, float friction, float stopSpeed, float deltaTime)
        {
            float speed = velocity.magnitude;
            if (speed < 0.001f)
                return Vector3.zero;

            float control = speed < stopSpeed ? stopSpeed : speed;
            float drop = control * friction * deltaTime;
            float newSpeed = Mathf.Max(speed - drop, 0f);

            return velocity * (newSpeed / speed);
        }

        /// <summary>
        /// Converts a jump height and gravity value into an initial jump speed.
        /// </summary>
        /// <param name="jumpHeight">Desired jump height.</param>
        /// <param name="gravity">Gravity acceleration, typically negative.</param>
        /// <returns>Initial upward velocity needed for the jump.</returns>
        public static float ToJumpForce(float jumpHeight, float gravity)
        {
            return Mathf.Sqrt(jumpHeight * -2f * gravity);
        }

        /// <summary>
        /// Removes the vertical component from a vector using world up.
        /// </summary>
        /// <param name="vector">Vector to flatten.</param>
        /// <returns>Flattened vector.</returns>
        public static Vector3 Flat(in Vector3 vector)
        {
            return new Vector3(vector.x, 0f, vector.z);
        }
    }
}
