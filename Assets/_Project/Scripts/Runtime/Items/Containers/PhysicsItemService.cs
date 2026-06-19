using Game.Lifecycle;
using Game.Services;
using UnityEngine;
using UnityEngine.Pool;

namespace Game.Items
{
    /// <summary>
    /// Spawns pooled physics-backed item containers into the world.
    /// </summary>
    public class PhysicsItemService : MonoBehaviour, IBootstrapable
    {
        [SerializeField] private PhysicsContainer _physicsContainerPrefab;

        private ObjectPool<PhysicsContainer> _pool;

        /// <inheritdoc/>
        public void Bootstrap()
        {
            ServiceLocator.Register(this);

            _pool = new ObjectPool<PhysicsContainer>
                (CreatePhysicsContainer,
                GetPhysicsContainer,
                ReleasePhysicsContainer,
                DestroyPhysicsContainer);
        }

        /// <summary>
        /// Tries to move an item out of a container and into a pooled physics container.
        /// </summary>
        /// <param name="position">Spawn position.</param>
        /// <param name="velocity">Initial linear velocity.</param>
        /// <param name="from">Container providing the item.</param>
        /// <returns><see langword="true"/> when the item was spawned successfully.</returns>
        public bool TrySpawnFromContainer(Vector3 position, Vector3 velocity, IContainer from)
        {
            var physics = _pool.Get();

            physics.SetData(position, velocity);

            var result = ItemTransferUtility.TryTransfer(from, physics);

            if (result == ItemTransferUtility.TransferResult.Success)
            {
                physics.Activate();
                return true;
            }

            _pool.Release(physics);
            return false;
        }

        /// <summary>
        /// Spawns an item directly into a pooled physics container.
        /// </summary>
        /// <param name="position">Spawn position.</param>
        /// <param name="velocity">Initial linear velocity.</param>
        /// <param name="item">Item to spawn.</param>
        public void Spawn(Vector3 position, Vector3 velocity, Item item)
        {
            var physics = _pool.Get();
            physics.SetData(position, velocity);
            physics.Insert(item);
            physics.Activate();
        }

        private void DestroyPhysicsContainer(PhysicsContainer container)
        {
            Destroy(container.gameObject);
        }

        private void ReleasePhysicsContainer(PhysicsContainer container)
        {
            container.Deactivate();
        }

        private void GetPhysicsContainer(PhysicsContainer container)
        {
            // We need to verify that the transfer is successful first
        }

        private PhysicsContainer CreatePhysicsContainer()
        {
            var instance = Instantiate(_physicsContainerPrefab, transform);
            instance.SetPool(_pool);
            return instance;
        }
    }
}
