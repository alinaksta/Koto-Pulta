using System;
using UnityEngine;

/// <summary>
/// Restricts an object field to values that implement a specific interface.
/// </summary>
[AttributeUsage(AttributeTargets.Field)]
public class RequireInterfaceAttribute : PropertyAttribute {
    /// <summary>
    /// Gets the interface type enforced for the field.
    /// </summary>
    public readonly Type InterfaceType;

    /// <summary>
    /// Creates an attribute that enforces the provided interface type.
    /// </summary>
    /// <param name="interfaceType">Interface required by the attributed field.</param>
    public RequireInterfaceAttribute(Type interfaceType) {
        Debug.Assert(interfaceType.IsInterface, $"{nameof(interfaceType)} needs to be an interface.");
        InterfaceType = interfaceType;
    }
}
