using Game.Items.Properties;
using System;
using UnityEngine;
using UnityEngine.Pool;
using static UnityEditor.Progress;

namespace Game.Items
{
    /// <summary>
    /// World-space container used by the physics item pool.
    /// </summary>
    public class PhysicsContainer : MonoBehaviour, IContainer
    {
        [SerializeField] private SpriteRenderer _spriteRenderer;

        private Rigidbody _rigidbody;

        private Item? _item;
        /// <inheritdoc/>
        public Item? Item => _item;

        /// <inheritdoc/>
        public bool IsEmpty => !_item.HasValue;

        private ObjectPool<PhysicsContainer> _pool;

        /// <inheritdoc/>
        public event Action<Item?> OnItemChanged = delegate { };

        private void Awake()
        {
            _rigidbody = GetComponent<Rigidbody>();
        }

        /// <summary>
        /// Assigns the pool that owns this container.
        /// </summary>
        /// <param name="pool">Owning pool used when the container is released.</param>
        public void SetPool(ObjectPool<PhysicsContainer> pool) 
            => _pool = pool;

        /// <summary>
        /// Positions the container and applies its initial velocity.
        /// </summary>
        /// <param name="positon">World position to place the container at.</param>
        /// <param name="velocity">Initial linear velocity.</param>
        public void SetData(Vector3 positon, Vector3 velocity)
        {
            _rigidbody.isKinematic = true;
            transform.position = positon;
            _rigidbody.position = positon;
            _rigidbody.isKinematic = false;
            _rigidbody.linearVelocity = velocity;
        }

        /// <summary>
        /// Clears visuals and disables the pooled object.
        /// </summary>
        public void Deactivate()
        {
            _item = null;
            _spriteRenderer.sprite = null;

            _rigidbody.linearVelocity = Vector3.zero;
            _rigidbody.angularVelocity = Vector3.zero;
            
            gameObject.SetActive(false);
        }

        /// <summary>
        /// Enables the pooled object and refreshes its sprite from the stored item.
        /// </summary>
        public void Activate()
        {
            gameObject.SetActive(true);

            if (_item.Value.Definition.TryGetProperty<SpriteProperty>(out var property))
                _spriteRenderer.sprite = property.Sprite;
        }

        /// <inheritdoc/>
        public bool CanRemove(in TransferRequest request) => !IsEmpty;

        /// <inheritdoc/>
        public bool CanInsert(in TransferRequest request) => IsEmpty;

        /// <inheritdoc/>
        public void Insert(Item item)
        {
            _item = item;
        }

        /// <inheritdoc/>
        /// <remarks>
        /// Removing the item releases this container back to its pool.
        /// </remarks>
        public Item? Remove()
        {
            if (IsEmpty)
                return null;

            var removed = _item;
            _item = null;
            _pool.Release(this);
            return removed;
        }
    }
}
