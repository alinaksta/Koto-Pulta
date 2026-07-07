using Game.Lifecycle;
using Game.Services;
using System.Collections.Generic;
using UnityEngine;

namespace Game.Characters
{
    public class WaiterQueueService : MonoBehaviour, IBootstrapable
    {
        private Transform _serviceCounterOrigin;

        private List<WaiterMealPoint> _mealPoints = new();
        private List<WaiterMealPoint> _unassignedMealPoints = new();

        public Vector3 ServiceCounterOrigin => _serviceCounterOrigin.position;

        public void Bootstrap()
        {
            ServiceLocator.Register(this);
        }

        public void SetServiceCounterOrigin(Transform origin)
        {
            _serviceCounterOrigin = origin;
        }

        public void AddPoint(WaiterMealPoint point)
        {
            if (_mealPoints.Contains(point))
                return;

            _mealPoints.Add(point);
            _unassignedMealPoints.Add(point);
        }

        public void RemovePoint(WaiterMealPoint point)
        {
            if (!_mealPoints.Contains(point))
                return;

            _mealPoints.Remove(point);

            if (_unassignedMealPoints.Contains(point))
                _unassignedMealPoints.Remove(point);
        }

        public bool TryGetUnassignedMealPoint(out WaiterMealPoint point)
        {
            point = null;
            int freePointsCount = _mealPoints.Count;

            if (freePointsCount == 0)
                return false;

            int lastIndex = freePointsCount - 1;

            point = _mealPoints[lastIndex];
            _mealPoints.RemoveAt(lastIndex);

            return true;
        }

        public void UnassignMealPoint(WaiterMealPoint point)
        {
            if (!_mealPoints.Contains(point) || _unassignedMealPoints.Contains(point)) 
                return;

            _unassignedMealPoints.Add(point);
        }
    }
}
