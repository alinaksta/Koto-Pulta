namespace Game.Player
{
    /// <summary>
    /// Describes an active camera blend between two snapshots.
    /// </summary>
    public readonly struct FocusTransition
    {
        /// <summary>
        /// Gets the transition starting snapshot.
        /// </summary>
        public readonly CameraSnapshot From;

        /// <summary>
        /// Gets the transition target snapshot.
        /// </summary>
        public readonly CameraSnapshot To;

        /// <summary>
        /// Gets the transition duration in seconds.
        /// </summary>
        public readonly float Duration;

        /// <summary>
        /// Gets the time when the transition started.
        /// </summary>
        public readonly float StartTime;

        /// <summary>
        /// Gets the time when the transition should finish.
        /// </summary>
        public float EndTime => Duration + StartTime;

        /// <summary>
        /// Creates a focus transition between two camera snapshots.
        /// </summary>
        /// <param name="from">Starting snapshot.</param>
        /// <param name="to">Target snapshot.</param>
        /// <param name="duration">Transition duration in seconds.</param>
        /// <param name="startTime">Time when the transition began.</param>
        public FocusTransition(CameraSnapshot from, CameraSnapshot to, float duration, float startTime)
        {
            From = from;
            To = to;
            Duration = duration;
            StartTime = startTime;
        }
    }
}
