using Game.Interaction;
using Game.Items;
using Game.Items.Properties;
using Itemworks.UnityEngine;
using UnityEngine;

/// <summary>
/// Allows to get rid of an item
/// </summary>
public class TrashBinContainer : MonoBehaviour, IInteractable, IContainerHolder
{
    private ItemContainer _container = new();
    private bool _isProcessing = false;

    /// <inheritdoc/>
    public IContainer Container => _container;

    private void Awake()
    {
        _container.OnItemChanged += OnContainerItemChanged;
        _container.Remove();
    }

    private void OnDisable()
    {
        _container.OnItemChanged -= OnContainerItemChanged;
    }

    private void OnContainerItemChanged(Item? item)
    {
        if (_isProcessing) return;
        _isProcessing = true;
        try
        {
            _container.Remove();
        }
        finally
        {
            _isProcessing = false;
        }
    }

    /// <inheritdoc/>
    public bool CanInteract(in InteractionContext context)
    {
        return true;
    }

    /// <inheritdoc/>
    public void OnInteractionHeld(in InteractionContext context, float delta) { }

    /// <inheritdoc/>
    public void OnInteractionStarted(in InteractionContext context) { }

    /// <inheritdoc/>
    public void OnInteractionStopped(in InteractionContext context) { }
}
