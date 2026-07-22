using Game.Interaction;
using Game.Items;
using UnityEngine;
using Game.Items.Components;

/// <summary>
/// Allows to get rid of an item
/// </summary>
public class TrashBinContainer : MonoBehaviour, IInteractable, IContainerHolder
{
    private BinContainer _container = new();
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


    private sealed class BinContainer : IContainer
        {
            private Item? _item;

            /// <summary>
            /// Gets the item currently stored in the waiter container.
            /// </summary>
            public Item? Item => _item;
            /// <summary>
            /// Gets whether the waiter container is empty.
            /// </summary>
            public bool IsEmpty => !_item.HasValue;

            public event System.Action<Item?> OnItemChanged = delegate { };

            /// <summary>
            /// Checks whether the waiter container can accept the supplied transfer request.
            /// </summary>
            public bool CanInsert(in TransferRequest request)
            {
                if (!IsEmpty)
                    return false;

                return !request.Item.TryGetComponent<WaiterComponent>(out _);
            }

            /// <summary>
            /// Checks whether the waiter container can remove its current item for the supplied transfer request.
            /// </summary>
            public bool CanRemove(in TransferRequest request) => !IsEmpty;

            /// <summary>
            /// Inserts an item into the waiter container.
            /// </summary>
            public void Insert(Item item)
            {
                _item = item;
                OnItemChanged.Invoke(_item);
            }

            /// <summary>
            /// Removes and returns the current item from the waiter container.
            /// </summary>
            public Item? Remove()
            {
                var removed = _item;
                _item = null;
                OnItemChanged.Invoke(_item);
                return removed;
            }
        }
}
