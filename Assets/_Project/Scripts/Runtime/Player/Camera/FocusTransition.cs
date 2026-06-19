namespace Game.Player
{
    public readonly struct FocusTransition
    {
        public readonly CameraSnapshot From;
        public readonly CameraSnapshot To;
        public readonly float Duration;
        public readonly float StartTime;

        public float EndTime => Duration + StartTime;

        public FocusTransition(CameraSnapshot from, CameraSnapshot to, float duration, float startTime)
        {
            From = from;
            To = to;
            Duration = duration;
            StartTime = startTime;
        }
    }
}