using System;
using UnityEngine;
using Object = UnityEngine.Object;

/// <summary>
/// Stores a Unity object reference while exposing it as an interface.
/// </summary>
/// <typeparam name="TInterface">Interface exposed to consumers.</typeparam>
/// <typeparam name="TObject">Unity object type serialized by Unity.</typeparam>
[Serializable]
public class InterfaceReference<TInterface, TObject> where TObject : Object where TInterface : class {
    [SerializeField, HideInInspector] TObject underlyingValue;

    /// <summary>
    /// Gets or sets the referenced object as the requested interface.
    /// </summary>
    public TInterface Value {
        get => underlyingValue switch {
            null => null,
            TInterface @interface => @interface,
            _ => throw new InvalidOperationException($"{underlyingValue} needs to implement interface {nameof(TInterface)}.")
        };
        set => underlyingValue = value switch {
            null => null,
            TObject newValue => newValue,
            _ => throw new ArgumentException($"{value} needs to be of type {typeof(TObject)}.", string.Empty)
        };
    }

    /// <summary>
    /// Gets or sets the serialized Unity object reference.
    /// </summary>
    public TObject UnderlyingValue {
        get => underlyingValue;
        set => underlyingValue = value;
    }
    
    /// <summary>
    /// Creates an empty reference.
    /// </summary>
    public InterfaceReference() { }
    
    /// <summary>
    /// Creates a reference from a serialized Unity object.
    /// </summary>
    /// <param name="target">Object to store.</param>
    public InterfaceReference(TObject target) => underlyingValue = target;
    
    /// <summary>
    /// Creates a reference from an interface implementation.
    /// </summary>
    /// <param name="interface">Implementation to store.</param>
    public InterfaceReference(TInterface @interface) => underlyingValue = @interface as TObject;
    
    /// <summary>
    /// Returns the referenced value as the interface type.
    /// </summary>
    /// <param name="obj">Reference to convert.</param>
    public static implicit operator TInterface(InterfaceReference<TInterface, TObject> obj) => obj.Value;
}

/// <summary>
/// Stores any Unity object that implements the requested interface.
/// </summary>
/// <typeparam name="TInterface">Interface exposed to consumers.</typeparam>
[Serializable]
public class InterfaceReference<TInterface> : InterfaceReference<TInterface, Object> where TInterface : class { }
