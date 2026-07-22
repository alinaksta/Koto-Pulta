using Game.Interaction;
using Game.Items;
using Game.Items.Properties;
using Itemworks.UnityEngine;
using UnityEngine;

/// <summary>
/// Exposes a shelf slot as a container and keeps its sprite in sync.
/// </summary>
public class ShelveContainer : MonoBehaviour, IInteractable, IContainerHolder
{
    [SerializeField] private SpriteRenderer _spriteRenderer;
    [SerializeField] private ItemDefinitionAsset _initialItem;

    private ItemContainer _container = new();
    /// <inheritdoc/>
    public IContainer Container => _container;

    private void Awake()
    {
        _container.OnItemChanged += OnContainerItemChanged;

        if (_initialItem != null)
        {
            var item = Item.FromId(_initialItem.Id);
            if (item.HasValue)
            {
                _container.Remove();
                _container.Insert(item.Value);
            }
        }
    }

    private void OnDisable()
    {
        _container.OnItemChanged -= OnContainerItemChanged;
    }

    private void OnContainerItemChanged(Item? item)
    {
        if (item.HasValue)
        {
            var definition = item.Value.Definition;

            if (definition.TryGetProperty<FoodProperty>(out var spriteProperty))
            {
                _spriteRenderer.sprite = spriteProperty.WorldSprite;
            }
            else
            {
                _spriteRenderer.sprite = null;
            }
        }
        else
        {
            _spriteRenderer.sprite = null;
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
