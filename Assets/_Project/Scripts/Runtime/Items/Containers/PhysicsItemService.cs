using Game.Lifecycle;
using Game.Services;
using UnityEngine;
using UnityEngine.Pool;

namespace Game.Items
{
    public class PhysicsItemService : MonoBehaviour, IBootstrapable
    {
        [SerializeField] private PhysicsContainer _physicsContainerPrefab;

        private ObjectPool<PhysicsContainer> _pool;

        public void Bootstrap()
        {
            ServiceLocator.Register(this);

            _pool = new ObjectPool<PhysicsContainer>
                (CreatePhysicsContainer,
                GetPhysicsContainer,
                ReleasePhysicsContainer,
                DestroyPhysicsContainer);
        }

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