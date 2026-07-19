using Game.Lifecycle;
using Game.Services;
using System.Collections.Generic;
using UnityEngine;

namespace Game.Characters
{
    /// <summary>
    /// Tracks waiter meal points and service counter routing.
    /// </summary>
    public class WaiterQueueService : MonoBehaviour, IBootstrapable
    {
        private Transform _serviceCounterOrigin;

        private List<WaiterMealPoint> _mealPoints = new();
        private List<WaiterMealPoint> _unassignedMealPoints = new();

        /// <summary>
        /// Gets the world position of the registered service counter origin.
        /// </summary>
        public Vector3 ServiceCounterOrigin => _serviceCounterOrigin.position;

        /// <inheritdoc/>
        public void Bootstrap()
        {
            ServiceLocator.Register(this);
        }

        /// <summary>
        /// Sets the service counter origin used by the waiter queue.
        /// </summary>
        public void SetServiceCounterOrigin(Transform origin)
        {
            _serviceCounterOrigin = origin;
        }

        /// <summary>
        /// Adds a waiter meal point to the queue.
        /// </summary>
        public void AddPoint(WaiterMealPoint point)
        {
            if (_mealPoints.Contains(point))
                return;

            _mealPoints.Add(point);
            _unassignedMealPoints.Add(point);
        }

        /// <summary>
        /// Removes a waiter meal point from the queue.
        /// </summary>
        public void RemovePoint(WaiterMealPoint point)
        {
            if (!_mealPoints.Contains(point))
                return;

            _mealPoints.Remove(point);

            if (_unassignedMealPoints.Contains(point))
                _unassignedMealPoints.Remove(point);
        }

        /// <summary>
        /// Tries to get an unassigned waiter meal point.
        /// </summary>
        public bool TryGetUnassignedMealPoint(out WaiterMealPoint point)
        {
            point = null;
            int freePointsCount = _unassignedMealPoints.Count;

            if (freePointsCount == 0)
                return false;

            int lastIndex = freePointsCount - 1;

            point = _unassignedMealPoints[lastIndex];
            _unassignedMealPoints.RemoveAt(lastIndex);

            return true;
        }

        /// <summary>
        /// Clears the current waiter assignment for the supplied meal point.
        /// </summary>
        public void UnassignMealPoint(WaiterMealPoint point)
        {
            if (point == null || !_mealPoints.Contains(point) || _unassignedMealPoints.Contains(point))
                return;

            _unassignedMealPoints.Add(point);
        }
    }
}
