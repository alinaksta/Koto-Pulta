using TMPro;
using UnityEngine;

namespace Game.Characters
{
    public class DebugWaiterProperties : MonoBehaviour
    {
        [SerializeField] private Waiter _waiter;
        [SerializeField] private TextMeshPro _text;

        private void Update()
        {
            if (_waiter == null || _text == null)
                return;

            string tableNumber = _waiter.TableNumber.HasValue
                ? _waiter.TableNumber.Value.ToString()
                : "-";

            string assignedCustomer = _waiter.AssignedCustomer != null
                ? _waiter.AssignedCustomer.name
                : "-";

            string currentTable = _waiter.CurrentTable != null
                ? _waiter.CurrentTable.TableNumber.ToString()
                : "-";

            string carriedItem = _waiter.CarryContainer.Item.HasValue
                ? _waiter.CarryContainer.Item.Value.Definition.Id
                : "-";

            _text.text =
                $"Service: {_waiter.ServiceState}\n" +
                $"Locomotion: {_waiter.LocomotionState}\n" +
                $"Assigned: {_waiter.IsAssigned}\n" +
                $"Idle: {_waiter.IsIdle}\n" +
                $"At Meal Point: {_waiter.AtMealPoint}\n" +
                $"Recovery: {_waiter.RecoveryTimer:0.00}\n" +
                $"Table: {tableNumber}\n" +
                $"Current Table: {currentTable}\n" +
                $"Customer: {assignedCustomer}\n" +
                $"Carried Item: {carriedItem}\n" +
                $"Recovering: {_waiter.IsRecovering}\n" +
                $"Ragdolled: {_waiter.IsRagdolled}";
        }
    }
}
