using Game.Interaction;
using Game.Items;
using Game.Items.Components;
using Game.Items.Properties;
using Itemworks.Core;
using Itemworks.UnityEngine;
using System;
using UnityEngine;
using UnityEngine.AI;

namespace Game.Characters
{
    public class CatWaiter : MonoBehaviour, IInteractable
    {
        [SerializeField] private ItemDefinitionAsset _waiterDefinition;
        [SerializeField] private CatWaiterContainer _carryContainer;
        [SerializeField] private Transform _worldVisualRoot;

        private readonly ItemContainer _selfContainer = new();

        private Rigidbody _rigidbody;
        private NavMeshAgent _agent;
        private ItemInstance _itemInstance;
        private int? _assignedTable;

        public IContainer CarryContainer => _carryContainer.Container;
        public int? AssignedTable => _assignedTable;

        public event Action<int?> OnTableAssignmentChanged = delegate { };

        private void Awake()
        {
            if (_carryContainer == null)
                Debug.LogError($"{nameof(CatWaiter)} on {name} requires a {nameof(CatWaiterContainer)} reference.");

            CreateItemInstance();

            _rigidbody = GetComponent<Rigidbody>();
            _agent = GetComponent<NavMeshAgent>();

            if (_rigidbody != null)
                _rigidbody.isKinematic = true;
        }

        public bool CanInteract(in InteractionContext context) => true;

        public void OnInteractionStarted(in InteractionContext context)
        {
            if (context.HandContainer.IsEmpty)
            {
                var result = ItemTransferUtility.TryTransfer(_selfContainer, context.HandContainer);
                if (result == ItemTransferUtility.TransferResult.Success)
                    EnterHandState();
                return;
            }

            ItemTransferUtility.TryTransfer(context.HandContainer, _carryContainer.Container);
        }

        public void OnInteractionHeld(in InteractionContext context, float delta) { }

        public void OnInteractionStopped(in InteractionContext context) { }

        public void AssignTable(int table)
        {
            if (_assignedTable == table)
                return;

            _assignedTable = table;
            OnTableAssignmentChanged.Invoke(_assignedTable);
        }

        public void DropFromHand(Item selfItem, Vector3 position)
        {
            ReleaseToWorld(selfItem, position, Vector3.zero);
        }

        public void ThrowFromHand(Item selfItem, Vector3 position, Vector3 velocity)
        {
            ReleaseToWorld(selfItem, position, velocity);
        }

        private void ReleaseToWorld(Item selfItem, Vector3 position, Vector3 velocity)
        {
            Debug.Log($"Tried releasing to world at {position}");

            _selfContainer.Insert(selfItem);

            gameObject.SetActive(true);
            transform.position = position;
            _rigidbody.position = position;

            _worldVisualRoot.localPosition = Vector3.zero;

            _agent.enabled = false;

            _rigidbody.isKinematic = false;
            transform.position = position;
            _rigidbody.position = position;
            _rigidbody.linearVelocity = velocity;
        }

        private void CreateItemInstance()
        {
            if (!ItemRegistry.Instance.TryGet(_waiterDefinition.Id, out var definition))
            {
                Debug.LogError("Waiter ItemDefinition is not regiestered!");
                return;
            }

            var instance = new ItemInstance(definition);
            if (!instance.TryGetComponent<WaiterComponent>(out var waiterComponent))
            {
                Debug.LogError($"Waiter ItemDefinition must have {nameof(WaiterProperty)}");
                return;
            }

            _itemInstance = instance;
            waiterComponent.Waiter = this;
            _selfContainer.Insert(new Item(instance));
        }

        private void EnterHandState()
        {
            if (_agent != null)
                _agent.enabled = false;

            if (_rigidbody != null)
            {
                _rigidbody.linearVelocity = Vector3.zero;
                _rigidbody.angularVelocity = Vector3.zero;
                _rigidbody.isKinematic = true;
            }

            gameObject.SetActive(false);
        }
    }
}
