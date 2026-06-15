namespace Itemworks.Core
{
    public interface IItemInitializer
    {
        void OnInstanceCreated(ItemInstance instance);
    }
}