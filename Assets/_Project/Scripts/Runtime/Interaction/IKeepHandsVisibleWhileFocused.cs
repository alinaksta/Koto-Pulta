namespace Game.Interaction
{
    /// <summary>
    /// Marker for focusables that keep the player's hands visible while the camera is focused on them
    /// (e.g. cooking stations, where the player keeps using the items in hand).
    /// </summary>
    public interface IKeepHandsVisibleWhileFocused
    {
        /// <summary>Lets the focusable hide the hands for a while (e.g. during a minigame).</summary>
        bool KeepHandsVisible { get; }
    }
}
