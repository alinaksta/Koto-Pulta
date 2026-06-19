using UnityEngine;

namespace Game.Player
{
    /// <summary>
    /// Stores a camera pose and field of view.
    /// </summary>
    public readonly struct CameraSnapshot
    {
        /// <summary>
        /// Gets the world position of the snapshot.
        /// </summary>
        public readonly Vector3 Position;

        /// <summary>
        /// Gets the world rotation of the snapshot.
        /// </summary>
        public readonly Quaternion Rotation;

        /// <summary>
        /// Gets the field of view stored in the snapshot.
        /// </summary>
        public readonly float Fov;

        /// <summary>
        /// Creates a camera snapshot from a pose and field of view.
        /// </summary>
        /// <param name="position">Snapshot position.</param>
        /// <param name="rotation">Snapshot rotation.</param>
        /// <param name="fov">Snapshot field of view.</param>
        public CameraSnapshot(Vector3 position, Quaternion rotation, float fov)
        {
            Position = position;
            Rotation = rotation;
            Fov = fov;
        }
    }
}
