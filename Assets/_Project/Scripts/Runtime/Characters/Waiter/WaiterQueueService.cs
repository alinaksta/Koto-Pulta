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
        private readonly Dictionary<WaiterMealPoint, Waiter> _waiterByMealPoint = new();
        private readonly Dictionary<Waiter, WaiterMealPoint> _mealPointByWaiter = new();

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
            if (point == null || _mealPoints.Contains(point))
                return;

            _mealPoints.Add(point);
        }

        /// <summary>
        /// Removes a waiter meal point from the queue.
        /// </summary>
        public void RemovePoint(WaiterMealPoint point)
        {
            if (!_mealPoints.Contains(point))
                return;

            _mealPoints.Remove(point);
            ReleaseMealPoint(point);
        }

        /// <summary>
        /// Reserves the highest-priority free meal point for a waiter.
        /// </summary>
        public bool TryReserveMealPoint(Waiter waiter, out WaiterMealPoint point)
        {
            point = null;

            if (waiter == null)
                return false;

            if (_mealPointByWaiter.TryGetValue(waiter, out var reservedPoint))
            {
                if (reservedPoint != null && _mealPoints.Contains(reservedPoint))
                {
                    point = reservedPoint;
                    return true;
                }

                ReleaseMealPoint(waiter);
            }

            int bestIndex = -1;
            int bestPriority = int.MinValue;
            for (int i = 0; i < _mealPoints.Count; i++)
            {
                WaiterMealPoint candidate = _mealPoints[i];
                if (candidate == null || _waiterByMealPoint.ContainsKey(candidate))
                    continue;

                if (bestIndex >= 0 && candidate.Priority <= bestPriority)
                    continue;

                bestIndex = i;
                bestPriority = candidate.Priority;
            }

            if (bestIndex < 0)
                return false;

            point = _mealPoints[bestIndex];
            _waiterByMealPoint[point] = waiter;
            _mealPointByWaiter[waiter] = point;

            return true;
        }

        /// <summary>
        /// Clears the current meal point reservation for the supplied waiter.
        /// </summary>
        public void ReleaseMealPoint(Waiter waiter)
        {
            if (waiter == null || !_mealPointByWaiter.TryGetValue(waiter, out var point))
                return;

            _mealPointByWaiter.Remove(waiter);

            if (point != null && _waiterByMealPoint.TryGetValue(point, out var owner) && owner == waiter)
                _waiterByMealPoint.Remove(point);
        }

        private void ReleaseMealPoint(WaiterMealPoint point)
        {
            if (point == null || !_waiterByMealPoint.TryGetValue(point, out var waiter))
                return;

            _waiterByMealPoint.Remove(point);

            if (waiter != null && _mealPointByWaiter.TryGetValue(waiter, out var reservedPoint) && reservedPoint == point)
                _mealPointByWaiter.Remove(waiter);
        }
    }
}
