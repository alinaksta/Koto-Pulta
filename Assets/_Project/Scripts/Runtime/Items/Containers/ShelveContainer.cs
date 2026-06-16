using Game.Interaction;
using Game.Items;
using Game.Items.Properties;
using Itemworks.UnityEngine;
using UnityEngine;

public class ShelveContainer : MonoBehaviour, IInteractable, IContainerHolder
{
    [SerializeField] private SpriteRenderer _spriteRenderer;
    [SerializeField] private ItemDefinitionAsset _initialItem;

    private ItemContainer _container = new();
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

            if (definition.TryGetProperty<SpriteProperty>(out var spriteProperty))
            {
                _spriteRenderer.sprite = spriteProperty.Sprite;
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

    public bool CanInteract(in InteractionContext context)
    {
        return true;
    }

    public void OnInteractionHeld(in InteractionContext context, float delta) { }

    public void OnInteractionStarted(in InteractionContext context) { }

    public void OnInteractionStopped(in InteractionContext context) { }
}
