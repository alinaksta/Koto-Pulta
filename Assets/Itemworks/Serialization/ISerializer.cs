using Itemworks.Core;

namespace Itemworks.Serialization
{
    /// <summary>
    /// Converts values between runtime and serialized representations.
    /// </summary>
    /// <typeparam name="TObject">Runtime object type.</typeparam>
    /// <typeparam name="TData">Serialized data type.</typeparam>
    public interface ISerializer<TObject, TData>
    {
        /// <summary>
        /// Creates a runtime object from serialized data.
        /// </summary>
        /// <param name="data">Serialized data to read.</param>
        /// <returns>Deserialized runtime object.</returns>
        TObject Deserialize(TData data);

        /// <summary>
        /// Converts a runtime object into serialized data.
        /// </summary>
        /// <param name="definition">Runtime object to serialize.</param>
        /// <returns>Serialized representation.</returns>
        TData Serialize(TObject definition);
    }

    /// <summary>
    /// Specializes <see cref="ISerializer{TObject, TData}"/> for item definitions.
    /// </summary>
    /// <typeparam name="T">Serialized item definition type.</typeparam>
    public interface IItemDefinitionSerializer<T> : ISerializer<ItemDefinition, T>
    {

    }
}
