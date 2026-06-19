using System.Collections.Generic;
using System;

namespace Itemworks.Core
{
    public class ItemInstance
    {
        public readonly ItemDefinition Definition;

        private readonly Dictionary<Type, ItemComponent> components = new();

        public ItemInstance(ItemDefinition definition)
        {
            Definition = definition;

            foreach (var property in definition.Properties)
                if (property is IItemInitializer initializer)
                    initializer.OnInstanceCreated(this);
        }
        
        public void AddComponent(ItemComponent component)
            => components[component.GetType()] = component;

        public bool TryGetComponent<T>(out T component) where T : ItemComponent
        {
            if (components.TryGetValue(typeof(T), out var value))
            {
                component = (T)value;
                return true;
            }
            component = null;
            return false;
        }

        public T GetComponent<T>() where T : ItemComponent
            => TryGetComponent<T>(out var component) ? component : default;

        public bool HasComponent<T>() where T : ItemComponent
            => components.ContainsKey(typeof(T));

        public void RemoveComponent<T>() where T : ItemComponent
            => components.Remove(typeof(T));
    }
}