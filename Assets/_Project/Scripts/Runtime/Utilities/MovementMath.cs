using UnityEngine;

namespace Game.Utils
{

    public static class MovementMath
    {
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

        public static float ToJumpForce(float jumpHeight, float gravity)
        {
            return Mathf.Sqrt(jumpHeight * -2f * gravity);
        }

        public static Vector3 Flat(in Vector3 vector)
        {
            return new Vector3(vector.x, 0f, vector.z);
        }
    }
}
