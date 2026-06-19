using Game.Items.Properties;
using System;
using UnityEngine;
using UnityEngine.Pool;
using static UnityEditor.Progress;

namespace Game.Items
{
    public class PhysicsContainer : MonoBehaviour, IContainer
    {
        [SerializeField] private SpriteRenderer _spriteRenderer;

        private Rigidbody _rigidbody;

        private Item? _item;
        public Item? Item => _item;

        public bool IsEmpty => !_item.HasValue;

        private ObjectPool<PhysicsContainer> _pool;

        public event Action<Item?> OnItemChanged = delegate { };

        private void Awake()
        {
            _rigidbody = GetComponent<Rigidbody>();
        }

        public void SetPool(ObjectPool<PhysicsContainer> pool) 
            => _pool = pool;

        public void SetData(Vector3 positon, Vector3 velocity)
        {
            _rigidbody.isKinematic = true;
            transform.position = positon;
            _rigidbody.position = positon;
            _rigidbody.isKinematic = false;
            _rigidbody.linearVelocity = velocity;
        }

        public void Deactivate()
        {
            _item = null;
            _spriteRenderer.sprite = null;

            _rigidbody.linearVelocity = Vector3.zero;
            _rigidbody.angularVelocity = Vector3.zero;
            
            gameObject.SetActive(false);
        }

        public void Activate()
        {
            gameObject.SetActive(true);

            if (_item.Value.Definition.TryGetProperty<SpriteProperty>(out var property))
                _spriteRenderer.sprite = property.Sprite;
        }

        public bool CanRemove(in TransferRequest request) => !IsEmpty;
        public bool CanInsert(in TransferRequest request) => IsEmpty;

        public void Insert(Item item)
        {
            _item = item;
        }

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