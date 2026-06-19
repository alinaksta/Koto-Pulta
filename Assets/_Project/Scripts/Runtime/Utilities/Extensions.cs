using System;
using UnityEngine;

namespace Game.Utils
{
    /// <summary>
    /// Provides small helper extensions used across runtime code.
    /// </summary>
    public static class Extensions
    {
        #region Vector3
        /// <summary>
        /// Removes the world-up component from a vector.
        /// </summary>
        /// <param name="a">Vector to flatten.</param>
        /// <returns>Flattened vector.</returns>
        public static Vector3 Flat(this Vector3 a)
        {
            return new Vector3(a.x, 0f, a.z);
        }

        /// <summary>
        /// Removes the component aligned with the provided up direction.
        /// </summary>
        /// <param name="a">Vector to flatten.</param>
        /// <param name="up">Plane normal to project away from.</param>
        /// <returns>Vector projected onto the plane defined by <paramref name="up"/>.</returns>
        public static Vector3 Flat(this Vector3 a, in Vector3 up)
        {
            return Vector3.ProjectOnPlane(a, up);
        }
        #endregion
    }
}
