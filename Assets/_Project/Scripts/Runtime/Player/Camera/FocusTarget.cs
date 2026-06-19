using System;
using UnityEngine;

namespace Game.Player
{
    /// <summary>
    /// Defines the camera pose and blending values used during focus.
    /// </summary>
    [Serializable]
    public sealed class FocusTarget
    {
        /// <summary>
        /// Transform used as the focus pose source.
        /// </summary>
        public Transform Transform;

        /// <summary>
        /// Gets the target world position.
        /// </summary>
        public Vector3 Position => Transform.position;

        /// <summary>
        /// Gets the target world rotation.
        /// </summary>
        public Quaternion Rotation => Transform.rotation;

        /// <summary>
        /// Gets or sets whether the target rotation should be applied while focused.
        /// </summary>
        public bool ApplyRotation = false;

        /// <summary>
        /// Lerp speed used when moving toward the focus position.
        /// </summary>
        public float PositionLerp = 24f;

        /// <summary>
        /// Lerp speed used when rotating toward the focus rotation.
        /// </summary>
        public float RotationLerp = 20f;

        /// <summary>
        /// Target field of view while focused.
        /// </summary>
        public float Fov = 90f;

        /// <summary>
        /// Lerp speed used when blending toward the focus field of view.
        /// </summary>
        public float FovLerp = 20f;

        /// <summary>
        /// Converts this focus target into a camera snapshot.
        /// </summary>
        /// <returns>Snapshot containing the target pose and field of view.</returns>
        public CameraSnapshot ToSnapshot()
            => new CameraSnapshot(Position, Rotation, Fov);
    }
}
