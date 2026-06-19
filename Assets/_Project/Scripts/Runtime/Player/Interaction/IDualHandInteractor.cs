using Game.Items;

namespace Game.Interaction
{
    public interface IDualHandInteractor
    {
        Hand GetHand(HandType handType);
        bool TryGetFreeHand(out IContainer freeHand);
    }
}
