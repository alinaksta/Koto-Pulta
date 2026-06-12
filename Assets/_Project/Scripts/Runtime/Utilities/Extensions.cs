using System;
using UnityEngine;

namespace Game.Utils
{
    public static class Extensions
    {
        #region Vector3
        public static Vector3 Flat(this Vector3 a)
        {
            return new Vector3(a.x, 0f, a.z);
        }

        public static Vector3 Flat(this Vector3 a, in Vector3 up)
        {
            return Vector3.ProjectOnPlane(a, up);
        }
        #endregion
    }
}
