using System.Collections.Generic;
using System;

namespace Itemworks.Core
{
    /// <summary>
    /// Represents a runtime item created from an <see cref="ItemDefinition"/>.
    /// </summary>
    public class ItemInstance
    {
        /// <summary>
        /// Gets the definition this instance was created from.
        /// </summary>
        public readonly ItemDefinition Definition;

        private readonly Dictionary<Type, ItemComponent> components = new();

        /// <summary>
        /// Creates a runtime instance and runs any property initializers.
        /// </summary>
        /// <param name="definition">Definition to instantiate.</param>
        public ItemInstance(ItemDefinition definition)
        {
            Definition = definition;

            foreach (var property in definition.Properties)
                if (property is IItemInitializer initializer)
                    initializer.OnInstanceCreated(this);
        }
        
        /// <summary>
        /// Adds or replaces a component by its runtime type.
        /// </summary>
        /// <param name="component">Component instance to store.</param>
        public void AddComponent(ItemComponent component)
            => components[component.GetType()] = component;

        /// <summary>
        /// Tries to get a runtime component by its exact type.
        /// </summary>
        /// <typeparam name="T">Component type to retrieve.</typeparam>
        /// <param name="component">Receives the stored component when found.</param>
        /// <returns><see langword="true"/> when the component exists.</returns>
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

        /// <summary>
        /// Gets a runtime component by its exact type.
        /// </summary>
        /// <typeparam name="T">Component type to retrieve.</typeparam>
        /// <returns>The component, or the default value when missing.</returns>
        public T GetComponent<T>() where T : ItemComponent
            => TryGetComponent<T>(out var component) ? component : default;

        /// <summary>
        /// Checks whether a component of the requested type exists.
        /// </summary>
        /// <typeparam name="T">Component type to check.</typeparam>
        /// <returns><see langword="true"/> when the component exists.</returns>
        public bool HasComponent<T>() where T : ItemComponent
            => components.ContainsKey(typeof(T));

        /// <summary>
        /// Removes a stored component by type.
        /// </summary>
        /// <typeparam name="T">Component type to remove.</typeparam>
        public void RemoveComponent<T>() where T : ItemComponent
            => components.Remove(typeof(T));
    }
}
