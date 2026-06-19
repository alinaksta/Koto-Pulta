using Game.Items;
using Game.Items.Components;
using Game.Items.Properties;
using UnityEngine;

namespace Game.Characters
{
    /// <summary>
    /// Displays and exposes the item currently carried by a waiter.
    /// </summary>
    public class WaiterContainer : MonoBehaviour, IContainerHolder
    {
        [SerializeField] private SpriteRenderer _spriteRenderer;

        private readonly CarryContainer _container = new();
        /// <inheritdoc/>
        public IContainer Container => _container;

        private void Awake()
        {
            OnContainerItemChanged(_container.Item);
        }

        private void OnEnable()
        {
            _container.OnItemChanged += OnContainerItemChanged;
        }

        private void OnDisable()
        {
            _container.OnItemChanged -= OnContainerItemChanged;
        }

        private void OnContainerItemChanged(Item? item)
        {
            if (item.HasValue && item.Value.Definition.TryGetProperty<SpriteProperty>(out var spriteProperty))
            {
                _spriteRenderer.sprite = spriteProperty.Sprite;
            }
            else
            {
                _spriteRenderer.sprite = null;
            }
        }

        private sealed class CarryContainer : IContainer
        {
            private Item? _item;

            public Item? Item => _item;
            public bool IsEmpty => !_item.HasValue;

            public event System.Action<Item?> OnItemChanged = delegate { };

            public bool CanInsert(in TransferRequest request)
            {
                if (!IsEmpty)
                    return false;

                return !request.Item.TryGetComponent<WaiterComponent>(out _);
            }

            public bool CanRemove(in TransferRequest request) => !IsEmpty;

            public void Insert(Item item)
            {
                _item = item;
                OnItemChanged.Invoke(_item);
            }

            public Item? Remove()
            {
                var removed = _item;
                _item = null;
                OnItemChanged.Invoke(_item);
                return removed;
            }
        }
    }
}
