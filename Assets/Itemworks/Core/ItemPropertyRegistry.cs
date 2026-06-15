using System;
using System.Collections.Generic;

namespace Itemworks.Core
{
    public class ItemPropertyRegistry
    {
        private static readonly Dictionary<string, Type> idToType = new();
        private static readonly Dictionary<Type, string> typeToId = new();

        public static void Register<TProperty>(string id)
        {
            var t = typeof(TProperty);
            idToType[id] = t;
            typeToId[t] = id;
        }

        public static bool TryGetType(string id, out Type type)
            => idToType.TryGetValue(id, out type);

        public static bool TryGetId(Type type, out string id)
            => typeToId.TryGetValue(type, out id);
    }
}