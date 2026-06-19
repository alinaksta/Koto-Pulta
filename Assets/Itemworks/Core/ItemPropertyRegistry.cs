using System;
using System.Collections.Generic;

namespace Itemworks.Core
{
    /// <summary>
    /// Maps property ids to runtime property types.
    /// </summary>
    public class ItemPropertyRegistry
    {
        private static readonly Dictionary<string, Type> idToType = new();
        private static readonly Dictionary<Type, string> typeToId = new();

        /// <summary>
        /// Registers an id for a property type.
        /// </summary>
        /// <typeparam name="TProperty">Property type to register.</typeparam>
        /// <param name="id">Stable id used for lookup.</param>
        public static void Register<TProperty>(string id)
        {
            var t = typeof(TProperty);
            idToType[id] = t;
            typeToId[t] = id;
        }

        /// <summary>
        /// Tries to get a property type by id.
        /// </summary>
        /// <param name="id">Registered property id.</param>
        /// <param name="type">Receives the registered type when found.</param>
        /// <returns><see langword="true"/> when the id is registered.</returns>
        public static bool TryGetType(string id, out Type type)
            => idToType.TryGetValue(id, out type);

        /// <summary>
        /// Tries to get the registered id for a property type.
        /// </summary>
        /// <param name="type">Registered property type.</param>
        /// <param name="id">Receives the registered id when found.</param>
        /// <returns><see langword="true"/> when the type is registered.</returns>
        public static bool TryGetId(Type type, out string id)
            => typeToId.TryGetValue(type, out id);
    }
}
