using Itemworks.Core;

namespace Itemworks.Serialization
{
    public interface ISerializer<TObject, TData>
    {
        TObject Deserialize(TData data);

        TData Serialize(TObject definition);
    }

    public interface IItemDefinitionSerializer<T> : ISerializer<ItemDefinition, T>
    {

    }
}
